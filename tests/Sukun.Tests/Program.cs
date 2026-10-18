using Sukun.Core;
using Sukun.Windows;

var tests = new List<(string, Func<Task>)>();
void Test(string name, Action run) => tests.Add((name, () => { run(); return Task.CompletedTask; }));
void AsyncTest(string name, Func<Task> run) => tests.Add((name, run));
void Assert(bool ok, string message = "Assertion failed") { if (!ok) throw new Exception(message); }
void Reject(Action run) { try { run(); } catch (Exception e) when (e is InvalidDataException or ArgumentException) { return; } throw new Exception("Expected rejection"); }
var now = new DateTimeOffset(2026, 10, 18, 12, 0, 0, TimeSpan.FromHours(3));
var html = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "ankara.html"));
var parsed = DiyanetParser.Parse(html, "9206", now);
Test("Official location names are not replaced by geographic guesses", () =>
{
    var catalog=System.Text.Json.JsonSerializer.Deserialize<List<Place>>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"Fixtures","places.json")))!;
    Assert(catalog.Single(p=>p.Id=="9535").Name=="Arnavutköy");
    Assert(catalog.Select(p=>p.Province).Distinct().Count()==81);
    Assert(catalog.Select(p=>p.Id).Distinct().Count()==catalog.Count);
});
Test("Real Diyanet fixture: identity and 14+ consecutive days", () => Assert(parsed.ContiguousDays(new DateOnly(2026, 10, 18)) >= 14));
Test("Real fixture clocks match official Ankara page", () => Assert(parsed.Days[new DateOnly(2026, 10, 18)].Clocks.SequenceEqual(new[] { "05:32", "06:55", "12:39", "15:42", "18:13", "19:31" })));
Test("Reject page for a different district", () => Reject(() => DiyanetParser.Parse(html, "9651", now)));
Test("Reject empty or changed HTML", () => Reject(() => DiyanetParser.Parse("var ilceId = 9206; <table></table>", "9206", now)));
Test("Reject invalid prayer clocks", () => Reject(() => DiyanetParser.Parse(html.Replace("05:32", "29:32"), "9206", now)));
Test("Reject conflicting duplicate dates", () => Reject(() => DiyanetParser.Parse(html + html.Replace("05:32", "05:31").Replace("var ilceId = 9206;", ""), "9206", now)));
Test("Future end date does not hide a current coverage gap", () => Reject(() => DiyanetParser.Parse(html, "9206", now.AddYears(8))));
Test("Coverage counts stop at the first hole", () => { var t = parsed with { Days = new(parsed.Days) }; t.Days.Remove(new DateOnly(2026, 10, 20)); Assert(t.ContiguousDays(new DateOnly(2026, 10, 18)) == 2); });
Test("Official clocks use Turkey UTC+3 even on a German PC", () => Assert(parsed.Days[new DateOnly(2026, 10, 18)].At(Prayer.Dhuhr).UtcDateTime.Hour == 9));
Test("Sunrise is never an automation prayer", () => { var s = new Settings { PlaceId = "9206", Prayers = Enum.GetValues<Prayer>().ToHashSet() }; Assert(BreakWindow.Build(parsed, s, now).Where(w => w.Key.Contains("2026-10-18")).Count() == 5); });
Test("Wrong location table never creates break windows", () => Assert(BreakWindow.Build(parsed, new Settings { PlaceId = "9651" }, now).Count == 0));
Test("Imsak is opt-in", () => Assert(!new Settings().Prayers.Contains(Prayer.Fajr)));
Test("Reject invalid settings", () => Reject(() => new Settings { AfterMinutes = -1 }.Validate()));
Test("Merging preserves stored future days", () => { var small = parsed with { Days = new(parsed.Days.Take(14).ToDictionary()), FetchedAt = now.AddHours(1) }; Assert(DiyanetParser.Merge(parsed, small).Days.Count == parsed.Days.Count); });

AsyncTest("Local storage roundtrip preserves settings and official dates", async () =>
{
    var root=Path.Combine(Environment.CurrentDirectory,"artifacts","storage-tests",Guid.NewGuid().ToString("N"));
    var store=new LocalStore(root);
    await store.SaveSettingsAsync(new Settings {PlaceId="9206", Enabled=true});
    await store.SaveTableAsync(parsed);
    Assert(store.LoadSettings().PlaceId=="9206");
    var loaded=await store.LoadTableAsync("9206");
    Assert(loaded?.Days.Count==parsed.Days.Count);
    Assert(Directory.GetFiles(root,"*.tmp").Length==0);
});
AsyncTest("Corrupt settings fail closed", async () =>
{
    var root=Path.Combine(Environment.CurrentDirectory,"artifacts","storage-tests",Guid.NewGuid().ToString("N"));
    var store=new LocalStore(root); await File.WriteAllTextAsync(Path.Combine(root,"settings.json"),"{broken}");
    Assert(!store.LoadSettings().Enabled && store.Warning is not null);
});
AsyncTest("Corrupt cache is not treated as an official timetable", async () =>
{
    var root=Path.Combine(Environment.CurrentDirectory,"artifacts","storage-tests",Guid.NewGuid().ToString("N"));
    var store=new LocalStore(root); await File.WriteAllTextAsync(Path.Combine(root,"times-9206.json"),"{}");
    Assert(await store.LoadTableAsync("9206") is null);
});
if (args.Contains("--live")) AsyncTest("LIVE direct Diyanet download, identity, and near-term coverage", async () =>
{
    using var client=new DiyanetClient(); var live=await client.DownloadAsync("9206",DateTimeOffset.UtcNow);
    Assert(live.ContiguousDays(Timetable.TurkeyDate(DateTimeOffset.UtcNow))>=14);
    Console.WriteLine($"Live source: {live.Days.Count} dates; last {live.Days.Keys.Max():yyyy-MM-dd}");
});
if (args.Contains("--preview-data"))
{
    var preview = new LocalStore(Path.Combine(Path.GetTempPath(), "Sukun-smoke"));
    await preview.SaveSettingsAsync(new Settings { PlaceId = "9206", Theme = "Dark" });
    await preview.SaveTableAsync(parsed);
    Console.WriteLine("Read-only UI preview prepared with official Ankara fixture.");
}

