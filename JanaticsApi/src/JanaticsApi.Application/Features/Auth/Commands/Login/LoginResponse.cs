namespace JanaticsApi.Application.Features.Auth.Commands.Login;

public sealed record LoginResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    Guid UserId,
    string Email,
    string Role);
