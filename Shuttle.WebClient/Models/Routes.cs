namespace Shuttle.WebClient.Models;

public static class Routes {
    public const string Home = "/";
    public const string Privacy = "/privacy";

    public static class Authentication {
        public const string Root = "/authentication";
        public const string Login = $"{Root}/login";
        public const string Logout = $"{Root}/logout";
    }

    public static class Debug {
        public const string Claims = "/debug/claims";
        public const string Roles = "/debug/roles";
    }

    public static class Admin {
        public const string Root = "/admin";
        public const string Hello = $"{Root}/hello";
    }

    public static class Articles {
        public const string Blogs = "/blogs";

        public static string Blog(string slug) => $"{Blogs}/{slug}";
    }

    public static class Players {
        public const string Root = "/players";

        public static string Player(int playerId) => $"{Root}/{playerId}";
    }

    public static class Users {
        public const string Root = "/users";

        public static string User(int userId) => $"{Root}/{userId}";

        public static string User(string userIdOrName) => $"{Root}/{userIdOrName}";
    }
}
