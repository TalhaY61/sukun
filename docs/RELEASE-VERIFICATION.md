# v1.0.0 verification

## Scope

The first numbered release retains the WPF/C# implementation from v0.1.0-beta.5. This change updates version metadata and documentation; it does not alter the media engine or prayer scheduling.

- Windows 10 (2004+) and Windows 11 x64; self-contained portable EXE, no separate .NET installation.
- Turkish/English UI, local settings and cached Diyanet prayer times; explicit first-run location choice.
- Tray operation, single instance, conservative pause/resume ownership and opt-in daily update checks.
- Stable releases check the stable update channel. Earlier prerelease builds with the newer updater can discover this release too.

## Verification

Release build, automated application checks, website release resolver checks and artifact integrity checks are rerun for this release. Their final results are recorded in the GitHub release notes and CI.

## Remaining runtime limits

- The distributed EXE is unsigned. Signing infrastructure preparation is not a trusted signature or provider approval; Windows may warn or block execution.
- Real media pause/resume succeeded on an earlier executable. This is not a successful real-media test of the final v1.0.0 package.
- Final-package startup, actual player pause/resume and resource measurements have not been verified for this exact executable.
- Separate Windows 10 hardware/VM, reboot/startup, multi-monitor/DPI and extended sleep/wake coverage remain incomplete. Automated fake-clock/session checks cannot replace these tests.
- Unattended venue use is not validated. Before relying on Sükûn, test your intended player and selected location with Windows security protections enabled.

## Manual checks

Choose a location and media source, select prayer breaks and enable auto pause. Confirm an actual scheduled pause/resume, no restart of already-paused media, tray hide/show, single instance, saved settings after restart, TR/EN, both themes and dropdown alignment. The developer media-control trial is available only in Debug builds, not the public Release UI.

Report problems at https://github.com/TalhaY61/sukun/issues/new/choose. Review attachments for personal information before uploading.
