using System.Net;
using System.Text;
using System.Text.Json;
using ei_back.Core.Application.Service.Email.Interfaces;
using ei_back.Core.Application.Service.Email;
using ei_back.Infrastructure.Email;
using ei_back.Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Resend;

namespace ei_back.Tests.Infrastructure;

public class ResendEmailSenderTests
{
    private sealed class CaptureHandler(HttpStatusCode status) : HttpMessageHandler
    {
        public string? Body { get; private set; }
        public string? Token { get; private set; }
        public int Calls { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            Token = request.Headers.Authorization?.Parameter;
            return new HttpResponseMessage(status)
            {
                Content = new StringContent(status == HttpStatusCode.OK
                    ? "{\"id\":\"49a3999c-0ce1-4ea6-ab68-afcd6dc2e794\"}"
                    : "{\"name\":\"validation_error\",\"message\":\"provider sensitive text\"}", Encoding.UTF8, "application/json")
            };
        }
    }

    private static ServiceProvider Provider(CaptureHandler handler, string token = "re_test_only", string from = "EI Dungeon <noreply@example.com>")
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Resend:ApiToken"] = token,
            ["Resend:From"] = from
        }).Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddServices();
        services.AddResend(o => o.ThrowExceptions = false).ConfigurePrimaryHttpMessageHandler(() => handler);
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task SendsPreparedContentUnchanged_WithConfiguredSender()
    {
        using var handler = new CaptureHandler(HttpStatusCode.OK);
        using var provider = Provider(handler);
        using var scope = provider.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        sender.Should().BeOfType<ResendEmailSender>();
        var email = new OutgoingEmail("player@example.com", "Assunto independente", "Texto com 000123", "<p>HTML com 000123</p>");
        await sender.SendAsync(email);
        handler.Calls.Should().Be(1);
        handler.Token.Should().Be("re_test_only");
        using var json = JsonDocument.Parse(handler.Body!);
        json.RootElement.GetProperty("from").GetString().Should().Be("EI Dungeon <noreply@example.com>");
        json.RootElement.GetProperty("to")[0].GetString().Should().Be(email.Recipient);
        json.RootElement.GetProperty("subject").GetString().Should().Be(email.Subject);
        json.RootElement.GetProperty("html").GetString().Should().Be(email.HtmlBody);
        json.RootElement.GetProperty("text").GetString().Should().Be(email.TextBody);
    }

    [Fact]
    public async Task ProviderRejection_IsNotTreatedAsSuccess_AndDoesNotExposeProviderMessage()
    {
        using var handler = new CaptureHandler(HttpStatusCode.BadRequest);
        using var provider = Provider(handler);
        using var scope = provider.CreateScope();
        Func<Task> send = () => scope.ServiceProvider.GetRequiredService<IEmailSender>()
            .SendAsync(new OutgoingEmail("player@example.com", "Subject", "Text", "<p>HTML</p>"));
        await send.Should().ThrowAsync<InvalidOperationException>().WithMessage("Resend did not accept the email.");
    }

    [Fact]
    public async Task CancelledDelivery_DoesNotCallProvider()
    {
        using var handler = new CaptureHandler(HttpStatusCode.OK);
        using var provider = Provider(handler);
        using var scope = provider.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        Func<Task> cancel = () => sender.SendAsync(new OutgoingEmail("player@example.com", "Subject", "Text", "<p>HTML</p>"), new CancellationToken(true));
        await cancel.Should().ThrowAsync<OperationCanceledException>();
        handler.Calls.Should().Be(0);
    }

    [Theory]
    [InlineData("", "noreply@example.com")]
    [InlineData("re_test_only", "")]
    [InlineData("re_test_only", "not-an-email")]
    public async Task MissingOrInvalidConfiguration_FailsWithoutSending(string token, string from)
    {
        using var handler = new CaptureHandler(HttpStatusCode.OK);
        using var provider = Provider(handler, token, from);
        using var scope = provider.CreateScope();
        Func<Task> send = () => scope.ServiceProvider.GetRequiredService<IEmailSender>()
            .SendAsync(new OutgoingEmail("player@example.com", "Subject", "Text", "<p>HTML</p>"));
        await send.Should().ThrowAsync<OptionsValidationException>();
        handler.Calls.Should().Be(0);
    }
}
