# Yazılım güncelleme akışı

İlk uygulama: sürüm bildirimi ve güvenilir GitHub yayın sayfası; yerinde otomatik binary değiştirme yok.

1. Projedeki sürüm numarası arttırılır (`src/Sukun.Windows/Sukun.Windows.csproj`, ör. 0.2.0).
2. Test ve paketleme yapılır, yerel commit oluşturulur; push ve yayın ayrıca yetkilendirilir.
3. `v0.2.0` etiketiyle kararlı GitHub Release yayımlanır ve paketler eklenir. Tek başına tag/push uygulamada güncelleme bildirimi üretmez.
4. Uygulama public latest-release API'sini kimlik doğrulamasız okur. Repo/release özel veya erişilemiyorsa kontrol hata durumunda kalır; “güncel” demez.
5. SemVer'in üç sayılı kararlı etiketleri karşılaştırılır; 0.10.0, 0.2.0'dan yenidir. Assembly revision alanı karşılaştırmayı bozmaz. Beta/draft ve geçersiz URL reddedilir.
6. Yeni sürümde düğme “Yeni sürüm” olur; kullanıcı tıklayınca o release açılır. Kullanıcıya görünmeden indirme/kurulum yapılmaz.

Otomatik kontrol opt-in ve 24 saatte en fazla bir girişimdir; son girişim diskte tutulur. Manuel kontrol ayrıca yapılabilir. Ağ kontrolü ses komutlarını/otomasyon sahipliğini değiştirmez.

Tam otomatik güncelleme sonraki ayrı iştir: doğrulanmış dağıtım anahtarıyla imzalanmış güncelleme manifesti/paket, platform/kanal seçimi, indirme bütünlüğü, uygulamanın güvenli kapanması, eski dosyayı kilit dışından atomik değiştirme ve başarısızlıkta geri dönüş gerekir. Sadece HTTPS ya da aynı release'ten indirilen bir SHA dosyası yayıncı imzasının yerine geçmez. Çalışan exe doğrudan kendi üzerine yazılmaz. Mevcut Windows Application Control engeli de kod imzalama/dağıtım ile ele alınmalıdır; güvenlik ayarı kapatılmaz.

Kurulumlu sürümde aynı Inno Setup AppId yeni sürümü mevcut kurulumun üzerine kurar; `%LOCALAPPDATA%/Sukun` tercihleri uninstall edilmez. Installer henüz gerçek yükseltme senaryosunda doğrulanmış değildir. Taşınabilir sürümde program kapatılıp yeni exe aynı dizine konur; tercih dosyaları zaten ayrı yerde kalır.
