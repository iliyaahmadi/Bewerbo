using System.IO;
using System.Net.Http;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Bewerbo.Services;

/// <summary>
/// Website icons for the application cards, fetched from Google's favicon service and kept on disk,
/// so each site is only downloaded once. Cards show the company initials while no icon is available.
/// </summary>
public sealed class FaviconCache(string folder)
{
    private static readonly HttpClient Http = CreateClient();

    private readonly Dictionary<string, Task<ImageSource?>> _icons = new(StringComparer.OrdinalIgnoreCase);

    public Task<ImageSource?> GetAsync(string host)
    {
        if (!_icons.TryGetValue(host, out var icon))
            _icons[host] = icon = LoadAsync(host);
        return icon;
    }

    private async Task<ImageSource?> LoadAsync(string host)
    {
        var path = Path.Combine(folder, host + ".png");
        try
        {
            byte[] bytes;
            if (File.Exists(path))
            {
                bytes = await File.ReadAllBytesAsync(path);
            }
            else
            {
                bytes = await Http.GetByteArrayAsync("https://www.google.com/s2/favicons?sz=64&domain=" + Uri.EscapeDataString(host));
                Directory.CreateDirectory(folder);
                await File.WriteAllBytesAsync(path, bytes);
            }

            return Decode(bytes);
        }
        catch (Exception)
        {
            // Offline, no icon for the site, or an unreadable image: the card keeps its initials.
            return null;
        }
    }

    private static BitmapImage Decode(byte[] bytes)
    {
        var image = new BitmapImage();
        image.BeginInit();
        image.StreamSource = new MemoryStream(bytes);
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.EndInit();
        image.Freeze();
        return image;
    }

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Bewerbo");
        return client;
    }
}
