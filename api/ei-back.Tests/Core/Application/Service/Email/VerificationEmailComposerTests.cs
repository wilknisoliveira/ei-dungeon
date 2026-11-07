using System.Globalization;
using System.Net;
using ei_back.Core.Application.Service.Verification;
using ei_back.Core.Domain.Enums;
using ei_back.Tests.Infrastructure;

namespace ei_back.Tests.Core.Application.Service.Email;

public class VerificationEmailComposerTests
{
    private sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo _culture = CultureInfo.CurrentCulture;
        private readonly CultureInfo _uiCulture = CultureInfo.CurrentUICulture;
        public CultureScope(string culture) => CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(culture);
        public void Dispose() { CultureInfo.CurrentCulture = _culture; CultureInfo.CurrentUICulture = _uiCulture; }
    }

    [Theory]
    [InlineData("en", VerificationCodePurpose.EmailVerification, "Verify", "verify your email", "YOUR CODE", "Do not share this code.")]
    [InlineData("en", VerificationCodePurpose.PasswordRecovery, "Password recovery", "create a new password", "YOUR CODE", "Do not share this code.")]
    [InlineData("pt-BR", VerificationCodePurpose.EmailVerification, "Confirme", "confirmar seu e-mail", "SEU CÓDIGO", "Não compartilhe este código.")]
    [InlineData("pt-BR", VerificationCodePurpose.PasswordRecovery, "Recuperação", "criar uma nova senha", "SEU CÓDIGO", "Não compartilhe este código.")]
    [InlineData("es", VerificationCodePurpose.EmailVerification, "Verifica", "verificar tu correo", "TU CÓDIGO", "No compartas este código.")]
    [InlineData("es", VerificationCodePurpose.PasswordRecovery, "Recuperación", "crear una nueva contraseña", "TU CÓDIGO", "No compartas este código.")]
    public void Compose_TranslatesWholeMessageAndPreservesLeadingZeroes(string culture, VerificationCodePurpose purpose,
        string subject, string instruction, string label, string warning)
    {
        using var scope = new CultureScope(culture);
        var delivery = new CodeDelivery("player@example.com", "000123", purpose, TimeSpan.FromMinutes(10));
        var email = LocalizationTestSupport.CreateComposer().Compose(delivery);
        email.Recipient.Should().Be(delivery.Email);
        email.Subject.Should().Contain(subject);
        email.TextBody.Should().Contain(instruction).And.Contain("000123").And.Contain(warning);
        WebUtility.HtmlDecode(email.HtmlBody).Should().Contain(instruction).And.Contain("<strong>000123</strong>").And.Contain(label).And.Contain(warning);
        email.HtmlBody.Should().Contain($"lang=\"{culture}\"");
        email.Subject.Should().NotContain("VerificationSubject").And.NotContain("RecoverySubject");
    }

    [Theory]
    [InlineData("en", "7.5 minutes")]
    [InlineData("pt-BR", "7,5 minutos")]
    [InlineData("es", "7,5 minutos")]
    public void Compose_FormatsActualLifetimeInSelectedLanguage(string culture, string expiration)
    {
        using var scope = new CultureScope(culture);
        var email = LocalizationTestSupport.CreateComposer().Compose(new("player@example.com", "000123",
            VerificationCodePurpose.EmailVerification, TimeSpan.FromMinutes(7.5)));
        email.TextBody.Should().Contain(expiration);
        email.HtmlBody.Should().Contain(expiration);
    }

    [Fact]
    public void Compose_EscapesDynamicHtmlContent()
    {
        using var scope = new CultureScope("en");
        var email = LocalizationTestSupport.CreateComposer().Compose(new("player@example.com", "<unsafe>",
            VerificationCodePurpose.EmailVerification, TimeSpan.FromMinutes(10)));
        email.HtmlBody.Should().Contain("&lt;unsafe&gt;").And.NotContain("<unsafe>");
    }

    [Fact]
    public void Compose_RejectsUnsupportedPurpose()
    {
        Action compose = () => LocalizationTestSupport.CreateComposer().Compose(new("player@example.com", "000123",
            (VerificationCodePurpose)99, TimeSpan.FromMinutes(10)));
        compose.Should().Throw<ArgumentOutOfRangeException>();
    }
}
