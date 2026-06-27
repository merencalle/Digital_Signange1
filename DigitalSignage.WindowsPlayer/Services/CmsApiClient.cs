using System.IO;
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

    /// <summary>
    /// Downloads a file to <paramref name="destinationPath"/>, resuming via HTTP Range
    /// if a partially-downloaded file already exists there (e.g. from a dropped connection).
    /// </summary>
    public async Task DownloadFileAsync(string relativePath, string destinationPath)
    {
        var uri = new Uri($"{BaseUrl}/{relativePath.TrimStart('/')}");
        var existingLength = File.Exists(destinationPath) ? new FileInfo(destinationPath).Length : 0;

        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        if (existingLength > 0)
        {
            request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(existingLength, null);
        }

        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        var resumed = existingLength > 0 && response.StatusCode == System.Net.HttpStatusCode.PartialContent;
        if (existingLength > 0 && !resumed)
        {
            File.Delete(destinationPath);
        }

        response.EnsureSuccessStatusCode();

        var fileMode = resumed ? FileMode.Append : FileMode.Create;
        await using var fileStream = new FileStream(destinationPath, fileMode, FileAccess.Write);
        await using var responseStream = await response.Content.ReadAsStreamAsync();
        await responseStream.CopyToAsync(fileStream);
    }
}
