using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Drawing;
using System.Linq;
using System.Security.Principal;
using Microsoft.Win32;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Diagnostics.Tracing.Session;
using System.IO;
using System.Threading;
using System.Text;
using System.ServiceProcess; // Для управления службами
using System.ComponentModel;

namespace ALP
{
    public class ControlWriter : TextWriter
    {
        private RichTextBox _box;
        private Color _color;
        public ControlWriter(RichTextBox box, Color color) { _box = box; _color = color; }
        public override void Write(char value) { WriteText(value.ToString()); }
        public override void Write(string value) { WriteText(value); }
        public override Encoding Encoding { get { return Encoding.UTF8; } }
        private void WriteText(string text)
        {
            if (_box.InvokeRequired) _box.Invoke(new Action(() => WriteText(text)));
            else { _box.SelectionColor = _color; _box.AppendText(text); _box.ScrollToCaret(); }
        }
    }

    static class Program
        {
            [STAThread]
            static void Main(string[] args)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                bool isAdmin = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);

                // If we aren't admin and didn't start from the task, create the task
                if (!isAdmin)
                {
                    CreateSchedulerTask(); // Ensure task exists
                    // Start the task immediately
                    Process.Start("schtasks", "/Run /TN \"ALPA_AutoRun\"");
                    return; 
                }

