# Sükûn — pause + û

İki ayrı, yuvarlak uçlu dikey bar pause işaretini; üstteki küçük şapka Sükûn’un û harfini çağrıştırır. Hilal, gradient, gölge ve efekt kullanılmaz.

## Dosyalar

- `app.ico`: EXE, kurulum ve pencere ikonu. 16/20/24/32/40/48/64/128/256 px, 32 bit alfa kanallı PNG kareleri.
- `app-512.png`, `app-256.png`, `app-64.png`, `app-32.png`, `app-16.png`: adaçayı zeminli ana logo. Ara Windows ölçekleri için 20/24/40/48/128 px de bulunur.
- `sukun-tray-dark.ico`: koyu görev çubuğu için kırık beyaz, şeffaf zemin.
- `sukun-tray-light.ico`: açık görev çubuğu için koyu, şeffaf zemin.
- `sukun-tray-{dark,light}-16.png` / `-32.png`: küçük tray görselleri; diğer DPI boyutları da dahildir.
- `sukun-mark.svg`: şeffaf, adaçayı işaret. `sukun-icon.svg`: ana renkli ikon.
- `sukun-light.svg`, `sukun-dark.svg` ve 256 px PNG karşılıkları: branding varyasyonları.

Ana sürüm referanstaki yumuşak, yuvarlatılmış formdur. Tray’de 16–32 px için barlar genişletilir, boşluk korunur ve çizgiler piksele hizalanır; şapka pause barlarına birleşmez. İki bar hiçbir varyasyonda alttan birleştirilmez.

Palet: adaçayı `#5E8B7E`, kırık beyaz `#F6F5F1`; koyu tema vurgu `#7FB09B`. Tek renkli tray seçimi Windows görev çubuğu temasını izler.

`scripts/generate-brand.ps1` tüm PNG ve ICO dosyalarını Windows WPF vektör çizimi ile yerel olarak yeniden üretir. Raster üretimi sadece geliştirme zamanındadır; uygulama gömülü ICO dosyalarını yükler, yeni bağımlılık yoktur. SVG kaynaklar ve WPF `Resources/Brand.xaml` aynı ana işareti taşır.
