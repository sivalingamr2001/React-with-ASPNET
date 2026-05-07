using JanaticsApi.Application.Common.Abstractions;

namespace JanaticsApi.Application.Features.Users.Commands.CreateUser;

public sealed record CreateUserCommand(
    string FirstName,
    string LastName,
    string Email,
    string Password) : ICommand<CreateUserResponse>;
