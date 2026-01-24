# ⚡ ALPA (Amazing Latency Performance Audit) v1.5

**ALPA** is a comprehensive system auditing and optimization utility developed by **amazingb01 (Adiru)**. It provides deep insight into Windows internals, helping gamers and power users diagnose input lag, micro-stutters, and hardware bottlenecks in real-time.

<img width="845" alt="ALPA Interface" src="https://github.com/user-attachments/assets/ebdfba59-4884-47cf-90d5-9fbc2de4cff6" />

---

## 🚀 Features

### 🔹 1. Advanced Driver Latency (Kernel Mode)
* **DPC & ISR Analysis:** Uses **Event Tracing for Windows (ETW)** to intercept kernel calls.
* **Real-Time Statistics:** Tracks **Current**, **Average**, **Minimum**, and **Maximum** latency (in µs) for every active driver.
* **Spike Detection:** Automatically logs high latency spikes (>500µs) causing frame drops.
* **CSV Export:** Automatically saves a detailed `ALPA_Drivers_Report.csv` upon exit for deeper analysis.

### 🔹 2. Process & Security Audit
* **Resource Monitor:** Detailed sorting by CPU Time, Threads, RAM, VRAM (GPU Memory), and Disk I/O.
* **Security Scanner:** Built-in heuristic detection for:
    * **Hidden Miners:** Checks specific paths (AppData/Temp) for disguised malware.
    * **Fake System Processes:** Detects fake `svchost.exe`, `csrss.exe`, etc., running from wrong directories.
    * **Hidden Consoles:** Identifies suspicious CMD/PowerShell windows running in the background.

### 🔹 3. Performance & Hardware Monitor
* **Interrupts Per Core:** Visualizes interrupt load distribution across CPU cores to detect "Core 0" bottlenecks.
* **Global I/O:** Monitors total Internet bandwidth and Disk usage percentage.
* **Memory Insight:** Tracks Page Faults, Available RAM, and Standby Cache.
* **Disk Diagnostics:** Monitors Queue Length and Response Time for NVMe/SSD/HDD.

### 🔹 4. Input & System Lag
* **Timer Resolution:** Displays the current Windows Timer Resolution (e.g., 0.5ms or 15.6ms).
* **Mouse Polling Rate:** Real-time Hz calculation using Raw Input.
* **System Tweaks Check:**
    * **MPO (Multi-Plane Overlay):** Detects if MPO is Enabled/Disabled.
    * **HAGS:** Checks Hardware Accelerated GPU Scheduling status.
    * **HPET:** Verifies if High Precision Event Timer is forced.
    * **TSC Invariant:** Checks CPU timer stability.

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

## 🔗 Connect with me

[![YouTube](https://img.shields.io/badge/YouTube-@adiruaim-FF0000?style=for-the-badge&logo=youtube)](https://www.youtube.com/@adiruaim)
[![TikTok](https://img.shields.io/badge/TikTok-@adiruhs-000000?style=for-the-badge&logo=tiktok)](https://www.tiktok.com/@adiruhs)
[![Donatello](https://img.shields.io/badge/Support-Donatello-orange?style=for-the-badge)](https://donatello.to/Adiru3)
