using Newtonsoft.Json;
using Swipe_Core;
using Swipe_Core.Functions;
using System.Windows;
using Microsoft.Win32;
using System.Reflection;
using System.IO;
using static Swipe_Application.SettingsView;
using System.Windows.Controls;

namespace Swipe_Application
{
public partial class SettingsView : System.Windows.Controls.UserControl
{
    public class SettingsData
    {
        public bool AutoStart { get; set; } = false;
        public float DTWTreshold { get; set; } = 2.25f;
    }

    private SettingsData _settingsData = new SettingsData();

    private FunctionManager? _functionManager = null;
    private Logger? _logger = null;
    private List<string> _logStrings = new List<string>();
    private bool _loaded = false;

    public SettingsView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var mainWindow = Utils.FindParent<MainWindow>(this);
        if (mainWindow != null)
        {
            _functionManager = mainWindow.FunctionManager;
            _logger = mainWindow.Logger;
            if (_logger != null)
            {
                _logger.OnLogEntry += OnLogEntry;
            }
        }
        Load();
        _loaded = true;
    }

    public void Unload()
    {
        if (_logger != null)
        {
            _logger.OnLogEntry -= OnLogEntry;
        }
    }

    public void UpdateLog()
    {
        Log.Text = string.Join(Environment.NewLine, _logStrings);
    }

    private void OnLogEntry(object? sender, string e)
    {
        this.Dispatcher.Invoke(() =>
                               {
                                   _logStrings.Add(e);
                                   UpdateLog();
                               });
    }

    private void DTWSlider_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
    {
        var slider = (Slider)sender;
        slider.Value = Math.Round(slider.Value, 2);

        if (_loaded == false)
        {
            return;
        }

        _settingsData.DTWTreshold = (float)slider.Value;
        _functionManager?.SetDTWTreshold(_settingsData.DTWTreshold);

        Save();
    }

    private void AutostartCheck_Checked(object sender, RoutedEventArgs e)
    {
        if (_loaded == false)
        {
            return;
        }

        _settingsData.AutoStart = true;
        RegistryKey? rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
        if (rk != null)
        {
            rk?.SetValue("Swipe", $"\"{Assembly.GetExecutingAssembly().Location}\"");
            _logger?.Log("[USER] Added to autostart");
        }
        else
        {
            _logger?.Log("[ERROR] Could not open registry path: SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run");
        }

        Save();
    }

    private void AutostartCheck_Unchecked(object sender, RoutedEventArgs e)
    {
        if (_loaded == false)
        {
            return;
        }

        _settingsData.AutoStart = false;
        RegistryKey? rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
        if (rk != null)
        {
            if (rk?.GetValue("Swipe") != null)
            {
                _logger?.Log("[USER] Removed from autostart");
                rk?.DeleteValue("Swipe");
            }
        }
        else
        {
            _logger?.Log("[ERROR] Could not open registry path: SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run");
        }

        Save();
    }

    private bool Save()
    {

        string json = JsonConvert.SerializeObject(
            _settingsData,
            new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All, Formatting = Formatting.Indented });

        var filename = @"Settings.json";
        File.WriteAllText(filename, json);

        return true;
    }

    private void Load()
    {
        var filename = @"Settings.json";

        if (!File.Exists(filename))
            return;
        string json = File.ReadAllText(filename);

        var loaded = JsonConvert.DeserializeObject<SettingsData>(
            json, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });

        if (loaded == null)
        {
            _logger?.Log("[ERROR] Failed to load settings");
            return;
        }

        _settingsData.AutoStart = loaded.AutoStart;
        _settingsData.DTWTreshold = loaded.DTWTreshold;

        AutostartCheck.IsChecked = loaded.AutoStart;
        _functionManager?.SetDTWTreshold(loaded.DTWTreshold);
        DTWSlider.Value = loaded.DTWTreshold;

        filename = @"Log.txt";

        if (!File.Exists(filename))
            return;

        var log = File.ReadLines(filename);
        foreach (var row in log)
        {
            _logStrings.Add(row);
        }
        UpdateLog();
    }
}
}
