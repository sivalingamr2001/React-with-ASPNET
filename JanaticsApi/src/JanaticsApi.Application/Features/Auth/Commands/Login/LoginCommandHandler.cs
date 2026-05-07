using JanaticsApi.Application.Common.Abstractions;
using JanaticsApi.Application.Common.Caching;
using JanaticsApi.Application.Common.Models;
using JanaticsApi.Domain.Repositories;
using JanaticsApi.Domain.Services;
using Microsoft.Extensions.Logging;

namespace JanaticsApi.Application.Features.Auth.Commands.Login;

public sealed class LoginCommandHandler : ICommandHandler<LoginCommand, LoginResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IPasswordHashingService _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IJwtTokenService jwtTokenService,
        IPasswordHashingService passwordHasher,
        IUnitOfWork unitOfWork,
        ICacheService cache,
        ILogger<LoginCommandHandler> logger)
    {
        _userRepository = userRepository;
        _jwtTokenService = jwtTokenService;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Result<LoginResponse>> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (user is null || !user.IsActive)
        {
            _logger.LogWarning("Failed login attempt for email {Email}", request.Email);
            return Result<LoginResponse>.Failure(
                Error.Unauthorized("Auth.InvalidCredentials", "Invalid email or password."));
        }

        if (user.IsLocked())
        {
            _logger.LogWarning("Locked account login attempt for user {UserId}", user.Id);
            return Result<LoginResponse>.Failure(
                Error.Unauthorized("Auth.AccountLocked", "Account is temporarily locked."));
        }

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            user.RecordFailedLoginAttempt();
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<LoginResponse>.Failure(
                Error.Unauthorized("Auth.InvalidCredentials", "Invalid email or password."));
        }

        user.ResetFailedLoginAttempts();

        var (accessToken, jti) = _jwtTokenService.GenerateAccessToken(user);
        var (refreshToken, refreshTokenExpiresAt) = _jwtTokenService.GenerateRefreshToken();

        user.SetRefreshToken(refreshToken, refreshTokenExpiresAt);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<LoginResponse>.Success(new LoginResponse(
            accessToken,
            refreshToken,
            refreshTokenExpiresAt,
            user.Id,
            user.Email.Value,
            user.Role.ToString()));
    }
}
