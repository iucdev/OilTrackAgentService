using System.Net.Http.Headers;
using System.Net.Http;
using System;
using Service.Clients.Utils;
using Sunp.Api.Client;

public class SunpApiClientSingleton {
    private static readonly Lazy<SunpApiClientSingleton> lazy = new Lazy<SunpApiClientSingleton>(() => new SunpApiClientSingleton());

    public static SunpApiClientSingleton Instance => lazy.Value;

    public SunpApiClient SunpApiClient { get; private set; }

    private SunpApiClientSingleton()
    {
        InitializeClient();
    }

    private void InitializeClient()
    {
        var url = ObjectSettingsSingleton.Instance.ObjectSettings.ApiUrl;
        var token = ObjectSettingsSingleton.Instance.ObjectSettings.ApiToken;

        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var sunpApiClient = new SunpApiClient(url, httpClient);

        SunpApiClient = sunpApiClient;
    }
}