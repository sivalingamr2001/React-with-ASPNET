using Asp.Versioning;
using JanaticsApi.Application.Common.Models;
using JanaticsApi.Application.Features.Users.Commands.UpdateUser;
using JanaticsApi.Shared.Abstractions;
using JanaticsApi.Shared.Constants;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace JanaticsApi.API.Endpoints.V1.Users;

public sealed class UpdateUserEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut(ApiRoutes.Users.Update, HandleAsync)
           .WithName("UpdateUser")
           .WithApiVersionSet(app.NewApiVersionSet().HasApiVersion(new ApiVersion(1, 0)).Build())
           .MapToApiVersion(1, 0)
           .WithTags("Users")
           .WithSummary("Update user profile")
           .Produces(StatusCodes.Status204NoContent)
           .ProducesProblem(StatusCodes.Status404NotFound)
           .ProducesProblem(StatusCodes.Status401Unauthorized)
           .RequireAuthorization(PolicyNames.RequireAuthenticatedUser)
           .WithOpenApi();
    }

    private static async Task<Results<NoContent, NotFound<ProblemDetails>, UnauthorizedHttpResult>> HandleAsync(
        Guid id,
        UpdateUserRequest request,
        IMediator mediator,
        ICurrentUserService currentUserService,
        CancellationToken cancellationToken)
    {
        if (!currentUserService.Roles.Contains("Admin") && currentUserService.UserId != id.ToString())
        {
            return TypedResults.Unauthorized();
        }

        var command = new UpdateUserCommand(id, request.FirstName, request.LastName);
        var result = await mediator.Send(command, cancellationToken);

        return result.Match<Results<NoContent, NotFound<ProblemDetails>, UnauthorizedHttpResult>>(
            _ => TypedResults.NoContent(),
            error => error.Type switch
            {
                ErrorType.NotFound => TypedResults.NotFound(new ProblemDetails { Title = error.Description, Detail = error.Code }),
                _ => TypedResults.Unauthorized()
            });
    }

    private sealed record UpdateUserRequest(string FirstName, string LastName);
}
