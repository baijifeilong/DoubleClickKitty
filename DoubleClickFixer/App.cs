// Created By BaiJiFeiLong@gmail.com at 2024-07-16 16:47:01+0800

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using NLog;
using NLog.Config;
using NLog.Targets;
using PInvoke;

namespace DoubleClickFixer;

internal class App
{
    public event EventHandler<ClickEvent> ClickEventTriggered = null!;

    private static App _app = null!;
    private DateTime _lastAcceptedAt = DateTime.Now;
    private bool _ignoring;
    private readonly Logger _logger;
    private readonly AppConfig _appConfig;
    private readonly FileInfo _configFileInfo;
    private ClickEvent? _lastAcceptedClickEvent;

    public int GetThresholdMillis()
    {
        return _appConfig.ThresholdMillis;
    }

    public void SetThresholdMillis(int millis)
    {
        _logger.Info("Setting threshold: {0}...", millis);
        _appConfig.ThresholdMillis = millis;
        PersistConfig();
    }

    public bool GetFixEnabled()
    {
        return _appConfig.FixEnabled;
    }

    public void SetFixEnabled(bool value)
    {
        _logger.Info("Setting fix enabled: {0}", value);
        _appConfig.FixEnabled = value;
        PersistConfig();
    }

    public bool GetMiddleAsLeft()
    {
        return _appConfig.MiddleAsLeft;
    }

    public void SetMiddleAsLeft(bool value)
    {
        _logger.Info($"Setting middle as left: {value}");
        _appConfig.MiddleAsLeft = value;
        PersistConfig();
    }

    public static int GetDefaultThresholdMillis()
    {
        return 120;
    }

    public TheLanguage GetLanguage()
    {
        return _appConfig.Language;
    }

    public void SetLanguage(TheLanguage language)
    {
        _logger.Info("Setting language: {0}...", language);
        _appConfig.Language = language;
        PersistConfig();
        Translation.Culture = new CultureInfo(language.ToLanguageCode());
    }

    public int GetTotalFix()
    {
        return _appConfig.EverydayFix.Values.Sum();
    }

    public int GetTodayFix()
    {
        return _appConfig.EverydayFix.GetValueOrDefault(DateOnly.FromDateTime(DateTime.Now), 0);
    }

    public static App GetApp()
    {
        return _app;
    }

    public static TheLanguage[] GetSupportedLanguages()
    {
        return Enum.GetValues<TheLanguage>();
    }

