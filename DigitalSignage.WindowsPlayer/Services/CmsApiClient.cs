using System.Net.Http;
using System.Net.Http.Json;
using DigitalSignage.Shared.Dtos;
using DigitalSignage.Shared.Models;

namespace DigitalSignage.WindowsPlayer.Services;

public class CmsApiClient
{
    private readonly HttpClient _http;

    public CmsApiClient(string baseUrl)
    {
        _http = new HttpClient { BaseAddress = new Uri(baseUrl) };
    }

    public string BaseUrl => _http.BaseAddress!.ToString().TrimEnd('/');

    public async Task<Device?> RegisterAsync(string uniqueId, string name, string deviceType, string ipAddress)
    {
        var request = new DeviceRegisterRequest
        {
            UniqueId = uniqueId,
            Name = name,
            DeviceType = deviceType,
            IpAddress = ipAddress
        };

        var response = await _http.PostAsJsonAsync("/api/devices/register", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Device>();
    }

    public async Task SendHeartbeatAsync(int deviceId)
    {
        var response = await _http.PostAsync($"/api/devices/{deviceId}/heartbeat", null);
        response.EnsureSuccessStatusCode();
    }

    public async Task<PlaylistContentDto?> GetPlaylistAsync(int deviceId)
    {
        var response = await _http.GetAsync($"/api/devices/{deviceId}/playlist");
        if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PlaylistContentDto>();
    }
}
