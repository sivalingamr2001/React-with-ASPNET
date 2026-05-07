using Asp.Versioning;
using JanaticsApi.Application.Features.Auth.Commands.RefreshToken;
using JanaticsApi.Shared.Constants;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace JanaticsApi.API.Endpoints.V1.Auth;

public sealed class RefreshTokenEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(ApiRoutes.Auth.RefreshToken, HandleAsync)
           .WithName("RefreshToken")
           .WithApiVersionSet(app.NewApiVersionSet().HasApiVersion(new ApiVersion(1, 0)).Build())
           .MapToApiVersion(1, 0)
           .WithTags("Auth")
           .WithSummary("Refresh JWT access token using a refresh token")
           .Produces<LoginResponse>(StatusCodes.Status200OK)
           .ProducesProblem(StatusCodes.Status401Unauthorized)
           .AllowAnonymous()
           .WithOpenApi();
    }

    private static async Task<Results<Ok<LoginResponse>, Unauthorized<ProblemDetails>>> HandleAsync(
        RefreshTokenCommand request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(request, cancellationToken);

        return result.Match<Results<Ok<LoginResponse>, Unauthorized<ProblemDetails>>>(
            value => TypedResults.Ok(value),
            error => TypedResults.Unauthorized());
    }
}
