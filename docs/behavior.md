# Davranış ve kabul koşulları

## İlk sürüm sınırı

Windows 10 2004+ / Windows 11, Türkiye, Diyanet'in yayımladığı ilçe/merkez kayıtları ve Windows medya oturumu sunan oynatıcılar. Uygulama ezan çalmaz, mikrofon kullanmaz, cihazın sesini topluca kapatmaz, Spotify Web API kullanmaz. İlk sürüm ön izlemedir; bütün oynatıcılar ve PC yapılandırmaları için uyumluluk iddiası yoktur.

## Kontrol kullanıcıda

| Durum | Beklenen davranış |
|---|---|
| İlk açılış | Konum yok; otomasyon kapalı. İl seçilince resmî merkez, yoksa o ilin sıralı listesindeki ilk bölge seçilir. |
| Konum değişimi | Otomasyon kapanır; eski ilçenin tablosu uygulanmaz. |
| Bir uyumlu oturum çalıyor | Otomatik mod bu oturumu hedefler. |
| Birden fazla oturum çalıyor | Mola atlanır; kullanıcıya oynatıcı tercihi önerilir. |
| Belirli uygulama tercih edilmiş | Yalnızca bu uygulama; yoksa başka uygulamaya geçilmez. Aynı uygulamada iki çalan oturum da belirsiz sayılır. |
| Oynatma zaten durmuş | Duraklatma ve sonradan başlatma yok. |
| Mola başlıyor | Yalnızca desteklenen duraklat komutu; toggle tuşu yok; başarılı sonuç ve paused durumu doğrulanır. |
| Mola sürerken kullanıcı tekrar oynatır | Tekrar durdurulmaz; otomatik devam hakkı bırakılır. |
| Parça, zaman çizgisi veya oturum değişir | Tespit edilen değişiklikte otomatik devam iptal edilir. |
| Kullanıcı devamı iptal eder / ayar değiştirir / otomasyonu kapatır | Hemen kontrol bırakılır; müzik otomatik açılmaz. |
| Otomatik devam zamanı | Aynı oturum, aynı medya bilgisi, aynı değişiklik sürümü ve paused durumu kontrol edilir; yalnızca bir play isteği. |
| Oynatıcı geç/başarısız yanıt verir | Sonradan tekrar tekrar komut yollanmaz. Duraklatma doğrulanmazsa otomatik devam yok. |
| PC uyur, saat değişir veya otomasyon gecikir | Aktif sahiplik bırakılır; geç gelen mola başlatılmaz. Saat geri alınınca gelecekte kalan işlenmiş başlangıçlar yeni zaman geçişi için yeniden kurulur; aynı geçişte tek tetikleme korunur. |
| Mola sırasında otomasyon açılır / süre veya kaynak değiştirilir | Geçmiş başlangıç işaretlenir; mevcut aralık geriye dönük çalıştırılmaz. |
| Medya kimliği yok | Açık pause isteği doğrulanırsa durur; aynı içeriği doğrulamak mümkün olmadığından otomatik play verilmez. |
| Belirli oturum kapalı | Seçim görünür kalır; başka oturuma/uygulamaya geçilmez. |
| Yetenek bayrakları | Chrome oynatırken CanPlay=false olabilir. Bu değer duraklatmayı engellemez; açık komut ve gerçek durum esas alınır. |
| X / tepsi | X gizler, otomasyon sürer; tepsiye sol tık pencereyi açar/gizler. Çıkış tamamen kapatır. |
| Uygulama mola ortasında açılır | Geçmiş başlangıç atlanır. Başka bir uygulamanın durdurduğu müzik açılmaz. |
| Çakışan molalar | Tek sürekli mola; arada müzik açılmaz. |
| Sistem sesi / görüşme / bildirim | Toplu ses kapatma yok. Kullanıcının seçtiği medya oturumu kapsamı geçerli. |
| Spotify Connect / TV / başka telefon müzik çalıyor | Ayrı cihaz desteği vaat edilmez; yerel oturum şarttır. |
| Canlı yayın / kaynağı kapalı oturum | Oynatıcının pause/play davranışına bağlıdır; kaldığı saniyeden devam garantisi yok. |
| Kullanıcı uykudan sonra devam etmek istiyor | Kendi oynatıcısından devam eder. Uygulama sessizlik sonrası sürpriz yapmaz. |

