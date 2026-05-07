using Asp.Versioning;
using JanaticsApi.Application.Features.Auth.Commands.Login;
using JanaticsApi.Shared.Constants;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace JanaticsApi.API.Endpoints.V1.Auth;

public sealed class LoginEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.Auth.Login, HandleAsync)
           .WithName("Login")
           .WithApiVersionSet(app.NewApiVersionSet().HasApiVersion(new ApiVersion(1, 0)).Build())
           .MapToApiVersion(1, 0)
           .WithTags("Auth")
           .WithSummary("Authenticate a user and issue tokens")
           .Produces<LoginResponse>(StatusCodes.Status200OK)
           .ProducesProblem(StatusCodes.Status401Unauthorized)
           .AllowAnonymous()
           .WithOpenApi();
    }

    private static async Task<Results<Ok<LoginResponse>, Unauthorized<ProblemDetails>>> HandleAsync(
        LoginCommand request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(request, cancellationToken);

        return result.Match<Results<Ok<LoginResponse>, Unauthorized<ProblemDetails>>>(
            value => TypedResults.Ok(value),
            error => TypedResults.Unauthorized());
    }
}
