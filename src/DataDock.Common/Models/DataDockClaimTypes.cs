namespace DataDock.Common.Models
{
    public static class DataDockClaimTypes
    {
        public const string GitHubAccessToken = "urn:tokens:github:accesstoken";

        public static string DataDockUserId => "urn:datadock:userId";

        public static string DataDockAdmin => "urn:datadock:admin";

        public static string GitHubLogin => "urn:github:login";

        public static string GitHubId => "urn:github:id";

        public static string GitHubName => "urn:github:name";

        public static string GitHubEmail => "urn:github:email";

        public static string GitHubUrl => "urn:github:url";

        public static string GitHubAvatar => "urn:github:avatar";

        public static string GitHubUser => "urn:datadock:github:user";

        public static string GitHubUserOrganization => "urn:datadock:github:organization";
    }
}
