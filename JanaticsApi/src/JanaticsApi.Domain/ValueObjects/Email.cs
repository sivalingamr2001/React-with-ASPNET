// src/EnterpriseApi.Domain/ValueObjects/Email.cs
using System.Text.RegularExpressions;
using JanaticsApi.Domain.Common;
using JanaticsApi.Domain.Exceptions;

namespace JanaticsApi.Domain.ValueObjects;

public sealed class Email : ValueObject
{
    private static readonly Regex EmailRegex = new(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase,
        TimeSpan.FromMilliseconds(250));

    public string Value { get; }

    private Email(string value) => Value = value;

    public static Email Create(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var normalized = value.Trim().ToLowerInvariant();

        if (!EmailRegex.IsMatch(normalized))
            throw new BusinessRuleViolationException($"'{value}' is not a valid email address.");

        return new Email(normalized);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(Email email) => email.Value;
}