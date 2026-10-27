using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using KickStreamHub.Converters;
using KickStreamHub.ViewModels;
using WpfColor = System.Windows.Media.Color;
using WpfImage = System.Windows.Controls.Image;

namespace KickStreamHub.Helpers;

/// <summary>
/// Kick sohbet mesajlarındaki [emote:ID:Name] ifadelerini gerçek görsel (InlineUIContainer + Image)
/// olarak ayrıştırır ve bellek içi önbellek (EmoteCache) ile yüksek performanslı şekilde TextBlock'a çizer.
/// </summary>
public static partial class RichChatHelper
{
    [GeneratedRegex(@"\[emote:(\d+):([^\]]+)\]")]
    private static partial Regex EmotePattern();

    private static readonly ConcurrentDictionary<string, BitmapImage> EmoteCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly StringToBrushConverter BrushConverter = new();
    private static readonly SolidColorBrush TimeBrush = CreateFrozenBrush(WpfColor.FromRgb(71, 85, 105));
    private static readonly SolidColorBrush MessageBrush = CreateFrozenBrush(WpfColor.FromRgb(226, 232, 240));
    private static readonly SolidColorBrush EmoteFallbackBrush = CreateFrozenBrush(WpfColor.FromRgb(83, 252, 24));

    private static readonly HttpClient EmoteHttp = CreateEmoteClient();

    private static HttpClient CreateEmoteClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/124.0");
        return client;
    }

    private static SolidColorBrush CreateFrozenBrush(WpfColor color)
    {
        var b = new SolidColorBrush(color);
        b.Freeze();
        return b;
    }

    public static readonly DependencyProperty MessageItemProperty =
        DependencyProperty.RegisterAttached(
            "MessageItem",
            typeof(ChatMessageItem),
            typeof(RichChatHelper),
            new PropertyMetadata(null, OnMessageItemChanged));

    public static void SetMessageItem(DependencyObject element, ChatMessageItem? value) =>
        element.SetValue(MessageItemProperty, value);

    public static ChatMessageItem? GetMessageItem(DependencyObject element) =>
        (ChatMessageItem?)element.GetValue(MessageItemProperty);

    private static void OnMessageItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBlock tb) return;
        tb.Inlines.Clear();

        if (e.NewValue is not ChatMessageItem item) return;

        // 1. Zaman damgası
        tb.Inlines.Add(new Run($"{item.TimeFormatted} ")
        {
            Foreground = TimeBrush,
            FontSize = 10,
        });

        // 2. Kullanıcı adı
        var userBrush = BrushConverter.Convert(item.ColorHex, typeof(System.Windows.Media.Brush), null, System.Globalization.CultureInfo.InvariantCulture) as System.Windows.Media.Brush ?? EmoteFallbackBrush;
        tb.Inlines.Add(new Run($"{item.Username}: ")
        {
            FontWeight = FontWeights.Bold,
            Foreground = userBrush,
        });

        // 3. Mesaj içeriği ve Kick Emote görselleri
        var raw = string.IsNullOrEmpty(item.RawMessage) ? item.Message : item.RawMessage;
        var matches = EmotePattern().Matches(raw);
        if (matches.Count == 0)
        {
            tb.Inlines.Add(new Run(raw) { Foreground = MessageBrush });
            return;
        }

        int lastIndex = 0;
        foreach (Match match in matches)
        {
            if (match.Index > lastIndex)
            {
                var textSegment = raw[lastIndex..match.Index];
                tb.Inlines.Add(new Run(textSegment) { Foreground = MessageBrush });
            }

            var emoteId = match.Groups[1].Value;
            var emoteName = match.Groups[2].Value;

            var img = new WpfImage
            {
                Width = 22,
                Height = 22,
                Margin = new Thickness(2, 0, 2, -4),
                Stretch = Stretch.Uniform,
                ToolTip = $":{emoteName}:",
            };
            RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);

            var container = new InlineUIContainer(img)
            {
                BaselineAlignment = BaselineAlignment.Center
            };
            tb.Inlines.Add(container);

            if (EmoteCache.TryGetValue(emoteId, out var cachedBmp))
            {
                img.Source = cachedBmp;
            }
            else
            {
                _ = LoadEmoteImageAsync(emoteId, img);
            }

            lastIndex = match.Index + match.Length;
        }

        if (lastIndex < raw.Length)
        {
            tb.Inlines.Add(new Run(raw[lastIndex..]) { Foreground = MessageBrush });
        }
    }

    private static async Task LoadEmoteImageAsync(string emoteId, WpfImage targetImage)
    {
        try
        {
            var url = $"https://files.kick.com/emotes/{emoteId}/fullsize";
            var bytes = await EmoteHttp.GetByteArrayAsync(url).ConfigureAwait(false);
            if (bytes.Length == 0) return;

            var bmp = new BitmapImage();
            using (var ms = new MemoryStream(bytes))
            {
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.DecodePixelHeight = 44;
                bmp.StreamSource = ms;
                bmp.EndInit();
            }
            bmp.Freeze();

            EmoteCache[emoteId] = bmp;

            await targetImage.Dispatcher.InvokeAsync(() =>
            {
                targetImage.Source = bmp;
            });
        }
        catch
        {
            // İndirilemezse sessizce geç
        }
    }
}