Windows, her kullanıcı niyetini bildirmez: zaten paused olan oynatıcıda tekrar pause verilmesi olay üretmeyebilir. Parça bilgileri eşit iki farklı içerik veya sağlayıcının bildirmediği değişiklik de ayırt edilemeyebilir. “Otomatik devamı iptal et” kesin kullanıcı kontrolüdür. Değişiklik olayı gürültülü bir oynatıcıda muhafazakâr davranıp otomatik devamı iptal edebiliriz; bu yüzden gerçek oynatıcı testi yayın kabul koşuludur.

## Vakitler

- Türkiye sivil saati UTC+3; PC'nin Almanya saat diliminde olması takvimi kaydırmaz.
- Aynı ilin bütün ilçeleri eşit sayılmaz. İlçe seçimi korunur, farklı ilçe kimliği döndüren sayfa reddedilir.
- Güneş için ezan molası yok. İmsak opt-in; sabah ezanı ile birebir aynı olduğu iddia edilmez.
- Varsayılan: öğle/ikindi/akşam/yatsı, 30 sn önce duraklat, vakitten 7 dk sonra devam.
- Ezan bitişi algılanmaz; zaman aralığı kullanılır.
- Başarılı indirme için bugünden en az 14 ardışık gün; altı saatin geçerliliği, sırası, gerçek tarih, yinelenen satır tutarlılığı kontrol edilir.
- Son tarihi ileri diye aradaki eksik günler dolu sayılmaz. Takvim yoksa hesaplama uydurulmaz.
- Güncelleme: 30 gün geçince veya ardışık kapsam 14 günün altına inince; otomatik ağ girişimi yer başına günde en çok bir kez. Son girişim yerelde saklanır. Kullanıcı “Yenile” ile elle deneyebilir.
- Eski geçerli veri, hata veya yapısal değişiklikte korunur; yeni günler doğrulandıktan sonra dosya atomik değiştirilir.
- Gelecekte yayımlanmış bütün satırlar saklanır; kaç yıl bulunduğu kaynağa bağlıdır.

## Yayından önce gerçek doğrulama

- Spotify masaüstü ve Chrome/Edge YouTube: pause, doğal sona eren mola, manuel resume/pause, parça değiştirme, sekme kapatma/açma.
- İki oynatıcı, aynı tarayıcıda iki medya oturumu, yayın ve reklam geçişi.
- Uyku/uyanma, kilit açma, Türkiye dışı PC saat dilimi, saat değişimi, gece yarısı ve güncel günün veri boşluğu.
- İlk kurulum, bozuk ayar/takvim, internet kesintisi, kaynağın farklı yere yönlendirmesi.
- %100/%150/%200 DPI, klavye, tepsi davranışı, ikinci örnek, autostart opt-in.
- Gizliyken ve görünürken CPU/working-set ölçümü; dosya boyutu RAM ölçüsü değildir.
- İmzalı kurulum / temiz Windows üzerinde kaldırma denemesi. Mevcut ön izleme bu şartların hepsini geçtiği anlamına gelmez.

## Geliştirici testi ve tanılama

Gelişmiş ayarlarda tek seferlik 10 saniye bekle → doğrulanmış pause → 10 saniye bekle → doğrulanmış play testi bulunur. Gerçek mola sürerken veya 35 saniye içinde çakışacaksa reddedilir. Test sırasında gerçek zamanlayıcı askıdadır; kaynak/konum/mola ayarı, saat/güç değişikliği veya iptal sahipliği bırakır. Başarısız komut tekrar denenmez. Ayarlar ve gerçek vakitler değişmez. Müzik zaten durmuşsa test onu açmaz.

Saat için host TimeProvider kullanır; testlerde duvar ve monoton saat ayrı ayrı ilerletilir. İşletim sistemi saati değiştirilmez. Yerel günlükler iki küçük dosyada döner, medya başlığı içermez. Günlük erişimi için ek paket veya telemetri eklenmez.
