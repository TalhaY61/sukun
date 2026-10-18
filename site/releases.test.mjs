import assert from 'node:assert/strict';
import {selectRelease,windowsAsset,resolveRelease} from './releases.js';
const make=(tag,beta=false)=>({tag_name:tag,draft:false,prerelease:beta,published_at:'2026-10-18T12:00:00Z',assets:[{name:'Sukun-win-x64.exe',state:'uploaded',size:100,browser_download_url:`https://github.com/TalhaY61/sukun/releases/download/${tag}/Sukun-win-x64.exe`}]});
const beta=make('v0.1.0-beta.1',true),stable=make('v0.1.0');
assert.equal(selectRelease([beta]),beta);
assert.equal(selectRelease([beta,stable]),stable);
assert.equal(selectRelease([]),null);
assert.equal(windowsAsset({...beta,draft:true}),null);
assert.equal(windowsAsset({...beta,assets:[{...beta.assets[0],browser_download_url:'https://evil.example/Sukun-win-x64.exe'}]}),null);
assert.equal(windowsAsset({...beta,assets:[{...beta.assets[0],state:'open'}]}),null);
assert.equal(selectRelease([{...stable,assets:[]},beta]).prerelease,false);
const originalFetch=globalThis.fetch;
try {
  globalThis.fetch=async url=>String(url).endsWith('/latest')?{status:404,ok:false}:{status:200,ok:true,json:async()=>[beta]};
  assert.equal((await resolveRelease()).tag_name,beta.tag_name);
  globalThis.fetch=async()=>({status:200,ok:true,json:async()=>stable});
  assert.equal((await resolveRelease()).tag_name,stable.tag_name);
  globalThis.fetch=async()=>({status:429,ok:false});
  await assert.rejects(resolveRelease());
} finally {globalThis.fetch=originalFetch;}
console.log('10 release resolver checks passed: stable, beta, missing/unsafe assets, rate limit.');
