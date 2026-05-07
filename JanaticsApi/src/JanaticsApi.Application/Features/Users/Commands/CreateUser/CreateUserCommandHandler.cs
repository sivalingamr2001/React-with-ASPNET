using JanaticsApi.Application.Common.Abstractions;
using JanaticsApi.Application.Common.Models;
using JanaticsApi.Domain.Entities.Users;
using JanaticsApi.Domain.Repositories;
using JanaticsApi.Domain.Services;
using JanaticsApi.Shared.Constants;

namespace JanaticsApi.Application.Features.Users.Commands.CreateUser;

public sealed class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, CreateUserResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHashingService _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public CreateUserCommandHandler(
        IUserRepository userRepository,
        IPasswordHashingService passwordHasher,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CreateUserResponse>> Handle(
        CreateUserCommand request,
        CancellationToken cancellationToken)
    {
        var existing = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existing is not null)
        {
            return Result<CreateUserResponse>.Failure(
                Error.Conflict("Users.EmailExists", "A user with the provided email already exists."));
        }

        var user = User.Create(
            request.FirstName,
            request.LastName,
            Domain.ValueObjects.Email.Create(request.Email),
            _passwordHasher.Hash(request.Password));

        await _userRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<CreateUserResponse>.Success(new CreateUserResponse(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email.Value,
            user.Role.ToString()));
    }
}
