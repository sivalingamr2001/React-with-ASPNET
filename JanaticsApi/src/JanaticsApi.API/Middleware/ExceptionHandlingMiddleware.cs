// src/EnterpriseApi.API/Middleware/ExceptionHandlingMiddleware.cs
using JanaticsApi.Application.Common.Exceptions;
using JanaticsApi.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace JanaticsApi.API.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            var correlationId = context.TraceIdentifier;

            _logger.LogError(
                exception,
                "Unhandled exception. CorrelationId: {CorrelationId} | Path: {Path} | Method: {Method}",
                correlationId,
                context.Request.Path,
                context.Request.Method);

            await HandleExceptionAsync(context, exception, correlationId);
        }
    }

    private static async Task HandleExceptionAsync(
        HttpContext context,
        Exception exception,
        string correlationId)
    {
        context.Response.ContentType = "application/problem+json";

        var (statusCode, problemDetails) = exception switch
        {
            ValidationException validationEx => (
                StatusCodes.Status422UnprocessableEntity,
                new ValidationProblemDetails(validationEx.Errors)
                {
                    Status = StatusCodes.Status422UnprocessableEntity,
                    Title = "Validation Failed",
                    Detail = "One or more validation errors occurred.",
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    Instance = context.Request.Path
                }),

            EntityNotFoundException notFoundEx => (
                StatusCodes.Status404NotFound,
                new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Resource Not Found",
                    Detail = notFoundEx.Message,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                    Instance = context.Request.Path
                }),

            BusinessRuleViolationException businessEx => (
                StatusCodes.Status409Conflict,
                new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Business Rule Violation",
                    Detail = businessEx.Message,
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.5.8",
                    Instance = context.Request.Path
                }),

            UnauthorizedAccessException => (
                StatusCodes.Status401Unauthorized,
                new ProblemDetails
                {
                    Status = StatusCodes.Status401Unauthorized,
                    Title = "Unauthorized",
                    Detail = "Authentication is required.",
                    Type = "https://tools.ietf.org/html/rfc7235#section-3.1",
                    Instance = context.Request.Path
                }),

            _ => (
                StatusCodes.Status500InternalServerError,
                new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Internal Server Error",
                    Detail = "An unexpected error occurred. Please try again later.",
                    Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                    Instance = context.Request.Path
                })
        };

        // Inject correlation ID into problem details
        problemDetails.Extensions["correlationId"] = correlationId;
        problemDetails.Extensions["timestamp"] = DateTimeOffset.UtcNow;

        context.Response.StatusCode = statusCode;

        await context.Response.WriteAsJsonAsync(
            problemDetails,
            options: null,
            contentType: "application/problem+json");
    }
}