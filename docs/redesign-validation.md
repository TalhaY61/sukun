# Sükûn doğrulama — 18 Ekim 2026

Kaynak: `C:/dev/projects/sukun`, mevcut WPF/C# projesi. Yeni ürün, web arayüzü, ağır UI paketi veya telemetri eklenmedi.

## Kök nedenler ve düzeltmeler

- Eski motor oynarken hem CanPause hem CanPlay arıyordu. Gerçek Chrome/YouTube Music oturumu Playing/Pause=true/Play=false bildiriyor; bu normal durum eski koşulda atlanıyordu. Açık Pause/Play isteği, API kabulü ve son gerçek durum artık ayrı doğrulanır. Toggle medya tuşu gönderilmez.
- Önceden işlenmiş vakit anahtarları saat geri alınsa da tutuluyordu. Saat/güç bildiriminde ve duvar/monoton saat farkında zaman çizelgesi yeniden kurulur. Gelecekte kalan başlangıç yeniden denenebilir; mevcut/geçmiş aralık atlanır. Aynı geçişte yinelenen komut verilmez.
- Oturum listesi uygulama tercihini yalnızca bazı seçimlerde ekliyordu; ayrıca kaybolan oturumdan sessizce uygulamaya dönüyordu. Kalıcı uygulama satırı, ayrı oturum anahtarı, kapalı seçimin görünür tutulması ve COM kimliğine göre oturum takibi eklendi.
- Durum da bütün pencereyle kayıyordu. Başlık, otomasyon, sıradaki namaz, vakitler ve alt eylemler sabitlendi; yalnızca ayarlar kayar.
- Ek native incelemede %150 DPI callback piksel ölçüleri ile pencere DIP ölçülerinin karıştırıldığı, klavyenin filtre öncesi vurguyu kullanabildiği ve scrollbar temasının 17 DIP minimum genişlik dayattığı bulundu. Birim dönüşümü, filtrelenmiş Items üzerinden ok/Enter seçimi, 6 DIP scrollbar ve piksel tabanlı sanallaştırılmış kaydırma ile girişin örtülmesi/yanlış seçim/son boşluk düzeltildi.
- Eski Popup yalnızca ekran kenarlarını gözetiyor, uzun medya metni genişliği büyütebiliyordu. Artık sahip pencerenin müsait alanına göre genişlik/yükseklik ve yukarı/aşağı konumu hesaplanır. Kapalı seçicide tekerlek ayarı değiştirmez.

## Otomatik doğrulama

- .NET 10 SDK ile Debug ve Release derlemeleri: 0 hata, 0 uyarı.
- Mevcut bağımlılıksız ayrı test projesi korundu; xUnit paketi eklemek gerekmedi.
- **67/67 deterministik test** başarılı. Saat geri/ileri sıçrama, UTC+3/gece yarısı, seçilmeyen vakit, önce/sonra süreleri, devre dışı otomasyon, başlangıç ortasında açılış, yinelenen olay, aynı tarayıcıdaki iki oturum, zaten durmuş medya, kapanan oturum, başarısız ve doğrulanmamış komut, kullanıcı/parça müdahalesi, arama, kalıcı seçim ve atomik kayıt kapsanır.
- Ayrı canlı Diyanet testiyle **68/68** başarılı. Ankara sayfası bu denemede 396 tarih ve son tarih 2027-12-31 döndürdü. Son tarihin ileri olması bütün aradaki günlerin dolu olduğu varsayımını oluşturmaz.
- Test saati gerçek Windows saatini değiştirmez; TimeProvider tabanlı sahte duvar/monoton saat kullanılır. Medya denemelerinde sahte IMediaControl oturumları bulunur.
- Yerel günlükte medya başlığı/yol bulunmadığı ve iki dosyayla sınırlı dönüşüm otomatik doğrulandı.

## Gerçek Windows denemesi

Bu derlemede önceki Application Control engeli tekrarlanmadı; güvenlik ayarı değiştirilmedi.

