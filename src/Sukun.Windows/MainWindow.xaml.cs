using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Win32;
using Sukun.Core;
using Forms = System.Windows.Forms;

namespace Sukun.Windows;

public partial class MainWindow : Window
{
    private readonly LocalStore store;
    private readonly Settings settings;
    private readonly TimeProvider clock = TimeProvider.System;
    private readonly ClockMonitor clockMonitor;
    private readonly LocalDiagnostics log;
    private readonly WindowsMedia media = new();
    private readonly DiyanetClient diyanet = new();
    private readonly SoftwareUpdateClient softwareUpdates = new();
    private bool checkingSoftware;
    private SoftwareRelease? availableRelease;
    private readonly BreakController controller;
    private BreakController? testController;
    private BreakWindow? testWindow;
    private bool testPauseConfirmed;
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly DispatcherTimer timer = new();
    private readonly Forms.NotifyIcon tray;
    private System.Drawing.Icon brandIcon;
    private readonly List<Place> places;
    private Timetable? table;
    private bool loading = true, exiting, mediaReady, editingLocation;
    private CancellationTokenSource? locationFetch;
    private string? downloadingId;
    private string lastSchedule = "";
    private readonly bool smoke;
    private IReadOnlyList<Sukun.Core.MediaState> lastMediaStates = [];
    private Func<string>? notice;
    private readonly List<Func<string>> testLines = [];
    private bool softwareChecked;
    private sealed record LanguageChoice(string Id, string Label) { public override string ToString() => Label; }
    private DateTimeOffset Now => clock.GetUtcNow();
    private sealed record Choice(int Value, string Label) { public override string ToString() => Label; }
    private sealed record ThemeChoice(string Id, string Label) { public override string ToString() => Label; }

    public MainWindow(bool smoke)
    {
        this.smoke = smoke;
        store = new LocalStore(smoke ? Path.Combine(Path.GetTempPath(), "Sukun-smoke") : null);
        settings = store.LoadSettings();
        L.Apply(settings.Language);
        ApplyTextResources();
        if (smoke) settings.Enabled = false;
        log = new LocalDiagnostics(store.Root, clock);
        clockMonitor = new ClockMonitor(clock);
        media.Log = log;
        controller = CreateController();
        InitializeComponent();
#if DEBUG
        DeveloperTools.Visibility = Visibility.Visible;
#endif
        brandIcon = BrandIcon.Create();
        Icon = BrandIcon.WindowImage();
        ThemeManager.Apply(settings.Theme, this);
        SourceInitialized += (_, _) => ThemeManager.Apply(settings.Theme, this);
        MaxHeight = SystemParameters.WorkArea.Height - 20;
        MinHeight = Math.Min(MinHeight, MaxHeight);
        MaxWidth = SystemParameters.WorkArea.Width - 20;
        MinWidth = Math.Min(MinWidth, MaxWidth);
        Width = Math.Min(Width, MaxWidth); Height = Math.Min(Height, MaxHeight);
        var resource = System.Windows.Application.GetResourceStream(new Uri("/Assets/places.json", UriKind.Relative))!;
        places = JsonSerializer.Deserialize<List<Place>>(resource.Stream) ?? [];
        ProvinceBox.ItemsSource = places.Select(p => p.Province).Distinct().OrderBy(s => s, StringComparer.Create(new System.Globalization.CultureInfo("tr-TR"), false)).ToArray();
        foreach (var box in PrayerBoxes()) box.IsChecked = settings.Prayers.Contains(Enum.Parse<Prayer>((string)box.Tag));
        UpdateCheck.IsChecked = settings.AutoRefresh;
        SoftwareUpdatesCheck.IsChecked = settings.AutoCheckSoftwareUpdates;
        SoftwareUpdateButton.ToolTip = L.Get("InstalledVersionHint", ReleaseSource.VersionLabel);
        StartupCheck.IsChecked = !smoke && StartupEnabled();
        var place = places.FirstOrDefault(p => p.Id == settings.PlaceId);
        if (place is not null)
        {
            ProvinceBox.SelectedItem = place.Province;
            PlaceBox.ItemsSource = ProvincePlaces(place.Province).ToArray();
            PlaceBox.SelectedItem = place;
        }
        PlayerBox.ItemsSource = MediaChoices.Build([], settings, WindowsMedia.DisplayName);
        PlayerBox.SelectedItem = ((IEnumerable<MediaChoice>)PlayerBox.ItemsSource).FirstOrDefault(p => p.Key == MediaChoices.SelectedKey(settings));
        tray = new Forms.NotifyIcon { Text = "Sükûn", Icon = brandIcon, Visible = !smoke };
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add(L.Get("OpenApp"), null, (_, _) => Dispatcher.Invoke(ShowApp));
        menu.Items.Add(L.Get("CancelResume"), null, (_, _) => Dispatcher.Invoke(() => CancelResume(this, new RoutedEventArgs())));
        menu.Items.Add(L.Get("Quit"), null, (_, _) => Dispatcher.Invoke(ExitApp));
        tray.ContextMenuStrip = menu;
        tray.MouseClick += (_, e) => { if (e.Button == Forms.MouseButtons.Left) Dispatcher.Invoke(() => { if (IsVisible) Hide(); else ShowApp(); }); };
        timer.Tick += async (_, _) => await TickAsync();
        IsVisibleChanged += (_, _) => { if (!loading) Wake(); };
        SystemEvents.PowerModeChanged += PowerChanged;
        SystemEvents.TimeChanged += TimeChanged;
        SystemEvents.UserPreferenceChanged += SystemThemeChanged;
        media.Changed += () => Dispatcher.BeginInvoke(Wake);
        RefreshLocalizedControls();
        Loaded += (_, _) => PositionNearTray();
    }

