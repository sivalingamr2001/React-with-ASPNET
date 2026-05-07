namespace JanaticsApi.Shared.Constants;

public static class ApiRoutes
{
    public const string Base = "/api";

    public static class Users
    {
        public const string Base = ApiRoutes.Base + "/v{version:apiVersion}/users";
        public const string Create = Base;
        public const string Update = Base + "/{id:guid}";
        public const string GetById = Base + "/{id:guid}";
    }

    public static class Auth
    {
        public const string Base = ApiRoutes.Base + "/v{version:apiVersion}/auth";
        public const string Login = Base + "/login";
        public const string RefreshToken = Base + "/refresh-token";
    }
}
