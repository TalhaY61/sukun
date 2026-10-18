# Sükûn download site

Static HTML/CSS/JavaScript; no framework, build step, analytics, external fonts or tracking scripts. The only API request is to GitHub’s public releases endpoint. GitHub receives normal HTTP connection details. Only the selected UI language is saved in browser localStorage.

`releases.js` prioritizes GitHub’s latest stable release. If none exists, it selects a published prerelease with a real, uploaded `Sukun-win-x64.exe` asset and clearly labels it beta. URLs must belong to this repository. API failures and missing assets show the real releases page rather than an invented download URL. New releases need no manual site edits.

The Pages workflow uploads only `site/`, with the project URL `https://talhay61.github.io/sukun/`. It does not change a user/portfolio Pages repository or any domain. JavaScript is optional for navigation; without it, the GitHub Releases link remains available.

Preview: `python -m http.server 8765 --directory site`.
