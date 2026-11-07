using System.Globalization;
using ei_back.Core.Application.Service.Email.Templates;
using ei_back.Core.Application.Service.Email.Interfaces;
using ei_back.Core.Application.Service.Verification;
using ei_back.Core.Domain.Enums;
using Microsoft.Extensions.Localization;

namespace ei_back.Core.Application.Service.Email;

/// <summary>Builds localized verification messages independently of the delivery provider.</summary>
public class VerificationEmailComposer(IStringLocalizer<VerificationEmailComposer> localizer) : IVerificationEmailComposer
{
    public OutgoingEmail Compose(CodeDelivery delivery)
    {
        var prefix = delivery.Purpose switch
        {
            VerificationCodePurpose.EmailVerification => "Verification",
            VerificationCodePurpose.PasswordRecovery => "Recovery",
            _ => throw new ArgumentOutOfRangeException(nameof(delivery.Purpose))
        };
        var language = localizer["Language"].Value;
        var minutes = delivery.ValidFor.TotalMinutes.ToString("0.##", CultureInfo.GetCultureInfo(language));
        var text = new VerificationEmailText(
            language, localizer[prefix + "Title"], localizer[prefix + "Instruction"],
            localizer["Expiration", minutes], localizer["Unsolicited"], localizer["CopyInstruction"],
            localizer["CodeLabel"], localizer["DoNotShare"], localizer["Footer"]);
        return new OutgoingEmail(delivery.Email, localizer[prefix + "Subject"],
            $"{text.Instruction} {text.CopyInstruction}\n\n{delivery.Code}\n\n{text.Expiration}\n{text.Unsolicited} {text.DoNotShare}\n\n{text.Footer}",
            VerificationEmailTemplate.Render(text, delivery.Code));
    }
}