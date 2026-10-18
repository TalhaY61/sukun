const owner = 'TalhaY61';
const repo = 'sukun';
// Temporary download rollback; set to null to restore automatic latest-release selection.
export const downloadTag = 'v1.0.0';
export const releasesPage = `https://github.com/${owner}/${repo}/releases${downloadTag ? '/tag/' + encodeURIComponent(downloadTag) : ''}`;
export function windowsAsset(release) {
  if (!release || release.draft || !release.published_at || !Array.isArray(release.assets)) return null;
  return release.assets.find(asset => {
    if (asset.name !== 'Sukun-win-x64.exe' || asset.state !== 'uploaded' || !(asset.size > 0)) return false;
    try {
      const url = new URL(asset.browser_download_url);
      return url.protocol === 'https:' && url.hostname === 'github.com' && !url.port && !url.username && !url.password && !url.search && !url.hash &&
        url.pathname === `/${owner}/${repo}/releases/download/${encodeURIComponent(release.tag_name)}/Sukun-win-x64.exe`;
    } catch { return false; }
  }) ?? null;
}
export function selectRelease(releases) {
  const published = releases.filter(r => !r.draft && r.published_at).sort((a,b) => Date.parse(b.published_at) - Date.parse(a.published_at));
  // Never describe a beta as stable; do not fall back to beta if a stable release exists without its EXE.
  return published.find(r => !r.prerelease) ?? published.find(r => r.prerelease && windowsAsset(r)) ?? null;
}
async function request(path) {
  const response = await fetch(`https://api.github.com/repos/${owner}/${repo}/${path}`, {
    headers: {Accept:'application/vnd.github+json'}, credentials:'omit', referrerPolicy:'no-referrer', signal:AbortSignal.timeout(10000)
  });
  if (response.status === 404) return null;
  if (!response.ok) throw new Error('Release service unavailable');
  return response.json();
}
export async function resolveRelease(pinnedTag = downloadTag) {
  if (pinnedTag) {
    const pinned = await request(`releases/tags/${encodeURIComponent(pinnedTag)}`);
    return pinned?.tag_name === pinnedTag && !pinned.prerelease && windowsAsset(pinned) ? pinned : null;
  }
  const latest = await request('releases/latest');
  if (latest && !latest.draft && !latest.prerelease) return latest;
  return selectRelease(await request('releases?per_page=100') ?? []);
}