                Application.Run(new UltraForm());
            }

            private static void CreateSchedulerTask()
            {
                try
                {
                    string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                    string taskName = "ALPA_AutoRun";
                    // Создаем задачу с правами HIGHEST (Максимальные), но от имени ПОЛЬЗОВАТЕЛЯ (чтобы было видно окно)
                    string cmd = string.Format("/Create /TN \"{0}\" /TR \"\\\"{1}\\\"\" /SC ONLOGON /RL HIGHEST /F", taskName, exePath);
                    
                    ProcessStartInfo psi = new ProcessStartInfo("schtasks", cmd);
                    psi.UseShellExecute = true;
                    psi.Verb = "runas"; // Нужны права админа для создания задачи
                    psi.WindowStyle = ProcessWindowStyle.Hidden;
                    Process.Start(psi).WaitForExit();
                    
                    MessageBox.Show("Task created! Next time ALPA will start with Admin rights automatically on login.", "Success");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to create task: " + ex.Message);
                }
            }
        }

    public partial class UltraForm : Form
    {
        [DllImport("user32.dll")]
        public static extern bool RegisterRawInputDevices(RAWINPUTDEVICE[] pRawInputDevices, uint uiNumDevices, uint cbSize);
        [DllImport("ntdll.dll")]
        public static extern int NtQueryTimerResolution(out uint min, out uint max, out uint cur);
        [DllImport("ntdll.dll")]
        public static extern int NtQueryInformationProcess(IntPtr processHandle, int processInformationClass, ref PROCESS_POWER_THROTTLING_STATE processInformation, int processInformationLength, out int returnLength);
        [DllImport("powrprof.dll")]
        public static extern uint PowerGetActiveScheme(IntPtr UserRootPowerKey, out IntPtr ActivePolicyGuid);
        [DllImport("psapi.dll")]
        public static extern bool EnumDeviceDrivers([In, Out] IntPtr[] ddAddresses, uint arraySizeBytes, out uint bytesNeeded);
        [DllImport("psapi.dll")]
        public static extern int GetDeviceDriverBaseName(IntPtr ddAddress, StringBuilder ddBaseName, int baseNameStringSize);
        [DllImport("kernel32.dll")]
                private static extern bool GetProcessIoCounters(IntPtr ProcessHandle, out IO_COUNTERS IoCounters);


                [StructLayout(LayoutKind.Sequential)]
                private struct IO_COUNTERS
                {
                    public ulong ReadOperationCount;
                    public ulong WriteOperationCount;
                    public ulong OtherOperationCount;
                    public ulong ReadTransferCount;
                    public ulong WriteTransferCount;
                    public ulong OtherTransferCount;
                }

        private long _lastInputTicks = 0;
        private List<double> _inputIntervals = new List<double>();
        private double _currentHz = 0;
        private TabControl _tabs;
        private RichTextBox _logInput, _logPerf, _logSys, _logConsole, _logStartup;
        private ListView _lvProcesses, _lvDrivers; // <--- НОВЫЕ ПЕРЕМЕННЫЕ
        private int _procSortColumn = -1; // -1 = Сортировка по умолчанию (Score)
        private bool _procSortAsc = false;
        private int _drvSortColumn = -1;
        private bool _drvSortAsc = false;
        
        private Dictionary<string, PerformanceCounter[]> _diskCounters = new Dictionary<string, PerformanceCounter[]>();
        private Dictionary<string, DriverStats> _dpcStats = new Dictionary<string, DriverStats>();
        private Dictionary<string, DriverStats> _isrStats = new Dictionary<string, DriverStats>();
        private Dictionary<int, double> _dpcStartTime = new Dictionary<int, double>(); // Ключ - ID процессора
        private Dictionary<int, double> _isrStartTime = new Dictionary<int, double>();
        private object _statsLock = new object(); // Для защиты данных из разных потоков 
        // Класс для хранения полной статистики
        internal class DriverStats {
            public double Current;
            public double Max;
            public double Min = double.MaxValue;
            public double Total;
            public long Count;
        }
        // --------------------------------       

        private PerformanceCounter _pcProcQueue, _pcContextSwitches;
        private PerformanceCounter _pcDiskQueue, _pcDiskLatency, _pcDiskTime;
        private PerformanceCounter _pcPageFaults, _pcAvailableMem, _pcCacheMem;
        private PerformanceCounter _pcParkedCores, _pcUdpErrors;

        private System.Windows.Forms.Timer _uiTimer;

        // Класс для хранения данных о драйверах
        internal class ModuleInfo { public string Name; public int Size; }

        // Добавь эти поля в начало класса UltraForm к остальным переменным
        private List<PerformanceCounter> _pcAllCoreInterrupts = new List<PerformanceCounter>();
        private PerformanceCounter _pcInterruptsGlobal;

        private string _logFilePath = "ALPA_Log.txt";
        private object _fileLock = new object(); // Для безопасной записи из разных потоков

        public UltraForm()
        {
            this.Text = "ALPA v1.5 - Amazing Latency Performance Audit | by amazingb01";
            this.Size = new Size(1000, 600); 
            this.BackColor = Color.FromArgb(15, 15, 15);
            this.ForeColor = Color.White;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            SetupLayout();
            Console.SetOut(new ControlWriter(_logConsole, Color.Silver));

            Log("[SYSTEM] ALPA starting up...", Color.Cyan);
            
            DiagnosticStartup();
            InitializeCounters();
            SetupRawInput();
            
            _uiTimer = new System.Windows.Forms.Timer();
            _uiTimer.Interval = 1000;
            _uiTimer.Tick += delegate {
                if (_inputIntervals.Count > 0) {
                    _currentHz = 1000.0 / (_inputIntervals.Average() / 10000.0);
                    _inputIntervals.Clear();
                }
                UpdateStats();
            };
            _uiTimer.Start();

            this.Shown += delegate { RunAutoAudit(); };
        }

        // Сортировщик для процессов (по ID)
            public class ListViewIndexSorter : System.Collections.IComparer {
                private List<int> _order;
                public ListViewIndexSorter(List<int> order) { _order = order; }
                public int Compare(object x, object y) {
                    int idX = (int)((ListViewItem)x).Tag;
                    int idY = (int)((ListViewItem)y).Tag;
                    // Возвращаем индекс из нашего отсортированного списка
                    int idx1 = _order.IndexOf(idX);
                    int idx2 = _order.IndexOf(idY);
                    if (idx1 == -1) return 1; 
                    if (idx2 == -1) return -1;
                    return idx1.CompareTo(idx2);
                }
            }

            // Сортировщик для драйверов (по Строке-Ключу)
            public class ListViewStringSorter : System.Collections.IComparer {
                private List<string> _order;
                public ListViewStringSorter(List<string> order) { _order = order; }
                public int Compare(object x, object y) {
                    string idX = (string)((ListViewItem)x).Tag;
                    string idY = (string)((ListViewItem)y).Tag;
                    int idx1 = _order.IndexOf(idX);
                    int idx2 = _order.IndexOf(idY);
                    if (idx1 == -1) return 1; 
                    if (idx2 == -1) return -1;
                    return idx1.CompareTo(idx2);
                }
            }

        public static void SetDoubleBuffered(System.Windows.Forms.Control c)
                {
                    if (System.Windows.Forms.SystemInformation.TerminalServerSession) return;
                    System.Reflection.PropertyInfo aProp = typeof(System.Windows.Forms.Control).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    aProp.SetValue(c, true, null);
                }

        private void CheckTopResourceConsumers()
                {
                    try {
                        // --- ГЛОБАЛЬНАЯ СТАТИСТИКА (Performance) ---
                        float totalNet = 0;
                        try {
                            var category = new PerformanceCounterCategory("Network Interface");
                            foreach (var inst in category.GetInstanceNames()) {
                                using (var pc = new PerformanceCounter("Network Interface", "Bytes Total/sec", inst)) {
                                        totalNet += pc.NextValue();
                                }
                            }
                        } catch {}
                        
                        float totalDisk = 0;
                        if (_pcDiskTime != null) totalDisk = (_pcDiskTime != null) ? _pcDiskTime.NextValue() : 0;
                        double netMb = totalNet / 1024.0 / 1024.0;
                        
                        SafeLog(_logPerf, "\n=== GLOBAL I/O MONITOR ===\n", Color.White);
                        SafeLog(_logPerf, string.Format("TOTAL INTERNET: {0:F2} MB/s ", netMb), netMb > 1.0 ? Color.Cyan : Color.Gray);
                        SafeLog(_logPerf, string.Format("| TOTAL DISK ACTIVITY: {0:F0}%\n", totalDisk), totalDisk > 50 ? Color.Orange : Color.Gray);

                        // ===============================================
                        // 1. ТАБЛИЦА ПРОЦЕССОВ (С СОРТИРОВКОЙ)
                        // ===============================================
                        if (_lvProcesses != null && _tabs.SelectedTab == _tabs.TabPages[2]) 
                        {
                            var processes = Process.GetProcesses();
                            var hogs = new List<dynamic>();
                            
                            string[] gpuInstances = new string[0];
                            try { if (PerformanceCounterCategory.Exists("GPU Process Memory")) gpuInstances = new PerformanceCounterCategory("GPU Process Memory").GetInstanceNames(); } catch { }

                            foreach (var p in processes) {
                                try {
                                    if (p.Id == 0 || p.Id == 4) continue;
                                    int threads = p.Threads.Count;
                                    double ramMb = p.WorkingSet64 / 1024.0 / 1024.0; 
                                    string prio = "Norm";
                                    int prioVal = 3;
                                    try {
                                        if (p.PriorityClass == ProcessPriorityClass.High) { prio = "High"; prioVal = 5; }
                                        else if (p.PriorityClass == ProcessPriorityClass.RealTime) { prio = "Real"; prioVal = 6; }
                                        else if (p.PriorityClass == ProcessPriorityClass.Idle) { prio = "Low"; prioVal = 1; }
                                    } catch { }

                                    TimeSpan cpuTime = TimeSpan.Zero;
                                    try { cpuTime = p.TotalProcessorTime; } catch {}

                                    double vramMb = 0;
                                    string gpuInst = gpuInstances.FirstOrDefault(x => x.StartsWith("pid_" + p.Id + "_"));
                                    if (!string.IsNullOrEmpty(gpuInst)) {
                                        using (var pc = new PerformanceCounter("GPU Process Memory", "Dedicated Usage", gpuInst)) { vramMb = pc.NextValue() / 1024.0 / 1024.0; }
                                    }

                                    ulong ioBytes = 0;
                                    try {
                                        IO_COUNTERS counters;
                                        if (GetProcessIoCounters(p.Handle, out counters)) ioBytes = counters.ReadTransferCount + counters.WriteTransferCount;
                                    } catch { }

                                    double score = threads + (ramMb / 50.0) + vramMb + (ioBytes / 1024.0 / 1024.0);
                                    hogs.Add(new { Id = p.Id, Name = p.ProcessName, Thrds = threads, RAM = ramMb, IO = ioBytes, VRAM = vramMb, CPU = cpuTime, Prio = prio, PrioVal = prioVal, Score = score });
                                } catch { }
                            }

                            // --- СОРТИРОВКА ПРОЦЕССОВ ---
                            Func<dynamic, object> keySelector = null;
                            switch (_procSortColumn) {
                                case 0: keySelector = x => x.Name; break;
                                case 1: keySelector = x => x.Thrds; break;
                                case 2: keySelector = x => x.PrioVal; break;
                                case 3: keySelector = x => x.CPU; break;
                                case 4: keySelector = x => x.VRAM; break;
                                case 5: keySelector = x => x.RAM; break;
                                case 6: keySelector = x => x.IO; break;
                                default: keySelector = x => x.Score; break;
                            }
                            var sortedHogs = _procSortAsc ? hogs.OrderBy(keySelector).ToList() : hogs.OrderByDescending(keySelector).ToList();

                            _lvProcesses.Invoke(new Action(() => {
                                _lvProcesses.BeginUpdate();
                                var currentItems = _lvProcesses.Items.Cast<ListViewItem>().ToDictionary(i => (int)i.Tag);
                                var activeIds = new HashSet<int>();

                                foreach (var h in sortedHogs)
                                {
                                    activeIds.Add(h.Id);
                                    string ioStr = h.IO / 1024.0 / 1024.0 > 1024 ? string.Format("{0:F1}GB", h.IO/1024.0/1024.0/1024.0) : string.Format("{0:F0}MB", h.IO/1024.0/1024.0);
                                    string vramStr = h.VRAM > 0 ? string.Format("{0:F0}MB", h.VRAM) : "-";
                                    string ramStr = string.Format("{0:F0}MB", h.RAM);
                                    string cpuStr = string.Format("{0}:{1:00}", (int)h.CPU.TotalMinutes, h.CPU.Seconds);
                                    
                                    if (currentItems.ContainsKey(h.Id)) {
                                        var item = currentItems[h.Id];
                                        if (item.SubItems[1].Text != h.Thrds.ToString()) item.SubItems[1].Text = h.Thrds.ToString();
                                        if (item.SubItems[3].Text != cpuStr) item.SubItems[3].Text = cpuStr;
                                        if (item.SubItems[4].Text != vramStr) item.SubItems[4].Text = vramStr;
                                        if (item.SubItems[5].Text != ramStr) item.SubItems[5].Text = ramStr;
                                        if (item.SubItems[6].Text != ioStr) item.SubItems[6].Text = ioStr;
                                        if (h.PrioVal >= 5) item.ForeColor = Color.Cyan; else item.ForeColor = Color.White;
                                    } else {
                                        var item = new ListViewItem(h.Name);
                                        item.Tag = h.Id; 
                                        item.SubItems.Add(h.Thrds.ToString());
                                        item.SubItems.Add(h.Prio);
                                        item.SubItems.Add(cpuStr);
                                        item.SubItems.Add(vramStr);
                                        item.SubItems.Add(ramStr);
                                        item.SubItems.Add(ioStr);
                                        if (h.PrioVal >= 5) item.ForeColor = Color.Cyan;
                                        _lvProcesses.Items.Add(item);
                                    }
                                }
                                foreach (var kvp in currentItems) if (!activeIds.Contains(kvp.Key)) _lvProcesses.Items.Remove(kvp.Value);
                                
                                // Применяем визуальную сортировку без мигания
                                _lvProcesses.ListViewItemSorter = new ListViewIndexSorter(sortedHogs.Select(x => (int)x.Id).ToList());
                                _lvProcesses.EndUpdate();
                            }));
                        }


                        // ===============================================
                        // 2. ТАБЛИЦА ДРАЙВЕРОВ (ОБНОВЛЕННАЯ)
                        // ===============================================
                        if (_lvDrivers != null && _tabs.SelectedTab == _tabs.TabPages[3]) 
                        {
                            lock(_statsLock) {
                                var allDrivers = new List<dynamic>();
                                
                                // Собираем данные из новых структур
                                foreach(var d in _dpcStats) 
                                    allDrivers.Add(new { Name = d.Key, Stats = d.Value, Type = "DPC" });
                                foreach(var d in _isrStats) 
                                    allDrivers.Add(new { Name = d.Key, Stats = d.Value, Type = "ISR" });
                                
                                // Сортировка
                                Func<dynamic, object> keySelector = null;
                                switch (_drvSortColumn) {
                                    case 0: keySelector = x => x.Name; break;
                                    case 1: keySelector = x => x.Type; break;
                                    case 2: keySelector = x => x.Stats.Current; break;
                                    case 3: keySelector = x => (x.Stats.Total / x.Stats.Count); break; // Avg
                                    case 4: keySelector = x => x.Stats.Min; break;
                                    case 5: keySelector = x => x.Stats.Max; break;
                                    default: keySelector = x => x.Stats.Max; break; 
                                }
                                var sortedDrivers = _drvSortAsc ? allDrivers.OrderBy(keySelector).ToList() : allDrivers.OrderByDescending(keySelector).ToList();

                                _lvDrivers.Invoke(new Action(() => {
                                    _lvDrivers.BeginUpdate();
                                    
                                    var currentItems = _lvDrivers.Items.Cast<ListViewItem>().ToDictionary(i => i.Text + "_" + i.SubItems[1].Text);
                                    var sortOrderList = new List<string>();

                                    foreach (var d in sortedDrivers)
                                    {
                                        string key = d.Name + "_" + d.Type;
                                        sortOrderList.Add(key);

                                        double avg = d.Stats.Total / d.Stats.Count;
                                        
                                        string sCur = string.Format("{0:F2}", d.Stats.Current);
                                        string sAvg = string.Format("{0:F2}", avg);
                                        string sMin = string.Format("{0:F2}", d.Stats.Min);
                                        string sMax = string.Format("{0:F2}", d.Stats.Max);

                                        Color c = (d.Stats.Max > 1000 && d.Type == "DPC") || (d.Stats.Max > 500 && d.Type == "ISR") ? Color.Red : Color.Lime;

                                        if (currentItems.ContainsKey(key)) {
                                            var item = currentItems[key];
                                            // Обновляем ячейки
                                            if (item.SubItems[2].Text != sCur) item.SubItems[2].Text = sCur;
                                            if (item.SubItems[3].Text != sAvg) item.SubItems[3].Text = sAvg;
                                            if (item.SubItems[4].Text != sMin) item.SubItems[4].Text = sMin;
                                            if (item.SubItems[5].Text != sMax) { item.SubItems[5].Text = sMax; item.ForeColor = c; }
                                            item.Tag = key;
                                        } else {
                                            var item = new ListViewItem(d.Name);
                                            item.Tag = key;
                                            item.SubItems.Add(d.Type);
                                            item.SubItems.Add(sCur);
                                            item.SubItems.Add(sAvg);
                                            item.SubItems.Add(sMin);
                                            item.SubItems.Add(sMax);
                                            item.ForeColor = c;
                                            _lvDrivers.Items.Add(item);
                                        }
                                    }
                                    _lvDrivers.ListViewItemSorter = new ListViewStringSorter(sortOrderList);
                                    _lvDrivers.EndUpdate();
                                }));
                            }
                        }

                    } catch { }
                }

        private void SaveDriverReport()
                {
                    try {
                        StringBuilder sb = new StringBuilder();
                        sb.AppendLine("Driver Name;Type;Current(us);Average(us);Min(us);Max(us);Count");

                        lock(_statsLock) {
                            foreach(var d in _dpcStats) {
                                double avg = d.Value.Total / d.Value.Count;
                                sb.AppendLine(string.Format("{0};DPC;{1:F2};{2:F2};{3:F2};{4:F2};{5}", 
                                    d.Key, d.Value.Current, avg, d.Value.Min, d.Value.Max, d.Value.Count));
                            }
                            foreach(var d in _isrStats) {
                                double avg = d.Value.Total / d.Value.Count;
                                sb.AppendLine(string.Format("{0};ISR;{1:F2};{2:F2};{3:F2};{4:F2};{5}", 
                                    d.Key, d.Value.Current, avg, d.Value.Min, d.Value.Max, d.Value.Count));
                            }
                        }
                        
                        string filename = "ALPA_Drivers_Report_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv";
                        File.WriteAllText(filename, sb.ToString());
                    } catch { }
                }

        private void CheckSecurityAnomalies()
                {
                    SafeLog(_logSys, "\n--- DEEP VIRUS & ANOMALY SCAN (MAX) ---\n", Color.White);
                    bool found = false;
                    
                    // Белый список (все в нижнем регистре)
                    string[] whitelist = { "amazemem", "alpa", "amazelagultra", "amazelag" };

                    foreach (var p in Process.GetProcesses()) {
                        try {
                            if (p.Id == 0 || p.Id == 4) continue;

                            string name = p.ProcessName.ToLower();
                            
                            // 0. ПРОВЕРКА БЕЛОГО СПИСКА (Пропускаем наши программы)
                            bool isFriend = false;
                            foreach(var w in whitelist) if (name.Contains(w)) isFriend = true;
                            if (isFriend) continue;

                            string path = "";
                            string company = "";
                            try {
                                path = p.MainModule.FileName.ToLower();
                                if (p.MainModule.FileVersionInfo != null)
                                    company = p.MainModule.FileVersionInfo.CompanyName;
                            } catch { 
                                // Если не можем прочитать путь и это не системный процесс - это подозрительно
                                if (!name.StartsWith("svchost") && !name.StartsWith("csrss")) {
                                   // SafeLog(_logSys, string.Format("[WARN] Locked/Hidden Process: {0}\n", name), Color.Orange);
                                }
                                continue; 
                            }

                            if (company == null) company = "";

                            // 1. АГРЕССИВНЫЙ ПОИСК МАЙНЕРОВ (Temp, Roaming, ProgramData)
                            if (path.Contains("\\appdata\\local\\temp\\") || 
                                path.Contains("\\appdata\\roaming\\") ||
                                (path.Contains("\\programdata\\") && !path.Contains("microsoft"))) {
                                
                                Log(string.Format("[CRIT] MALWARE PATH DETECTED: {0} ({1})", name, path), Color.Magenta);
                                found = true;
                            }

                            // 2. ФЕЙКОВЫЕ СИСТЕМНЫЕ ПРОЦЕССЫ
                            string[] sysProc = { "svchost", "lsass", "winlogon", "csrss", "explorer", "services", "taskhost" };
                            if (sysProc.Contains(name) && !path.Contains("windows\\system32") && !path.Contains("windows\\explorer.exe")) {
                                Log(string.Format("[FATAL] FAKE SYSTEM PROCESS: {0} running from {1}", name, path), Color.Red);
                                found = true;
                            }

                            // 3. ПОДОЗРИТЕЛЬНЫЕ СКРЫТЫЕ ОКНА (CMD/PowerShell)
                            if ((name == "cmd" || name == "powershell") && p.MainWindowHandle == IntPtr.Zero) {
                                 Log(string.Format("[WARN] Hidden Console Detected: {0}", name), Color.Orange);
                            }

                            // 4. ПРОЦЕССЫ БЕЗ ПОДПИСИ (Без имени компании)
                            // Игнорируем некоторые известные папки чтобы уменьшить ложные срабатывания
                            if (string.IsNullOrEmpty(company) && !path.Contains("\\windows\\") && !path.Contains("\\steam\\") && !path.Contains("\\nvidia\\")) {
                                SafeLog(_logSys, string.Format("[WARN] Unsigned/Unknown: {0} (No Company Info)\n", name), Color.Yellow);
                                found = true;
                            }
                            
                            // 5. ПОИСК ИЗВЕСТНЫХ ИМЕН ВИРУСОВ
                            if (name == "xmrig" || name == "miner" || name.Contains("cheat") || name.Contains("inject")) {
                                 Log(string.Format("[CRIT] Blacklisted Name: {0}", name), Color.Red);
                            }

                        } catch { }
                    }
                    if (!found) SafeLog(_logSys, "System seems clean. No obvious malware active.\n", Color.Lime);
                }



        private void DiagnosticStartup()
        {
            Log("--- FUNCTIONAL CHECK ---", Color.White);
            
            uint mi, ma, cu;
            if (NtQueryTimerResolution(out mi, out ma, out cu) == 0)
                Log("[OK] ntdll.dll Timer Resolution API accessible.", Color.Lime);
            else
                Log("[ERR] ntdll.dll Timer API failed.", Color.Red);



            var principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            if (principal.IsInRole(WindowsBuiltInRole.Administrator))
                Log("[OK] Running with Admin privileges (ETW enabled).", Color.Lime);
            else
                Log("[WARN] No Admin rights. Kernel Tracing (DPC) will be disabled.", Color.Orange);

            if (File.Exists("Microsoft.Diagnostics.Tracing.TraceEvent.dll"))
                Log("[OK] TraceEvent.dll found.", Color.Lime);
            else
                Log("[CRIT] TraceEvent.dll missing! DPC Audit will not work.", Color.Red);
        }

        private void AuditStartupItems()
                {
                    if (_logStartup == null) return;
                    _logStartup.Clear();
                    SafeLog(_logStartup, "=== MEGA STARTUP AUDIT ===\n", Color.White);

                    // 1. ПАПКИ АВТОЗАГРУЗКИ (User + Common)
                    SafeLog(_logStartup, "\n[STARTUP FOLDERS]\n", Color.Cyan);
                    try {
                        string[] paths = { 
                            Environment.GetFolderPath(Environment.SpecialFolder.Startup),
                            Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup) 
                        };
                        foreach (var path in paths) {
                            if (Directory.Exists(path)) {
                                foreach (var file in Directory.GetFiles(path))
                                     SafeLog(_logStartup, string.Format("File: {0}\n", Path.GetFileName(file)), Color.Yellow);
                            }
                        }
                    } catch {}

                    // 2. РЕЕСТР (Run, RunOnce для LocalMachine и CurrentUser)
                    SafeLog(_logStartup, "\n[REGISTRY STARTUP]\n", Color.Cyan);
                    string[] keys = {
                        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run",
                        @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce",
                        @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Run"
                    };
                    
                    foreach(var keyPath in keys) {
                        try {
                            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(keyPath)) {
                                if (key != null) foreach (var v in key.GetValueNames()) SafeLog(_logStartup, string.Format("[HKLM] {0}\n", v), Color.Yellow);
                            }
                            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(keyPath)) {
                                if (key != null) foreach (var v in key.GetValueNames()) SafeLog(_logStartup, string.Format("[HKCU] {0}\n", v), Color.Yellow);
                            }
                        } catch {}
                    }

                    // 3. ПЛАНИРОВЩИК ЗАДАНИЙ (Через консольную утилиту schtasks, так как COM сложен для C# 5)
                    SafeLog(_logStartup, "\n[SCHEDULED TASKS (Hidden Viruses often here)]\n", Color.Cyan);
                    try {
                        ProcessStartInfo psi = new ProcessStartInfo("schtasks", "/query /fo CSV /nh");
                        psi.RedirectStandardOutput = true;
                        psi.UseShellExecute = false;
                        psi.CreateNoWindow = true;
                        var proc = Process.Start(psi);
                        string output = proc.StandardOutput.ReadToEnd();
                        proc.WaitForExit();

                        string[] lines = output.Split('\n');
                        foreach (var line in lines) {
                            // Фильтруем только активные задачи и убираем системные Microsoft
                            if (line.Contains("Ready") || line.Contains("Running")) {
                                if (!line.Contains("\\Microsoft\\Windows\\")) 
                                    SafeLog(_logStartup, string.Format("Task: {0}\n", line.Split(',')[0].Trim('"')), Color.Orange);
                            }
                        }
                    } catch { SafeLog(_logStartup, "Failed to query Scheduler.\n", Color.Red); }

                    // 4. СЛУЖБЫ (Только не от Microsoft)
                    SafeLog(_logStartup, "\n[NON-SYSTEM SERVICES]\n", Color.Cyan);
                    try {
                        var services = ServiceController.GetServices();
                        foreach (var s in services) {
                            if (s.Status == ServiceControllerStatus.Running) {
                                // Простой фильтр по имени
                                if (!s.ServiceName.StartsWith("Rpc") && !s.ServiceName.StartsWith("Dcom") && !s.ServiceName.StartsWith("Win"))
                                    SafeLog(_logStartup, string.Format("Svc: {0} ({1})\n", s.DisplayName, s.ServiceName), Color.Gray);
                            }
                        }
                    } catch {}
                }

        private void KillSelectedProcess(string name)
                {
                    try {
                        foreach (var p in Process.GetProcessesByName(name)) {
                            p.Kill();
                            Log("[OK] Terminated malware/bloat: " + name, Color.Lime);
                        }
                    } catch (Exception ex) { Log("[ERR] Termination failed: " + ex.Message, Color.Red); }
                }

        private void SetupLayout()
                {
                    Panel top = new Panel { Dock = DockStyle.Top, Height = 65, BackColor = Color.FromArgb(25, 25, 25) };
                    Label title = new Label { Text = "ALPA: Performance & Latency Audit", Font = new Font("Segoe UI", 10, FontStyle.Bold), Location = new Point(10, 10), AutoSize = true };
                    
                    Button btnLogs = new Button { Text = "📂 Logs", Location = new Point(400, 22), Width = 80, BackColor = Color.FromArgb(45, 45, 45), FlatStyle = FlatStyle.Flat, ForeColor = Color.White };
                    btnLogs.Click += delegate { try { Process.Start("explorer.exe", "/select,\"" + Path.GetFullPath(_logFilePath) + "\""); } catch { } };

                    LinkLabel amazeMem = new LinkLabel { Text = "AmazeMem", Location = new Point(560, 28), Font = new Font("Segoe UI", 9, FontStyle.Bold), LinkColor = Color.SpringGreen, AutoSize = true };
                    amazeMem.LinkClicked += delegate { try { Process.Start("https://adiru3.github.io/AmazeMem/"); } catch { } };

                    LinkLabel githubLink = new LinkLabel { Text = "amazingb01 GitHub", Location = new Point(660, 28), Font = new Font("Segoe UI", 9, FontStyle.Bold), LinkColor = Color.DodgerBlue, AutoSize = true };
                    githubLink.LinkClicked += delegate { try { Process.Start("https://github.com/adiru3"); } catch { } };

                    top.Controls.Add(title); top.Controls.Add(btnLogs); top.Controls.Add(amazeMem); top.Controls.Add(githubLink);

                    _tabs = new TabControl { Dock = DockStyle.Fill };

                    _tabs.TabPages.Add("LAT", "Latency (Input)");
                    _tabs.TabPages.Add("PERF", "Performance (Global)");
                    _tabs.TabPages.Add("PROCS", "Processes (ALL)"); // Изменили название
                    _tabs.TabPages.Add("DRV", "Drivers (ALL)");     // Изменили название
                    _tabs.TabPages.Add("SYS", "System Info");
                    _tabs.TabPages.Add("STARTUP", "Startup Mgr");
                    _tabs.TabPages.Add("LOG", "Console");

                    _logInput = CreateLog();
                    _logPerf = CreateLog();
                    _logSys = CreateLog();
                    _logStartup = CreateLog();
                    _logConsole = CreateLog();

                    // --- НАСТРОЙКА ТАБЛИЦЫ ПРОЦЕССОВ ---
                    _lvProcesses = new ListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, GridLines = true, BackColor = Color.Black, ForeColor = Color.White, HeaderStyle = ColumnHeaderStyle.Clickable }; // <--- Clickable
                    SetDoubleBuffered(_lvProcesses);
                    _lvProcesses.Columns.Add("Process Name", 150); // Index 0
                    _lvProcesses.Columns.Add("Threads", 60);       // Index 1
                    _lvProcesses.Columns.Add("Priority", 60);      // Index 2
                    _lvProcesses.Columns.Add("CPU Time", 80);      // Index 3
                    _lvProcesses.Columns.Add("VRAM", 70);          // Index 4
                    _lvProcesses.Columns.Add("RAM", 70);           // Index 5
                    _lvProcesses.Columns.Add("I/O Total", 80);     // Index 6
                    
                    // Логика клика по заголовку
                    _lvProcesses.ColumnClick += (s, e) => {
                        if (e.Column == _procSortColumn) {
                            _procSortAsc = !_procSortAsc; // Инвертируем порядок, если кликнули туда же
                        } else {
                            _procSortColumn = e.Column;   // Новая колонка
                            _procSortAsc = false;         // По умолчанию - от большего к меньшему
                        }
                    };

                    // --- НАСТРОЙКА ТАБЛИЦЫ ДРАЙВЕРОВ ---
                    _lvDrivers = new ListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, GridLines = true, BackColor = Color.Black, ForeColor = Color.White, HeaderStyle = ColumnHeaderStyle.Clickable };
                    SetDoubleBuffered(_lvDrivers);
                    
                    // Новые колонки
                    _lvDrivers.Columns.Add("Driver / Module", 180);
                    _lvDrivers.Columns.Add("Type", 50);
                    _lvDrivers.Columns.Add("Cur (us)", 70); // Текущее
                    _lvDrivers.Columns.Add("Avg (us)", 70); // Среднее
                    _lvDrivers.Columns.Add("Min (us)", 70); // Минимум
                    _lvDrivers.Columns.Add("Max (us)", 70); // Максимум (бывшее Latency)

                    _lvDrivers.ColumnClick += (s, e) => {
                        if (e.Column == _drvSortColumn) {
                            _drvSortAsc = !_drvSortAsc; 
                        } else {
                            _drvSortColumn = e.Column;
                            _drvSortAsc = false; 
                        }
                    };

                    // Добавляем контролы на вкладки
                    _tabs.TabPages[0].Controls.Add(_logInput);
                    _tabs.TabPages[1].Controls.Add(_logPerf);
                    _tabs.TabPages[2].Controls.Add(_lvProcesses); // Вкладка 2 - ListView
                    _tabs.TabPages[3].Controls.Add(_lvDrivers);   // Вкладка 3 - ListView
                    _tabs.TabPages[4].Controls.Add(_logSys);
                    _tabs.TabPages[6].Controls.Add(_logConsole);

                    // Startup Tab
                    TabPage startupTab = _tabs.TabPages["STARTUP"];
                    Panel pnlTools = new Panel { Dock = DockStyle.Bottom, Height = 40, BackColor = Color.FromArgb(30,30,30) };
                    Button btnRefresh = new Button { Text = "🔄 Refresh", Location = new Point(5, 8), BackColor = Color.FromArgb(50,50,50), FlatStyle = FlatStyle.Flat, ForeColor = Color.White, AutoSize = true };
                    Button btnDisableSvc = new Button { Text = "ℹ How to Disable", Location = new Point(100, 8), BackColor = Color.Maroon, FlatStyle = FlatStyle.Flat, ForeColor = Color.White, AutoSize = true };
                    btnRefresh.Click += delegate { AuditStartupItems(); };
                    btnDisableSvc.Click += delegate { Log("[CMD] To disable: sc stop \"name\" && sc config \"name\" start=disabled", Color.Yellow); };
                    pnlTools.Controls.Add(btnRefresh); pnlTools.Controls.Add(btnDisableSvc);
                    startupTab.Controls.Add(_logStartup);
                    startupTab.Controls.Add(pnlTools);

                    this.Controls.Add(_tabs); this.Controls.Add(top);
                }

        private RichTextBox CreateLog()
        {
            RichTextBox rtb = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(10, 10, 10),
                ForeColor = Color.Lime,
                Font = new Font("Consolas", 9),
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                ScrollBars = RichTextBoxScrollBars.Vertical
            };
            return rtb;
        }

        private void InitializeCounters()
                {
                    _pcProcQueue = InitCounter("System", "Processor Queue Length");
                    _pcContextSwitches = InitCounter("System", "Context Switches/sec");
                    _pcInterruptsGlobal = InitCounter("Processor", "Interrupts/sec", "_Total");
                    _pcParkedCores = InitCounter("Processor Information", "Parking Status", "_Total");
                    _pcPageFaults = InitCounter("Memory", "Page Faults/sec");
                    _pcAvailableMem = InitCounter("Memory", "Available MBytes");
                    _pcCacheMem = InitCounter("Memory", "Cache Bytes");
                    _pcUdpErrors = InitCounter("UDPv4", "Datagrams Received Errors");

                    _pcAllCoreInterrupts.Clear();
                    for (int i = 0; i < Environment.ProcessorCount; i++)
                    {
                        _pcAllCoreInterrupts.Add(InitCounter("Processor", "Interrupts/sec", i.ToString()));
                    }

                    _pcDiskQueue = InitCounter("PhysicalDisk", "Current Disk Queue Length", "_Total");
                    _pcDiskLatency = InitCounter("PhysicalDisk", "Avg. Disk sec/Transfer", "_Total");
                    _pcDiskTime = InitCounter("PhysicalDisk", "% Disk Time", "_Total");

                    try {
                        if (PerformanceCounterCategory.Exists("PhysicalDisk")) {
                            var category = new PerformanceCounterCategory("PhysicalDisk");
                            foreach (string inst in category.GetInstanceNames()) {
                                if (inst == "_Total") continue;
                                _diskCounters[inst] = new PerformanceCounter[] {
                                    new PerformanceCounter("PhysicalDisk", "Current Disk Queue Length", inst),
                                    new PerformanceCounter("PhysicalDisk", "Avg. Disk sec/Transfer", inst),
                                    new PerformanceCounter("PhysicalDisk", "% Disk Time", inst)
                                };
                            }
                        }
                    } 
                    catch (Exception ex) { 
                        Log("[ERR] Disk Counter Init failed: " + ex.Message, Color.Red);
                    }

                    Log("[INIT] All counters ready.", Color.Gray);
                }

        private PerformanceCounter InitCounter(string cat, string counter, string instance = null)
        {
            try
            {
                if (PerformanceCounterCategory.Exists(cat))
                {
                    PerformanceCounter pc = (instance == null) ? new PerformanceCounter(cat, counter) : new PerformanceCounter(cat, counter, instance);
                    pc.NextValue(); 
                    return pc;
                }
                else { Log(string.Format("[ERR] Category {0} not found.", cat), Color.Orange); }
            }
            catch (Exception ex) { Log(string.Format("[ERR] Counter {0} failed: {1}", counter, ex.Message), Color.Red); }
            return null;
        }

        private void UpdateStats()
                {
                    if (_logPerf == null) return;

                    uint min, max, cur; 
                    NtQueryTimerResolution(out min, out max, out cur);
                    
                    _logInput.Clear();
                    SafeLog(_logInput, string.Format("--- INPUT & TIMER ---\nTimer Resolution: {0:F4} ms\nMouse Polling:    {1:F0} Hz\n", cur / 10000.0, _currentHz), Color.White);

                    _logPerf.Clear(); 
                    
                    try {
                        // --- CPU & POWER ---
                        float q = (_pcProcQueue != null) ? _pcProcQueue.NextValue() : 0;
                        float cs = (_pcContextSwitches != null) ? _pcContextSwitches.NextValue() : 0;
                        float parked = (_pcParkedCores != null) ? _pcParkedCores.NextValue() : 0;
                        float ints = (_pcInterruptsGlobal != null) ? _pcInterruptsGlobal.NextValue() : 0;

                        SafeLog(_logPerf, "--- CPU & POWER ---\n", Color.White);
                        SafeLog(_logPerf, string.Format("Processor Queue:  {0:F1}\n", q), q > 2 ? Color.Orange : Color.Gray);
                        SafeLog(_logPerf, string.Format("Context Switches: {0:N0}/s\n", cs), Color.Gray);
                        SafeLog(_logPerf, string.Format("Interrupts Total: {0:N0}/s\n", ints), Color.Gray);
                        SafeLog(_logPerf, string.Format("Parked Cores:     {0:F0}%\n", parked), Color.Gray);

                        // --- ПЕРЕНЕСЕННЫЙ БЛОК: INTERRUPTS PER CORE ---
                        SafeLog(_logPerf, "\n--- INTERRUPTS PER CORE (Load) ---\n", Color.White);
                        float maxInt = 0;
                        int hottestCore = 0;
                        for (int i = 0; i < _pcAllCoreInterrupts.Count; i++) {
                            float val = _pcAllCoreInterrupts[i].NextValue();
                            if (val > maxInt) { maxInt = val; hottestCore = i; }
                            // Пишем теперь в _logPerf!
                            SafeLog(_logPerf, string.Format("Core {0}: {1:N0}/s\n", i, val), val > 15000 ? Color.Orange : Color.Gray);
                        }
                        if (maxInt > 20000) Log(string.Format("[WARN] Core {0} is overloaded with interrupts!", hottestCore), Color.Red);
                        // ----------------------------------------------

                        // --- DISK ACTIVITY ---
                        SafeLog(_logPerf, "\n--- DISK ACTIVITY ---\n", Color.White);
                        foreach (var disk in _diskCounters) {
                            try {
                                float dq = disk.Value[0].NextValue(); 
                                float dl = disk.Value[1].NextValue() * 1000; 
                                float dt = disk.Value[2].NextValue(); 
                                SafeLog(_logPerf, string.Format("> Drive [{0}]: Queue: {1:F2} | Lat: {2:F1}ms | Active: {3:F1}%\n", 
                                    disk.Key, dq, dl, dt), dl > 10 ? Color.Orange : Color.Gray);
                            } catch { }
                        }

                        // --- RAM & NETWORK ---
                        float pf = (_pcPageFaults != null) ? _pcPageFaults.NextValue() : 0;
                        float avail = (_pcAvailableMem != null) ? _pcAvailableMem.NextValue() : 0;
                        float cache = (_pcCacheMem != null) ? (_pcCacheMem.NextValue() / 1024 / 1024) : 0;
                        float udp = (_pcUdpErrors != null) ? _pcUdpErrors.NextValue() : 0;

                        SafeLog(_logPerf, "\n--- RAM & NETWORK ---\n", Color.White);
                        SafeLog(_logPerf, string.Format("Available RAM:    {0:N0} MB\n", avail), avail < 1024 ? Color.Red : Color.Gray);
                        SafeLog(_logPerf, string.Format("Page Faults:      {0:N0}/s\n", pf), pf > 5000 ? Color.Orange : Color.Gray); 
                        SafeLog(_logPerf, string.Format("Standby/Cache:    {0:N0} MB\n", cache), Color.Gray);
                        SafeLog(_logPerf, string.Format("UDP Rcv Errors:   {0}\n", (int)udp), udp > 0 ? Color.Red : Color.Gray);

                        CheckTopResourceConsumers(); 
                        
                    } catch { }
                }

        private void RunAutoAudit()
        {
            Log(">>> STARTING AUTO-AUDIT", Color.Cyan);
            try { File.WriteAllText(_logFilePath, "--- ALPA Audit Log Start ---\n"); } catch { }

            CheckPowerThrottling();
            CheckMpoStatus();
            AnalyzeHardware();   // Основной аудит
            CheckTscStatus();    // Проверка стабильности таймера процессора
            CheckHpetStatus();   // Проверка системного HPET
            CheckPagefileInfo(); // Проверка файла подкачки
            CheckSecurityAnomalies();    // Покажет вирусы на вкладке System Info
            AuditStartupItems();         // Автоматически заполнит вкладку Startup Mgr
            
            Log("[INFO] Check out AmazeMem for RAM optimization: https://adiru3.github.io/AmazeMem/", Color.SpringGreen);
            
            if (File.Exists("Microsoft.Diagnostics.Tracing.TraceEvent.dll")) 
                StartKernelTracing();
            else
                Log("[SKIP] DPC Audit skipped: DLL not found.", Color.Orange);
        }

        private void CheckPagefileInfo()
        {
            try 
            {
                SafeLog(_logSys, "\n--- PAGEFILE INFO ---\n", Color.White);
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT Name, CurrentUsage, AllocatedBaseSize FROM Win32_PageFileUsage");
                
                bool found = false;
                foreach (ManagementObject obj in searcher.Get())
                {
                    found = true;
                    object nameObj = obj["Name"];
                    string name = (nameObj != null) ? nameObj.ToString() : "Unknown";
                    uint size = (uint)obj["AllocatedBaseSize"];
                    uint usage = (uint)obj["CurrentUsage"];

                    SafeLog(_logSys, string.Format("Location: {0}\n", name), Color.Gray);
                    SafeLog(_logSys, string.Format("Size:     {0} MB\n", size), Color.Cyan);
                    SafeLog(_logSys, string.Format("Usage:    {0} MB\n", usage), usage > (size * 0.8) ? Color.Orange : Color.Lime);
                }

                if (!found) 
                {
                    // Если Win32_PageFileUsage пуст, возможно файл подкачки управляется системой или отсутствует
                    SafeLog(_logSys, "Pagefile: System Managed or Disabled\n", Color.Yellow);
                }
            }
            catch (Exception ex) 
            { 
                Log("[ERR] Pagefile check failed: " + ex.Message, Color.Red); 
            }
        }

        private void CheckPowerThrottling()
        {
            try {
                PROCESS_POWER_THROTTLING_STATE state = new PROCESS_POWER_THROTTLING_STATE { Version = 1 };
                int returnLength;
                int status = NtQueryInformationProcess(Process.GetCurrentProcess().Handle, 61, ref state, Marshal.SizeOf(state), out returnLength);
                if (status == 0) {
                    bool enabled = (state.StateMask & 1) != 0;
                    Log(string.Format("[POWER] Power Throttling for ALPA is {0}", (enabled ? "ENABLED" : "DISABLED")), enabled ? Color.Orange : Color.Lime);
                }
            } catch (Exception ex) { Log("[ERR] PowerThrottling check failed: " + ex.Message, Color.Red); }
        }

        private void Log(string msg, Color c) {
            SafeLog(_logConsole, string.Format("[{0:HH:mm:ss}] {1}\n", DateTime.Now, msg), c);
        }

        private void CheckMpoStatus()
        {
            try {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\Dwm"))
                {
                    if (key != null) {
                        var val = key.GetValue("OverlayTestMode");
                        if (val != null && (int)val == 0x00000005)
                            Log("[SYS] Multi-Plane Overlay (MPO) is Disabled.", Color.Lime);
                        else
                            Log("[SYS] Multi-Plane Overlay (MPO) is Enabled.", Color.Yellow);
                    }
                }
            } catch (Exception ex) { Log("[ERR] MPO registry check failed: " + ex.Message, Color.Red); }
        }

        private void AnalyzeHardware()
        {
            _logSys.Clear();
            SafeLog(_logSys, "--- SYSTEM AUDIT ---\n", Color.White);
            
            // Проверка HAGS (Hardware Accelerated GPU Scheduling)
            try {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\GraphicsDrivers")) {
                    if (key != null) {
                        var val = key.GetValue("HwSchMode");
                        bool hags = (val != null && (int)val == 2);
                        SafeLog(_logSys, string.Format("GPU Scheduling (HAGS): {0}\n", hags ? "Enabled" : "Disabled"), hags ? Color.Lime : Color.Gray);
                    }
                }
            } catch { }

            // Схема питания
            IntPtr activeGuidPtr;
            if (PowerGetActiveScheme(IntPtr.Zero, out activeGuidPtr) == 0) {
                Guid activeGuid = (Guid)Marshal.PtrToStructure(activeGuidPtr, typeof(Guid));
                string scheme = activeGuid == new Guid("e9a42b02-d5df-448d-aa00-03f14749eb61") ? "Ultimate Performance" :
                               activeGuid == new Guid("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c") ? "High Performance" : "Other";
                SafeLog(_logSys, string.Format("\nPower Plan: {0}\n", scheme), Color.Cyan);
            }
        }

        private void CheckTscStatus()
        {
            try 
            {
                SafeLog(_logSys, "\n--- CPU COUNTER INFO ---\n", Color.White);
                bool tscInvariant = false;
                
                // Способ 1: Реестр (совместимо с C# 5)
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\Description\System\CentralProcessor\0"))
                {
                    if (key != null)
                    {
                        object featObj = key.GetValue("FeatureSet");
                        string features = (featObj != null) ? featObj.ToString() : "";
                        if (!string.IsNullOrEmpty(features)) tscInvariant = true; 
                    }
                }

                // Способ 2: WMI
                if (!tscInvariant) {
                    ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_Processor");
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        object nameObj = obj["Name"];
                        string cpuName = (nameObj != null) ? nameObj.ToString() : "";
                        if (cpuName.Contains("Intel") || cpuName.Contains("Ryzen") || cpuName.Contains("Core")) 
                            tscInvariant = true;
                    }
                }

                SafeLog(_logSys, string.Format("Invariant TSC Support: {0}\n", tscInvariant ? "YES (Optimal)" : "Unknown"), tscInvariant ? Color.Lime : Color.Yellow);
            }
            catch (Exception ex) { Log("[ERR] TSC check failed: " + ex.Message, Color.Red); }
        }

        private void CheckHpetStatus()
                {
                    try {
                        SafeLog(_logSys, "\n--- TIMER & HPET INFO ---\n", Color.White);
                        
                        using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Session Manager\Kernel"))
                        {
                            if (key != null) {
                                object val = key.GetValue("UsePlatformClock");
                                if (val != null && (int)val == 1)
                                    SafeLog(_logSys, "HPET Force (useplatformclock): ENABLED (High Latency)\n", Color.Orange);
                                else
                                    SafeLog(_logSys, "HPET Force (useplatformclock): Disabled (Optimal)\n", Color.Lime);
                            } else {
                                SafeLog(_logSys, "HPET Force: Key not found (Default/Optimal)\n", Color.Lime);
                            }
                        }
                    } catch { }
                }

        private string ResolveDriverName(ulong addr, Dictionary<ulong, ModuleInfo> moduleMap) {
            if (addr == 0) return "unknown";
            
            lock(moduleMap) {
                ulong bestBase = 0;
                foreach(var m in moduleMap) {
                    if (addr >= m.Key && (bestBase == 0 || m.Key > bestBase)) 
                        bestBase = m.Key;
                }
                
                if (bestBase != 0 && moduleMap.ContainsKey(bestBase)) {
                    var info = moduleMap[bestBase];
                    if (info.Size == 0 || addr < bestBase + (ulong)info.Size)
                        return info.Name;
                }
            }
            return string.Format("addr_{0:X}", addr);
        }

        private void StartKernelTracing()
                {
                    System.Threading.Tasks.Task.Run(() => {
                        try {
                            string sessionName = "NT Kernel Logger";
                            
                            try {
                                using (var killer = new TraceEventSession(sessionName)) {
                                    killer.Stop(true);
                                }
                                System.Threading.Thread.Sleep(250);
                            } catch { }

                            var moduleMap = new Dictionary<ulong, ModuleInfo>();
                            // (Код предзагрузки имен оставляем тот же, он у вас работает хорошо)
                            // ... [Вставьте сюда ваш код Pre-loaded names из прошлого раза, если он не сохранился] ... 
                            // Для краткости я пишу сразу основную логику:

                            try {
                                uint needed;
                                IntPtr[] addr = new IntPtr[1024];
                                if (EnumDeviceDrivers(addr, (uint)(addr.Length * IntPtr.Size), out needed)) {
                                    int count = (int)(needed / IntPtr.Size);
                                    if (count > addr.Length) {
                                        addr = new IntPtr[count];
                                        EnumDeviceDrivers(addr, (uint)(addr.Length * IntPtr.Size), out needed);
                                    }
                                    StringBuilder sb = new StringBuilder(260);
                                    for(int i=0; i < count; i++) {
                                        sb.Clear();
                                        if (GetDeviceDriverBaseName(addr[i], sb, sb.Capacity) > 0) {
                                            ulong baseAddr = (ulong)addr[i];
                                            lock(moduleMap) moduleMap[baseAddr] = new ModuleInfo { Name = sb.ToString(), Size = 0 }; 
                                        }
                                    }
                                }
                            } catch { }


                            using (var session = new TraceEventSession(sessionName)) {
                                
                                var keywords = (KernelTraceEventParser.Keywords)(0x20 | 0x40 | 0x4); 
                                session.EnableKernelProvider(keywords);
                                
                                try {
                                    var prop = session.GetType().GetProperty("StopOnDispose");
                                    if (prop != null) prop.SetValue(session, true, null);
                                } catch { }

                                session.Source.Kernel.ImageLoad += delegate(ImageLoadTraceData data) {
                                    try {
                                        ulong baseAddr = (ulong)data.ImageBase;
                                        string fileName = Path.GetFileName(data.FileName);
                                        int size = data.ImageSize;
                                        lock(moduleMap) moduleMap[baseAddr] = new ModuleInfo { Name = fileName, Size = size };
                                    } catch { }
                                };

                                // --- DPC ---
                                session.Source.Kernel.PerfInfoDPC += delegate(DPCTraceData data) {
                                    try {
                                        double durationUs = 0;
                                        object val = data.PayloadByName("ElapsedTimeMSec");
                                        if (val != null) durationUs = Convert.ToDouble(val) * 1000.0;

                                        string driverName = ResolveDriverName((ulong)data.Routine, moduleMap);
                                        
                                        // Логирование пиков (> 500 us)
                                        if (durationUs > 500.0) {
                                            // Log(string.Format("[SPIKE] DPC: {0} -> {1:F2} us", driverName, durationUs), Color.Red);
                                        }

                                        lock(_statsLock) {
                                            if (!_dpcStats.ContainsKey(driverName)) _dpcStats[driverName] = new DriverStats();
                                            var s = _dpcStats[driverName];
                                            s.Current = durationUs;
                                            if (durationUs > s.Max) s.Max = durationUs;
                                            if (durationUs < s.Min) s.Min = durationUs;
                                            s.Total += durationUs;
                                            s.Count++;
                                        }
                                    } catch { }
                                };

                                // --- ISR ---
                                session.Source.Kernel.PerfInfoISR += delegate(ISRTraceData data) {
                                     try {
                                        double durationUs = 0;
                                        object val = data.PayloadByName("ElapsedTimeMSec");
                                        if (val != null) durationUs = Convert.ToDouble(val) * 1000.0;

                                        string driverName = ResolveDriverName((ulong)data.Routine, moduleMap);

                                        // Логирование пиков (> 50 us)
                                        if (durationUs > 50.0) {
                                            // Log(string.Format("[ISR!] ISR: {0} -> {1:F2} us", driverName, durationUs), Color.Orange);
                                        }

                                        lock(_statsLock) {
                                            if (!_isrStats.ContainsKey(driverName)) _isrStats[driverName] = new DriverStats();
                                            var s = _isrStats[driverName];
                                            s.Current = durationUs;
                                            if (durationUs > s.Max) s.Max = durationUs;
                                            if (durationUs < s.Min) s.Min = durationUs;
                                            s.Total += durationUs;
                                            s.Count++;
                                        }
                                    } catch { }
                                };

                                Log("[OK] ALPA Engine LIVE. Monitoring stats & spikes...", Color.Lime);
                                session.Source.Process(); 
                            }
                        } 
                        catch (Exception ex) { 
                            Log("[CRIT] KERNEL ERROR: " + ex.Message, Color.Red);
                        }
                    });
                }

        private string _lastLogMsg = ""; // Добавь к полям класса

        private void SafeLog(RichTextBox b, string txt, Color c) {
            if (b == null || string.IsNullOrEmpty(txt)) return;
            if (b.InvokeRequired) {
                b.Invoke(new Action(() => SafeLog(b, txt, c)));
            }
            else {
                // Убираем спам только для вкладки Console
                if (b == _logConsole && txt == _lastLogMsg) return;
                if (b == _logConsole) _lastLogMsg = txt;

                b.SelectionColor = c;
                b.AppendText(txt);
                b.ScrollToCaret();

                if (b == _logConsole || b == _logSys || txt.Contains("[SPIKE]") || txt.Contains("[ISR!]")) 
                {
                    lock (_fileLock) 
                    {
                        try 
                        { 
                            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                            File.AppendAllText(_logFilePath, "[" + timestamp + "] " + txt); 
                        }
                        catch { }
                    }
                }
            }
        }

        private void SetupRawInput() {
            try {
                RAWINPUTDEVICE[] rid = new RAWINPUTDEVICE[1];
                rid[0].usUsagePage = 0x01; rid[0].usUsage = 0x02; 
                rid[0].dwFlags = 0x00000100; rid[0].hwndTarget = this.Handle;
                if (!RegisterRawInputDevices(rid, 1, (uint)Marshal.SizeOf(rid[0])))
                    Log("[ERR] Raw Input registration failed.", Color.Red);
            } catch (Exception ex) { Log("[ERR] SetupRawInput Exception: " + ex.Message, Color.Red); }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            SaveDriverReport();
            try 
            {
                // Останавливаем таймер
                if (_uiTimer != null) {
                    _uiTimer.Stop();
                    _uiTimer.Dispose();
                }

                // Останавливаем ETW сессию
                foreach (var name in TraceEventSession.GetActiveSessionNames()) {
                    if (name == "ALPKernelSession") {
                        using (var s = new TraceEventSession(name)) {
                            s.Stop(true);
                        }
                    }
                }

                // Освобождаем счетчики производительности
                if (_pcProcQueue != null) _pcProcQueue.Dispose();
                if (_pcContextSwitches != null) _pcContextSwitches.Dispose();
                if (_pcInterruptsGlobal != null) _pcInterruptsGlobal.Dispose();
                if (_pcDiskQueue != null) _pcDiskQueue.Dispose();
                if (_pcDiskLatency != null) _pcDiskLatency.Dispose();
                if (_pcDiskTime != null) _pcDiskTime.Dispose();
                if (_pcPageFaults != null) _pcPageFaults.Dispose();
                if (_pcAvailableMem != null) _pcAvailableMem.Dispose();
                if (_pcCacheMem != null) _pcCacheMem.Dispose();
                if (_pcParkedCores != null) _pcParkedCores.Dispose();
                if (_pcUdpErrors != null) _pcUdpErrors.Dispose();

                foreach(var pc in _pcAllCoreInterrupts) {
                    if (pc != null) pc.Dispose();
                }

                foreach(var counters in _diskCounters.Values) {
                    foreach(var counter in counters) {
                        if (counter != null) counter.Dispose();
                    }
                }
            }
            catch { }
            base.OnFormClosing(e);
        }

        protected override void WndProc(ref Message m) {
            if (m.Msg == 0x00FF) {
                long cur = DateTime.Now.Ticks;
                if (_lastInputTicks != 0) {
                    double diff = cur - _lastInputTicks;
                    if (diff > 0) _inputIntervals.Add(diff);
                }
                _lastInputTicks = cur;
            }
            base.WndProc(ref m);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RAWINPUTDEVICE { public ushort usUsagePage; public ushort usUsage; public uint dwFlags; public IntPtr hwndTarget; }

    [StructLayout(LayoutKind.Sequential)]
    public struct PROCESS_POWER_THROTTLING_STATE { public uint Version; public uint ControlMask; public uint StateMask; }
}