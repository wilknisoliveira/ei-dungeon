using ei_back.Core.Domain.Enums;

namespace ei_back.Core.Application.Service.Verification;

// Ephemeral delivery payload: never persist, serialize in API responses, or log this object.
public sealed record CodeDelivery(string Email, string Code, VerificationCodePurpose Purpose, TimeSpan ValidFor);