# Insch incelemesi — 18 Ekim 2026

Kaynak proje: `C:/dev/projects/riyaduz_dhikr` (salt okunur incelendi, değiştirilmedi).

İncelenen parçalar:
- `scripts/lib/diyanet_public_timetable.mjs`
- `scripts/refresh_diyanet_timetables.mjs`
- `scripts/import_diyanet_places.mjs`
- `docs/audits/diyanet-source-audit-2026-10-10.md`
- `docs/audits/diyanet-daily-refresh-2026-10-14.md`
- `assets/data/diyanet_turkey_index.json` (yalnızca kamuya açık katalog alanları)

Bulgular:
1. Diyanet'in kamuya açık Türkçe sayfalarının `<table>` / tarihli `<tr>` / 8 hücreli satırları okunuyor. Miladi tarih, Hicri tarih, 6 saat. Aynı gün haftalık/aylık/yıllık tablolarda tekrarlanabiliyor; değerler tutarlı olmalı.
2. İlçe kaydı `var ilceId` ile doğrulanabiliyor. Katalogdaki eski bir kimliğin farklı yere yönlenmesi mümkün; sessizce kabul edilmemeli.
3. Insch'in güncel yapısı yalnızca uygulamanın doğrudan siteden okumasından ibaret değil; geliştirici tarafında yenileme ve Cloudflare/GitHub üzerinden veri dağıtımı da var. Bu bağımlılığı yeni masaüstü uygulamasına taşımıyoruz.
4. 81 ilde 869 ilçe/merkez kaydı var. İl merkezi tablosunu ilin bütün ilçelerine uygulamak doğru değil. Önceki incelemede 10 Ekim imsak Kocaeli 05:35, Gebze 05:37 örneği kayıtlı.
5. İleri son tarih, aradaki bütün günlerin mevcut olduğu anlamına gelmiyor. 2026 sonundaki boşluklar önceki auditlerde açıkça belgelenmiş.
6. Yeni uygulamanın 18 Ekim'deki canlı Ankara denemesi: 396 tarih, son tarih **2027-12-31**. Bu deneme 2028 kapsamını doğrulamadı; başka uçların veya yerlerin kapsamı farklı olabilir. Uygulama yıl varsayımı yapmıyor.

Yeni ürün kendi C# okuyucusuyla, seçilen tek ilçeyi doğrudan resmî hosttan indirir. Insch kodu, private key'leri, API kimlik bilgileri ve Cloudflare yapılandırması alınmadı. Yer kataloğundan koordinatlar çıkarılarak yalnızca kamuya açık ad ve kimlikler kullanıldı.
