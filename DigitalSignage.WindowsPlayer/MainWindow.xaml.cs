using System.Net;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DigitalSignage.Shared.Models;
using DigitalSignage.WindowsPlayer.Services;

namespace DigitalSignage.WindowsPlayer;

public partial class MainWindow : Window
{
    private static readonly Random Jitter = new();

    private readonly CmsApiClient _api;
    private readonly MediaCache _mediaCache = new();
    private readonly DispatcherTimer _heartbeatTimer = new();
    private readonly DispatcherTimer _playlistTimer = new();
    private readonly DispatcherTimer _advanceTimer = new();

    private List<ContentItem> _items = new();
    private int _currentIndex = -1;
    private int? _deviceId;

    public MainWindow()
    {
        InitializeComponent();

        var baseUrl = Environment.GetEnvironmentVariable("CMS_BASE_URL") ?? "https://localhost:5110";
        _api = new CmsApiClient(baseUrl);

        // Jittered intervals so a fleet of players doesn't hammer the CMS in lockstep.
        _heartbeatTimer.Interval = JitteredInterval(30);
        _playlistTimer.Interval = JitteredInterval(30);
        _advanceTimer.Interval = TimeSpan.FromSeconds(8);

        _heartbeatTimer.Tick += async (_, _) =>
        {
            _heartbeatTimer.Interval = JitteredInterval(30);
            await SendHeartbeatAsync();
        };
        _playlistTimer.Tick += async (_, _) =>
        {
            _playlistTimer.Interval = JitteredInterval(30);
            await RefreshPlaylistAsync();
        };
        _advanceTimer.Tick += async (_, _) => await ShowNextAsync();

        Loaded += MainWindow_Loaded;
    }

    private static TimeSpan JitteredInterval(int baseSeconds) =>
        TimeSpan.FromSeconds(baseSeconds + Jitter.Next(-5, 6));

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        PlaceholderText.Text = "Registering with CMS...";
        PlaceholderText.Visibility = Visibility.Visible;

        var uniqueId = DeviceIdentity.GetOrCreateUniqueId();
        var device = await _api.RegisterAsync(uniqueId, Environment.MachineName, "WindowsPlayer", GetLocalIpAddress());
        _deviceId = device?.Id;

        if (_deviceId is null)
        {
            PlaceholderText.Text = "Could not register with CMS.";
            return;
        }

        _heartbeatTimer.Start();
        _playlistTimer.Start();
        await RefreshPlaylistAsync();
    }

    private async Task SendHeartbeatAsync()
    {
        if (_deviceId is null)
        {
            return;
        }

        try
        {
            await _api.SendHeartbeatAsync(_deviceId.Value);
        }
        catch
        {
            // Network hiccup; the next heartbeat tick will retry.
        }
    }

    private async Task RefreshPlaylistAsync()
    {
        if (_deviceId is null)
        {
            return;
        }

        try
        {
            var playlist = await _api.GetPlaylistAsync(_deviceId.Value);
            var newItems = playlist?.Items ?? new List<ContentItem>();

            var changed = newItems.Select(i => i.Id).SequenceEqual(_items.Select(i => i.Id)) == false;
            if (!changed)
            {
                return;
            }

            _items = newItems;
            _currentIndex = -1;

            if (_items.Count == 0)
            {
                _advanceTimer.Stop();
                ShowPlaceholder("No content assigned.");
            }
            else
            {
                await ShowNextAsync();
            }
        }
        catch
        {
            // Network hiccup; the next playlist tick will retry.
        }
    }

    private async Task ShowNextAsync()
    {
        if (_items.Count == 0)
        {
            ShowPlaceholder("No content assigned.");
            return;
        }

        _currentIndex = (_currentIndex + 1) % _items.Count;
        var item = _items[_currentIndex];

        _advanceTimer.Stop();
        VideoDisplay.Stop();

        if (item.ContentType != "Image" && item.ContentType != "Video")
        {
            ShowPlaceholder($"Unsupported content type '{item.ContentType}': {item.Name}");
            _advanceTimer.Start();
            return;
        }

        string localPath;
        try
        {
            // Cached items play straight from disk - no network hit on repeat loops.
            localPath = await _mediaCache.EnsureCachedAsync(item, destPath => _api.DownloadFileAsync(item.FilePath, destPath));
        }
        catch
        {
            ShowPlaceholder($"Could not download '{item.Name}'.");
            _advanceTimer.Start();
            return;
        }

        var uri = new Uri(localPath);

        switch (item.ContentType)
        {
            case "Image":
                ImageDisplay.Source = new BitmapImage(uri);
                ShowOnly(ImageDisplay);
                _advanceTimer.Start();
                break;

            case "Video":
                VideoDisplay.Source = uri;
                ShowOnly(VideoDisplay);
                VideoDisplay.Play();
                break;
        }
    }

    private async void VideoDisplay_MediaEnded(object sender, RoutedEventArgs e)
    {
        await ShowNextAsync();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
        }
    }

    private void ShowPlaceholder(string message)
    {
        PlaceholderText.Text = message;
        ShowOnly(PlaceholderText);
    }

    private void ShowOnly(UIElement visible)
    {
        ImageDisplay.Visibility = visible == ImageDisplay ? Visibility.Visible : Visibility.Collapsed;
        VideoDisplay.Visibility = visible == VideoDisplay ? Visibility.Visible : Visibility.Collapsed;
        PlaceholderText.Visibility = visible == PlaceholderText ? Visibility.Visible : Visibility.Collapsed;
    }

    private static string GetLocalIpAddress()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            var address = host.AddressList.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);
            return address?.ToString() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}
