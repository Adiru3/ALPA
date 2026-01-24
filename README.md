# ⚡ ALPA (Amazing Latency Performance Audit) v1.2

**ALPA** is a high-performance system auditing utility developed by **amazingb01 (Adiru)**. It is designed for gamers, power users, and system optimizers to diagnose real-time latencies and performance bottlenecks that affect gameplay and system responsiveness.

<img width="504" height="206" alt="image" src="https://github.com/user-attachments/assets/2bde7a15-77d4-4c70-9f12-c336fd659093" />

---

## 🚀 Features

### 🔹 1. Latency & Input Analysis
* **Timer Resolution:** High-precision monitoring of the Windows system timer resolution via `ntdll.dll`.
* **Mouse Polling Rate:** Real-time calculation of mouse frequency (Hz) using `Raw Input` (WM_INPUT) to ensure your peripheral is performing at its rated speed.

### 🔹 2. Performance Monitoring
* **Power Throttling:** Detects if Windows is restricting process power via `NtQueryInformationProcess`.
* **Core Parking:** Monitors the percentage of parked CPU cores that cause wake-up latencies.
* **Processor Queue Length:** Detects if your CPU threads are being bottlenecked by too many active processes.
* **Context Switches:** Monitors how often the CPU switches between different execution threads—a high count often indicates background "noise" or bloatware.

### 🔹 3. Advanced Resource Audit (PERF)
* **Disk I/O:** Monitors Disk Queue Length, Response Time (ms), and Active Time to identify stutters during asset loading.
* **Memory Insight:** Tracks Page Faults, available RAM, and the System Cache (Standby List) volume.
* **Network & UDP:** Detects UDP receive errors to diagnose connection stability in online shooters.

### 🔹 4. Kernel Tracing (ETW)
* **DPC/ISR Audit:** Uses Event Tracing for Windows (ETW) to monitor **Deferred Procedure Calls (DPC)**.
* **Spike Detection:** Automatically logs any DPC spike exceeding **500µs** in red. This is the ultimate way to find problematic drivers causing micro-stutters and input lag.

### 🔹 5. System Diagnostics
* **Hardware Info:** Quick view of OS version, CPU thread count, and physical disk models (NVMe/SSD/HDD).
* **Auto-Audit:** The application automatically initiates a full system scan upon launch.

---

## 🛠 Technical Details

* **Language:** C# (.NET Framework 4.8)
* **Architecture:** Optimized for minimal overhead.
* **Author:** amazingb01 (Adiru)

---

## ⚠️ Important Requirements

### 🛡️ Run as Administrator
To access **Kernel Tracing** and monitor **DPC spikes**, ALPA must be launched with **Administrator Privileges**. 
* If launched as a standard user, the Console will display: `[WARN] Running as User. Kernel trace disabled!`.

## 📖 How to Use

1.  **Launch:** Run `ALPA.exe` as Administrator.
2.  **Auto-Audit:** Wait a few seconds for the console to confirm `[OK] Admin rights confirmed. Kernel trace enabled`.
3.  **Check Latency:** Go to the **LAT** tab and move your mouse quickly to see your real-time Polling Rate.
4.  **Find Stutters:** Keep ALPA running in the background while gaming. Check the **Console (LOG)** tab afterward. If you see `[SPIKE]` entries, a specific driver is delaying your CPU.

## 🔗 Connect with me

[![YouTube](https://img.shields.io/badge/YouTube-@adiruaim-FF0000?style=for-the-badge&logo=youtube)](https://www.youtube.com/@adiruaim)
[![TikTok](https://img.shields.io/badge/TikTok-@adiruhs-000000?style=for-the-badge&logo=tiktok)](https://www.tiktok.com/@adiruhs)
[![Donatello](https://img.shields.io/badge/Support-Donatello-orange?style=for-the-badge)](https://donatello.to/Adiru3)