- Açık ve koyu WPF pencere, sabit başlık/vakitler, kaydırılan gelişmiş ayarlar ve tik kutuları görüldü.
- Medya Popup'ının alan yetmediğinde yukarı açılıp pencere sınırlarında kaldığı görüldü. Medya araması canlı filtrelendi; Escape önceki seçimi korudu. Son açık/koyu ön izlemede süre listesi sınır içinde yukarı açıldı ve son elemandan sonraki gereksiz boşluk giderildi; kapalı süre seçicisinde tekerlek değeri değiştirmedi. Arama yer tutucusu çakışması bu incelemede bulunup düzeltildi.
- Chrome tabanlı **YouTube Music** gerçek oturumunda 10 saniye bekleme → doğrulanmış pause → 10 saniye bekleme → doğrulanmış play başarılı. Yanındaki zaten duraklatılmış başka Chrome oturumu başlatılmadı.
- İkinci denemede X ile pencere gizlendi; işlem devam etti ve aynı pause/play çevrimi yerel günlükte doğrulandı. Bu test gerçek namaz saatini bekleyerek yapılmadı; ana motoru kullanan gelişmiş test ile yapıldı.
- Gerçek günlükte pauseEnabled=true/playEnabled=false ve komut sonrası Paused→Playing görülmesi kök neden teşhisini destekliyor. Şarkı adı rapora/günlüğe alınmadı.
- Kısa performans örneği: görünmez pencerede 30 saniyede CPU artışı 0.00 saniye ölçüldü. Görsel/erişilebilirlik incelemesi sonrası working set yaklaşık 242 MB, private bellek 131 MB; sonraki temiz açılışta yaklaşık 197/110 MB idi. Bunlar tek makine/anlık örneklerdir, bellek üst sınırı veya uzun süreli sızıntı testi değildir. Küçük paket boyutu düşük RAM garantisi sayılmaz.

## Tamamlanmamış fiziksel denemeler

Gerçek Spotify/Edge/Firefox/VLC, bütün oynatıcı ve reklam/canlı yayın varyasyonları, gerçek uyku/uyanma, Windows tema ayarını dışarıdan değiştirme, %100/%125 DPI ekranları ve çoklu monitör geçişleri fiziksel olarak denenmedi. Native tepsi ikonuna sol tıkla aç/gizle yolu kodda uygulanmıştır; gerçek test X ile gizlenme ve gizliyken motorun çalışmasıyla sınırlıdır. Mevcut %150 DPI ekranda gerçek popup yerleşimi incelendi; %100/%125/%150 birim dönüşümü ayrıca regresyon testiyle kapsanır. İl/ilçe aramasının bütün katalog kombinasyonları native olarak tek tek gezilmedi; Türkçe eşleştirme/katalog/veri doğrulaması otomatik testlidir. Native ön izlemede ista → İstanbul, aşağı ok/Enter ve otomatik İstanbul merkez seçimi de doğrulandı. Installer kurulumu/kaldırması, imzalama ve uzun süreli RAM profili bu çalışmanın doğrulanmış çıktısı değildir.

Yeni arayüz kaynakları Resources/Strings.tr.xaml altında; diğer diller için zemin hazır, çeviriler henüz yok. İşletim sistemi her ses üreten sayfayı medya oturumu olarak sunmaz; genel Chrome sesini otomatik duraklatma garantisi verilmez.

## Takip düzenlemesi: seçim, medya adları ve tek uygulama

- Kartların yaklaşan vakit vurgusu seçim rengiyle karışıyordu. Yalnızca seçili kartlar renkli zemine sahiptir; boş kutu/Mola yok ve tik/Mola görünür. Yaklaşan kart ayrı Sıradaki yazısını kullanır. Seçili olmayan yaklaşan Akşam kartı gerçek koyu WPF pencerede kontrol edildi.
- Kaynaklar uygulama+oturum olarak iki kez listeleniyordu. Normal tek oturumlu kaynak başına tek kalıcı satır var; bir Windows kimliği altında çoklu oturum varsa ayrılır. Kapanmış seçim menüye satır eklemez, başka hedefe geçmez. Kısa ad ve alt satırdaki durum/başlık gerçek WPF Popup'ta kontrol edildi.
- Bu PC'deki YouTube Music kısayolunun gerçek Windows AUMID değeri, Chrome'un 32 karakterli --app-id değerinden farklı/kısaltılmıştı. Tam kayıtlı AUMID ile eşleştirme yapıldı ve gerçek listede YT Music görüldü; başlığa bakarak site tahmini yapılmıyor.
- Preview için ayrı kilit kaldırıldı. Normal/Debug/preview/taşınabilir sürümler Local\Sukun.Desktop kilidini paylaşır. İkinci süreç aktivasyon sinyali verip çıkar. Gerçek Release açıkken Debug --smoke başlatıldı; süreç sayısı 1 kaldı ve mevcut pencere öne geldi. Kilit/aktivasyon/yeniden açılma ayrıca Windows testleriyle doğrulandı.
- Son test projesi **74/74** başarılı; Release derlemesi 0 hata/uyarı. Bu tur medya pause/play motoru değiştirilmedi, gerçek Spotify/Media Player pause/play testi yapılmış sayılmaz; yalnızca listede adları/durumları görüldü.
- Geri bildirim, GitHub'da kullanıcının kendisinin dolduracağı forma gider. Form gönderme veya otomatik günlük paylaşımı test edilmedi/eklenmedi. Yerel issue şablonlarının GitHub üzerinde etkinleşmesi push sonrasıdır.
