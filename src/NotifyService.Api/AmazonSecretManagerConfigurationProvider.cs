using System.Text;
using System.Text.Json;
using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;

namespace NotifyService.Api;

public class AmazonSecretManagerConfigurationProvider(string region, string secretName) : ConfigurationProvider
{
    private readonly string _region = region;
    private readonly string _secretName = secretName;

    public override void Load()
    {
        var secret = GetSecret();
        Data = JsonSerializer.Deserialize<Dictionary<string, string>>(secret)!;
    }

    private string GetSecret()
    {
        var request = new GetSecretValueRequest
        {
            SecretId = _secretName,
            VersionStage = "AWSCURRENT"
        };

        using var client = new AmazonSecretsManagerClient(RegionEndpoint.GetBySystemName(_region));
        var response = client.GetSecretValueAsync(request).Result;
        string secretString;
        if (response.SecretString is not null)
        {
            secretString = response.SecretString;
        }
        else
        {
            var memoryStream = response.SecretBinary;
            var reader = new StreamReader(memoryStream);
            secretString = Encoding.UTF8.GetString(Convert.FromBase64String(reader.ReadToEnd()));
        }
        return secretString;
    }
}