    private App()
    {
        _app = this;
        const string layout = "[${date}] [${level:uppercase=true}] [${logger}] ${message:withexception=true}";
        var consoleTarget = new ConsoleTarget { Layout = layout };
        var applicationName = Assembly.GetExecutingAssembly().GetName().Name;
        var fileTarget = new FileTarget { FileName = $"{applicationName}.${{shortdate}}.log", Layout = layout };
        var debuggerTarget = new DebuggerTarget { Layout = layout };
        var configuration = new LoggingConfiguration();
        configuration.AddRule(LogLevel.Debug, LogLevel.Fatal, consoleTarget);
        configuration.AddRule(LogLevel.Debug, LogLevel.Fatal, debuggerTarget);
        configuration.AddRule(LogLevel.Debug, LogLevel.Fatal, fileTarget);
        LogManager.Configuration = configuration;
        _logger = LogManager.GetLogger(nameof(App));

        _configFileInfo = new FileInfo($"{applicationName}.Config.json");
        _appConfig = _configFileInfo.Exists
            ? JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(_configFileInfo.FullName))!
            : new AppConfig
            {
                Language = TheLanguage.EnUs, FixEnabled = true, MiddleAsLeft = false,
                ThresholdMillis = GetDefaultThresholdMillis(), EverydayFix = new Dictionary<DateOnly, int>()
            };
        _logger.Info("Current config: {0}", _appConfig);
        if (_configFileInfo.Exists) PersistConfig();
        Translation.Culture = new CultureInfo(_appConfig.Language.ToLanguageCode());
    }

    private void PersistConfig()
    {
        File.WriteAllText(_configFileInfo.FullName,
            JsonSerializer.Serialize(_appConfig, new JsonSerializerOptions { WriteIndented = true }));
    }

    private void Run()
    {
        _logger.Info("Setting up hook...");
        User32.SetWindowsHookEx(User32.WindowsHookType.WH_MOUSE_LL, MouseHookProc, 0, 0);
        _logger.Info("Hook set up");
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }

    [STAThread]
    private static void Main()
    {
        new App().Run();
    }

    private void TriggerClickEvent(int delayMillis, bool accepted)
    {
        if (!accepted)
        {
            var today = DateOnly.FromDateTime(DateTime.Now);
            if (!_appConfig.EverydayFix.TryAdd(today, 1)) _appConfig.EverydayFix[today] += 1;
            PersistConfig();
        }

        var clickEvent = new ClickEvent { TriggeredAt = DateTime.Now, DelayMillis = delayMillis, Accepted = accepted };
        if (_lastAcceptedClickEvent is { IsDoubleClick: false }
            && clickEvent.Accepted
            && (clickEvent.TriggeredAt - _lastAcceptedClickEvent.TriggeredAt).TotalMilliseconds < 500)
        {
            clickEvent.IsDoubleClick = true;
        }

        if (clickEvent.Accepted) _lastAcceptedClickEvent = clickEvent;
        ClickEventTriggered.Invoke(this, clickEvent);
    }

    [SuppressMessage("ReSharper", "ConvertIfStatementToSwitchStatement")]
    [SuppressMessage("ReSharper", "InvertIf")]
    private int MouseHookProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode < 0) return User32.CallNextHookEx(0, nCode, wParam, lParam);
        if (wParam == (int)User32.WindowMessage.WM_LBUTTONDOWN)
        {
            var currentDateTime = DateTime.Now;
            var deltaMillis = (int)(currentDateTime - _lastAcceptedAt).TotalMilliseconds;
            if (_appConfig.FixEnabled && deltaMillis < _appConfig.ThresholdMillis)
            {
                _logger.Info($"Left mouse down, rejected with milliseconds: {deltaMillis}");
                _ignoring = true;
                TriggerClickEvent(deltaMillis, false);
                return 1;
            }

            _lastAcceptedAt = currentDateTime;
            _logger.Info($"Left mouse down, accepted: {deltaMillis}");
            TriggerClickEvent(deltaMillis, true);
        }
        else if (wParam == (int)User32.WindowMessage.WM_LBUTTONUP)
        {
            if (_ignoring)
            {
                _ignoring = false;
                return 1;
            }
        }
        else if (wParam == (int)User32.WindowMessage.WM_MBUTTONDOWN && _appConfig.MiddleAsLeft)
        {
            Task.Run(() =>
            {
                var input = new User32.INPUT { type = User32.InputType.INPUT_MOUSE };
                input.Inputs.mi.dwFlags = User32.MOUSEEVENTF.MOUSEEVENTF_LEFTDOWN;
                User32.SendInput(1, new[] { input }, Marshal.SizeOf<User32.INPUT>());
            });
            return 1;
        }
        else if (wParam == (int)User32.WindowMessage.WM_MBUTTONUP && _appConfig.MiddleAsLeft)
        {
            Task.Run(() =>
            {
                var input = new User32.INPUT { type = User32.InputType.INPUT_MOUSE };
                input.Inputs.mi.dwFlags = User32.MOUSEEVENTF.MOUSEEVENTF_LEFTUP;
                User32.SendInput(1, new[] { input }, Marshal.SizeOf<User32.INPUT>());
            });
            return 1;
        }

        return User32.CallNextHookEx(0, nCode, wParam, lParam);
    }
}