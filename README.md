<div align="center">

# 🟢 LD KickWatch

**Kick.com için Modern Masaüstü Canlı Yayın İzleme Stüdyosu ve Sohbet İstemcisi**

[![Release](https://img.shields.io/badge/Release-v1.7.0-53FC18?style=for-the-badge&logo=github)](https://github.com/LilDosto/LD-KickWatch/releases/latest)
[![Platform](https://img.shields.io/badge/Platform-Windows%2010%20%2F%2011%20(x64)-0078D6?style=for-the-badge&logo=windows)](https://github.com/LilDosto/LD-KickWatch)
[![.NET](https://img.shields.io/badge/.NET-8.0%20WPF-512BD4?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![License](https://img.shields.io/badge/Lisans-MIT-53FC18?style=for-the-badge)](./LICENSE)

Favori **Kick.com** yayıncılarınızı takip edin, anlık canlı yayın ve kategori bildirimleri alın, **Pencere İçi Web Oynatıcı (WebView2)** veya **Pencere İçi Gömülü VLC** ile yayınları izleyin, **2x2 Çoklu Yayın (Squad View)** modunda 4 yayını aynı anda takip edin ve gerçek **Kick Emote** destekli canlı sohbete bağlanın.

[📦 **Son Sürümü İndir (v1.7.0 Releases)**](https://github.com/LilDosto/LD-KickWatch/releases/latest) • [🚀 **Özellikler**](#-başlıca-özellikler) • [⌨ **Kısayollar**](#-klavye-kısayolları) • [🛠 **Derleme**](#-kaynak-koddan-derleme)

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

### Seçenek 1: Resmi Windows Kurulum Sihirbazı (Önerilen)
1. Releases sayfasından **[`LD-KickWatch-Setup-v1.7.0.exe`](https://github.com/LilDosto/LD-KickWatch/releases/latest)** kurulum sihirbazını indirin.
2. Kurulumu çalıştırın:
   - Masaüstüne otomatik simgeli kısayol oluşturulur.
   - Başlat Menüsü'ne "LD KickWatch" eklenir.
   - Windows "Uygulamalar ve Özellikler" (Denetim Masası) listesine kaydedilir ve temiz kaldırma (Uninstaller) desteği sunar.
   - Yönetici yetkisi (UAC) gerektirmez; saniyeler içinde kurulur.

### Seçenek 2: Taşınabilir Tek Dosya (Portable)
1. [`publish/LDKickWatch.exe`](./publish/LDKickWatch.exe) veya `LD-KickWatch-v1.7.0-win-x64.zip` dosyasını indirin.
2. Doğrudan çift tıklayarak çalıştırın (`.NET Runtime` içine gömülüdür).
3. İsterseniz uygulama içi **Ayarlar (⚙)** menüsünden **"🖥️ Masaüstü Kısayolu Oluştur"** butonuna basarak tek tıkla masaüstünüze simgeli kısayol ekleyebilirsiniz.

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

### 🟢 v1.7.0 (29 Ekim 2026) - Resmi Windows Kurulum Sihirbazı & Masaüstü Kısayol Entegrasyonu
- **📦 Resmi Windows Setup (.exe) Kurulum Paketi:** LD KickWatch artık normal bir Windows programı gibi tek tıkla kurulabilir (`LD-KickWatch-Setup-v1.7.0.exe`).
  - Kurulum tamamlandığında Masaüstüne özel simgeli kısayol ekler.
  - Başlat Menüsü'ne "LD KickWatch" program kaydını ekler.
  - Denetim Masası / Windows Ayarları "Program Ekle veya Kaldır" listesine resmi kayıt açar ve sorunsuz kaldırıcı (Uninstaller) sunar.
  - Yönetici (Administrator) izni gerektirmeden kullanıcı profiline anında kurulur.
- **🖥️ Uygulama İçi Tek Tıkla Masaüstü Kısayolu Oluşturma:** Taşınabilir (Portable) sürümü kullananlar için Ayarlar (⚙) sekmesine *"Masaüstü Kısayolu Oluştur"* butonu eklendi. Tek tıkla masaüstüne uygulama simgesiyle tam çalışan `.lnk` kısayolu bırakır.
- **🛡 Gelişmiş Kurulum Koruması:** Kurulum veya güncelleme esnasında LD KickWatch açık ise otomatik algılayıp güvenli şekilde kapatır ve dosyaları sorunsuz günceller.

### 🟢 v1.6.0 (28 Ekim 2026) - Büyük Akışkanlık & Sıfır Donma Optimize Yaması
- **🚀 Donanımsal 60 FPS Render & Animasyon Motoru:** WPF alt motoruna 60 FPS hedef kare hızı (`DesiredFrameRate = 60`) ve tam donanım hızlandırmalı render optimizasyonu uygulandı.
- **✨ Kanal Listesi UI Sanallaştırması (Smooth 60 FPS Scrolling):** Takip edilen yayıncılar listesinde donanımsal `VirtualizingStackPanel` ve `Recycling` modu etkinleştirildi. 100+ kanal eklendiğinde dahi sıfır takılma ile kaydırma sağlandı.
- **🖼 Bellek İçi Dondurulmuş Avatar Önbelleği (In-Memory Avatar Cache):** Yayıncı profil fotoğrafları bellek içine alınıp thread-safe şekilde donduruldu (`Freeze()`). Liste kaydırılırken disk I/O ve görsel yeniden çizim maliyeti tamamen ortadan kaldırıldı.
- **⚡ Asenkron Emote Birleştirme (Single-Flight Emotes):** Yoğun sohbette aynı anda geçen popüler ifadeler (örn. KEKW, LUL) eş zamanlı olarak tek bir asenkron görevle indirilip önbelleğe alınır; CPU ve ağ yükü sıfıra indirildi.
- **🛡 UI Thread Koruma & Non-Blocking Polling:** Arka planda çalışan canlı yayın durum kontrolleri ve avatar yüklemeleri, UI iş parçacığını asla bloke etmeyecek şekilde `DispatcherPriority.Background` asenkron çağrılarına dönüştürüldü. Pencereler ve menüler her an yağ gibi akıcı kalır.

### 🟢 v1.5.0 (28 Ekim 2026)
- **⚡ Akıcı ve Kesintisiz Canlı Sohbet (Background Dispatcher & Batching):** Canlı sohbette mesajların birkaç saniye sonra tıkanması ve akmaması sorunu tamamen çözüldü. Gelen mesajlar non-blocking kuyruk (`ConcurrentQueue`) ve 75 ms toplu arka plan işleme (`DispatcherTimer`) ile UI'ya aktarılıyor; 100.000 izleyicili yayınlarda dahi sohbet su gibi akıyor.
- **🛡 UI Virtualization Entegrasyonu:** Sohbet kutusuna UI sanallaştırma ve geri dönüşüm (`Recycling`) eklendi, gereksiz DOM ölçümleri ve CPU darboğazı ortadan kaldırıldı.
- **🎯 Havada Kalan/Bozulan Kalite Menüsü Yerel Açılır Menüye (ContextMenu) Taşındı:** WinForms (`WindowsFormsHost` / `VlcPanel`) ve WebView2 üzerindeki WPF Airspace (saydam popup çakışması) nedeniyle oluşan donma ve bug'lar tamamen giderildi. Artık YouTube/Twitch tarzı tıklandığında anında açılan, seçili kaliteyi tik işaretiyle (✓) gösteren şık ve sıfır hatalı bir kalite menüsü bulunuyor.

### 🟢 v1.4.0 (28 Ekim 2026)
- **💬 Canlı Sohbet Bağlantı Sorunu Düzeltildi:** Kick'in yenilediği güncel Pusher WebSocket protokolü ve anahtarı entegre edildi. Artık tüm yayıncıların canlı sohbet akışları anında ve kesintisiz bağlanıyor.
- **🔍 Kalite Seçim Kutusu Büyütüldü ve Ferahlatıldı:** Oynatıcı çubuğundaki kalite kutucuğu genişletildi, yazıların dikey/yatay kırpılması ve sıkışıklık tamamen giderildi; okunabilir, şık ve ferah bir tasarıma kavuşturuldu.
- **🛑 Kesin Kapanış & Windows Job Object ile VLC Süreç Temizliği:** Uygulama kapatıldığında VLC veya alt süreçlerin arkada asılı kalması engellendi. Windows Job Object (`KILL_ON_JOB_CLOSE`) ile işletim sistemi seviyesinde tüm süreçler anında ve temiz şekilde sonlandırılır.
- **🛡 Çift Açılma İyileştirmesi:** İkinci örnek açılmaya çalışıldığında modal kutu bekletmeden mevcut açık pencereyi sessizce öne getirir ve yeni örneği anında sonlandırır.

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
