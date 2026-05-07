// src/EnterpriseApi.Application/Common/Exceptions/ValidationException.cs
using FluentValidation.Results;

namespace JanaticsApi.Application.Common.Exceptions;

public sealed class ValidationException : Exception
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException(IEnumerable<ValidationFailure> failures)
        : base("One or more validation failures occurred.")
    {
        Errors = failures
            .GroupBy(f => f.PropertyName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Select(f => f.ErrorMessage).ToArray(),
                StringComparer.OrdinalIgnoreCase);
    }
}