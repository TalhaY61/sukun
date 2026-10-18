<p align="center"><img src="site/assets/logo.svg" width="72" alt="Sükûn logosu"></p>

# Sükûn

**Vakti gelince, küçük bir ara.** Namaz vakitlerinde müziği veya videoyu duraklatan, ardından belirlediğiniz sürede devam ettiren küçük bir Windows uygulaması.

**A little pause when it’s time.** A small Windows tray app that pauses media at prayer time and resumes it after your chosen interval.

**[İndir / Download](https://talhay61.github.io/sukun/)** · [GitHub Releases](https://github.com/TalhaY61/sukun/releases) · [Hata bildir / Report an issue](https://github.com/TalhaY61/sukun/issues/new/choose)

> **v0.1.0-beta.4 — Test sürümü / Test release.** Henüz kararlı sürüm değildir. / Not a stable release yet.

| Türkçe · Açık | English · Light |
| :---: | :---: |
| <img src="site/assets/sukun_light_tr.png" width="320" alt="Sükûn Türkçe açık tema"> | <img src="site/assets/sukun_light_en.png" width="320" alt="Sükûn English light theme"> |
| **Türkçe · Koyu** | **English · Dark** |
| <img src="site/assets/sukun_dark_tr.png" width="320" alt="Sükûn Türkçe koyu tema"> | <img src="site/assets/sukun_dark_en.png" width="320" alt="Sükûn English dark theme"> |

## Başlangıç · Getting started

1. İndirme sayfasından **Windows x64 EXE** dosyasını indirin. Kalıcı bir klasöre koyup açın; **.NET kurulumu gerekmez**. ZIP seçerseniz önce tümünü çıkartın.
2. İl/vakit bölgenizi seçin. Diyanet’ten indirilen geçerli tarihler çevrimdışı kullanılabilir; her idari ilçe ayrı bir vakit bölgesi değildir.
3. Medya kaynağını, vakitleri ve mola süresini seçip **Otomatik duraklatma** anahtarını açın. Otomatik seçim yalnızca tek kaynak çalıyorsa çalışır.
4. X düğmesi tepsiye gizler. Tepsi ikonuna tıklayarak açın; tamamen kapatmak için **Çıkış**’ı kullanın. TR/EN ve tema tercihleri kaydedilir.

Download the Windows x64 EXE, keep it in a permanent folder and open it—no .NET installation needed. Choose your Turkish location, media source and prayer times, then enable **Auto pause**. Saved prayer dates work offline. Close hides the app in the tray; **Quit** exits. Switch between English and Turkish without restarting.

**Windows 10 (2004 / build 19041 veya sonrası) ve Windows 11, x64.** Windows 8 hedeflenmez. The beta is unsigned; Windows may warn or block it. The published EXE was blocked by Application Control on the development machine; final-package launch remains unverified there. Player compatibility varies: a browser must expose a Windows media session. Sükûn resumes only the session it successfully paused; it never starts already-paused media.

## Güncellemeler · Updates

Gelişmiş ayarlarda **Yeni uygulama sürümlerini denetle** seçeneğini açarsanız GitHub günde bir kontrol edilir. Beta sürümleri yeni betaları da görür; kararlı sürümler kararlı kanalda kalır. Yeni sürüm için bir kez tepsi bildirimi gösterilir (Windows bildirim ayarlarına bağlıdır). Güncelle’ye tıklayıp onaylayınca yayın sayfası açılır. Otomatik indirme, kurulum veya yeniden başlatma yoktur. Müzik yayını kesilmez.

Enable **Check for app updates** in Advanced settings for daily checks. Beta builds include newer betas; stable builds stay on the stable channel. A one-time tray notice depends on Windows notification settings. Update asks before opening the release page; it never installs or restarts automatically.

Spor salonu, kafe veya etkinlikte kullanmadan önce doğru oynatıcıyı seçip kısa bir gerçek deneme yapın. Otomatik seçim birden fazla kaynak çalıyorsa tahmin yapmaz. PC uyurken/kapalıyken çalışmaz; Windows medya oturumu sunmayan DJ/oynatıcı uygulamalarını kontrol edemez. Bu beta, gözetimsiz işletme kullanımı için henüz doğrulanmamıştır.

Before venue use, choose the intended player and run a real trial. Automatic selection does not guess when several sources are playing. The PC must be awake and the player must expose a Windows media session. Unattended venue operation has not been validated for this beta.

## Gizlilik ve lisans · Privacy & license

Ücretsiz, reklamsız ve açık kaynak. Hesap, telemetri ve otomatik hata gönderimi yoktur. Ayarlar ve sınırlı tanılama günlükleri cihazınızda kalır. Vakit indirme Diyanet’e, isteğe bağlı uygulama sürüm kontrolü GitHub’a bağlanır. **[Gizlilik / Privacy](PRIVACY.md)** · **[MIT lisansı / License](LICENSE)** · [Üçüncü taraf bildirimleri](THIRD_PARTY_NOTICES.md).

Free, ad-free and open source. No accounts, telemetry or automatic crash uploads. Settings and bounded diagnostic logs stay on your device. Prayer downloads contact Diyanet; optional update checks contact GitHub.

## Geliştirme ve katkı · Development & contributing

Windows + **.NET 10 SDK**. WPF/C#, ek UI framework’ü yok.

```powershell
dotnet build Sukun.sln -c Release
dotnet run --project tests/Sukun.Tests -c Release
dotnet run --project src/Sukun.Windows -c Release
./scripts/publish.ps1
```

Yayın betiği `artifacts/Sukun-win-x64.exe`, ZIP ve `SHA256SUMS.txt` üretir. Trimming/AOT kapalıdır. Log veya kişisel ayarları commit etmeyin. Hata raporu, küçük PR veya özellik önerileri hoş karşılanır. / Bug reports, focused pull requests and suggestions are welcome. See [contributing](CONTRIBUTING.md) and [beta verification](docs/BETA-VERIFICATION.md).

Geliştiren / Made by [Talha · @justtalha_](https://x.com/justtalha_).

## Code signing policy

İmzalama hazırlığı ve yayın süreci / Signing preparation and release process: [Code signing policy](docs/CODE_SIGNING.md). Mevcut beta henüz imzalı değildir. / The current beta is not yet signed.
