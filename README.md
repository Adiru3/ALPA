# ⚡ ALPA (Amazing Latency Performance Audit) v1.5

**ALPA** is a comprehensive system auditing and optimization utility developed by **amazingb01 (Adiru)**. It provides deep insight into Windows internals, helping gamers and power users diagnose input lag, micro-stutters, and hardware bottlenecks in real-time.

---

## 🚀 Features

<img width="850" height="508" alt="image" src="https://github.com/user-attachments/assets/f431e1c1-1619-4300-88ea-ed85a914705b" />

### 🔹 1. Advanced Driver Latency (Kernel Mode)
* **DPC & ISR Analysis:** Uses **Event Tracing for Windows (ETW)** to intercept kernel calls.
* **Real-Time Statistics:** Tracks **Current**, **Average**, **Minimum**, and **Maximum** latency (in µs) for every active driver.
* **Spike Detection:** Automatically logs high latency spikes (>500µs) causing frame drops.
* **CSV Export:** Automatically saves a detailed `ALPA_Drivers_Report.csv` upon exit for deeper analysis.

<img width="820" height="590" alt="image" src="https://github.com/user-attachments/assets/47c6d60e-ce10-4495-9bd5-f63bf7fd9c01" />

### 🔹 2. Process & Security Audit
* **Resource Monitor:** Detailed sorting by CPU Time, Threads, RAM, VRAM (GPU Memory), and Disk I/O.
* **Security Scanner:** Built-in heuristic detection for:
    * **Hidden Miners:** Checks specific paths (AppData/Temp) for disguised malware.
    * **Fake System Processes:** Detects fake `svchost.exe`, `csrss.exe`, etc., running from wrong directories.
    * **Hidden Consoles:** Identifies suspicious CMD/PowerShell windows running in the background.

<img width="819" height="452" alt="image" src="https://github.com/user-attachments/assets/897d212b-b4f2-4382-ac63-b749a1711c05" />

### 🔹 3. Performance & Hardware Monitor
* **Interrupts Per Core:** Visualizes interrupt load distribution across CPU cores to detect "Core 0" bottlenecks.
* **Global I/O:** Monitors total Internet bandwidth and Disk usage percentage.
* **Memory Insight:** Tracks Page Faults, Available RAM, and Standby Cache.
* **Disk Diagnostics:** Monitors Queue Length and Response Time for NVMe/SSD/HDD.

<img width="869" height="468" alt="image" src="https://github.com/user-attachments/assets/d74c6666-b0e4-48fc-8f35-f990a95d6660" />

### 🔹 4. Input & System Lag
* **Timer Resolution:** Displays the current Windows Timer Resolution (e.g., 0.5ms or 15.6ms).
* **Mouse Polling Rate:** Real-time Hz calculation using Raw Input.

<img width="820" height="452" alt="image" src="https://github.com/user-attachments/assets/ea0c9f25-671a-495b-849b-e1e2402a12d2" />

* **System Tweaks Check:**
    * **MPO (Multi-Plane Overlay):** Detects if MPO is Enabled/Disabled.
    * **HAGS:** Checks Hardware Accelerated GPU Scheduling status.
    * **HPET:** Verifies if High Precision Event Timer is forced.
    * **TSC Invariant:** Checks CPU timer stability.

<img width="875" height="585" alt="image" src="https://github.com/user-attachments/assets/a249b9f5-20e2-4de3-97f2-faa7b98eb362" />

### 🔹 5. Startup Manager
* **Deep Audit:** Scans multiple startup locations often missed by Task Manager:
    * Startup Folders (User/Common).
    * Registry Keys (Run/RunOnce for HKLM & HKCU).
    * **Task Scheduler:** Detects hidden tasks often used by malware.
    * **Non-System Services:** Lists active third-party services.

---

## 🛠 Technical Details

* **Version:** 1.5
* **Language:** C# (.NET Framework 4.8)
* **Core Tech:** ETW (KernelTraceControl), P/Invoke (NtQuerySystemInformation), PerformanceCounters.
* **Author:** amazingb01 (Adiru)

---

## ⚠️ Important Requirements

### 🛡️ Run as Administrator
To access **Kernel Tracing (DPC/ISR)**, **Pagefile Info**, and **Security Scans**, ALPA must be launched with **Administrator Privileges**.
* The application automatically creates a scheduled task (`ALPA_AutoRun`) to launch with highest privileges on logon if needed.

---

## 📖 How to Use

1. **Launch:** Run `ALPA.exe` as Administrator.
2. **Calibration:** Wait for `[OK] ALPA Engine LIVE` in the Console tab.
3. **Gaming Test:** Keep ALPA running in the background while playing.
4. **Analyze:**
    * Go to **Drivers (ALL)** to see which driver has the highest `Max (us)`.
    * Go to **Performance** to check if one CPU core is overloaded with Interrupts.
    * Check **ALPA_Log.txt** or the CSV report after closing for a summary of lag spikes.

---

## 🎮 Usage

### Interactive Mode
Run `LightweightAV.exe` to see the console menu:

* **Full System Scan:** Scans the entire `C:\` drive using multi-threading.
* **Memory Scan:** Checks all running processes for known signatures and heuristics.
* **Real-Time Monitor:** Watches for new/modified files instantly.
* **Add to Startup:** Adds the program to the Registry to start automatically (Minimized).
* **Scan Specific Path:** Scan a single file or folder (supports drag & drop).

### Silent / Tray Mode
To start the antivirus in the background (System Tray only), run:

```dos
LightweightAV.exe /minimized

> **Note:** Right-click the shield icon in the tray to open the console or exit.

---

## 🔍 Technical Details

* **Entropy Check:** Uses the Shannon entropy formula to calculate data density. Files with entropy $H > 7.5$ are flagged as suspicious (likely Packed, Encrypted, or Obfuscated).
  
  The formula used is:
  $$H = -\sum_{i=1}^{n} P(x_i) \log_2 P(x_i)$$

* **User Mode Hooking:** Uses `FileSystemWatcher` to monitor file system events (Created, Changed, Renamed) in real-time without the need for complex kernel-mode drivers.

* **P/Invoke:** Utilizes Windows API imports (`kernel32.dll`, `user32.dll`) to interact with the OS for tasks like managing console window visibility and process handling.

* **Registry Persistence:** Implements auto-start functionality by writing the executable path to:
  `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`

## 🔗 Connect with me

[![YouTube](https://img.shields.io/badge/YouTube-@adiruaim-FF0000?style=for-the-badge&logo=youtube)](https://www.youtube.com/@adiruaim)
[![TikTok](https://img.shields.io/badge/TikTok-@adiruhs-000000?style=for-the-badge&logo=tiktok)](https://www.tiktok.com/@adiruhs)
[![Donatello](https://img.shields.io/badge/Support-Donatello-orange?style=for-the-badge)](https://donatello.to/Adiru3)
