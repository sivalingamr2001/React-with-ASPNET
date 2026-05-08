using Janatics.DataEngine.Domain.Enumerations;

namespace Janatics.DataEngine.Domain.Entities;

public sealed class QueryParameter
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string ParameterKey { get; init; } = default!;
    public string DisplayName { get; init; } = default!;
    public FieldDataType DataType { get; init; }
    public bool IsRequired { get; init; }
    public string? DefaultValue { get; init; }
    public int SortOrder { get; init; }
}