var start = now.AddMinutes(1);
var window = new BreakWindow("test", Prayer.Dhuhr, start, start.AddSeconds(10));
IReadOnlyList<BreakWindow> windows = [window];
async Task<(FakeMedia, BreakController)> Ready(Playback state = Playback.Playing)
{
    var m = new FakeMedia { States = [new("a", "spotify", "track1", state, true, true)] };
    var c = new BreakController(m);
    await c.TickAsync(now, true, windows, null);
    return (m, c);
}
AsyncTest("Pause and resume exactly once", async () =>
{
    var (m, c) = await Ready(); await c.TickAsync(start, true, windows, null); Assert(c.IsActive && m.Pauses == 1);
    await c.TickAsync(start.AddSeconds(5), true, windows, null); await c.TickAsync(window.End, true, windows, null);
    await c.TickAsync(window.End.AddSeconds(1), true, windows, null); Assert(m.Plays == 1 && m.Pauses == 1 && !c.IsActive);
});
AsyncTest("Already-paused music is never started", async () => { var (m,c) = await Ready(Playback.Paused); await c.TickAsync(start,true,windows,null); await c.TickAsync(window.End,true,windows,null); Assert(m.Pauses == 0 && m.Plays == 0); });
AsyncTest("Startup inside a break skips it", async () => { var m = new FakeMedia(); var c = new BreakController(m); await c.TickAsync(start,true,windows,null); await c.TickAsync(start.AddSeconds(2),true,windows,null); Assert(m.Pauses == 0); });
AsyncTest("Two playing sessions in automatic mode are not guessed", async () => { var(m,c)=await Ready(); m.States.Add(m.States[0] with {SessionId="b", AppId="chrome"}); await c.TickAsync(start,true,windows,null); Assert(m.Pauses==0); });
AsyncTest("Explicit player preference isolates the target", async () => { var(m,c)=await Ready(); m.States.Add(m.States[0] with {SessionId="b",AppId="chrome"}); await c.TickAsync(start,true,windows,"spotify"); Assert(m.Pauses==1 && m.States[1].Playback==Playback.Playing); });
AsyncTest("Explicit session isolates two sources from the same browser", async () => { var(m,c)=await Ready(); m.States[0]=m.States[0] with {AppId="chrome"}; m.States.Add(m.States[0] with {SessionId="b"}); await c.TickAsync(start,true,windows,"session:b"); Assert(m.Pauses==1 && m.States[0].Playback==Playback.Playing && m.States[1].Playback==Playback.Paused); });
Test("Appearance preference has a stable default and validates values", () => { Assert(new Settings().Theme=="System"); Reject(()=>new Settings {Theme="unrecognized"}.Validate()); });
AsyncTest("User resumes music: no re-pause or automatic play", async () => { var(m,c)=await Ready(); await c.TickAsync(start,true,windows,null); m.States[0]=m.States[0] with {Playback=Playback.Playing}; await c.TickAsync(start.AddSeconds(2),true,windows,null); await c.TickAsync(window.End,true,windows,null); Assert(m.Pauses==1 && m.Plays==0); });
AsyncTest("Play then pause between polls cancels ownership via revision", async () => { var(m,c)=await Ready(); await c.TickAsync(start,true,windows,null); m.States[0]=m.States[0] with {Revision=4}; await c.TickAsync(window.End,true,windows,null); Assert(m.Plays==0); });
AsyncTest("Track change cancels automatic resume", async () => { var(m,c)=await Ready(); await c.TickAsync(start,true,windows,null); m.States[0]=m.States[0] with {Fingerprint="track2"}; await c.TickAsync(window.End,true,windows,null); Assert(m.Plays==0); });
AsyncTest("Reopened player is not the same session", async () => { var(m,c)=await Ready(); await c.TickAsync(start,true,windows,null); m.States[0]=m.States[0] with {SessionId="new"}; await c.TickAsync(window.End,true,windows,null); Assert(m.Plays==0); });
AsyncTest("Turning off never starts music", async () => { var(m,c)=await Ready(); await c.TickAsync(start,true,windows,null); await c.TickAsync(start.AddSeconds(1),false,windows,null); await c.TickAsync(window.End,true,windows,null); Assert(m.Plays==0 && !c.IsActive); });
AsyncTest("Explicit cancel leaves the player alone", async () => { var(m,c)=await Ready(); await c.TickAsync(start,true,windows,null); c.Cancel("user"); await c.TickAsync(window.End,true,windows,null); Assert(m.Plays==0); });
AsyncTest("Wake after long sleep never unexpectedly resumes", async () => { var(m,c)=await Ready(); await c.TickAsync(start,true,windows,null); await c.TickAsync(start.AddHours(1),true,windows,null); Assert(m.Plays==0); });
AsyncTest("Late timer skips missed break", async () => { var(m,c)=await Ready(); var longWindow=window with {End=start.AddMinutes(10)}; await c.TickAsync(start.AddMinutes(2),true,[longWindow],null); Assert(m.Pauses==0); });
AsyncTest("Failed pause never owns a resume", async () => { var(m,c)=await Ready(); m.PauseSucceeds=false; await c.TickAsync(start,true,windows,null); await c.TickAsync(window.End,true,windows,null); Assert(m.Plays==0); });
AsyncTest("Unconfirmed pause never owns a resume", async () => { var(m,c)=await Ready(); m.ChangeOnPause=false; await c.TickAsync(start,true,windows,null); await c.TickAsync(window.End,true,windows,null); Assert(m.Plays==0); });
AsyncTest("Unknown track cannot safely auto-resume", async () => { var(m,c)=await Ready(); m.States[0]=m.States[0] with {Fingerprint=""}; await c.TickAsync(start,true,windows,null); await c.TickAsync(window.End,true,windows,null); Assert(m.Pauses==1 && m.Plays==0); });
AsyncTest("Overlapping breaks do not restart music in the middle", async () => { var(m,c)=await Ready(); var w2=new BreakWindow("second",Prayer.Asr,start.AddSeconds(5),start.AddSeconds(20)); await c.TickAsync(start,true,[window,w2],null); await c.TickAsync(window.End,true,[window,w2],null); Assert(m.Plays==0); await c.TickAsync(w2.End,true,[window,w2],null); Assert(m.Plays==1); });
AsyncTest("Backward clock movement cancels ownership", async () => { var(m,c)=await Ready(); await c.TickAsync(start,true,windows,null); await c.TickAsync(now,true,windows,null); Assert(!c.IsActive && m.Plays==0); });
AsyncTest("Enabling during an already passed start does not interrupt", async () => { var(m,c)=await Ready(); await c.TickAsync(start,false,windows,null); await c.TickAsync(start.AddSeconds(1),true,windows,null); Assert(m.Pauses==0); });


