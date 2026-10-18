# Code signing policy

**Status: preparation only.** Sükûn has not been accepted by a signing provider and the published release is unsigned. A successful build or SHA256 match is not proof of a trusted publisher. No signing service has been enabled by this document.

## Distribution choice

The main download remains the self-contained Windows x64 EXE: no separate .NET installation. The optional `./scripts/publish.ps1 -Lite` output requires the **.NET 10 Desktop Runtime x64** (not just the base runtime or SDK). A smaller download does not guarantee lower RAM use. Self-contained releases must be rebuilt and redistributed when their bundled runtime needs a security update.

## Maintainer setup — SignPath Foundation candidate

1. Review [eligibility and terms](https://signpath.org/terms.html), then [apply](https://signpath.org/apply). Approval is not guaranteed, particularly for a new project with little established reputation. The maintainer must complete account setup, MFA and any contractual acceptance; no credentials belong in source code or chat.
2. Application details: Sükûn; MIT; https://github.com/TalhaY61/sukun; https://talhay61.github.io/sukun/; WPF/C# utility pausing only the media it owns at selected prayer times. Maintainer: [TalhaY61](https://github.com/TalhaY61). Current release: [v1.0.0](https://github.com/TalhaY61/sukun/releases/tag/v1.0.0).
3. If approved, install the official SignPath GitHub App for **this repository only**, review the requested permissions, and link the project to the GitHub trusted build system. Foundation signing identifies **SignPath Foundation** as certificate publisher. Confirm that arrangement before enrollment.
4. Configure a release-signing policy requiring manual approval. Author/reviewer/release approver for this project is TalhaY61; outside contributions require maintainer review. Maintain MFA for repository and signing access. Add the provider's exact required attribution after acceptance; do not claim sponsorship before acceptance.
5. Create the GitHub environment `code-signing`, restrict deployment branches to `main`, and require maintainer review where supported. Do not expose signing credentials to pull-request workflows. Put the restricted submitter token in the environment secret `SIGNPATH_API_TOKEN`. Set environment variables `SIGNPATH_ORGANIZATION_ID`, `SIGNPATH_PROJECT_SLUG`, `SIGNPATH_SIGNING_POLICY_SLUG` to the actual assigned values. Use **release signing**, not a self-signed test certificate.
6. Import `packaging/signpath-artifact.xml` as a starting artifact configuration and validate it in SignPath. Enforce product and build version metadata according to the approved policy. Only Sükûn's own EXE is submitted for signing; do not replace Microsoft signatures on bundled runtime components. Validate single-file .NET packaging with the provider before the first release.
7. Run **Build and request trusted signing** from Actions on `main`. It builds/tests on a GitHub-hosted Windows runner, uploads the EXE for origin verification, requests signing and waits for approval. Missing configuration, rejection, an untrusted signature or missing timestamp stops the job. It never silently falls back to publishing an unsigned EXE.
8. Download `Sukun-win-x64-signed`, test that exact EXE on clean supported Windows machines with security protections enabled, and inspect its publisher and signature. Test media pause/resume, startup, tray, settings and sleep/wake. Signing does not guarantee acceptance under every enterprise policy or eliminate all SmartScreen reputation prompts.
9. Publish a **new release** after validation, attaching the signed EXE, signed-file ZIP and SHA256SUMS. Checksums must be computed **after** signing. This workflow intentionally does not create releases or overwrite existing release files automatically.

The existing Build and test workflow continues to produce **unsigned previews**. Only the explicit signing workflow can create the signed artifact.

## Privacy

See [PRIVACY.md](../PRIVACY.md). The desktop app contains no signing credentials, accounts or telemetry. Signing sends public build artifacts and build-origin metadata to the signing service during CI; it does not run on users' computers. Prayer data retrieval contacts Diyanet and optional update checks contact GitHub as documented.

## Alternatives

If Foundation enrollment is declined, consider a Microsoft Store **MSIX** submission (Store handles signing after certification) or a trusted CA certificate eligible for the maintainer's identity/country. Store distribution requires packaging changes and separate validation of tray/startup/media behavior. Store signing does **not** sign the separate GitHub portable EXE. Azure Artifact Signing eligibility must be checked; individual public-trust availability is currently restricted to the USA and Canada. No paid service is authorized by this preparation.

References: [SignPath GitHub integration](https://docs.signpath.io/trusted-build-systems/github), [Microsoft signing options](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/code-signing-options), [.NET deployment](https://learn.microsoft.com/en-us/dotnet/core/deploying/).
