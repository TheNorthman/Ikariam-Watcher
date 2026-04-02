using System;
using System.Drawing;
using System.Media;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace IkariamWatcher
{
    internal class TrayApplicationContext : ApplicationContext
    {
        private readonly NotifyIcon _notifyIcon;
        private readonly AlarmManager _alarmManager;
        private readonly Control _syncControl;
        private bool _enabled = true;
        private bool _playSound = true;
        private bool _showToaster = false;

        private static readonly Regex _titleTimeRegex = new(@"(?:(\d{1,2})h\s*)?(\d{1,2})m(?:\s*(\d{1,2})s)?", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex _worldNameRegex = new(@"World\s+([^\-–:()]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public TrayApplicationContext()
        {
            _syncControl = new Control();
            _syncControl.CreateControl();

            var menu = new ContextMenuStrip();

            var enableItem = new ToolStripMenuItem("Enable") { CheckOnClick = true, Checked = _enabled };
            enableItem.CheckedChanged += (s, e) => { _enabled = enableItem.Checked; };

            var soundItem = new ToolStripMenuItem("Play sound") { CheckOnClick = true, Checked = _playSound };
            soundItem.CheckedChanged += (s, e) => { _playSound = soundItem.Checked; };

            var toastItem = new ToolStripMenuItem("Show notification") { CheckOnClick = true, Checked = _showToaster };
            toastItem.CheckedChanged += (s, e) => { _showToaster = toastItem.Checked; };

            var exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += (s, e) => ExitThread();

            menu.Items.Add(enableItem);
            menu.Items.Add(soundItem);
            menu.Items.Add(toastItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(exitItem);

            Icon iconCopy;
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                using var stream = asm.GetManifestResourceStream("Ikariam_Watcher.favicon.ico");
                if (stream != null)
                    iconCopy = new Icon(stream);
                else
                    iconCopy = SystemIcons.Application;
            }
            catch
            {
                iconCopy = SystemIcons.Application;
            }

            _notifyIcon = new NotifyIcon
            {
                Icon = iconCopy,
                Text = "Ikariam Watcher",
                ContextMenuStrip = menu,
                Visible = true
            };

            _notifyIcon.DoubleClick += (s, e) => { /* no windows to show */ };

            _alarmManager = new AlarmManager();
            _alarmManager.ActiveAlarmsChanged += AlarmManager_ActiveAlarmsChanged;
            _alarmManager.AlarmFired += AlarmManager_AlarmFired;

            _alarmManager.Start();
        }

        private void AlarmManager_AlarmFired(object? sender, Alarm e)
        {
            void act()
            {
                if (!_enabled)
                    return;

                if (_showToaster)
                    _notifyIcon.ShowBalloonTip(5000, "Alarm", $"{e.Title} reached zero", ToolTipIcon.Info);

                if (_playSound)
                    SystemSounds.Exclamation.Play();
            }

            if (_syncControl.InvokeRequired)
                _syncControl.BeginInvoke((Action)act);
            else
                act();
        }

        private void AlarmManager_ActiveAlarmsChanged(object? sender, EventArgs e)
        {
            void act()
            {
                var alarms = _alarmManager.GetActiveAlarms();
                if (!alarms.Any())
                {
                    _notifyIcon.Text = "Ikariam Watcher (No alarms)";
                    return;
                }

                var lines = new List<string>();
                foreach (var a in alarms.OrderBy(a => a.Target))
                {
                    var title = a.Title ?? string.Empty;
                    var match = _titleTimeRegex.Match(title);
                    string countdown = match.Success ? match.Value.Trim() : string.Empty;

                    string world = title;
                    if (match.Success)
                    {
                        world = title.Remove(match.Index, match.Length).Trim();
                        world = world.TrimEnd('-', '–', ':', ' ');
                    }

                    var worldMatch = _worldNameRegex.Match(world);
                    if (worldMatch.Success)
                        world = worldMatch.Groups[1].Value.Trim();
                    else
                    {
                        var parts = world.Split(new[] { '-', '–', ':' }, StringSplitOptions.RemoveEmptyEntries)
                                         .Select(p => p.Trim())
                                         .Where(p => !string.IsNullOrEmpty(p) && !string.Equals(p, "Ikariam", StringComparison.OrdinalIgnoreCase))
                                         .ToArray();
                        if (parts.Length > 0)
                            world = parts.Last();
                    }

                    if (string.IsNullOrEmpty(world)) world = "Unknown";

                    var line = string.IsNullOrEmpty(countdown)
                        ? $"{world} - ({FormatRemaining(a)})"
                        : $"{world} - {countdown} ({FormatRemaining(a)})";

                    lines.Add(line);
                }

                var tooltip = string.Join(Environment.NewLine, lines);
                _notifyIcon.Text = TruncateForNotifyIcon(tooltip);
            }

            if (_syncControl.InvokeRequired)
                _syncControl.BeginInvoke((Action)act);
            else
                act();
        }

        private string FormatRemaining(Alarm a)
        {
            var remaining = a.Target - DateTime.Now;
            if (remaining.TotalHours >= 1)
                return $"{(int)remaining.TotalHours}h {remaining.Minutes}m {remaining.Seconds}s";
            return $"{remaining.Minutes}m {remaining.Seconds}s";
        }

        private const int NotifyIconTextMax = 127;
        private static string TruncateForNotifyIcon(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            if (s.Length <= NotifyIconTextMax) return s;
            var max = Math.Max(0, NotifyIconTextMax - 1);
            return s.Substring(0, max) + "…";
        }

        protected override void ExitThreadCore()
        {
            _alarmManager.Dispose();
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            base.ExitThreadCore();
        }
    }
}
