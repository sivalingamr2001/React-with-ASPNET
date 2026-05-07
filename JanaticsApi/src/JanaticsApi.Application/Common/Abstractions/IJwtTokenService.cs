using JanaticsApi.Domain.Entities.Users;
using System.Security.Claims;

namespace JanaticsApi.Application.Common.Abstractions;

public interface IJwtTokenService
{
    (string accessToken, string jti) GenerateAccessToken(User user);
    (string token, DateTimeOffset expiresAt) GenerateRefreshToken();
    ClaimsPrincipal? ValidateExpiredToken(string token);
}
