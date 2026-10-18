# Kaynaklar

- Namaz vakitlerinin ve yer kimliklerinin kaynağı: Diyanet İşleri Başkanlığı, https://namazvakitleri.diyanet.gov.tr . Bu proje resmî Diyanet uygulaması değildir. MIT kod lisansı üçüncü taraf vakit verisini yeniden lisanslamaz.
- `Assets/places.json`: kullanıcının Insch projesindeki kamuya açık Diyanet yer kataloğundan yalnızca kimlik, il adı, resmî yer adı ve merkez niteliği çıkarıldı (18 Ekim 2026). GeoNames `displayName` tahmini kullanılmaz. Koordinat ve kullanıcı verisi alınmadı. Insch kodu kopyalanmadı. Katalog 81 ilde 869 kayıt içerir; bazı eski kaynak kimlikleri resmî sitede başka yere yönlenebilir. Uygulama kimlik uyuşmazlığını reddeder. İstanbul'un resmî açılır listesi Üsküdar ve Ümraniye'yi ayrı seçenekler olarak sunmaz; katalog idari ilçelerin eksiksiz listesi değildir.
- `tests/.../Fixtures/ankara.html`: 18 Ekim 2026 tarihinde https://namazvakitleri.diyanet.gov.tr/tr-TR/9206 adresinden alınan test örneği. Ürünle takvim verisi olarak dağıtılmaz.
- Windows medya kontrolü: Microsoft Windows API; üçüncü taraf müzik hesabı/API anahtarı gerekmez.
- G-Helper ekran görüntüsü yalnızca kullanıcının verdiği boyut/yoğunluk referansıdır. G-Helper kaynak kodu ve görselleri projeye alınmadı.

- Microsoft Fluent UI System Icons: yalnızca Code, Bug, Arrow Sync, Power, Checkmark, Chevron Down ve Chevron Right Regular SVG'leri; MIT lisansı. Kaynak: https://github.com/microsoft/fluentui-system-icons , sabit revizyon `a347af95478f989038cccbf085978865264c048a`. SVG'lerden WPF Geometry üretildi; çalışma anında ikon paketi veya ağ isteği yok. Tam lisans `THIRD_PARTY_NOTICES.md` dosyasındadır.
- Sükûn pause + û işareti bu proje için özgün vektör olarak hazırlandı. SVG ve yeniden üretim betiği kaynaklara dahildir; üçüncü taraf marka logosu kullanılmaz.
