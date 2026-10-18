import {resolveRelease,windowsAsset,releasesPage} from './releases.js';
const copy = {
  tr:{heading:'Vakti gelince,\nküçük bir ara.',description:'Namaz vakti geldiğinde çalan müziği veya videoyu duraklatır. Belirlediğiniz süre sonunda kaldığınız yerden devam eder.',downloadNote:'Ücretsiz · Reklamsız · .NET kurulumu gerekmez',betaNote:'İlk test sürümü; henüz dijital olarak imzalanmadı. Windows çalıştırılmasını engelleyebilir. Güvenlik korumalarınızı kapatmayın.',previewLabel:'Küçük bir pencere. Günün geri kalanı sizin.',previewCaption:'Windows tepsisinde, ihtiyaç duyduğunuzda elinizin altında.',feature1:'Sizden bir adım önce',feature1Body:'Konumunuzu ve oynatıcınızı seçin. Hangi vakitlerde, ne kadar ara verileceğini siz belirleyin.',feature2:'Vakitler cebinizde',feature2Body:'Diyanet’ten indirilen geçerli günler cihazınızda saklanır. Kayıtlı tarihlerde internet gerekmez.',feature3:'Sade. Yerel. Açık.',feature3Body:'Hesap yok, reklam yok, telemetri yok. Ayarlar cihazınızda kalır; kaynak kodu herkese açıktır.',startTitle:'İndirin, açın, konumunuzu seçin.',startBody:'Windows 10 (2004 ve sonrası) veya Windows 11 x64. EXE’yi kalıcı bir klasöre koyun. Kapat düğmesi uygulamayı tepsiye gizler; tamamen kapatmak için Çıkış’ı kullanın.',feedback:'Bir şey yolunda gitmedi mi? Hata bildirin ↗',footer:'Küçük bir ara, açık kaynak.',privacy:'Gizlilik',loading:'Sürümler yükleniyor…',loadingStatus:'Yayımlanan Windows sürümü kontrol ediliyor.',stable:'Windows için indir',beta:'Beta sürümünü indir',browse:'GitHub sürümlerini aç',unavailable:'İndirme şu anda doğrulanamadı. GitHub’daki yayımları kontrol edin.',noRelease:'Windows EXE henüz yayımlanmamış. GitHub yayımlarını takip edebilirsiniz.',betaStatus:'İlk test sürümü · Kararlı sürüm değildir.',stableStatus:'Kararlı sürüm',alt:'Sükûn uygulamasının Türkçe arayüzü',lightPreview:'Açık',darkPreview:'Koyu',madeBy:'Geliştiren'},
  en:{heading:"A little pause\nwhen it’s time.",description:'Sükûn pauses your music or video at prayer time, then resumes where you left off after the interval you choose.',downloadNote:'Free · Ad-free · No .NET installation needed',betaNote:'First test release, not yet digitally signed. Windows may block it from running. Keep your security protections enabled.',previewLabel:'A small window. The rest of the day is yours.',previewCaption:'In your Windows tray, right there when you need it.',feature1:'One less thing to remember',feature1Body:'Choose your location and player. You decide which prayers to pause for, and for how long.',feature2:'Prayer times, kept close',feature2Body:'Valid dates downloaded from Diyanet stay on your device. Saved dates work without an internet connection.',feature3:'Simple. Local. Open.',feature3Body:'No account, ads or telemetry. Settings stay on your device, and the source code is open to everyone.',startTitle:'Download, open, choose your location.',startBody:'Windows 10 (2004 or later) or Windows 11 x64. Keep the EXE in a permanent folder. Close hides Sükûn in the tray; choose Quit to exit completely.',feedback:'Something not quite right? Report an issue ↗',footer:'A little pause. Open source.',privacy:'Privacy',loading:'Checking releases…',loadingStatus:'Looking for a published Windows version.',stable:'Download for Windows',beta:'Download the beta',browse:'View GitHub releases',unavailable:'The download could not be verified right now. Please check GitHub releases.',noRelease:'The Windows EXE has not been published yet. Follow releases on GitHub.',betaStatus:'First test release · Not a stable version.',stableStatus:'Stable release',alt:'Sükûn desktop app in English',lightPreview:'Light',darkPreview:'Dark',madeBy:'Made by'}
};
let lang = 'en';
try {lang=localStorage.getItem('sukun-site-language') ?? (navigator.language.startsWith('tr')?'tr':'en');} catch {lang=navigator.language.startsWith('tr')?'tr':'en';}
if (!(lang in copy)) lang='en';
let currentRelease=null, loaded=false, failed=false;
let previewTheme='light';
function updatePreview() {
  const screenshot=document.querySelector('#screenshot');
  screenshot.src=`assets/sukun_${previewTheme}_${lang}.png`;
  screenshot.alt=`${copy[lang].alt} · ${copy[lang][previewTheme==='light'?'lightPreview':'darkPreview']}`;
  for(const button of document.querySelectorAll('[data-preview-theme]')) button.setAttribute('aria-pressed',String(button.dataset.previewTheme===previewTheme));
}
for(const button of document.querySelectorAll('[data-preview-theme]')) button.addEventListener('click',()=>{previewTheme=button.dataset.previewTheme;updatePreview();});
function updateDownload() {
  const c=copy[lang], button=document.querySelector('#download'), label=document.querySelector('#download-label'), status=document.querySelector('#release-status');
  const asset=windowsAsset(currentRelease);
  button.href=asset?.browser_download_url ?? releasesPage;
  label.textContent=!loaded?c.loading:asset?(currentRelease.prerelease?c.beta:c.stable):c.browse;
  status.textContent=!loaded?c.loadingStatus:asset?`${currentRelease.tag_name} · ${currentRelease.prerelease?c.betaStatus:c.stableStatus} · ${(asset.size/1048576).toFixed(1)} MB`:failed?c.unavailable:c.noRelease;
  document.querySelector('.beta-note').hidden=Boolean(asset && !currentRelease.prerelease);
}
function setLanguage(value) {
  lang=value; const c=copy[lang]; document.documentElement.lang=lang;
  for (const node of document.querySelectorAll('[data-i18n]')) {
    const key=node.dataset.i18n;
    if(key==='heading') node.replaceChildren(...c.heading.split('\n').flatMap((part,i)=>i?[document.createElement('br'),document.createTextNode(part)]:[document.createTextNode(part)]));
    else node.textContent=c[key];
  }
  for(const button of document.querySelectorAll('[data-lang]')) button.setAttribute('aria-pressed',String(button.dataset.lang===lang));
  updatePreview();
  document.title=`Sükûn — ${c.heading.replace('\n',' ')}`;
  document.querySelector('meta[name="description"]').content=c.description;
  updateDownload();
}
for(const button of document.querySelectorAll('[data-lang]')) button.addEventListener('click',()=>{
  setLanguage(button.dataset.lang); try{localStorage.setItem('sukun-site-language',lang);}catch{}
});
setLanguage(lang);
resolveRelease().then(release=>{currentRelease=release;}).catch(()=>{failed=true;}).finally(()=>{loaded=true;updateDownload();});
