using JanaticsApi.Application.Common.Abstractions;

namespace JanaticsApi.Application.Features.Auth.Commands.Login;

public sealed record LoginCommand(
    string Email,
    string Password) : ICommand<LoginResponse>;
