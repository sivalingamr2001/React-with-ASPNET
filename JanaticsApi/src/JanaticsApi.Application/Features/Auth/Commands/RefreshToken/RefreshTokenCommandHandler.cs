using JanaticsApi.Application.Common.Abstractions;
using JanaticsApi.Application.Common.Caching;
using JanaticsApi.Application.Common.Models;
using JanaticsApi.Domain.Repositories;
using JanaticsApi.Domain.Services;
using Microsoft.Extensions.Logging;

namespace JanaticsApi.Application.Features.Auth.Commands.RefreshToken;

public sealed class RefreshTokenCommandHandler : ICommandHandler<RefreshTokenCommand, LoginResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IPasswordHashingService _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cache;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        IUserRepository userRepository,
        IJwtTokenService jwtTokenService,
        IPasswordHashingService passwordHasher,
        IUnitOfWork unitOfWork,
        ICacheService cache,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _userRepository = userRepository;
        _jwtTokenService = jwtTokenService;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Result<LoginResponse>> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user is null || !user.IsActive)
        {
            _logger.LogWarning("Refresh token attempt failed for email {Email}", request.Email);
            return Result<LoginResponse>.Failure(
                Error.Unauthorized("Auth.InvalidCredentials", "Invalid email or refresh token."));
        }

        if (!user.ValidateRefreshToken(request.RefreshToken))
        {
            return Result<LoginResponse>.Failure(
                Error.Unauthorized("Auth.InvalidRefreshToken", "Refresh token is invalid or expired."));
        }

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
