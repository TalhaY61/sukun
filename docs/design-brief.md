# Sükûn arayüz kararı

Küçük bir Windows tepsi aracında kişi müziğine ara verilip verilmeyeceğini ve hangi kaynağın denetleneceğini bir bakışta anlamalı. İmza: günün altı vakti tek sırada; yalnızca yaklaşan namaz yumuşak vurgulu, mola seçili olanlarda küçük tik. Büyük dashboard, dekoratif desen ve sürekli animasyon yok.

480×656 DIP pencere, çalışma alanına göre üst sınır. Durum, sonraki namaz/geri sayım, vakitler ve GitHub/Güncelle/Çıkış sabittir. Yalnızca ayarlar kayar; gelişmiş bölüm açılınca pencere büyümez. Geçerli konum kısa özet olur, Değiştir ile düzenlenir.

Kullanıcının istediği açık palet: F6F5F1 zemin, FFFFFF yüzey, F1F3EF ikincil yüzey, D9DED6 sınır, 1E2421 ana metin, 5E6962 yardımcı metin, 7F8A83 soluk metin; 5E8B7E vurgu, 4F786C etkileşim, E6F0EB vurgu zemini, A8C3B8 vurgu sınırı. Koyu karşılıklar: 161B19, 1D2421, 222A26, 313A35, F2F4F0, C5CDC7, 98A39C, 7FB09B, 6A9A86, 22342D, 456356. Yeşil seçili/aktif durumla sınırlıdır; genel hover nötrdür.

Segoe UI, 23 DIP marka, 20 DIP sıradaki namaz, 14 DIP vakit, 12 DIP kontroller, 11 DIP yardımcı metin. 34 DIP seçici ve en az 32 DIP liste satırı. Arama seçili kaynağı değiştirmez; seçim üzerine kaydedilir. Kapalı seçicinin tekerleği ayarı değiştirmez. Liste genişliği sahibi alanla, yüksekliği pencerenin müsait alanıyla sınırlıdır. Gerekiyorsa yukarı açılır; uzun metin üç nokta ve tooltip kullanır. Sağ kenarda 6 DIP scrollbar bulunur.

Ana otomasyon bir switch, başlangıç/veri seçenekleri tik kutusudur. Açık/Kapalı durumu gerçekten kullanılan Settings.Enabled değerinden çizilir. Vakitler hazır değilse açılmaz ve nedeni açıklanır. Tema tercihi otomasyon sahipliğini iptal etmez. Sistem teması Windows bildirimlerinde yenilenir; yüksek karşıtlık renkleri işletim sisteminden alınır.

Arayüz metinleri Resources/Strings.tr.xaml altında düzenlenmeye başlandı; dinamik hata/durum mesajları hâlâ Türkçedir. Mevcut uygulamada başka bir çeviri yoktu; tamamlanmış İngilizce/Almanca/Arapça desteği iddia edilmez. WPF native semantiği, klavye ve erişilebilir kontrol adları korunur. Per-monitor DPI bildirimi manifesttedir; WinForms yalnızca NotifyIcon içindir.
