namespace ei_back.Core.Application.UseCase.GoogleAuth.Dtos;

public record CurrentAccountDtoResponse(Guid Id, string Email, string Role, bool HasPassword, bool GoogleLinked);
