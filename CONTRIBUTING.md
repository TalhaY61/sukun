# Contributing / Katkıda bulunma

Hata ve öneriler için [GitHub Issues](https://github.com/TalhaY61/sukun/issues/new/choose) kullanın. Hata bildirimine sürüm, Windows sürümü, oynatıcı ve tekrar adımlarını ekleyin. Günlükleri paylaşmadan önce inceleyin; token, kişisel ayar veya oynatma geçmişi eklemeyin.

Please use the issue forms for bugs and suggestions. Include app/Windows versions, player and reproduction steps. Review any attachments before sharing them. Never submit credentials or private settings.

For code: keep WPF/C#, the compact UI and low dependency count. Build and run `tests/Sukun.Tests` on Windows. Media changes need explicit pause/play verification and must never start media the app did not pause. Use the central `.resx` files for all UI text and review both languages. See `src/Sukun.Core/Localization/README.md`.

`scripts/publish.ps1` makes the self-contained x64 distribution and SHA256 checksums. Binaries, logs, local caches and credentials do not belong in Git. GitHub Pages source is isolated in `site/`.