var repository = GitHubRepository.Parse("https://github.com/TalhaY61/sukun.git")!;
string ReleaseJson(string version = "v0.2.0", string? url = null, bool preview = false, bool draft = false) =>
    System.Text.Json.JsonSerializer.Serialize(new { tag_name = version, html_url = url ?? "https://github.com/TalhaY61/sukun/releases/tag/" + version, prerelease = preview, draft });
Test("Repository normalization drops only the git suffix", () => Assert(repository.Page.AbsoluteUri == "https://github.com/TalhaY61/sukun"));
Test("Repository config refuses arbitrary hosts, credentials and paths", () =>
{
    foreach (var url in new[] { "http://github.com/TalhaY61/sukun", "https://example.com/TalhaY61/sukun", "https://github.com/a/b/releases", "https://token@github.com/a/b", "https://github.com/a/b?token=secret", "https://github.com:444/a/b" })
        Assert(GitHubRepository.Parse(url) is null);
});
Test("Stable numeric versions compare correctly including assembly revisions", () =>
{
    var release = SoftwareRelease.Parse(ReleaseJson("v0.10.0"), repository);
    Assert(release.IsNewerThan(new Version(0, 2, 9, 0)));
    Assert(!release.IsNewerThan(new Version(0, 10, 0, 0)));
    Assert(!release.IsNewerThan(new Version(1, 0, 0)));
});
Test("Drafts and prereleases never become stable update prompts", () =>
{
    Reject(() => SoftwareRelease.Parse(ReleaseJson(preview: true), repository));
    Reject(() => SoftwareRelease.Parse(ReleaseJson(draft: true), repository));
    Reject(() => SoftwareRelease.Parse(ReleaseJson("v0.3.0-beta.1"), repository));
});
Test("Release links cannot switch host, repository or transport", () =>
{
    foreach (var url in new[] { "https://example.com/update.exe", "https://github.com/another/repo/releases/tag/v0.2.0", "http://github.com/TalhaY61/sukun/releases/tag/v0.2.0", "https://github.com/TalhaY61/sukun/releases/tag/v0.2.0?redirect=evil" })
        Reject(() => SoftwareRelease.Parse(ReleaseJson(url: url), repository));
});
Test("Release link must identify the announced version", () => Reject(() => SoftwareRelease.Parse(ReleaseJson(url: "https://github.com/TalhaY61/sukun/releases/tag/v9.9.9"), repository)));
Test("Software update checks are opt-in and distinct from timetable refresh", () => Assert(!new Settings().AutoCheckSoftwareUpdates && new Settings().AutoRefresh));


