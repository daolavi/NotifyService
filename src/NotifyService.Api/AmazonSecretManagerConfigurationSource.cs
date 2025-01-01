namespace NotifyService.Api;

public class AmazonSecretManagerConfigurationSource(string region, string secretName) : IConfigurationSource
{
    private readonly string _region = region;
    private readonly string _secretName = secretName;
    
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new AmazonSecretManagerConfigurationProvider(_region, _secretName);
    }
}