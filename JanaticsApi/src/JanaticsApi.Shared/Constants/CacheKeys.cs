// src/EnterpriseApi.Shared/Constants/CacheKeys.cs
namespace JanaticsApi.Shared.Constants;

public static class CacheKeys
{
    public static class Users
    {
        public static string ById(Guid id) => $"users:id:{id}";
        public static string ByEmail(string email) => $"users:email:{email.ToLower()}";
        public static string PagedList(int page, int size, string? search) =>
            $"users:list:p{page}:s{size}:q{search ?? "all"}";
        public const string Pattern = "users:";
    }

    public static class Auth
    {
        public static string RevokedToken(string jti) => $"auth:revoked:{jti}";
        public static string UserPermissions(Guid userId) => $"auth:perms:{userId}";
    }
}