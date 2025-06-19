namespace ProfessionalProfiles.Configurations
{
    public static class BuildVariables
    {
        public static string Version => Environment.GetEnvironmentVariable("VERSION_TAG") ?? "Dev Version";
    }
}
