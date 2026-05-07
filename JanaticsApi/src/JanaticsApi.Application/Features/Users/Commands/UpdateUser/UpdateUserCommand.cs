using JanaticsApi.Application.Common.Abstractions;

namespace JanaticsApi.Application.Features.Users.Commands.UpdateUser;

public sealed record UpdateUserCommand(
    Guid UserId,
    string FirstName,
    string LastName) : ICommand;
