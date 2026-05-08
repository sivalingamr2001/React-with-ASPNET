using Janatics.DataEngine.Domain.Entities;
using Janatics.DataEngine.Domain.Enumerations;
using Janatics.DataEngine.Domain.Interfaces;

namespace Janatics.DataEngine.Api;

public static class MetadataSeedExtensions
{
    public static async Task SeedDataEngineMetadataAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var metadataRepository = scope.ServiceProvider.GetRequiredService<IMetadataRepository>();

        var userEntityId = Guid.Parse("f847902d-8b73-426d-947e-80c22591db86");
        await metadataRepository.UpsertEntityAsync(
            new MetadataEntity
            {
                Id = userEntityId,
                TenantCode = "default",
                EntityKey = "users",
                DisplayName = "Users",
                TableName = "Users",
                PrimaryKeyField = "id",
                ProviderType = DataProviderType.SqlServer,
                ConnectionName = "DefaultConnection",
                Fields =
                [
                    new MetadataField
                    {
                        EntityId = userEntityId,
                        FieldKey = "id",
                        ColumnName = "Id",
                        DisplayName = "Id",
                        DataType = FieldDataType.Integer,
                        IsReadOnly = true,
                        IsSystemField = true
                    },
                    new MetadataField
                    {
                        EntityId = userEntityId,
                        FieldKey = "name",
                        ColumnName = "Name",
                        DisplayName = "Name",
                        DataType = FieldDataType.String
                    },
                    new MetadataField
                    {
                        EntityId = userEntityId,
                        FieldKey = "email",
                        ColumnName = "Email",
                        DisplayName = "Email",
                        DataType = FieldDataType.String
                    }
                ]
            });

        await metadataRepository.UpsertQueryDefinitionAsync(
            new QueryDefinition
            {
                TenantCode = "default",
                QueryKey = "users.list",
                DisplayName = "User List",
                RootEntityKey = "users",
                ProviderType = DataProviderType.SqlServer,
                ConnectionName = "DefaultConnection",
                SqlTemplate = "SELECT Id, Name, Email FROM Users",
                IsActive = true
            });
    }
}