AsyncTest("Chrome playing flag does not require CanPlay", async () => { var(m,c)=await Ready(); m.States[0]=m.States[0] with {CanPlay=false,AppId="chrome"}; await c.TickAsync(start,true,windows,null); await c.TickAsync(window.End,true,windows,null); Assert(m.Pauses==1 && m.Plays==1 && c.Outcome==BreakOutcome.Resumed); });
AsyncTest("Advisory capability flags are checked by actual command result", async () => { var(m,c)=await Ready(); m.States[0]=m.States[0] with {CanPlay=false,CanPause=false}; await c.TickAsync(start,true,windows,null); Assert(m.Pauses==1 && c.IsActive); });
AsyncTest("Rejected resume is a terminal failure", async () => { var(m,c)=await Ready(); m.PlaySucceeds=false; await c.TickAsync(start,true,windows,null); await c.TickAsync(window.End,true,windows,null); await c.TickAsync(window.End.AddSeconds(1),true,windows,null); Assert(m.Plays==1 && c.Outcome==BreakOutcome.Failed && !c.IsActive); });
AsyncTest("Accepted but unconfirmed resume fails", async () => { var(m,c)=await Ready(); m.ChangeOnPlay=false; await c.TickAsync(start,true,windows,null); await c.TickAsync(window.End,true,windows,null); Assert(c.Outcome==BreakOutcome.Failed); });
AsyncTest("Closed session is not resumed", async () => { var(m,c)=await Ready(); await c.TickAsync(start,true,windows,null); m.States.Clear(); await c.TickAsync(window.End,true,windows,null); Assert(m.Plays==0 && !c.IsActive); });
AsyncTest("Windows API exception is contained", async () => { var(m,c)=await Ready(); m.ThrowOnRead=true; await c.TickAsync(start,true,windows,null); Assert(c.Outcome==BreakOutcome.Failed && !c.IsActive); });
AsyncTest("Clock rewind rearms a future handled event exactly once", async () => { var(m,c)=await Ready(); await c.TickAsync(start,true,windows,null); await c.TickAsync(window.End,true,windows,null); c.RebaseClock(now,windows); await c.TickAsync(start,true,windows,null); await c.TickAsync(start,true,windows,null); Assert(m.Pauses==2 && m.Plays==1); });
AsyncTest("Clock change into a running interval skips it", async () => { var(m,c)=await Ready(); c.RebaseClock(start.AddSeconds(1),windows); await c.TickAsync(start.AddSeconds(2),true,windows,null); Assert(m.Pauses==0); });
AsyncTest("Forward clock jump skips a missed start", async () => { var(m,c)=await Ready(); var w=window with {End=start.AddMinutes(10)}; c.RebaseClock(start.AddMinutes(1),[w]); await c.TickAsync(start.AddMinutes(1),true,[w],null); Assert(m.Pauses==0); });
AsyncTest("Disabled automation sends no commands", async () => { var(m,c)=await Ready(); await c.TickAsync(start,false,windows,null); await c.TickAsync(window.End,false,windows,null); Assert(m.Pauses==0 && m.Plays==0); });
Test("Selected prayers, lead seconds and resume minutes build exact boundaries", () => { var s=new Settings {PlaceId="9206", Prayers=[Prayer.Asr], BeforeSeconds=60,AfterMinutes=5}; var w=BreakWindow.Build(parsed,s,now).Single(w=>w.Key.Contains("2026-10-18")); var at=parsed.Days[new DateOnly(2026,10,18)].At(Prayer.Asr); Assert(w.Prayer==Prayer.Asr && w.Start==at.AddSeconds(-60) && w.End==at.AddMinutes(5)); });
Test("Hidden-window scheduler wakes exactly at the approaching boundary", () => { Assert(ScheduleWakeup.Delay(start.AddSeconds(-5),windows,false,false)==TimeSpan.FromSeconds(5)); Assert(ScheduleWakeup.Delay(now.AddHours(-2),windows,false,false)==TimeSpan.FromSeconds(60)); Assert(ScheduleWakeup.Delay(now,windows,true,false)==TimeSpan.FromSeconds(1)); });
Test("Clock monitor distinguishes wall jumps from ordinary elapsed time", () => { var clock=new TestClock(now); var monitor=new ClockMonitor(clock); Assert(!monitor.Observe()); clock.Advance(TimeSpan.FromMinutes(2)); Assert(!monitor.Observe()); clock.ShiftWall(TimeSpan.FromHours(-1)); Assert(monitor.Observe()); clock.ShiftWall(TimeSpan.FromHours(2)); Assert(monitor.Observe()); monitor.Reset(); Assert(!monitor.Observe()); });
Test("Turkey midnight and timezone changes preserve the same instant", () => { var instant=new DateTimeOffset(2026,10,18,21,30,0,TimeSpan.Zero); Assert(Timetable.TurkeyDate(instant)==new DateOnly(2026,10,19)); var s=new Settings {PlaceId="9206"}; Assert(BreakWindow.Build(parsed,s,now).SequenceEqual(BreakWindow.Build(parsed,s,now.ToOffset(TimeSpan.FromHours(-7))))); });
Test("Turkish search matches keyboard variants", () => { Assert(SearchText.Matches("İstanbul","ista")); Assert(SearchText.Matches("İSTANBUL","istanbul")); Assert(SearchText.Matches("Ümraniye","umraniye")); Assert(SearchText.Matches("IĞDIR","ığdır")); Assert(!SearchText.Matches("Ankara","istanbul")); });
Test("One Chrome and one music app produce three choices including automatic", () =>
{
    var states=new MediaState[]{new("a","chrome","video",Playback.Paused,false,true),new("b","music","song",Playback.Playing,true,false)};
    var choices=MediaChoices.Build(states,new Settings(),x=>x=="music"?"YT Music":"Chrome");
    Assert(choices.Count==3 && choices.Any(i=>i.Key=="app:chrome") && choices.Any(i=>i.Label=="YT Music"));
    Assert(choices.Single(i=>i.Label=="YT Music").Detail.Contains(L.Get("Playing")));
});
Test("Two sessions in the same browser remain independently selectable without duplicate app row", () =>
{
    var states=new MediaState[]{new("a","chrome","video",Playback.Playing,true,false),new("b","chrome","music",Playback.Playing,true,false)};
    var choices=MediaChoices.Build(states,new Settings(),x=>"Chrome");
    Assert(choices.Count==3 && choices.Count(i=>i.SessionId is not null)==2);
    Assert(choices.All(i=>i.Key!="app:chrome"));
});
Test("Existing app preference is represented when several sessions share its identity", () =>
{
    var states=new MediaState[]{new("a","chrome","video",Playback.Playing,true,false),new("b","chrome","music",Playback.Paused,false,true)};
    var settings=new Settings {PreferredApp="chrome"};
    Assert(MediaChoices.Build(states,settings,x=>x).Single(i=>i.Key==MediaChoices.SelectedKey(settings)).Detail == L.Get("OpenSessions", 2));
});
Test("Closed selected session creates no ghost row and never changes the target", () =>
{
    var settings=new Settings {PreferredApp="chrome",PreferredSessionId="old"};
    var choices=MediaChoices.Build([new("new","chrome","video",Playback.Playing,true,false)],settings,x=>x);
    Assert(choices.Count==2 && choices.All(i=>i.Key!=MediaChoices.SelectedKey(settings)));
    Assert(MediaChoices.Target(settings)=="session:old");
});
Test("A live explicit pin is preserved without a duplicate app row", () =>
{
    var settings=new Settings {PreferredApp="chrome",PreferredSessionId="a"};
    var choices=MediaChoices.Build([new("a","chrome","video",Playback.Playing,true,false)],settings,x=>x);
    Assert(choices.Count==2 && choices.Any(i=>i.Key==MediaChoices.SelectedKey(settings)));
});
Test("Unavailable application is not a selectable ghost", () =>
{
    var settings=new Settings {PreferredApp="spotify"};
    Assert(MediaChoices.Build([],settings,x=>x).Count==1 && MediaChoices.Target(settings)=="spotify");
});
Test("Same display names do not merge different Windows application identities", () =>
{
    var states=new MediaState[]{new("a","chromeA","video",Playback.Playing,true,false),new("b","chromeB","music",Playback.Playing,true,false)};
    var choices=MediaChoices.Build(states,new Settings(),x=>"Chrome");
    Assert(choices.Count==3 && choices.Select(i=>i.Label).Distinct().Count()==3);
});
Test("Windows instance lock rejects a second owner and signals activation", () =>
{
    if (!OperatingSystem.IsWindows()) return;
    var name="Local\\Sukun.Test."+Guid.NewGuid().ToString("N");
    using var primary=new SingleInstance(name);
    Assert(primary.IsPrimary);
    using var activated=new ManualResetEventSlim();
    primary.Listen(()=>activated.Set());
    Exception? workerError=null;
    var worker=new Thread(()=>
    {
        if (!OperatingSystem.IsWindows()) return;
        try { using var second=new SingleInstance(name); Assert(!second.IsPrimary,"Second owner acquired the lock"); second.RequestActivation(); }
        catch(Exception error) { workerError=error; }
    });
    worker.Start(); Assert(worker.Join(TimeSpan.FromSeconds(3)),"Second instance did not finish");
    if(workerError is not null) throw workerError;
    Assert(activated.Wait(TimeSpan.FromSeconds(3)),"Existing instance was not activated");
});
Test("Windows instance lock is released on clean exit", () =>
{
    if (!OperatingSystem.IsWindows()) return;
    var name="Local\\Sukun.Test."+Guid.NewGuid().ToString("N");
    using(var first=new SingleInstance(name)) Assert(first.IsPrimary);
    using var next=new SingleInstance(name); Assert(next.IsPrimary);
});
AsyncTest("Concurrent saves remain atomic and preserve session preference", async () => { var root=Path.Combine(Environment.CurrentDirectory,"artifacts","storage-tests",Guid.NewGuid().ToString("N")); var store=new LocalStore(root); var s=new Settings {PlaceId="9206",PreferredApp="chrome",PreferredSessionId="abc"}; await Task.WhenAll(Enumerable.Range(0,12).Select(_=>store.SaveSettingsAsync(s))); Assert(store.LoadSettings().PreferredSessionId=="abc" && Directory.GetFiles(root,"*.tmp").Length==0); });
Test("Local diagnostics omit media titles and executable paths", () => { var root=Path.Combine(Environment.CurrentDirectory,"artifacts","storage-tests",Guid.NewGuid().ToString("N")); var log=new LocalDiagnostics(root,new TestClock(now)); log.Media([new("session123","C:/private/user/player.exe","private song",Playback.Playing,true,false)]); var content=File.ReadAllText(Path.Combine(log.DirectoryPath,"diagnostics.log")); Assert(!content.Contains("private") && !content.Contains("song") && content.Contains("other")); });
Test("Diagnostics rotate into only two bounded files", () => { var root=Path.Combine(Environment.CurrentDirectory,"artifacts","storage-tests",Guid.NewGuid().ToString("N")); var log=new LocalDiagnostics(root,new TestClock(now)); for(var i=0;i<600;i++) log.Write("test",new string('x',1000)); Assert(Directory.GetFiles(log.DirectoryPath).Length==2); Assert(Directory.GetFiles(log.DirectoryPath).All(p=>new FileInfo(p).Length<260000)); });

