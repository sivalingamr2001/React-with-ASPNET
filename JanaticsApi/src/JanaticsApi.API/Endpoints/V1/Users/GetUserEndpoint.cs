// src/EnterpriseApi.API/Endpoints/V1/Users/GetUserEndpoint.cs
using Asp.Versioning;
using JanaticsApi.Application.Features.Users.Queries.GetUserById;
using JanaticsApi.Shared.Constants;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace JanaticsApi.API.Endpoints.V1.Users;

public sealed class GetUserEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(ApiRoutes.Users.GetById, HandleAsync)
           .WithName("GetUserById")
           .WithApiVersionSet(app.NewApiVersionSet().HasApiVersion(new ApiVersion(1, 0)).Build())
           .MapToApiVersion(1, 0)
           .WithTags("Users")
           .WithSummary("Get user by identifier")
           .Produces<GetUserByIdResponse>(StatusCodes.Status200OK)
           .ProducesProblem(StatusCodes.Status404NotFound)
           .ProducesProblem(StatusCodes.Status401Unauthorized)
           .RequireAuthorization(PolicyNames.RequireAuthenticatedUser)
           .WithOpenApi();
    }

    private static async Task<Results<Ok<GetUserByIdResponse>, NotFound<ProblemDetails>, UnauthorizedHttpResult>> HandleAsync(
        Guid id,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new GetUserByIdQuery(id);
        var result = await mediator.Send(query, cancellationToken);

        return result.Match<Results<Ok<GetUserByIdResponse>, NotFound<ProblemDetails>, UnauthorizedHttpResult>>(
            value => TypedResults.Ok(value),
            error => error.Type switch
            {
                ErrorType.NotFound => TypedResults.NotFound(
                    new ProblemDetails { Title = error.Description, Detail = error.Code }),
                _ => TypedResults.Unauthorized()
            });
    }
}