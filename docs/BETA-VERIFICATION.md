# v0.1.0-beta.3 verification

This is a public test release, not a stable compatibility claim.

## Release scope

- Developer media trial, trial details and local-log shortcut are hidden in Release packages. Debug builds retain them. Normal startup/cache/update preferences stay visible. Bounded local diagnostics and media/automation behavior are unchanged.

- Same WPF/C# Windows 10 (2004+) / Windows 11 x64 app, Turkish/English UI, local settings/cache, tray operation and conservative media ownership rules.
- Existing pause + circumflex branding and four supplied screenshots are included. No new media engine or automation behavior is introduced by this release.
- New repository history starts with a clean snapshot. The previous repository is retained as `sukun_archive`; its historical alerts and downloaded copies are not erased by creating a new repository.
- Main download is a self-contained EXE. No separate .NET installation is needed; trimming and Native AOT remain disabled.

## Automated verification

- Local Windows x64 Release solution build: 0 warnings / 0 errors.
- Application automated suite: **84/84 passed**, including clock/session ownership, localization, single instance, location identity, popup geometry and fixture credential guard.
- Website release resolver: **10 checks passed**.
- This is automated/source verification, not a successful real-media test of the final distributed EXE.

## Runtime verification limits

- The published beta is **unsigned**. Trusted signing enrollment has not been completed. The prepared signing workflow is not proof of a signature.
- This development machine previously blocked an unsigned beta under Windows Application Control. Security protections are not disabled or bypassed for testing. Direct beta.3 EXE startup, current performance and actual player pause/resume are not yet validated.
- A real YouTube Music pause/resume trial succeeded on an earlier executable, with other already-paused players remaining paused. That is historical evidence, not a successful beta.3 runtime test.
- A separate Windows 10 device/VM, reboot/startup registration, physical multi-monitor/DPI cases and long sleep/wake still require user testing. Automated fake-clock/session checks do not replace these hardware tests.
- Previous RAM/startup observations do not describe this exact packaged binary and are not performance promises.

## User smoke check

On a system that permits the EXE, choose a location and source, enable auto pause and use Advanced settings → media control test. Confirm pause/resume, no restart of previously paused media, tray hide/show, single instance, preferences after restart, TR/EN, both themes and dropdown alignment. Do not disable security protections to run a beta. Report issues via https://github.com/TalhaY61/sukun/issues/new/choose after reviewing attachments for personal data.
