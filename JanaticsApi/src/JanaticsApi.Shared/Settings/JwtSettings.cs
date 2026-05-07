// src/EnterpriseApi.Shared/Settings/JwtSettings.cs
using System.ComponentModel.DataAnnotations;

namespace JanaticsApi.Shared.Settings;

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    [Required]
    [MinLength(32)]
    public string SecretKey { get; init; } = default!;

    [Required]
    public string Issuer { get; init; } = default!;

    [Required]
    public string Audience { get; init; } = default!;

    [Range(1, 60)]
    public int AccessTokenExpirationMinutes { get; init; } = 15;

    [Range(1, 90)]
    public int RefreshTokenExpirationDays { get; init; } = 30;
}