    private BreakController CreateController()
    {
        var instance = new BreakController(media);
        instance.Diagnostic += d => log.Write(d.Code, new { d.Detail, session = d.SessionId is null ? null : d.SessionId[..Math.Min(8, d.SessionId.Length)], state = d.State?.ToString() });
        return instance;
    }
    public async void Start()
    {
        try
        {
            table = await store.LoadTableAsync(settings.PlaceId);
            try { await media.InitializeAsync(); mediaReady = true; }
            catch (Exception e) { log.Error("media-initialize", e); Notice(() => L.Get("MediaInitFailed")); }
            loading = false;
            if (settings.Enabled && ReadinessReason() is not null) { settings.Enabled = false; if (!smoke) await store.SaveSettingsAsync(settings); }
            log.Write("started", new { settings.PlaceId, settings.Enabled, provider = "Diyanet", calculationZone = "UTC+03:00" });
            if (store.Warning is not null) Notice(() => store.Warning ?? "");
            clockMonitor.Reset();
            await TickAsync();
        }
        catch (Exception e) { loading = false; settings.Enabled = false; log.Error("startup-failed", e); Notice(() => Friendly(e)); }
        finally { Wake(); }
    }
    private void Wake() { if (!exiting && !loading) { timer.Interval = TimeSpan.FromMilliseconds(100); timer.Start(); } }
    private string? ReadinessReason() => smoke ? L.Get("PreviewMediaOff") : !mediaReady ? L.Get("MediaNotReady")
        : settings.PlaceId is null ? L.Get("SelectLocationFirst")
        : table?.PlaceId != settings.PlaceId || table.ContiguousDays(Timetable.TurkeyDate(Now)) == 0 ? L.Get("RefreshToday")
        : settings.Prayers.Count == 0 ? L.Get("SelectOnePrayer") : null;
    private async Task TickAsync()
    {
        if (loading || exiting || !await gate.WaitAsync(0)) return;
        timer.Stop();
        try
        {
            var now = Now;
            var windows = BreakWindow.Build(table, settings, now);
            if (clockMonitor.Observe()) Rebase(now, windows);
            if (settings.Enabled && ReadinessReason() is { } reason)
            { settings.Enabled = false; controller.Cancel(() => ReadinessReason() ?? L.Get("AutomationOff")); Notice(() => ReadinessReason() ?? L.Get("AutomationOff")); if (!smoke) await store.SaveSettingsAsync(settings); }
            if (testController is not null) await TickTestAsync(now);
            else await controller.TickAsync(now, !smoke && settings.Enabled, windows, MediaChoices.Target(settings));
            if (IsVisible && !PlayerBox.IsDropDownOpen && mediaReady) await RefreshPlayersAsync();
            Render(now, windows);
            timer.Interval = testController is not null ? TimeSpan.FromSeconds(1) : ScheduleWakeup.Delay(now, windows, controller.IsActive, IsVisible);
        }
        catch (Exception e) { controller.Cancel(() => L.Get("MediaInterrupted")); StopTest(() => L.Get("TrialInterrupted")); log.Error("tick-failed", e); Notice(() => Friendly(e)); timer.Interval = TimeSpan.FromSeconds(15); }
        finally { gate.Release(); if (!exiting) timer.Start(); }
        if (!smoke && settings.AutoRefresh && settings.PlaceId is { } id && CanRefreshAutomatically(id) &&
            (table is null || Now - table.FetchedAt > TimeSpan.FromDays(30) || table.ContiguousDays(Timetable.TurkeyDate(Now)) < 14)) _ = RefreshAsync(false);
        if (!smoke && settings.AutoCheckSoftwareUpdates && !checkingSoftware &&
            (!settings.LastSoftwareCheckAttempt.HasValue || Now - settings.LastSoftwareCheckAttempt.Value >= TimeSpan.FromDays(1))) _ = CheckSoftwareAsync(false);
    }
    private void Render(DateTimeOffset now, IReadOnlyList<BreakWindow> windows)
    {
        var today = Timetable.TurkeyDate(now);
        var overview = PrayerOverview.Build(table, settings, now);
        var hasNext = overview.NextPrayer.HasValue;
        var nextBreak = overview.NextAutomaticBreak;
        StatusLabel.Text = controller.IsActive ? L.Get("OnBreak") : settings.Enabled ? L.Get("Active") : L.Get("Off");
        StatusDot.SetResourceReference(System.Windows.Shapes.Shape.FillProperty, settings.Enabled ? "Accent" : "Muted");
        AutomationToggle.IsChecked = settings.Enabled;
        AutomationToggle.IsEnabled = settings.Enabled || ReadinessReason() is null;
        AutomationToggle.ToolTip = ReadinessReason() ?? L.Get("AutomationHint");
        System.Windows.Controls.ToolTipService.SetShowOnDisabled(AutomationToggle, true);
        NextPrayerText.Text = hasNext ? L.Get("NextPrayerValue", L.Prayer(overview.NextPrayer!.Value), L.Time(overview.NextAt!.Value)) : settings.PlaceId is null ? L.Get("ChooseLocationTitle") : L.Get("WaitingTimes");
        var remaining = overview.NextAt - now;
        NextTimeText.Text = remaining.HasValue ? L.Duration(remaining.Value) : "—";
        StatusDetail.Text = controller.IsActive ? controller.Message : !settings.Enabled ? L.Get("AutomationOff") : ReadinessReason() ?? (nextBreak is not null
            ? L.Get("NextBreakValue", L.Prayer(nextBreak.Prayer), L.Time(nextBreak.Start, true)) : L.Get("WaitingBreak"));
        CancelButton.Visibility = controller.IsActive ? Visibility.Visible : Visibility.Collapsed;
        var count = table?.ContiguousDays(today) ?? 0;
        CoverageText.Text = downloadingId == settings.PlaceId && downloadingId is not null ? L.Get("DownloadingTimes") : count > 0 ? L.Get(count == 1 ? "OfflineDay" : "OfflineDays", count) : settings.PlaceId is null ? L.Get("ProvinceLoadsCenter") : L.Get("NoTimesToday");
        CoverageText.ToolTip = count > 0 ? L.Get("OfflineUntil", today.AddDays(count - 1).ToString("d", L.Culture)) : L.Get("DistrictExplanation");
        RefreshButton.IsEnabled = downloadingId != settings.PlaceId || downloadingId is null;
        var selectedSource = (PlayerBox.ItemsSource as IEnumerable<MediaChoice>)?.FirstOrDefault(p => p.Key == MediaChoices.SelectedKey(settings));
        var unavailableSource = selectedSource is null && MediaChoices.SelectedKey(settings) != "auto";
        PlayerBox.Tag = unavailableSource ? L.Get("SourceClosed", WindowsMedia.DisplayName(settings.PreferredApp ?? "")) : L.Get("SearchMedia");
        PlayerBox.ToolTip = unavailableSource ? L.Get("SourceUnavailableHint") : selectedSource?.Tooltip;
        PlayerDetail.Text = unavailableSource ? settings.PreferredSessionId is null ? L.Get("AppWillReconnect") : L.Get("SessionClosedHint") : (PlayerBox.ItemsSource as IEnumerable<MediaChoice>)?.Count() == 1 ? L.Get("NoMediaHint") : controller.Outcome is BreakOutcome.Failed or BreakOutcome.Skipped ? controller.Message : settings.PreferredSessionId is not null
            ? L.Get("PinnedSessionHint") : settings.PreferredApp is not null ? L.Get("AppSourceHint") : L.Get("AutoSourceHint");
        TodayLabel.Text = today.ToString("dd MMMM", L.Culture);
        var times = table?.Days.GetValueOrDefault(today)?.Clocks;
        var labels = new[] { FajrTime, SunriseTime, DhuhrTime, AsrTime, MaghribTime, IshaTime };
        for (var i = 0; i < labels.Length; i++) labels[i].Text = times?[i] ?? "--:--";
        foreach (var box in PrayerBoxes())
        {
            box.IsEnabled = times is not null;
            CardState.SetIsNext(box, hasNext && Timetable.TurkeyDate(overview.NextAt!.Value) == today && overview.NextPrayer == Enum.Parse<Prayer>((string)box.Tag));
        }
        var place = places.FirstOrDefault(p => p.Id == settings.PlaceId);
        var compact = place is not null && count > 0 && !editingLocation;
        LocationEditor.Visibility = compact ? Visibility.Collapsed : Visibility.Visible;
        LocationSummary.Visibility = compact ? Visibility.Visible : Visibility.Collapsed;
        LocationSummary.Text = place is null ? "" : $"{place.Province} · {place.DisplayName}";
        ChangeLocationButton.Visibility = compact ? Visibility.Visible : Visibility.Collapsed;
        tray.Text = settings.Enabled ? L.Get("TrayActive") : L.Get("TrayOff");
        TestMediaButton.IsEnabled = !smoke && mediaReady && testController is null && !controller.IsActive;
        var schedule = $"{settings.PlaceId}|{settings.Enabled}|{nextBreak?.Start:O}|{controller.ActiveUntil:O}|{TimeZoneInfo.Local.Id}";
        if (schedule != lastSchedule) { lastSchedule = schedule; log.Write("schedule", new { settings.PlaceId, settings.Enabled, provider = "Diyanet", calculationZone = "UTC+03:00", next = nextBreak?.Start, activeUntil = controller.ActiveUntil }); }
    }
    private async Task RefreshPlayersAsync()
    {
        lastMediaStates = await media.ReadAsync();
        var items = MediaChoices.Build(lastMediaStates, settings, WindowsMedia.DisplayName);
        var current = (PlayerBox.ItemsSource as IEnumerable<MediaChoice>)?.ToArray() ?? [];
        if (current.SequenceEqual(items)) return;
        var wasLoading = loading; loading = true;
        try { PlayerBox.ItemsSource = items; PlayerBox.SelectedItem = items.FirstOrDefault(p => p.Key == MediaChoices.SelectedKey(settings)); }
        finally { loading = wasLoading; }
    }
    private async Task ChangeAsync(Action change)
    {
        if (loading || exiting) return;
        await gate.WaitAsync();
        try
        {
            controller.Cancel(() => L.Get("SettingsChanged"));
            StopTest(() => L.Get("SettingsTrialCancelled"));
            change();
            if (ReadinessReason() is not null) settings.Enabled = false;
            // A new preference must not retroactively start an interval already in progress.
            await controller.TickAsync(Now, false, BreakWindow.Build(table, settings, Now), MediaChoices.Target(settings));
            if (!smoke) await store.SaveSettingsAsync(settings);
            Render(Now, BreakWindow.Build(table, settings, Now));
        }
        catch (Exception e) { settings.Enabled = false; log.Error("settings-failed", e); Notice(() => Friendly(e)); Render(Now, BreakWindow.Build(table, settings, Now)); }
        finally { gate.Release(); }
        Wake();
    }
    private async Task RefreshAsync(bool manual)
    {
        if (settings.PlaceId is not { } id || downloadingId == id || (!manual && !CanRefreshAutomatically(id))) return;
        locationFetch?.Cancel();
        var fetch = locationFetch = new CancellationTokenSource();
        downloadingId = id;
        Render(Now, BreakWindow.Build(table, settings, Now));
        try
        {
            await gate.WaitAsync();
            try { settings.LastFetchAttempts[id] = Now; if (!smoke) await store.SaveSettingsAsync(settings); }
            finally { gate.Release(); }
            var incoming = await diyanet.DownloadAsync(id, Now, fetch.Token);
            await gate.WaitAsync();
            try
            {
                if (fetch.IsCancellationRequested || settings.PlaceId != id || exiting) return;
                var merged = DiyanetParser.Merge(table, incoming);
                await store.SaveTableAsync(merged); table = merged; editingLocation = false; Notice(null);
                log.Write("timetable-loaded", new { place = id, days = merged.Days.Count, provider = "Diyanet" });
            }
            finally { gate.Release(); }
        }
        catch (OperationCanceledException) when (fetch.IsCancellationRequested) { }
        catch (Exception e) { log.Error("timetable-failed", e); if (settings.PlaceId == id) Notice(() => L.Get("RefreshFailed", Friendly(e))); }
        finally
        {
            if (locationFetch == fetch) { downloadingId = null; locationFetch = null; }
            fetch.Dispose();
            if (!exiting) { Render(Now, BreakWindow.Build(table, settings, Now)); Wake(); }
        }
    }
    private bool CanRefreshAutomatically(string id) => downloadingId != id && (!settings.LastFetchAttempts.TryGetValue(id, out var attempted) || Now - attempted >= TimeSpan.FromDays(1));
    private IEnumerable<Place> ProvincePlaces(string province) => places.Where(p => p.Province == province).OrderByDescending(p => p.IsCenter).ThenBy(p => p.Name, StringComparer.Create(new System.Globalization.CultureInfo("tr-TR"), false));
    private IEnumerable<System.Windows.Controls.CheckBox> PrayerBoxes() => [FajrCheck, DhuhrCheck, AsrCheck, MaghribCheck, IshaCheck];
    private async void ProvinceChanged(object sender, SelectionChangedEventArgs e)
    {
        if (loading || ProvinceBox.IsFiltering || ProvinceBox.SelectedItem is not string province) return;
        if (places.FirstOrDefault(p => p.Id == settings.PlaceId)?.Province == province && PlaceBox.SelectedItem is Place current && current.Id == settings.PlaceId) return;
        var candidates = ProvincePlaces(province).ToArray();
        if (candidates.Length == 0) return;
        loading = true;
        PlaceBox.ItemsSource = candidates; PlaceBox.SelectedItem = candidates.FirstOrDefault(p => p.IsCenter) ?? candidates[0];
        loading = false;
        await SelectPlaceAsync((Place)PlaceBox.SelectedItem);
    }
    private async void PlaceChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!loading && !PlaceBox.IsFiltering && PlaceBox.SelectedItem is Place place) await SelectPlaceAsync(place);
    }
    private async Task SelectPlaceAsync(Place place)
    {
        if (settings.PlaceId == place.Id) return;
        locationFetch?.Cancel(); locationFetch = null; downloadingId = null;
        await ChangeAsync(() => { settings.PlaceId = place.Id; settings.Enabled = false; table = null; });
        var cached = await store.LoadTableAsync(place.Id);
        await gate.WaitAsync();
        try { if (settings.PlaceId != place.Id) return; table = cached; editingLocation = false; Render(Now, BreakWindow.Build(table, settings, Now)); }
        finally { gate.Release(); }
        // Explicit location selection is itself a request to obtain its missing data.
        if (!smoke && (cached is null || cached.ContiguousDays(Timetable.TurkeyDate(Now)) == 0)) await RefreshAsync(true);
        else Wake();
    }
    private void EditLocation(object sender, RoutedEventArgs e) { editingLocation = true; Render(Now, BreakWindow.Build(table, settings, Now)); ProvinceBox.Focus(); }
    private async void PlayerChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!loading && !PlayerBox.IsFiltering && PlayerBox.SelectedItem is MediaChoice choice && choice.Key != MediaChoices.SelectedKey(settings))
            await ChangeAsync(() => { settings.PreferredApp = choice.AppId; settings.PreferredSessionId = choice.SessionId; });
    }
    private async void TimingChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!loading && BeforeBox.SelectedItem is Choice before && AfterBox.SelectedItem is Choice after && (settings.BeforeSeconds != before.Value || settings.AfterMinutes != after.Value))
            await ChangeAsync(() => { settings.BeforeSeconds = before.Value; settings.AfterMinutes = after.Value; });
    }
    private async void PrayerChanged(object sender, RoutedEventArgs e) => await ChangeAsync(() => settings.Prayers = PrayerBoxes().Where(b => b.IsChecked == true).Select(b => Enum.Parse<Prayer>((string)b.Tag)).ToHashSet());
    private async void UpdateChanged(object sender, RoutedEventArgs e)
    {
        if (loading) return;
        try { settings.AutoRefresh = UpdateCheck.IsChecked == true; if (!smoke) await store.SaveSettingsAsync(settings); }
        catch (Exception error) { log.Error("settings-save", error); Notice(() => Friendly(error)); }
    }
    private async void ToggleAutomation(object sender, RoutedEventArgs e)
    {
        var desired = AutomationToggle.IsChecked == true;
        if (desired && ReadinessReason() is not null) { AutomationToggle.IsChecked = settings.Enabled; Notice(() => ReadinessReason() ?? L.Get("AutomationOff")); return; }
        await ChangeAsync(() => settings.Enabled = desired);
    }
    private async void RefreshTimes(object sender, RoutedEventArgs e) { if (!smoke) await RefreshAsync(true); }
    private async void CancelResume(object sender, RoutedEventArgs e) => await ChangeAsync(() => { });
    private void PositionNearTray()
    {
        var workArea = SystemParameters.WorkArea;
        const double gap = 16;
        Left = Math.Max(workArea.Left, workArea.Right - Width - gap);
        Top = Math.Max(workArea.Top, workArea.Bottom - Height - gap);
    }
    internal void ShowApp() { if (exiting) return; Show(); WindowState = WindowState.Normal; PositionNearTray(); Activate(); Wake(); }
    protected override void OnClosing(CancelEventArgs e)
    {
        if (!exiting && smoke) ExitApp();
        else if (!exiting) { e.Cancel = true; Hide(); }
        base.OnClosing(e);
    }
    private void ExitApp()
    {
        if (exiting) return;
        exiting = true; timer.Stop(); locationFetch?.Cancel();
        controller.Cancel(() => L.Get("AppClosed")); StopTest(() => L.Get("AppClosed"));
        tray.Visible = false; tray.Dispose(); brandIcon.Dispose(); diyanet.Dispose(); media.Dispose(); softwareUpdates.Dispose();
        SystemEvents.PowerModeChanged -= PowerChanged; SystemEvents.TimeChanged -= TimeChanged; SystemEvents.UserPreferenceChanged -= SystemThemeChanged;
        System.Windows.Application.Current.Shutdown();
    }
    private void Rebase(DateTimeOffset now, IReadOnlyList<BreakWindow> windows)
    { controller.RebaseClock(now, windows); StopTest(() => L.Get("ClockTrialCancelled")); clockMonitor.Reset(); }
    private async Task RebaseAsync()
    {
        if (loading || exiting) return;
        await gate.WaitAsync();
        try { Rebase(Now, BreakWindow.Build(table, settings, Now)); Render(Now, BreakWindow.Build(table, settings, Now)); }
        finally { gate.Release(); }
        Wake();
    }
    private void PowerChanged(object sender, PowerModeChangedEventArgs e) => Dispatcher.BeginInvoke(async () => await RebaseAsync());
    private void TimeChanged(object? sender, EventArgs e) => Dispatcher.BeginInvoke(async () => { TimeZoneInfo.ClearCachedData(); await RebaseAsync(); });
    private void SystemThemeChanged(object sender, UserPreferenceChangedEventArgs e) => Dispatcher.BeginInvoke(() =>
    {
        if (exiting) return;
        if (settings.Theme == "System") ThemeManager.Apply(settings.Theme, this);
        var previousIcon = brandIcon;
        brandIcon = BrandIcon.Create(); tray.Icon = brandIcon; previousIcon.Dispose();
    });
    private async void ThemeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (loading || ThemeBox.SelectedItem is not ThemeChoice choice) return;
        await gate.WaitAsync();
        try { settings.Theme = choice.Id; ThemeManager.Apply(settings.Theme, this); if (!smoke) await store.SaveSettingsAsync(settings); }
        catch (Exception error) { log.Error("theme-save", error); Notice(() => Friendly(error)); }
        finally { gate.Release(); }
    }
    private async void ScanPlayers(object sender, RoutedEventArgs e)
    {
        await gate.WaitAsync();
        try { await RefreshPlayersAsync(); }
        catch (Exception error) { log.Error("media-scan", error); Notice(() => Friendly(error)); }
        finally { gate.Release(); }
    }
    private async void StartMediaTest(object sender, RoutedEventArgs e)
    {
        if (smoke || !mediaReady) return;
        await gate.WaitAsync();
        try
        {
            var now = Now;
            var windows = BreakWindow.Build(table, settings, now);
            if (testController is not null || controller.IsActive || (settings.Enabled && windows.Any(w => w.Start < now.AddSeconds(35) && w.End > now)))
            { Notice(() => L.Get("TrialConflict")); return; }
            testController = CreateController(); testPauseConfirmed = false;
            testWindow = new BreakWindow("media-test", Prayer.Dhuhr, now.AddSeconds(10), now.AddSeconds(20));
            await testController.TickAsync(now, true, [testWindow], MediaChoices.Target(settings));
            var states = await media.ReadAsync();
            log.Media(states);
            testLines.Clear();
            foreach (var state in states)
                testLines.Add(() => L.Get("TrialSession", WindowsMedia.DisplayName(state.AppId), L.Playback(state.Playback), L.Get(state.CanPause ? "Yes" : "No"), L.Get(state.CanPlay ? "Yes" : "No")));
            if (states.Count == 0) testLines.Add(() => L.Get("TrialNoSessions"));
            testLines.Add(() => L.Get("TrialWait"));
            RenderTestDetails();
            TestDetails.ScrollToEnd();
            TestDetails.Visibility = Visibility.Visible; CancelTestButton.Visibility = Visibility.Visible; TestMediaButton.IsEnabled = false;
        }
        catch (Exception error) { StopTest(() => L.Get("TrialStartFailed")); log.Error("test-start", error); Notice(() => Friendly(error)); }
        finally { gate.Release(); }
        Wake();
    }
    private async Task TickTestAsync(DateTimeOffset now)
    {
        if (testController is null || testWindow is null) return;
        await testController.TickAsync(now, true, [testWindow], MediaChoices.Target(settings));
        if (testController.Outcome == BreakOutcome.Paused && !testPauseConfirmed)
        {
            testPauseConfirmed = true;
            testWindow = testWindow with { End = Now.AddSeconds(10) };
            testLines.Add(() => L.Get("TrialPauseConfirmed")); RenderTestDetails();
            TestDetails.ScrollToEnd();
        }
        if (testController.Outcome is BreakOutcome.Failed or BreakOutcome.Skipped or BreakOutcome.Cancelled or BreakOutcome.Resumed)
        { var completed = testController; StopTest(() => completed.Message); }
    }
    private void StopTest(Func<string> reason)
    {
        if (testController is null) return;
        if (testController.IsActive || testController.Outcome == BreakOutcome.Idle) testController.Cancel(reason);
        log.Write("test-finished", new { result = testController.Outcome.ToString() });
        testController = null; testWindow = null;
        testLines.Add(reason); RenderTestDetails();
        TestDetails.ScrollToEnd();
        CancelTestButton.Visibility = Visibility.Collapsed; TestMediaButton.IsEnabled = mediaReady && !smoke;
    }
    private async void CancelMediaTest(object sender, RoutedEventArgs e)
    { await gate.WaitAsync(); try { StopTest(() => L.Get("TrialCancelled")); } finally { gate.Release(); } Wake(); }
    private void OpenDiagnostics(object sender, RoutedEventArgs e)
    {
        try { Directory.CreateDirectory(log.DirectoryPath); System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(log.DirectoryPath) { UseShellExecute = true }); }
        catch (Exception error) { Notice(() => Friendly(error)); }
    }
    private async Task CheckSoftwareAsync(bool manual)
    {
        if (checkingSoftware || exiting) return;
        var repository = ReleaseSource.Repository;
        if (repository is null) { if (manual) Notice(() => L.Get("NoUpdateRepository")); return; }
        checkingSoftware = true;
        SoftwareUpdateButton.IsEnabled = false;
        SoftwareUpdateLabel.Text = L.Get("CheckingUpdates");
        try
        {
            await gate.WaitAsync();
            try
            {
                settings.LastSoftwareCheckAttempt = Now;
                if (!smoke) await store.SaveSettingsAsync(settings);
            }
            finally { gate.Release(); }
            availableRelease = await softwareUpdates.CheckAsync(repository);
            softwareChecked = true;
            if (exiting) return;
            SoftwareUpdateLabel.Text = availableRelease is null ? L.Get("UpToDate") : L.Get("NewRelease");
            SoftwareUpdateButton.ToolTip = availableRelease is null ? L.Get("InstalledVersion", ReleaseSource.VersionLabel) : L.Get("ReleaseReadyHint", availableRelease.Tag);
            var release = availableRelease;
            if (manual || release is not null) Notice(() => release is null
                ? L.Get("CurrentVersion", ReleaseSource.VersionLabel)
                : L.Get("ReleaseReady", release!.Tag));
        }
        catch (Exception error)
        {
            if (!exiting)
            {
                SoftwareUpdateLabel.Text = availableRelease is null ? L.Get("Update") : L.Get("NewRelease");
                if (manual) Notice(() => L.Get("UpdateCheckFailed", Friendly(error)));
            }
        }
        finally { checkingSoftware = false; if (!exiting) { SoftwareUpdateButton.IsEnabled = true; RenderUpdateState(); } }
    }
    private async void UpdateApplication(object sender, RoutedEventArgs e)
    {
        if (availableRelease is not null) OpenLink(availableRelease.Page);
        else await CheckSoftwareAsync(true);
    }
    private void OpenFeedback(object sender, RoutedEventArgs e)
    {
        if (ReleaseSource.Repository is { } repository) OpenLink(new Uri(repository.Page.AbsoluteUri.TrimEnd('/') + "/issues/new/choose"));
        else Notice(() => L.Get("NoFeedbackRepository"));
    }
    private void OpenProject(object sender, RoutedEventArgs e)
    {
        if (ReleaseSource.Repository is { } repository) OpenLink(repository.Page);
        else Notice(() => L.Get("NoRepository"));
    }
    private void OpenLink(Uri uri)
    {
        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true }); }
        catch (Exception error) { Notice(() => Friendly(error)); }
    }
    private void QuitApplication(object sender, RoutedEventArgs e) => ExitApp();
    private async void SoftwareUpdatesChanged(object sender, RoutedEventArgs e)
    {
        if (loading) return;
        await gate.WaitAsync();
        try
        {
            settings.AutoCheckSoftwareUpdates = SoftwareUpdatesCheck.IsChecked == true;
            if (!smoke) await store.SaveSettingsAsync(settings);
        }
        catch (Exception error) { Notice(() => Friendly(error)); }
        finally { gate.Release(); }
        if (settings.AutoCheckSoftwareUpdates && !smoke &&
            (!settings.LastSoftwareCheckAttempt.HasValue || Now - settings.LastSoftwareCheckAttempt.Value >= TimeSpan.FromDays(1)))
            await CheckSoftwareAsync(false);
    }
    private void DetailsExpanded(object sender, RoutedEventArgs e) { }
    private void DetailsCollapsed(object sender, RoutedEventArgs e) { }
    private void Notice(Func<string>? text) { notice = text; RenderNotice(); }
    private void RenderNotice() { var text = notice?.Invoke(); NoticeText.Text = text; NoticeText.Visibility = string.IsNullOrWhiteSpace(text) ? Visibility.Collapsed : Visibility.Visible; }
    private void RenderTestDetails() => TestDetails.Text = string.Join(Environment.NewLine, testLines.Select(line => line()));
    private static string Friendly(Exception e) => e is System.Net.Http.HttpRequestException or TaskCanceledException
        ? L.Get("NetworkError") : e is InvalidDataException ? L.Error(e) : L.Get("OperationFailed");

    private static void ApplyTextResources()
    {
        foreach (var key in L.Keys) System.Windows.Application.Current.Resources[key] = L.Get(key);
    }
    private void RefreshLocalizedControls()
    {
        var wasLoading = loading; loading = true;
        try
        {
            foreach (var box in new[] { ThemeBox, LanguageBox, BeforeBox, AfterBox, ProvinceBox, PlaceBox, PlayerBox }) box.IsDropDownOpen = false;
            ApplyTextResources();
            Language = System.Windows.Markup.XmlLanguage.GetLanguage(L.Culture.IetfLanguageTag);
            LanguageBox.ItemsSource = new[] { new LanguageChoice("tr", "TR"), new LanguageChoice("en", "EN") };
            LanguageBox.SelectedItem = ((IEnumerable<LanguageChoice>)LanguageBox.ItemsSource).Single(c => c.Id == L.Language);
            ThemeBox.ItemsSource = new[] { new ThemeChoice("System", L.Get("ThemeSystem")), new ThemeChoice("Light", L.Get("ThemeLight")), new ThemeChoice("Dark", L.Get("ThemeDark")) };
            ThemeBox.SelectedItem = ((IEnumerable<ThemeChoice>)ThemeBox.ItemsSource).Single(c => c.Id == settings.Theme);
            BeforeBox.ItemsSource = new[] { 0, 15, 30, 60, 120, 300 }.Append(settings.BeforeSeconds).Distinct().Order().Select(s => new Choice(s, s == 0 ? L.Get("AtPrayerTime") : s < 60 ? L.Get("Seconds", s) : L.Get("Minutes", s / 60d))).ToArray();
            AfterBox.ItemsSource = new[] { 1, 3, 5, 7, 10, 15, 20, 30, 60 }.Append(settings.AfterMinutes).Distinct().Order().Select(m => new Choice(m, L.Get("Minutes", m))).ToArray();
            BeforeBox.SelectedItem = ((IEnumerable<Choice>)BeforeBox.ItemsSource).First(c => c.Value == settings.BeforeSeconds);
            AfterBox.SelectedItem = ((IEnumerable<Choice>)AfterBox.ItemsSource).First(c => c.Value == settings.AfterMinutes);
            if (PlaceBox.ItemsSource is IEnumerable<Place> locationItems) PlaceBox.ItemsSource = locationItems.ToArray();
            PlaceBox.SelectedItem = (PlaceBox.ItemsSource as IEnumerable<Place>)?.FirstOrDefault(p => p.Id == settings.PlaceId);
            var items = MediaChoices.Build(lastMediaStates, settings, WindowsMedia.DisplayName);
            PlayerBox.ItemsSource = items; PlayerBox.SelectedItem = items.FirstOrDefault(p => p.Key == MediaChoices.SelectedKey(settings));
            if (tray.ContextMenuStrip is { } menu)
            {
                menu.Items[0].Text = L.Get("OpenApp"); menu.Items[1].Text = L.Get("CancelResume"); menu.Items[2].Text = L.Get("Quit");
            }
            RenderNotice(); RenderTestDetails(); RenderUpdateState();
        }
        finally { loading = wasLoading; }
    }
    private void RenderUpdateState()
    {
        SoftwareUpdateLabel.Text = L.Get(checkingSoftware ? "CheckingUpdates" : availableRelease is not null ? "NewRelease" : softwareChecked ? "UpToDate" : "Update");
        SoftwareUpdateButton.ToolTip = availableRelease is { } release ? L.Get("ReleaseReadyHint", release.Tag) : L.Get("InstalledVersionHint", ReleaseSource.VersionLabel);
    }
    private async void LanguageChanged(object sender, SelectionChangedEventArgs e)
    {
        if (loading || LanguageBox.SelectedItem is not LanguageChoice choice || choice.Id == settings.Language) return;
        await gate.WaitAsync();
        try
        {
            settings.Language = choice.Id; L.Apply(choice.Id);
            RefreshLocalizedControls(); Render(Now, BreakWindow.Build(table, settings, Now));
            if (!smoke) await store.SaveSettingsAsync(settings);
        }
        catch (Exception error) { log.Error("language-save", error); Notice(() => Friendly(error)); }
        finally { gate.Release(); }
    }

    private static bool StartupEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
        return key?.GetValue("Sukun") is string || key?.GetValue("Vakit" + "Molasi") is string;
    }
    private void StartupChanged(object sender, RoutedEventArgs e)
    {
        if (smoke || loading) return;
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
            if (StartupCheck.IsChecked == true) key.SetValue("Sukun", $"\"{Environment.ProcessPath}\" --background");
            else key.DeleteValue("Sukun", false);
            key.DeleteValue("Vakit" + "Molasi", false);
        }
        catch (Exception error) { StartupCheck.IsChecked = StartupEnabled(); Notice(() => Friendly(error)); }
    }
}
