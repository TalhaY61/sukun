# UI localization

`Strings.resx`: neutral English. `Strings.tr.resx`: Turkish satellite resources.
All UI text lives here, including status messages, errors, tooltips, tray actions and the media-control trial. Application names, media titles and Diyanet location names are external content, not translated UI.

`L.Get(key, arguments)` formats text in the selected culture. XAML uses `DynamicResource` keys populated from the same resources; there is no separate XAML translation dictionary. Do not add translated strings directly to XAML/C#.

Windows UI language selects the initial locale (Turkish → TR, other languages → EN). `Settings.Language` is nullable for existing settings and becomes an explicit preference after the user chooses a language. Unsupported values fall back to English without discarding other settings.

Language changes rebuild presentation labels under the loading guard and save settings directly. They do not call `ChangeAsync`, cancel an active break, rebase the clock, or change source/prayer IDs. Message factories keep visible status, notices and trial history translatable without restarting automation. Exceptions use `L.Data(key)` so an already-visible error retains its translation identity.

Culture controls date, time and number formatting. Prayer calculations and displayed timetable clocks remain UTC+03:00. Protocol URLs, cache IDs and Diyanet parsing remain invariant. Debug log keys, API identifiers and HRESULTs remain technical identifiers.

To add another language: add `Strings.de.resx` / `Strings.ar.resx`, preserve the keys and numbered format arguments, extend `L.Normalize` / culture mapping and add a language choice. Arabic also needs explicit right-to-left layout review before being offered. No localization package is required.
