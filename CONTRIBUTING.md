# Contributing

[TR — Türkçe](#tr--türkçe) · [EN — English](#en--english)

## TR — Türkçe

### Hata ve öneriler

[GitHub Issues](https://github.com/TalhaY61/sukun/issues/new/choose) sayfasındaki hata veya özellik önerisi formunu kullanın. Kod bilmeniz gerekmez; bir GitHub hesabı yeterlidir. Türkçe veya İngilizce yazabilirsiniz.

Hata bildirirken uygulama sürümünü, Windows sürümünü, medya oynatıcısını ve sorunu nasıl tekrar oluşturduğunuzu belirtin. Ekleri paylaşmadan önce inceleyin: kimlik bilgileri, kişisel ayarlar veya özel içerikler göndermeyin. Bildirimler herkese açıktır.

### Kod katkısı

1. Projeyi **Fork** ile kendi hesabınıza kopyalayın ve ayrı bir dalda değişiklik yapın.
2. Mevcut WPF/C# yapısını, kompakt arayüzü ve düşük bağımlılık sayısını koruyun. Kullanıcı metinleri merkezi `.resx` dosyalarında olsun; iki dili de güncelleyin. Ayrıntılar: [yerelleştirme rehberi](src/Sukun.Core/Localization/README.md).
3. Windows üzerinde `dotnet build Sukun.sln -c Release` ve `dotnet run --project tests/Sukun.Tests -c Release` çalıştırın. Medya değişikliklerini gerçek oynatıcıyla da kontrol edin; uygulama kendisinin durdurmadığı medyayı başlatmamalıdır.
4. Değişikliği ve doğruladığınız davranışı açıklayan bir **Pull Request** açın. Test edemediğiniz noktaları belirtin. Öneriniz inceleme sonrasında birleştirilir.

EXE, günlük, önbellek, kişisel ayar ve kimlik bilgilerini Git’e eklemeyin. Web sitesi `site/` klasöründedir. Yayın betiği `scripts/publish.ps1`, dağıtım dosyalarını ve SHA256 sağlama toplamlarını üretir.

## EN — English

### Bugs and suggestions

Use the bug or feature form on [GitHub Issues](https://github.com/TalhaY61/sukun/issues/new/choose). No coding knowledge is needed; a GitHub account is enough. Reports in English or Turkish are welcome.

Include the app version, Windows version, media player and steps to reproduce a bug. Review attachments before sharing: leave out credentials, personal settings and private content. Reports are public.

### Code contributions

1. **Fork** the repository and make your changes on a separate branch in your copy.
2. Keep the existing WPF/C# structure, compact UI and low dependency count. Put user-facing text in the central `.resx` resources and update both languages. See the [localization guide](src/Sukun.Core/Localization/README.md).
3. On Windows, run `dotnet build Sukun.sln -c Release` and `dotnet run --project tests/Sukun.Tests -c Release`. Verify media changes with a real player too: the app must never start media it did not pause.
4. Open a **pull request** describing the change and what you verified. Mention anything you could not test. Changes are merged after review.

Do not commit binaries, logs, caches, personal settings or credentials. Website source lives in `site/`. The publish script `scripts/publish.ps1` creates distribution files and SHA256 checksums.
