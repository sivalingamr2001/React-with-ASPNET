using Asp.Versioning;
using JanaticsApi.Application.Common.Models;
using JanaticsApi.Application.Features.Users.Commands.CreateUser;
using JanaticsApi.Shared.Constants;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace JanaticsApi.API.Endpoints.V1.Users;

public sealed class CreateUserEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.Users.Create, HandleAsync)
           .WithName("CreateUser")
           .WithApiVersionSet(app.NewApiVersionSet().HasApiVersion(new ApiVersion(1, 0)).Build())
           .MapToApiVersion(1, 0)
           .WithTags("Users")
           .WithSummary("Create a new user")
           .Produces<CreateUserResponse>(StatusCodes.Status201Created)
           .ProducesProblem(StatusCodes.Status409Conflict)
           .ProducesProblem(StatusCodes.Status401Unauthorized)
           .RequireAuthorization(PolicyNames.RequireAdminRole)
           .WithOpenApi();
    }

    private static async Task<Results<CreatedAtRoute<CreateUserResponse>, Conflict<ProblemDetails>, UnauthorizedHttpResult>> HandleAsync(
        CreateUserCommand request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(request, cancellationToken);

        return result.Match<Results<CreatedAtRoute<CreateUserResponse>, Conflict<ProblemDetails>, UnauthorizedHttpResult>>(
            value => TypedResults.CreatedAtRoute("GetUserById", new { id = value.Id, version = "1.0" }, value),
            error => error.Type switch
            {
                ErrorType.Conflict => TypedResults.Conflict(new ProblemDetails { Title = error.Description, Detail = error.Code }),
                _ => TypedResults.Unauthorized()
            });
    }
}
