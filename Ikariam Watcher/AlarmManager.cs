using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Timers;

namespace IkariamWatcher
{
    public class Alarm
    {
        public IntPtr HWnd { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime Target { get; set; }
        public bool Fired { get; set; }
    }

    public class AlarmManager : IDisposable
    {
        public int LastScannedWindowCount { get; private set; }
        public int LastAlarmCount { get; private set; }
        public List<string> LastScannedTitles { get; private set; } = new();

        public event EventHandler? ScanCompleted;

        private readonly System.Timers.Timer _scanTimer;
        private readonly System.Timers.Timer _tickTimer;
        private readonly Dictionary<IntPtr, Alarm> _alarms = new();
        // pattern matching real window title timers: optional hours, mandatory minutes, optional seconds
        // examples matched: "38m 55s", "01h 39m 46s", "19m 13s"
        private readonly Regex _timeRegex = new(@"(?:(\d{1,2})h\s*)?(\d{1,2})m(?:\s*(\d{1,2})s)?", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public event EventHandler? ActiveAlarmsChanged;
        public event EventHandler<Alarm>? AlarmFired;

        public AlarmManager()
        {
            _scanTimer = new System.Timers.Timer(5000) { AutoReset = true };
            _scanTimer.Elapsed += (s, e) => ScanWindows();

            _tickTimer = new System.Timers.Timer(1000) { AutoReset = true };
            _tickTimer.Elapsed += (s, e) => Tick();
        }

        public void Start()
        {
            ScanWindows();
            _scanTimer.Start();
            _tickTimer.Start();
        }

        public void Stop()
        {
            _scanTimer.Stop();
            _tickTimer.Stop();
        }

        public IEnumerable<Alarm> GetActiveAlarms()
        {
            lock (_alarms)
            {
                return _alarms.Values.Where(a => !a.Fired).OrderBy(a => a.Target).Select(a => new Alarm
                {
                    HWnd = a.HWnd,
                    Title = a.Title,
                    Target = a.Target,
                    Fired = a.Fired
                }).ToList();
            }
        }

        private void Tick()
        {
            List<Alarm> toFire = new();
            var now = DateTime.Now;
            lock (_alarms)
            {
                foreach (var alarm in _alarms.Values)
                {
                    // Compute remaining time and fire only when crossing the 5-minute threshold
                    var remaining = alarm.Target - now;
                    if (!alarm.Fired && remaining <= TimeSpan.FromMinutes(5))
                    {
                        alarm.Fired = true;
                        toFire.Add(alarm);
                    }
                }
            }

            foreach (var a in toFire)
            {
                var firedHandler = AlarmFired;
                firedHandler?.Invoke(this, a);

                var activeHandler = ActiveAlarmsChanged;
                activeHandler?.Invoke(this, EventArgs.Empty);
            }
        }

        private void ScanWindows()
        {
            var found = new Dictionary<IntPtr, (string title, DateTime target)>();
            int scanned = 0;
            var titles = new List<string>();

            EnumWindows((hwnd, lParam) =>
            {
                if (!IsWindowVisible(hwnd)) return true;
                int len = GetWindowTextLength(hwnd);
                if (len == 0) return true;
                var sb = new StringBuilder(len + 1);
                GetWindowText(hwnd, sb, sb.Capacity);
                var title = sb.ToString();
                scanned++;
                titles.Add(title);
                var match = _timeRegex.Match(title);
                if (match.Success)
                {
                    if (TryParseMatch(match, out var ts) && ts.TotalSeconds > 0)
                    {
                        found[hwnd] = (title, DateTime.Now + ts);
                    }
                }
                return true;
            }, IntPtr.Zero);

            bool changed = false;
            lock (_alarms)
            {
                // add/update
                foreach (var kv in found)
                {
                    if (_alarms.TryGetValue(kv.Key, out var existing))
                    {
                        // update title if changed
                        if (existing.Title != kv.Value.title)
                        {
                            existing.Title = kv.Value.title;
                            changed = true;
                        }

                        // update target if changed more than 1 second
                        if (Math.Abs((existing.Target - kv.Value.target).TotalSeconds) > 1)
                        {
                            // If the new target is greater than previous, reset Fired flag (countdown increased)
                            if (kv.Value.target > existing.Target)
                            {
                                existing.Fired = false;
                            }

                            existing.Target = kv.Value.target;
                            changed = true;
                        }
                    }
                    else
                    {
                        _alarms[kv.Key] = new Alarm { HWnd = kv.Key, Title = kv.Value.title, Target = kv.Value.target, Fired = false };
                        changed = true;
                    }
                }

                // remove windows that disappeared
                var toRemove = _alarms.Keys.Where(k => !found.ContainsKey(k)).ToList();
                foreach (var r in toRemove)
                {
                    _alarms.Remove(r);
                    changed = true;
                }
            }

            if (changed)
            {
                var handler = ActiveAlarmsChanged;
                handler?.Invoke(this, EventArgs.Empty);
            }

            LastScannedWindowCount = scanned;
            LastAlarmCount = found.Count;
            LastScannedTitles = titles;
            var scanHandler = ScanCompleted;
            scanHandler?.Invoke(this, EventArgs.Empty);
        }

        // Public rescan helper
        public void Rescan()
        {
            ScanWindows();
        }

        private bool TryParseMatch(Match match, out TimeSpan ts)
        {
            ts = TimeSpan.Zero;
            if (!match.Success) return false;

            // groups: 1 = hours (optional), 2 = minutes (mandatory), 3 = seconds (optional)
            int hours = ParseGroup(match.Groups[1]);
            int mins = ParseGroup(match.Groups[2]);
            int secs = ParseGroup(match.Groups[3]);

            if (hours == 0 && mins == 0 && secs == 0) return false;

            // build TimeSpan: new TimeSpan(days, hours, minutes, seconds) -> days=0
            ts = new TimeSpan(0, hours, mins, secs);
            return true;
        }

        private int ParseGroup(System.Text.RegularExpressions.Group g)
        {
            if (g == null || !g.Success) return 0;
            if (int.TryParse(g.Value, out var v)) return v;
            return 0;
        }

        public void Dispose()
        {
            Stop();
            _scanTimer.Dispose();
            _tickTimer.Dispose();
        }

        #region PInvoke
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
        #endregion
    }
}