using JanaticsApi.Application.Common.Abstractions;
using JanaticsApi.Application.Common.Models;
using JanaticsApi.Domain.Repositories;

namespace JanaticsApi.Application.Features.Users.Queries.GetUserById;

public sealed class GetUserByIdQueryHandler : IQueryHandler<GetUserByIdQuery, GetUserByIdResponse>
{
    private readonly IUserRepository _userRepository;

    public GetUserByIdQueryHandler(IUserRepository userRepository) =>
        _userRepository = userRepository;

    public async Task<Result<GetUserByIdResponse>> Handle(
        GetUserByIdQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null)
            return Result<GetUserByIdResponse>.Failure(
                Error.NotFound("Users.NotFound", "User not found."));

        return Result<GetUserByIdResponse>.Success(new GetUserByIdResponse(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email.Value,
            user.Role.ToString()));
    }
}
