using JanaticsApi.Application.Common.Abstractions;

namespace JanaticsApi.Application.Features.Users.Queries.GetUserById;

public sealed record GetUserByIdQuery(Guid UserId) : IQuery<GetUserByIdResponse>;
