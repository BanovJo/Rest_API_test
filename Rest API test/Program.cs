using Microsoft.Identity.Client;
using System;
using System.Net.Http;
using System.Threading.Tasks;

public interface ITokenProvider
{
    Task<string> GetAccessToken();
}

public class AzureADTokenProvider : ITokenProvider
{
    private readonly string clientId;
    private readonly string clientSecret;
    private readonly string tenantId;
    private readonly string authorityUrl;

    public AzureADTokenProvider(string clientId, string clientSecret, string tenantId)
    {
        this.clientId = clientId;
        this.clientSecret = clientSecret;
        this.tenantId = tenantId;
        this.authorityUrl = $"https://login.microsoftonline.com/{tenantId}";
    }

    public async Task<string> GetAccessToken()
    {
        IConfidentialClientApplication app = ConfidentialClientApplicationBuilder
            .Create(clientId)
            .WithClientSecret(clientSecret)
            .WithAuthority(new Uri(authorityUrl))
            .Build();

        string[] scopes = { "https://analysis.windows.net/powerbi/api/.default" };
        AuthenticationResult result = await app.AcquireTokenForClient(scopes).ExecuteAsync();
        return result.AccessToken;
    }
}

public class PowerBIApiClient
{
    private readonly string apiUrl;
    private readonly ITokenProvider tokenProvider;

    public PowerBIApiClient(string apiUrl, ITokenProvider tokenProvider)
    {
        this.apiUrl = apiUrl;
        this.tokenProvider = tokenProvider;
    }

    public async Task<string> CallApi(string apiEndpoint)
    {
        HttpClient client = new HttpClient();
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, $"{apiUrl}{apiEndpoint}");

        string accessToken = await tokenProvider.GetAccessToken();
        request.Headers.Add("Authorization", $"Bearer {accessToken}");

        HttpResponseMessage response = await client.SendAsync(request);
        return await response.Content.ReadAsStringAsync();
    }
}

public class Program
{
    private static async Task Main()
    {
        string clientId = "YOUR_CLIENT_ID";
        string clientSecret = "YOUR_CLIENT_SECRET";
        string tenantId = "YOUR_TENANT_ID";
        string apiUrl = "https://api.powerbi.com/";

        ITokenProvider tokenProvider = new AzureADTokenProvider(clientId, clientSecret, tenantId);
        PowerBIApiClient apiClient = new PowerBIApiClient(apiUrl, tokenProvider);

        // Primjer poziva različitih API-ja
        string groupsData = await apiClient.CallApi("v1.0/myorg/groups");
        Console.WriteLine(groupsData);

        string reportsData = await apiClient.CallApi("v1.0/myorg/reports");
        Console.WriteLine(reportsData);

        string datasetsData = await apiClient.CallApi("v1.0/myorg/datasets");
        Console.WriteLine(datasetsData);
    }
}