Test("Popup placement uses correct units at 100, 125 and 150 percent DPI", () =>
{
    foreach(var scale in new[]{1d,1.25,1.5})
    {
        var below=PopupGeometry.Place(200*scale,224*scale,34*scale,-18,-311,450,311,scale,scale);
        Assert(Math.Abs(below.Y/scale-37)<.001 && Math.Abs(below.X)<.001);
        var above=PopupGeometry.Place(400*scale,224*scale,34*scale,-18,-440,450,180,scale,scale);
        Assert(Math.Abs(above.Y/scale+227)<.001);
        Assert(above.Y/scale>=-440 && above.Y/scale+224<=180);
    }
});

Test("Prayer countdown and next enabled automation are independent", () =>
{
    var time=parsed.Days[new DateOnly(2026,10,18)].At(Prayer.Asr).AddMinutes(1);
    var settings=new Settings { PlaceId="9206",Enabled=true,Prayers=[Prayer.Isha] };
    var overview=PrayerOverview.Build(parsed,settings,time);
    Assert(overview.NextPrayer==Prayer.Maghrib && overview.NextAutomaticBreak?.Prayer==Prayer.Isha);
    Assert(overview.NextAt>time && overview.NextAutomaticBreak!.Start>overview.NextAt);
});
Test("Disabled automation exposes prayer countdown without a planned automatic break", () =>
{
    var overview=PrayerOverview.Build(parsed,new Settings {PlaceId="9206",Enabled=false},now);
    Assert(overview.NextAt>now && overview.NextAutomaticBreak is null);
});
Test("Prayer overview uses Turkey time and advances after midnight", () =>
{
    var time=new DateTimeOffset(2026,10,18,21,30,0,TimeSpan.Zero);
    var settings=new Settings {PlaceId="9206",Enabled=true};
    var overview=PrayerOverview.Build(parsed,settings,time);
    Assert(overview.NextPrayer==Prayer.Fajr && Timetable.TurkeyDate(overview.NextAt!.Value)==new DateOnly(2026,10,19));
    Assert(overview==PrayerOverview.Build(parsed,settings,time.ToOffset(TimeSpan.FromHours(-7))));
});
Test("Wrong location cannot produce an overview", () => Assert(PrayerOverview.Build(parsed,new Settings {PlaceId="9651"},now).NextAt is null));
AsyncTest("Application preference follows a reopened app for future pauses", async () =>
{
    var(m,c)=await Ready();
    m.States.Clear();
    await c.TickAsync(now.AddSeconds(10),true,windows,"spotify");
    m.States.Add(new("reopened","spotify","track",Playback.Playing,true,false));
    await c.TickAsync(start,true,windows,"spotify");
    Assert(m.Pauses==1 && m.States.Single().Playback==Playback.Paused);
});

