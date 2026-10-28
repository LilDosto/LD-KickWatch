<div align="center">

# 🟢 LD KickWatch

**Kick.com için Modern Masaüstü Canlı Yayın İzleme Stüdyosu ve Sohbet İstemcisi**

[![Release](https://img.shields.io/badge/Release-v1.3.0-53FC18?style=for-the-badge&logo=github)](https://github.com/LilDosto/LD-KickWatch/releases/latest)
[![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%2F%2011%20(x64)-0078D6?style=for-the-badge&logo=windows)](https://github.com/LilDosto/LD-KickWatch)
[![.NET](https://img.shields.io/badge/.NET-8.0%20WPF-512BD4?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![License](https://img.shields.io/badge/Lisans-MIT-53FC18?style=for-the-badge)](./LICENSE)

Favori **Kick.com** yayıncılarınızı takip edin, anlık canlı yayın ve kategori bildirimleri alın, **Pencere İçi Web Oynatıcı (WebView2)** veya **Pencere İçi Gömülü VLC** ile yayınları izleyin, **2x2 Çoklu Yayın (Squad View)** modunda 4 yayını aynı anda takip edin ve gerçek **Kick Emote** destekli canlı sohbete bağlanın.

[📦 **Son Sürümü İndir (v1.3.0 Releases)**](https://github.com/LilDosto/LD-KickWatch/releases/latest) • [🚀 **Özellikler**](#-başlıca-özellikler) • [⌨ **Kısayollar**](#-klavye-kısayolları) • [🛠 **Derleme**](#-kaynak-koddan-derleme)

</div>

---

## 🚀 Başlıca Özellikler

### 🎬 Gelişmiş Çift Oynatıcı Motoru (Pencere İçi Web & Pencere İçi VLC)
- **🌐 Dahili Modern Web Oynatıcı (WebView2):** Ekstra hiçbir programa ihtiyaç duymadan yayınları doğrudan uygulama içinde açar.
- **🎬 Pencere İçi Gömülü VLC (`--drawable-hwnd`):** VLC Media Player'ı ayrı bir pencere açmadan doğrudan uygulama arayüzünün içine gömer. Streamlink kurulu olmasa bile Kick API'den alınan doğrudan HLS (`playback_url`) akışı ile çalışır.
- **⚡ Tek Tıkla Motor Değiştirme:** Yayını izlerken üst kontrol barındaki **🌐 Web** ve **🎬 VLC (Pencere İçi)** butonlarıyla anında oynatıcı motorları arasında geçiş yapabilirsiniz.
- **🚀 Harici VLC & MPV Desteği:** İsterseniz Streamlink üzerinden harici VLC veya MPV penceresinde `best`, `1080p60`, `720p60`, `480p`, `360p`, `160p` ve `audio_only` kalitelerinde izleyebilirsiniz.

### ⛶ Gerçek Tam Ekran & Sinema Modu
- **Tam Ekran (`F11` / `Esc` / Çift Tıklama):** Hem oynatıcı barındaki **⛶ Tam Ekran** butonu, hem `F11` kısayolu, hem VLC paneline çift tıklama hem de Kick oynatıcısının içindeki tam ekran butonu (`ContainsFullScreenElementChanged`) tam senkronize çalışır.
- **Sinema Modu (`🗖`):** Pencereyi tam ekran yapmadan sağ panelin tamamını video oynatıcıya ayırır.
- **Bağımsız Pencere & PiP (`📌 Üstte Tut`):** Yayını ayrı bir pencerede açıp her zaman üstte (Picture-in-Picture tarzı) sabitleyebilirsiniz.

### ⊞ 2x2 Çoklu Yayın Stüdyosu (Multi-Stream / Squad View)
- Sol menüdeki **⊞ Çoklu Yayın** sekmesinden aynı anda **4 canlı yayını (2x2 Izgara)** veya **2 canlı yayını (Yan Yana)** izleyebilirsiniz.
- **⚡ Canlı Kanalları Otomatik Doldur:** O an yayında olan favori kanallarınızı tek tıkla 4 ekrana otomatik yerleştirir.
- **🔊 Tek Tıkla Ses Odaklama (Solo Audio):** İstediğiniz ekranın **🔊 Bu Ekranı Dinle** butonuna tıkladığınızda o yayının sesi açılır, diğer 3 ekran otomatik olarak sessize alınır.

### 💬 Yerleşik (Split-View) Sohbet Çekmecesi & Görsel Kick Emote Desteği
- **Yan Sohbet Çekmecesi:** Ana pencerenin sağ tarafında (Sinema Modu ve Tam Ekran dahil) açılıp kapanabilen yerleşik canlı sohbet paneli.
- **Bağımsız Sohbet Penceresi:** Saydamlık (Opacity), yazı boyutu ve **📌 Üstte Tut** ayarlarına sahip harici sohbet penceresi.
- **😎 Gerçek Kick Emote Render Sistemi:** Pusher WebSocket üzerinden gelen `[emote:ID:Name]` ifadeleri asenkron olarak indirilip bellek içi önbelleğe (`EmoteCache`) alınır ve sohbette gerçek görsel ifadeler olarak gösterilir.

### 🔔 Sesli Bildirimler, Kategori Değişikliği ve Anahtar Kelime Uyarıları
- **Canlı Yayın Bildirimi:** Takip ettiğiniz yayıncı yayına girdiğinde masaüstü bildirimi ve yerleşik çift tonlu armonik bildirim zili (veya seçtiğiniz özel `.wav` sesi) çalar.
- **🎮 Kategori / Oyun Değişikliği Uyarısı:** Yayıncı yayındayken oynadığı oyunu veya kategoriyi değiştirdiğinde anında bildirim verir.
- **🎯 Anahtar Kelime Yakalayıcı:** Belirlediğiniz kelimeler (örn: `çekiliş, turnuva, valorant, derbi`) yayın başlığında veya kategorisinde geçtiğinde özel sesli ve görsel uyarı gönderir.

### 🏷 Favori Gruplama / Etiketleme ve JSON/CSV Yedekleme
- Kanallara özel grup etiketleri (`FPS`, `IRL`, `E-Spor`, `Favoriler` vb.) atayabilir ve sol panelden gruba göre filtreleyebilirsiniz.
- **📤 Dışa Aktar / 📥 İçe Aktar:** Takip listenizi ve grup etiketlerinizi `.json` veya `.csv` formatında yedekleyebilir veya geri yükleyebilirsiniz.
- **⚡ Toplu Kanal Ekleme:** Virgül veya boşlukla ayrılmış birden fazla kanal adını ya da `kick.com/kanal` bağlantısını tek seferde ekleyebilirsiniz.

---

## 📥 Kurulum ve Kullanım

### Seçenek 1: Hazır Tek Dosya İle Çalıştırma (Önerilen)
1. Bu depodaki [`publish/LDKickWatch.exe`](./publish/LDKickWatch.exe) dosyasını indirin.
2. Çift tıklayarak doğrudan çalıştırın (`.NET Runtime` dahil tüm bağımlılıklar tek `.exe` içine gömülüdür, kurulum gerektirmez).

### Seçenek 2: Kaynak Koddan Derleme
Gereksinimler: [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (Windows)

```powershell
git clone https://github.com/LilDosto/LD-KickWatch.git
cd LD-KickWatch

# Geliştirme modunda çalıştırma
dotnet run --project src/KickStreamHub/KickStreamHub.csproj

# Sıkıştırılmış bağımsız tek dosya (.exe) olarak derleme
dotnet publish src/KickStreamHub/KickStreamHub.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:DebugType=none -p:DebugSymbols=false -o publish
```

---

## ⌨ Klavye Kısayolları

| Kısayol | İşlev |
| :--- | :--- |
| **`F11`** / **`Esc`** | Oynatıcıyı Tam Ekran yap / Tam ekrandan çık |
| **`F5`** | Tüm kanalların canlı yayın durumunu anında yenile |
| **`Ctrl + F`** | Kanal, başlık, kategori ve grup arama kutusuna odaklan |
| **`Çift Tıklama`** | Kanal listesindeki yayıncıyı başlat / VLC panelinde tam ekrana geç |
| **`Enter`** | Kanal ekleme veya grup etiketi kutusundaki işlemi onayla |
| **`Ctrl + Q`** | Uygulamadan tamamen çık |

---

## 🏗 Proje Mimarisi

```text
LD-KickWatch/
├── publish/
│   └── LDKickWatch.exe                # Bağımsız tek dosya (Self-Contained) Windows sürümü
├── src/KickStreamHub/
│   ├── Helpers/
│   │   └── RichChatHelper.cs          # Görsel Kick Emote ayrıştırıcı ve bellek önbelleği
│   ├── Models/
│   │   ├── AppConfig.cs               # Kullanıcı tercihleri ve grup modelleri
│   │   ├── ChannelStatus.cs           # Canlı yayın, izleyici ve HLS akış durumu
│   │   └── KickApiModels.cs           # Kick API v1/v2 JSON DTO modelleri
│   ├── Services/
│   │   ├── KickApiService.cs          # Thread-safe HTTP/2 Kick API istemcisi
│   │   ├── LiveStatusPollingService.cs# Eşzamanlı (SemaphoreSlim) periyodik durum sorgulayıcı
│   │   ├── PusherChatService.cs       # Otomatik yeniden bağlanan Pusher WebSocket istemcisi
│   │   ├── StreamlinkService.cs       # Pencere içi gömülü VLC (--drawable-hwnd) & harici oynatıcı servisi
│   │   ├── SoundService.cs            # PCM WAV sentezleyici ve özel ses bildirim servisi
│   │   ├── AvatarCacheService.cs      # Dosya kilitlemeyen yerel profil resmi önbelleği
│   │   ├── ConfigService.cs           # %LOCALAPPDATA%\LDKickWatch JSON yapılandırma yöneticisi
│   │   ├── NotificationService.cs     # Sistem tepsisi (System Tray) ve masaüstü bildirimleri
│   │   ├── StartupService.cs          # Windows başlangıç (Registry) entegrasyonu
│   │   └── LoggingService.cs          # Otomatik rotasyonlu (5 MB) log servisi
│   ├── ViewModels/
│   │   ├── MainViewModel.cs           # Ana stüdyo, filtreleme, bildirim ve çoklu yayın mantığı
│   │   ├── ChannelCardViewModel.cs    # Kanal kartı görünüm modeli
│   │   ├── ChatViewModel.cs           # Yerleşik ve harici sohbet görünüm modeli
│   │   └── MultiStreamSlotViewModel.cs# 2x2 Squad View ekran modeli
│   ├── Views/
│   │   ├── MainWindow.xaml(.cs)       # Ana uygulama penceresi
│   │   ├── PlayerWindow.xaml(.cs)     # Bağımsız PiP destekli oynatıcı penceresi
│   │   └── ChatWindow.xaml(.cs)       # Bağımsız canlı sohbet penceresi
│   └── Resources/
│       ├── Styles.xaml                # Özel koyu tema ve modern WPF kontrol şablonları
│       └── app.ico                    # Çoklu çözünürlüklü LD KickWatch simgesi
└── LICENSE
```

---

## 🆕 Sürüm Geçmişi

### 🟢 v1.3.0 (28 Ekim 2026)
- **⚙ Oynatıcı İçi Anlık Kalite Seçimi:** Yayın çözünürlüğü ve kalite seçimi (`best`, `1080p60`, `720p60`, `720p`, `480p`, `360p`, `160p`, `audio_only`) doğrudan video oynatıcı araç çubuğuna taşındı.
- **⚡ Ultra Hızlı Pencere İçi VLC Motoru:** Streamlink/Cloudflare bekleme ve donma sorunları tamamen giderildi. Kick AWS IVS master playlisti doğrudan bellek içinde ayrıştırılarak VLC'ye iletiliyor; yayınlar 0.3 saniyede gecikmesiz açılıyor.
- **🎨 Aktif Oynatıcı Göstergesi:** `🌐 Web` ve `🎬 VLC` butonları, o anda hangi motorun oynatıldığını parlayan renkli vurgularla anında gösterir.
- **🧹 Sadeleştirilmiş Ayarlar:** Kullanıcı deneyimini bozan varsayılan oynatıcı ve kalite ayarları Ayarlar sekmesinden kaldırılarak oynatıcıya entegre edildi.

### 🟢 v1.2.0 (27 Ekim 2026)
- **🎬 Pencere İçi VLC Desteği:** WinForms HWND entegrasyonuyla harici pencere açmadan uygulama içinde VLC desteği.
- **💬 Split-View Yan Sohbet Paneli:** Ana pencerenin sağ tarafında açılıp kapanan yerleşik Kick sohbet çekmecesi.
- **⊞ 2x2 Çoklu Yayın (Squad View):** 4 canlı yayını aynı anda izleme stüdyosu.
- **😀 Kick Emote Çizimi:** Kick sohbetinde özel emote'ların görsel olarak çizilmesi.
- **🔔 Gelişmiş Bildirimler:** Kategori değişikliği ve anahtar kelime alarmları.
- **🛡 Tekil Çalışma (Single Instance):** Çift açılmayı engelleme ve temiz arka plan kapanışı.

---

## 📄 Lisans

Bu proje [MIT Lisansı](./LICENSE) altında lisanslanmıştır.
