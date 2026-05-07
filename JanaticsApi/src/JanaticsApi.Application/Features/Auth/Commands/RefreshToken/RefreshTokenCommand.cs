using JanaticsApi.Application.Common.Abstractions;

namespace JanaticsApi.Application.Features.Auth.Commands.RefreshToken;

public sealed record RefreshTokenCommand(
    string Email,
    string RefreshToken) : ICommand<LoginResponse>;