Test("Stable release replaces the beta with the same numeric version", () =>
{
    var release = new SoftwareRelease(new Version(0,1,0), "v0.1.0", new Uri("https://github.com/TalhaY61/sukun/releases/tag/v0.1.0"));
    Assert(release.IsNewerThan(new Version(0,1,0), true));
    Assert(!release.IsNewerThan(new Version(0,1,0), false));
});
Test("Live localization changes dynamic media labels without changing target IDs", () =>
{
    var original=L.Language;
    try
    {
        var settings=new Settings();
        var states=new MediaState[]{new("playing","music","song",Playback.Playing,true,false)};
        L.Apply("tr"); var tr=MediaChoices.Build(states,settings,_=>"YT Music").First();
        L.Apply("en"); var en=MediaChoices.Build(states,settings,_=>"YT Music").First();
        Assert(tr.Label=="Otomatik · YT Music" && en.Label=="Automatic · YT Music");
        Assert(tr.Key==en.Key && MediaChoices.Target(settings) is null);
        Assert(L.Time(new DateTimeOffset(2026,10,18,12,0,0,TimeSpan.Zero))=="15:00");
        Assert(L.Normalize("de-DE")=="en" && L.Normalize("tr-TR")=="tr");
    }
    finally {L.Apply(original);}
});
Test("Language preference survives a settings roundtrip and old settings remain compatible", () =>
{
    var saved=System.Text.Json.JsonSerializer.Serialize(new Settings{Language="en",PlaceId="9206",Enabled=true});
    var settings=System.Text.Json.JsonSerializer.Deserialize<Settings>(saved)!; settings.Validate();
    Assert(settings.Language=="en" && settings.Enabled && settings.PlaceId=="9206");
    Assert(System.Text.Json.JsonSerializer.Deserialize<Settings>("{}")!.Language is null);
});

Test("Full-width dropdowns share the exact input edges at each DPI", () =>
{
    foreach (var scale in new[]{1d,1.25,1.5})
    {
        var p=PopupGeometry.Place(400*scale,100*scale,32*scale,0,-100,400,300,scale,scale);
        Assert(Math.Abs(p.X)<.001);
        var right=PopupGeometry.Place(84*scale,100*scale,30*scale,-316,-100,84,300,scale,scale);
        Assert(Math.Abs(right.X)<.001);
    }
});
Test("Prayer fixture excludes third-party credentials and external scripts", () =>
{
    Assert(!System.Text.RegularExpressions.Regex.IsMatch(html, "AIza" + "[0-9A-Za-z_-]{35}"));
    Assert(!System.Text.RegularExpressions.Regex.IsMatch(html, @"https?://|\bsrc\s*=", System.Text.RegularExpressions.RegexOptions.IgnoreCase));
});
string PublishedUpdate(string tag, bool preview = false, bool draft = false, string? assetUrl = null, bool hasAsset = true, bool published = true, string? page = null) =>
    System.Text.Json.JsonSerializer.Serialize(new {
        tag_name = tag, html_url = page ?? "https://github.com/TalhaY61/sukun/releases/tag/" + Uri.EscapeDataString(tag),
        prerelease = preview, draft, published_at = published ? "2026-10-18T12:00:00Z" : null,
        assets = hasAsset ? new[] { new { name = "Sukun-win-x64.exe", state = "uploaded", size = 12345,
            browser_download_url = assetUrl ?? "https://github.com/TalhaY61/sukun/releases/download/" + Uri.EscapeDataString(tag) + "/Sukun-win-x64.exe" } } : []
    });
string UpdateFeed(params string[] items) => "[" + string.Join(',', items) + "]";

