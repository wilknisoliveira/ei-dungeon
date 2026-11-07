using ei_back.Core.Application.Service.Verification;

namespace ei_back.Core.Application.Service.Email.Interfaces;

/// <summary>Composes application-owned verification and recovery messages.</summary>
public interface IVerificationEmailComposer
{
    OutgoingEmail Compose(CodeDelivery delivery);
}
