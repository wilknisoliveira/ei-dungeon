using AutoMapper;
using ei_back.Core.Application.Repository;
using ei_back.Core.Application.Service.Verification.Interfaces;
using ei_back.Core.Application.UseCase.User.Dtos;
using ei_back.Core.Application.UseCase.User.Interfaces;
using ei_back.Core.Application.Utils;
using ei_back.Core.Domain.Enums;
using ei_back.Infrastructure.Exceptions.ExceptionTypes;

namespace ei_back.Core.Application.UseCase.User;

public class CreateUserUseCase : ICreateUserUseCase
{
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IVerificationFlowService _verificationFlowService;

    public CreateUserUseCase(IUserRepository userRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        IVerificationFlowService verificationFlowService)
    {
        _userRepository = userRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _verificationFlowService = verificationFlowService;
    }

    public async Task<UserDtoResponse> Handler(UserDtoRequest request)
    {
        var normalized = EmailNormalizer.Normalize(request.Email);
        if (await _userRepository.FindByUserName(request.UserName) != null || await _userRepository.FindByEmail(normalized) != null)
            throw new BadRequestException("Invalid registration data.");

        var user = _mapper.Map<Domain.Entity.User>(request);
        user.Id = Guid.NewGuid();
        user.NormalizedEmail = normalized;
        user.Role = UserRole.CommonUser;
        user.EmailVerifiedAt = null;
        user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
        user.CreatedAt = user.UpdatedAt = DateTimeOffset.UtcNow;

        var saved = await _userRepository.CreateAsync(user);
        var delivery = await _verificationFlowService.PrepareAsync(saved, VerificationCodePurpose.EmailVerification);
        try
        {
            // Save the new account and its first code together before attempting delivery.
            await _unitOfWork.CommitAsync();
        }
        catch (PersistenceConflictException)
        {
            throw new Domain.DomainExceptions.RegistrationConflictException();
        }
        await _verificationFlowService.DeliverAsync(delivery);
        return _mapper.Map<UserDtoResponse>(saved);
    }
}