Test("Prerelease numbers compare numerically and release candidates precede stable", () =>
{
    Assert(UpdateVersion.Parse("0.1.0-beta.10")!.CompareTo(UpdateVersion.Parse("0.1.0-beta.9")) > 0);
    Assert(UpdateVersion.Parse("0.1.0-rc.1")!.CompareTo(UpdateVersion.Parse("0.1.0-beta.99")) > 0);
    Assert(UpdateVersion.Parse("0.1.0")!.CompareTo(UpdateVersion.Parse("0.1.0-rc.1")) > 0);
    Assert(UpdateVersion.Parse("v0.1.0-beta.3+new")!.CompareTo(UpdateVersion.Parse("0.1.0-beta.3+old")) == 0);
});
Test("Malformed and ambiguous version strings are rejected", () =>
{
    foreach (var tag in new[] {"0.1", "01.1.0", "0.1.0.1", "0.1.0-beta.01", "0.1.0-", "0.1.0+", "0.1.0-hello_world", new string('1', 101)})
        Assert(UpdateVersion.Parse(tag) is null, tag);
    Reject(() => SoftwareRelease.Parse(ReleaseJson("v0.1.0-beta.4", preview: false), repository, true));
});
Test("Beta installations discover later beta releases and stable of the same base version", () =>
{
    var beta = PublishedUpdate("v0.1.0-beta.4", true);
    Assert(SoftwareRelease.FindUpdate(UpdateFeed(beta), repository, "0.1.0-beta.3")?.Tag == "v0.1.0-beta.4");
    Assert(SoftwareRelease.FindUpdate(UpdateFeed(beta, PublishedUpdate("v0.1.0")), repository, "0.1.0-beta.3")?.Tag == "v0.1.0");
});
Test("Stable installations never move to a prerelease channel", () =>
{
    Assert(SoftwareRelease.FindUpdate(UpdateFeed(PublishedUpdate("v9.0.0-beta.1", true)), repository, "0.1.0") is null);
    Assert(SoftwareRelease.FindUpdate(UpdateFeed(PublishedUpdate("v9.0.0-beta.1", true), PublishedUpdate("v0.1.1")), repository, "0.1.0")?.Tag == "v0.1.1");
});
Test("Releases are ordered by version rather than feed order or lexical beta suffix", () =>
{
    var result = SoftwareRelease.FindUpdate(UpdateFeed(PublishedUpdate("v0.1.0-beta.9", true), PublishedUpdate("v0.1.0-beta.10", true), PublishedUpdate("v0.1.0-beta.4", true)), repository, "0.1.0-beta.3");
    Assert(result?.Tag == "v0.1.0-beta.10");
});
Test("Updates cannot downgrade or reoffer an already installed version", () =>
{
    Assert(SoftwareRelease.FindUpdate(UpdateFeed(PublishedUpdate("v0.1.9")), repository, "0.2.0-beta.1") is null);
    Assert(SoftwareRelease.FindUpdate(UpdateFeed(PublishedUpdate("v0.1.0-beta.3+new", true)), repository, "0.1.0-beta.3+old") is null);
    Assert(SoftwareRelease.FindUpdate(PublishedUpdate("v0.1.0"), repository, "0.1.0") is null);
});
Test("Drafts, unpublished releases and releases missing an EXE are never offered", () =>
{
    var json = UpdateFeed(PublishedUpdate("v0.2.0", draft: true), PublishedUpdate("v0.3.0", published: false), PublishedUpdate("v0.4.0", hasAsset: false));
    Assert(SoftwareRelease.FindUpdate(json, repository, "0.1.0") is null);
});
Test("Foreign download URLs and release pages are ignored without hiding a valid update", () =>
{
    foreach (var url in new[] {"https://example.com/Sukun-win-x64.exe", "http://github.com/TalhaY61/sukun/releases/download/v0.2.0/Sukun-win-x64.exe", "https://github.com/other/repo/releases/download/v0.2.0/Sukun-win-x64.exe", "https://github.com/TalhaY61/sukun/releases/download/v0.2.0/Sukun-win-x64.exe?redirect=1", "https://github.com:444/TalhaY61/sukun/releases/download/v0.2.0/Sukun-win-x64.exe"})
        Assert(SoftwareRelease.FindUpdate(PublishedUpdate("v0.2.0", assetUrl: url), repository, "0.1.0") is null);
    var json = UpdateFeed(PublishedUpdate("v8.0.0", page: "https://example.com/release"), "{\"tag_name\":false}", PublishedUpdate("v0.2.0"));
    Assert(SoftwareRelease.FindUpdate(json, repository, "0.1.0")?.Tag == "v0.2.0");
});
AsyncTest("HTTP update client uses the beta feed without credentials and stable uses latest", async () =>
{
    using var betaHandler = new StubReleaseHandler(_ => new(System.Net.HttpStatusCode.OK) { Content = new System.Net.Http.StringContent(UpdateFeed(PublishedUpdate("v0.1.0-beta.4", true))) });
    using var betaClient = new SoftwareUpdateClient(betaHandler);
    Assert((await betaClient.CheckAsync(repository, "0.1.0-beta.3"))?.Tag == "v0.1.0-beta.4");
    Assert(betaHandler.LastUri == repository.ReleasesApi && !betaHandler.HadAuthorization && betaHandler.Calls == 1);
    using var stableHandler = new StubReleaseHandler(_ => new(System.Net.HttpStatusCode.OK) { Content = new System.Net.Http.StringContent(PublishedUpdate("v0.1.1")) });
    using var stableClient = new SoftwareUpdateClient(stableHandler);
    Assert((await stableClient.CheckAsync(repository, "0.1.0"))?.Tag == "v0.1.1");
    Assert(stableHandler.LastUri == repository.LatestReleaseApi && !stableHandler.HadAuthorization);
});
AsyncTest("A repository without a matching published release does not invent an update", async () =>
{
    using var handler = new StubReleaseHandler(_ => new(System.Net.HttpStatusCode.NotFound));
    using var client = new SoftwareUpdateClient(handler);
    Assert(await client.CheckAsync(repository, "0.1.0") is null);
});
AsyncTest("Rate limits and network errors do not become successful update results", async () =>
{
    foreach (var status in new[] {System.Net.HttpStatusCode.Forbidden, System.Net.HttpStatusCode.TooManyRequests})
    {
        using var handler = new StubReleaseHandler(_ => new(status)); using var client = new SoftwareUpdateClient(handler);
        var rejected=false; try {await client.CheckAsync(repository, "0.1.0");} catch(InvalidDataException){rejected=true;}
        Assert(rejected && handler.Calls==1);
    }
    using var offline = new StubReleaseHandler(_ => throw new System.Net.Http.HttpRequestException("offline"));
    using var offlineClient = new SoftwareUpdateClient(offline);
    var failed=false; try {await offlineClient.CheckAsync(repository, "0.1.0-beta.3");} catch(System.Net.Http.HttpRequestException){failed=true;}
    Assert(failed && offline.Calls==1);
});
Test("Update scheduling checks once daily and recovers from a wall clock rewind", () =>
{
    Assert(SoftwareUpdateSchedule.IsDue(now, null));
    Assert(!SoftwareUpdateSchedule.IsDue(now.AddHours(23), now));
    Assert(SoftwareUpdateSchedule.IsDue(now.AddDays(1), now));
    Assert(SoftwareUpdateSchedule.IsDue(now.AddHours(-2), now));
    Assert(!SoftwareUpdateSchedule.IsDue(now.AddSeconds(-10), now));
});
Test("Update notification waits for a safe interval and stays opt-in", () =>
{
    var release=SoftwareRelease.Parse(ReleaseJson(), repository);
    var settings=new Settings();
    Assert(!SoftwareUpdateSchedule.ShouldNotify(settings, release, false));
    settings.AutoCheckSoftwareUpdates=true;
    Assert(!SoftwareUpdateSchedule.ShouldNotify(settings, release, true));
    Assert(SoftwareUpdateSchedule.ShouldNotify(settings, release, false));
    settings.LastNotifiedSoftwareVersion=release.Tag;
    Assert(!SoftwareUpdateSchedule.ShouldNotify(settings, release, false));
    Assert(!SoftwareUpdateSchedule.ShouldNotify(settings, null, false));
});
AsyncTest("Notified update version persists across restart without changing automation choices", async () =>
{
    var root=Path.Combine(Environment.CurrentDirectory,"artifacts","update-settings-tests",Guid.NewGuid().ToString("N"));
    var store=new LocalStore(root);
    var settings=new Settings{AutoCheckSoftwareUpdates=true,LastNotifiedSoftwareVersion="v0.2.0",Enabled=true,PlaceId="9206",PreferredApp="player"};
    await store.SaveSettingsAsync(settings); var loaded=store.LoadSettings();
    Assert(loaded.Enabled && loaded.PreferredApp=="player" && loaded.LastNotifiedSoftwareVersion=="v0.2.0");
    Assert(!SoftwareUpdateSchedule.ShouldNotify(loaded,SoftwareRelease.Parse(ReleaseJson(),repository),false));
    Assert(System.Text.Json.JsonSerializer.Deserialize<Settings>("{\"AutoCheckSoftwareUpdates\":false}")!.LastNotifiedSoftwareVersion is null);
});
AsyncTest("Idle and disabled automation do not poll Windows media sessions", async () =>
{
    var(m,c)=await Ready();
    for(var i=1;i<20;i++) await c.TickAsync(now.AddSeconds(i),true,windows,null);
    await c.TickAsync(now.AddSeconds(20),false,windows,null);
    Assert(m.Reads==0 && m.Pauses==0 && m.Plays==0);
});
Test("Update prompt and tray notice resources exist in both supported languages", () =>
{
    foreach(var lang in new[]{"en","tr"})
    foreach(var key in new[]{"UpdateNotificationTitle","UpdateNotificationBody","UpdateQuestion","UpdateAfterBreak","InvalidUpdateResponse"})
        Assert(L.ForLanguage(key,lang)!=key);
});

