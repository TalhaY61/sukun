# Gizlilik

Sükûn hesap, reklam, analitik, telemetri veya otomatik hata raporu içermez. Bizim sunucumuz yoktur. Oynatma geçmişi tutulmaz. Mikrofon, konum, kamera ve yönetici yetkisi istenmez.

## Yerel kayıt

`%LOCALAPPDATA%\Sukun` altında:
- İlçe kimliği, otomasyon tercihleri, seçilmiş oynatıcı uygulama kimliği ve varsa geçici oturum kimliği, son vakit indirme girişimi.
- Kullanıcının seçtiği ilçenin doğrulanmış vakit tablosu.
- `logs/diagnostics.log` ve `diagnostics.previous.log`: UTC/yerel saat ve saat dilimi, seçili yer kimliği ve Diyanet kaynağı, otomasyon durumu ve sonraki başlangıç, kısaltılmış rastgele oturum kimliği, bilinen uygulama sınıfı (Chrome/Spotify/diğer), oynatma durumu, komut kabulü/doğrulaması, hata türü ve Windows hata kodu. Her dosya yaklaşık 256 KB'da döner; eski dosya değiştirilir. Medya başlıkları/sanatçılar, uygulama yolları, kullanıcı adı ve token kaydedilmez. Günlükler ağ üzerinden gönderilmez; günlük klasörü uygulama kapalıyken silinebilir.

Geçici şarkı/oynatıcı bilgisi Windows medya arayüzünden RAM'de okunur; doğru oturumu yeniden başlatmak için kullanılır, diske yazılmaz ve ağa gönderilmez. Bellek veya işletim sistemi çökme dökümleri üzerinde mutlak gizlilik iddiası yoktur.

Windows ile başlat seçeneği açılırsa yalnızca kullanıcının `HKCU\Software\Microsoft\Windows\CurrentVersion\Run\Sukun` değeri değiştirilir. Yönetici izni gerekmez.

## Ağ

Vakitler `https://namazvakitleri.diyanet.gov.tr` üzerinden seçilmiş ilçenin resmî sayfasından indirilir. Vakit indirmede başka hosta yönlendirme reddedilir. Karşı sunucu IP adresini, istenen ilçe kimliğini ve standart HTTP bağlantı bilgilerini görebilir. Bu bilgiler geliştiriciye gönderilmez.

Otomatik güncellemeyi kapatmak otomatik istekleri durdurur. “Yenile” ve yeni konum seçildiğinde eksik vakitlerin indirilmesi kullanıcının başlattığı ağ istekleridir. Kayıtlı geçerli günler internet olmadan çalışır. Uygulama sürüm kontrolü ayrı bir tercihtir: varsayılan olarak kapalıdır. “Yeni uygulama sürümlerini denetle” açılırsa en fazla günde bir kez `api.github.com/repos/TalhaY61/sukun/releases/latest` okunur. “Güncelle” düğmesi kullanıcı başlatımlı aynı kontrolü yapar. Hesap, token, cihaz kimliği veya oynatıcı bilgisi gönderilmez; GitHub normal HTTP bağlantı bilgilerini ve IP adresini görebilir. User-Agent sabit `Sukun` değeridir. Kontrol otomatik dosya indirme veya kurulum yapmaz.

GitHub düğmesi kaynak depoyu, bulunan yeni sürümün düğmesi doğrulanmış GitHub yayın sayfasını varsayılan tarayıcıda açar. Tarayıcının kendi oturum/çerez tercihleri geçerlidir. Yerel kayıtta son sürüm kontrolü girişim zamanı da bulunur.

Geliştirme için kullanılan .NET SDK ve GitHub Actions ürünün ağ davranışından ayrıdır.

## Kaynak adları ve geri bildirim

YouTube Music adını doğrulamak için, varsa kullanıcının ve ortak Start menüsünün `YouTube Music.lnk` kaydındaki `System.AppUserModel.ID` bir kez yerel olarak okunur. Kısayol çalıştırılmaz/değiştirilmez; tarayıcı geçmişi, çerezleri ve profil içeriği okunmaz. Kimlik eşleşmezse uygulama Chrome/Edge adıyla kalır. Bu kimlikler ağa gönderilmez.

“Hata bildir / öneri gönder” yalnızca GitHub Issues formunu varsayılan tarayıcıda açar; seçili konum, medya başlığı, günlük, cihaz bilgisi veya e-posta adresi URL'ye eklenmez ve otomatik bildirim gönderilmez. Gönderme işlemi kullanıcıya aittir; GitHub bildirimi herkese açık olabilir.

## English summary and website

No account, ads, analytics, telemetry or automatic crash uploads. Settings, cached prayer times and bounded logs stay in `%LOCALAPPDATA%/Sukun`. Track titles and history are not logged or sent. Prayer downloads contact Diyanet; optional app update checks contact GitHub. These providers can see normal connection details. No backend is operated by this project.

The download website is hosted on GitHub Pages. It requests public release metadata from `api.github.com` to select a real Windows download. GitHub receives ordinary HTTP connection details; the site has no analytics, external fonts or tracking scripts. Only the website language preference is stored in browser localStorage. Opening GitHub links uses your browser’s own privacy settings.
