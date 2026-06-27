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
    private readonly CmsApiClient _api;
    private readonly DispatcherTimer _heartbeatTimer = new() { Interval = TimeSpan.FromSeconds(30) };
    private readonly DispatcherTimer _playlistTimer = new() { Interval = TimeSpan.FromSeconds(30) };
    private readonly DispatcherTimer _advanceTimer = new() { Interval = TimeSpan.FromSeconds(8) };

    private List<ContentItem> _items = new();
    private int _currentIndex = -1;
    private int? _deviceId;

    public MainWindow()
    {
        InitializeComponent();

        var baseUrl = Environment.GetEnvironmentVariable("CMS_BASE_URL") ?? "http://localhost:5109";
        _api = new CmsApiClient(baseUrl);

        _heartbeatTimer.Tick += async (_, _) => await SendHeartbeatAsync();
        _playlistTimer.Tick += async (_, _) => await RefreshPlaylistAsync();
        _advanceTimer.Tick += (_, _) => ShowNext();

        Loaded += MainWindow_Loaded;
    }

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
                ShowNext();
            }
        }
        catch
        {
            // Network hiccup; the next playlist tick will retry.
        }
    }

    private void ShowNext()
    {
        if (_items.Count == 0)
        {
            ShowPlaceholder("No content assigned.");
            return;
        }

        _currentIndex = (_currentIndex + 1) % _items.Count;
        var item = _items[_currentIndex];
        var uri = new Uri($"{_api.BaseUrl}/{item.FilePath.TrimStart('/')}");

        _advanceTimer.Stop();
        VideoDisplay.Stop();

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

            default:
                ShowPlaceholder($"Unsupported content type '{item.ContentType}': {item.Name}");
                _advanceTimer.Start();
                break;
        }
    }

    private void VideoDisplay_MediaEnded(object sender, RoutedEventArgs e)
    {
        ShowNext();
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