Test("Fresh installations have no preset city and cannot schedule Ankara by accident", () =>
{
    var fresh = new Settings();
    Assert(fresh.PlaceId is null && !fresh.Enabled && fresh.Language is null);
    Assert(BreakWindow.Build(parsed, fresh, now).Count == 0);
    var path=Path.Combine(Environment.CurrentDirectory,"artifacts","first-run-tests",Guid.NewGuid().ToString("N"));
    var loaded=new LocalStore(path).LoadSettings();
    Assert(loaded.PlaceId is null && !loaded.Enabled);
    foreach(var language in new[]{"en","tr"})
        Assert(L.ForLanguage("ChooseLocationAction",language)!="ChooseLocationAction");
});
var failed = 0;
foreach (var (name, run) in tests)
{
    try { await run(); Console.WriteLine($"PASS {name}"); }
    catch (Exception e) { failed++; Console.Error.WriteLine($"FAIL {name}: {e}"); }
}
Console.WriteLine($"{tests.Count - failed}/{tests.Count} passed");
return failed == 0 ? 0 : 1;

sealed class FakeMedia : IMediaControl
{
    public List<MediaState> States { get; set; } = [];
    public int Pauses, Plays, Reads;
    public bool PauseSucceeds=true, ChangeOnPause=true, PlaySucceeds=true, ChangeOnPlay=true, ThrowOnRead;
    public Task<IReadOnlyList<MediaState>> ReadAsync() { Reads++; if(ThrowOnRead) throw new System.Runtime.InteropServices.COMException("test"); return Task.FromResult<IReadOnlyList<MediaState>>(States.ToArray()); }
    public Task<bool> PauseAsync(string id) { Pauses++; if(PauseSucceeds && ChangeOnPause) Change(id,Playback.Paused); return Task.FromResult(PauseSucceeds); }
    public Task<bool> PlayAsync(string id) { Plays++; if(PlaySucceeds && ChangeOnPlay) Change(id,Playback.Playing); return Task.FromResult(PlaySucceeds); }
    private void Change(string id,Playback state) { var i=States.FindIndex(s=>s.SessionId==id); if(i>=0) States[i]=States[i] with {Playback=state}; }
}

sealed class TestClock(DateTimeOffset start) : TimeProvider
{
    private DateTimeOffset wall=start;
    private long stamp;
    public override DateTimeOffset GetUtcNow()=>wall;
    public override long TimestampFrequency=>TimeSpan.TicksPerSecond;
    public override long GetTimestamp()=>stamp;
    public void Advance(TimeSpan elapsed) { wall+=elapsed; stamp+=elapsed.Ticks; }
    public void ShiftWall(TimeSpan difference)=>wall+=difference;
}

sealed class StubReleaseHandler(Func<System.Net.Http.HttpRequestMessage, System.Net.Http.HttpResponseMessage> respond) : System.Net.Http.HttpMessageHandler
{
    public Uri? LastUri;
    public bool HadAuthorization;
    public int Calls;
    protected override Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Calls++; LastUri=request.RequestUri; HadAuthorization=request.Headers.Authorization is not null;
        return Task.FromResult(respond(request));
    }
}
