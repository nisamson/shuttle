namespace Shuttle.WebClient.Models;

public static class Routes {
    public const string Home = "/";
    public const string Privacy = "/privacy";

    public static class Authentication {
        public const string Root = "/authentication";
        public const string Login = $"{Root}/login";
        public const string Logout = $"{Root}/logout";
    }
}
