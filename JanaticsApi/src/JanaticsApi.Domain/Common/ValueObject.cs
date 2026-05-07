// src/EnterpriseApi.Domain/Common/ValueObject.cs
namespace JanaticsApi.Domain.Common;

public abstract class ValueObject : IEquatable<ValueObject>
{
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public override bool Equals(object? obj) =>
        obj is ValueObject other && ValuesAreEqual(other);

    public bool Equals(ValueObject? other) =>
        other is not null && ValuesAreEqual(other);

    private bool ValuesAreEqual(ValueObject other) =>
        GetType() == other.GetType() &&
        GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());

    public override int GetHashCode() =>
        GetEqualityComponents()
            .Aggregate(default(int), HashCode.Combine);

    public static bool operator ==(ValueObject? left, ValueObject? right) =>
        left?.Equals(right) ?? right is null;

    public static bool operator !=(ValueObject? left, ValueObject? right) =>
        !(left == right);
}