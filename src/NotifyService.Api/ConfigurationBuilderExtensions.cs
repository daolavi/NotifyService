namespace NotifyService.Api;

public static class ConfigurationBuilderExtensions
{
    public static void AddAmazonSecretManager(this IConfigurationBuilder configurationBuilder, string region, string secretName)
    {
        var configurationSource = new AmazonSecretManagerConfigurationSource(region, secretName);
        configurationBuilder.Add(configurationSource);
    }
}