<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>ALPA v1.5 // Terminal Access</title>
    <style>
        :root {
            --bg: #000000;
            --text: #ffffff;
            --dim: #555555;
            --border: #1a1a1a;
            --accent: #00ffff;
            --warn: #ffff00;
            --error: #ff0000;
            --success: #00ff00;
        }

        body {
            background-color: var(--bg);
            color: var(--text);
            font-family: 'Consolas', 'Monaco', 'Courier New', monospace;
            margin: 0;
            padding: 20px;
            display: flex;
            justify-content: center;
        }

        .terminal-container {
            width: 100%;
            max-width: 900px;
            border: 1px solid var(--border);
            padding: 30px;
            box-shadow: 0 0 20px rgba(0,0,0,1);
        }

        .lang-switcher {
            display: flex;
            gap: 10px;
            margin-bottom: 20px;
            justify-content: flex-end;
        }

        .lang-btn {
            background: transparent;
            border: 1px solid var(--border);
            color: var(--dim);
            padding: 5px 10px;
            cursor: pointer;
            font-family: inherit;
            font-size: 12px;
        }

        .lang-btn.active {
            border-color: var(--accent);
            color: var(--accent);
        }

        .terminal-header {
            border-bottom: 1px double var(--border);
            padding-bottom: 20px;
            margin-bottom: 30px;
        }

        .br-line { color: var(--accent); font-weight: bold; font-size: 1.2rem; }
        .sub-line { color: var(--dim); font-size: 0.8rem; margin-top: 5px; }

        .section { margin-bottom: 35px; }
        .section-tag {
            background: #111;
            color: var(--dim);
            padding: 2px 10px;
            font-size: 11px;
            text-transform: uppercase;
            letter-spacing: 2px;
            display: inline-block;
            margin-bottom: 15px;
        }

        /* Стиль для картинок */
        .terminal-img {
            max-width: 100%;
            border: 1px solid #333;
            margin: 20px 0;
            display: block;
            opacity: 0.95;
        }
        
        .feature-block {
            margin-bottom: 40px;
            border-left: 2px solid #222;
            padding-left: 15px;
        }

        .feature-title {
            color: var(--accent);
            font-weight: bold;
            margin-bottom: 10px;
            display: block;
        }

        ul { list-style: none; padding: 0; margin: 0; }
        li { margin-bottom: 8px; font-size: 14px; position: relative; padding-left: 20px; line-height: 1.4; }
        li::before { content: ">"; position: absolute; left: 0; color: var(--dim); }

        b { color: #fff; }
        .code-inline { color: var(--success); font-weight: bold; }

        .action-row {
            margin-top: 50px;
            display: flex;
            gap: 20px;
            flex-wrap: wrap;
        }

        .btn {
            background: #fff;
            color: #000;
            padding: 10px 25px;
            text-decoration: none;
            font-weight: bold;
            font-size: 13px;
            border: 1px solid #fff;
        }

        .btn:hover { background: transparent; color: #fff; }

        .social-badges { margin-top: 30px; display: flex; gap: 10px; flex-wrap: wrap; }

        footer {
            margin-top: 50px;
            border-top: 1px solid var(--border);
            padding-top: 20px;
            font-size: 11px;
            color: var(--dim);
            text-align: center;
        }

        [data-lang] { display: none; }
        [data-lang].visible { display: block; }
    </style>
</head>
<body>

<div class="terminal-container">
    <div class="lang-switcher">
        <button class="lang-btn active" onclick="setLang('en')">EN</button>
        <button class="lang-btn" onclick="setLang('ru')">RU</button>
        <button class="lang-btn" onclick="setLang('ua')">UA</button>
        <button class="lang-btn" onclick="setLang('tr')">TR</button>
    </div>

    <header class="terminal-header">
        <div class="br-line">⚡ ALPA (Amazing Latency Performance Audit) v1.5</div>
        <div class="sub-line">ROOT@ADIRU:~# LOADING_FULL_FEATURE_SET...</div>
    </header>

    <img src="https://github.com/user-attachments/assets/f431e1c1-1619-4300-88ea-ed85a914705b" alt="Main Interface" class="terminal-img">

    <div class="section">
        <div class="section-tag">[01] SYSTEM_DESCRIPTION</div>
        <div data-lang="en" class="visible">
            <b>ALPA</b> is a comprehensive system auditing and optimization utility developed by <b>amazingb01 (Adiru)</b>. It provides deep insight into Windows internals, helping gamers and power users diagnose input lag, micro-stutters, and hardware bottlenecks in real-time.
        </div>
        <div data-lang="ru"><b>ALPA</b> — это комплексная утилита для аудита и оптимизации, разработанная <b>amazingb01 (Adiru)</b>. Она предоставляет глубокое понимание внутренних процессов Windows, помогая геймерам диагностировать задержки ввода (input lag), микро-фризы и узкие места оборудования в реальном времени.</div>
        <div data-lang="ua"><b>ALPA</b> — це комплексна утиліта для аудиту та оптимізації, розроблена <b>amazingb01 (Adiru)</b>. Вона надає глибоке розуміння внутрішніх процесів Windows, допомагаючи геймерам діагностувати затримки вводу, мікро-фризи та вузькі місця обладнання в реальному часі.</div>
        <div data-lang="tr"><b>ALPA</b>, <b>amazingb01 (Adiru)</b> tarafından geliştirilen kapsamlı bir sistem denetim ve optimizasyon aracıdır. Windows'un derinliklerine inerek oyuncuların giriş gecikmelerini, mikro takılmaları ve donanım darboğazlarını gerçek zamanlı olarak teşhis etmelerine yardımcı olur.</div>
    </div>

    <div class="section">
        <div class="section-tag">[02] CORE_FUNCTIONALITY_V1.5</div>
        
        <div data-lang="en" class="visible">
            
            <div class="feature-block">
                <span class="feature-title">🔹 1. Advanced Driver Latency (Kernel Mode)</span>
                <img src="https://github.com/user-attachments/assets/47c6d60e-ce10-4495-9bd5-f63bf7fd9c01" alt="Driver Latency" class="terminal-img">
                <ul>
                    <li><b>DPC & ISR Analysis:</b> Uses <b>Event Tracing for Windows (ETW)</b> to intercept kernel calls.</li>
                    <li><b>Real-Time Statistics:</b> Tracks <b>Current</b>, <b>Average</b>, <b>Minimum</b>, and <b>Maximum</b> latency (in µs) for every active driver.</li>
                    <li><b>Spike Detection:</b> Automatically logs high latency spikes (>500µs) causing frame drops.</li>
                    <li><b>CSV Export:</b> Automatically saves a detailed <span class="code-inline">ALPA_Drivers_Report.csv</span> upon exit.</li>
                </ul>
            </div>

            <div class="feature-block">
                <span class="feature-title">🔹 2. Process & Security Audit</span>
                <img src="https://github.com/user-attachments/assets/897d212b-b4f2-4382-ac63-b749a1711c05" alt="Security Scan" class="terminal-img">
                <ul>
                    <li><b>Resource Monitor:</b> Detailed sorting by CPU Time, Threads, RAM, VRAM (GPU Memory), and Disk I/O.</li>
                    <li><b>Security Scanner:</b> Built-in heuristic detection for:</li>
                    <li>- <b>Hidden Miners:</b> Checks specific paths (AppData/Temp) for disguised malware.</li>
                    <li>- <b>Fake System Processes:</b> Detects fake <span class="code-inline">svchost.exe</span>, <span class="code-inline">csrss.exe</span> running from wrong directories.</li>
                    <li>- <b>Hidden Consoles:</b> Identifies suspicious CMD/PowerShell windows running in the background.</li>
                </ul>
            </div>

            <div class="feature-block">
                <span class="feature-title">🔹 3. Performance & Hardware Monitor</span>
                <img src="https://github.com/user-attachments/assets/d74c6666-b0e4-48fc-8f35-f990a95d6660" alt="Performance Monitor" class="terminal-img">
                <ul>
                    <li><b>Interrupts Per Core:</b> Visualizes interrupt load distribution across CPU cores to detect "Core 0" bottlenecks.</li>
                    <li><b>Global I/O:</b> Monitors total Internet bandwidth and Disk usage percentage.</li>
                    <li><b>Memory Insight:</b> Tracks Page Faults, Available RAM, and Standby Cache.</li>
                    <li><b>Disk Diagnostics:</b> Monitors Queue Length and Response Time for NVMe/SSD/HDD.</li>
                </ul>
            </div>

            <div class="feature-block">
                <span class="feature-title">🔹 4. Input & System Lag</span>
                <img src="https://github.com/user-attachments/assets/ea0c9f25-671a-495b-849b-e1e2402a12d2" alt="Input Lag" class="terminal-img">
                <ul>
                    <li><b>Timer Resolution:</b> Displays the current Windows Timer Resolution (e.g., 0.5ms or 15.6ms).</li>
                    <li><b>Mouse Polling Rate:</b> Real-time Hz calculation using Raw Input.</li>
                    <li><b>System Tweaks Check:</b></li>
                    <li>- <b>MPO:</b> Detects if Multi-Plane Overlay is Enabled/Disabled.</li>
                    <li>- <b>HAGS:</b> Checks Hardware Accelerated GPU Scheduling status.</li>
                    <li>- <b>HPET:</b> Verifies if High Precision Event Timer is forced.</li>
                    <li>- <b>TSC Invariant:</b> Checks CPU timer stability.</li>
                </ul>
            </div>

            <div class="feature-block">
                <span class="feature-title">🔹 5. Startup Manager</span>
                <img src="https://github.com/user-attachments/assets/a249b9f5-20e2-4de3-97f2-faa7b98eb362" alt="Startup Manager" class="terminal-img">
                <ul>
                    <li><b>Deep Audit:</b> Scans startup locations often missed by Task Manager:</li>
                    <li>- Startup Folders (User/Common).</li>
                    <li>- Registry Keys (Run/RunOnce for HKLM & HKCU).</li>
                    <li>- <b>Task Scheduler:</b> Detects hidden tasks often used by malware.</li>
                    <li>- <b>Non-System Services:</b> Lists active third-party services.</li>
                </ul>
            </div>

        </div>

        <div data-lang="ru">
            <p style="color:var(--dim)">[Перевод основных функций]</p>
            <div class="feature-block">
                <span class="feature-title">🔹 1. Задержки драйверов (Ядро)</span>
                <img src="https://github.com/user-attachments/assets/47c6d60e-ce10-4495-9bd5-f63bf7fd9c01" class="terminal-img">
                <ul><li>Анализ DPC/ISR через ETW. Статистика (Мин/Макс/Среднее) и авто-лог пиков > 500мкс. Экспорт CSV.</li></ul>
            </div>
            <div class="feature-block">
                <span class="feature-title">🔹 2. Безопасность и Процессы</span>
                <img src="https://github.com/user-attachments/assets/897d212b-b4f2-4382-ac63-b749a1711c05" class="terminal-img">
                <ul><li>Сортировка по VRAM/CPU. Поиск скрытых майнеров, фейковых системных процессов и скрытых консолей.</li></ul>
            </div>
            <div class="feature-block">
                <span class="feature-title">🔹 3. Производительность</span>
                <img src="https://github.com/user-attachments/assets/d74c6666-b0e4-48fc-8f35-f990a95d6660" class="terminal-img">
                <ul><li>Распределение прерываний по ядрам (Core 0 bottleneck). Мониторинг очереди диска и интернета.</li></ul>
            </div>
            <div class="feature-block">
                <span class="feature-title">🔹 4. Ввод и Лаги</span>
                <img src="https://github.com/user-attachments/assets/ea0c9f25-671a-495b-849b-e1e2402a12d2" class="terminal-img">
                <ul><li>Hz мыши, Timer Resolution. Проверка MPO, HAGS, HPET и TSC.</li></ul>
            </div>
            <div class="feature-block">
                <span class="feature-title">🔹 5. Автозагрузка</span>
                <img src="https://github.com/user-attachments/assets/a249b9f5-20e2-4de3-97f2-faa7b98eb362" class="terminal-img">
                <ul><li>Глубокий скан: Реестр, Папки, Планировщик задач и Службы.</li></ul>
            </div>
        </div>

        <div data-lang="ua">
            <p style="color:var(--dim)">[Переклад основних функцій]</p>
            <div class="feature-block">
                <span class="feature-title">🔹 1. Затримки драйверів (Ядро)</span>
                <img src="https://github.com/user-attachments/assets/47c6d60e-ce10-4495-9bd5-f63bf7fd9c01" class="terminal-img">
                <ul><li>Аналіз DPC/ISR через ETW. Статистика (Мін/Макс/Середнє) та авто-лог піків > 500мкс. Експорт CSV.</li></ul>
            </div>
            <div class="feature-block">
                <span class="feature-title">🔹 2. Безпека та Процеси</span>
                <img src="https://github.com/user-attachments/assets/897d212b-b4f2-4382-ac63-b749a1711c05" class="terminal-img">
                <ul><li>Сортування по VRAM/CPU. Пошук прихованих майнерів, фейкових системних процесів та прихованих консолей.</li></ul>
            </div>
            <div class="feature-block">
                <span class="feature-title">🔹 3. Продуктивність</span>
                <img src="https://github.com/user-attachments/assets/d74c6666-b0e4-48fc-8f35-f990a95d6660" class="terminal-img">
                <ul><li>Розподіл переривань по ядрах (Core 0 bottleneck). Моніторинг черги диска та інтернету.</li></ul>
            </div>
            <div class="feature-block">
                <span class="feature-title">🔹 4. Ввід та Лаги</span>
                <img src="https://github.com/user-attachments/assets/ea0c9f25-671a-495b-849b-e1e2402a12d2" class="terminal-img">
                <ul><li>Hz миші, Timer Resolution. Перевірка MPO, HAGS, HPET та TSC.</li></ul>
            </div>
            <div class="feature-block">
                <span class="feature-title">🔹 5. Автозавантаження</span>
                <img src="https://github.com/user-attachments/assets/a249b9f5-20e2-4de3-97f2-faa7b98eb362" class="terminal-img">
                <ul><li>Глибокий скан: Реєстр, Папки, Планувальник завдань та Служби.</li></ul>
            </div>
        </div>

        <div data-lang="tr">
            <p style="color:var(--dim)">[Temel Özellikler Çevirisi]</p>
             <div class="feature-block">
                <span class="feature-title">🔹 1. Sürücü Gecikmesi (Çekirdek)</span>
                <img src="https://github.com/user-attachments/assets/47c6d60e-ce10-4495-9bd5-f63bf7fd9c01" class="terminal-img">
                <ul><li>ETW ile DPC/ISR analizi. İstatistikler (Min/Maks/Ort) ve > 500µs ani artış kaydı. CSV Dışa Aktarma.</li></ul>
            </div>
            <div class="feature-block">
                <span class="feature-title">🔹 2. Güvenlik ve Süreçler</span>
                <img src="https://github.com/user-attachments/assets/897d212b-b4f2-4382-ac63-b749a1711c05" class="terminal-img">
                <ul><li>VRAM/CPU sıralaması. Gizli madenciler, sahte sistem süreçleri ve gizli konsollar için tarama.</li></ul>
            </div>
            <div class="feature-block">
                <span class="feature-title">🔹 3. Performans</span>
                <img src="https://github.com/user-attachments/assets/d74c6666-b0e4-48fc-8f35-f990a95d6660" class="terminal-img">
                <ul><li>Çekirdek başına kesinti dağılımı (Core 0 darboğazı). Disk kuyruğu ve internet takibi.</li></ul>
            </div>
            <div class="feature-block">
                <span class="feature-title">🔹 4. Giriş ve Gecikme</span>
                <img src="https://github.com/user-attachments/assets/ea0c9f25-671a-495b-849b-e1e2402a12d2" class="terminal-img">
                <ul><li>Mouse Hz, Zamanlayıcı Çözünürlüğü. MPO, HAGS, HPET ve TSC kontrolleri.</li></ul>
            </div>
             <div class="feature-block">
                <span class="feature-title">🔹 5. Başlangıç Yöneticisi</span>
                <img src="https://github.com/user-attachments/assets/a249b9f5-20e2-4de3-97f2-faa7b98eb362" class="terminal-img">
                <ul><li>Derin tarama: Kayıt Defteri, Klasörler, Görev Zamanlayıcı ve Hizmetler.</li></ul>
            </div>
        </div>
    </div>

    <div class="section">
        <div class="section-tag">[03] ACCESS_REQUIREMENTS</div>
        <div class="warning-box">
            <b style="color:var(--warn)" data-lang="en" class="visible">[!] ADMIN_PRIVILEGES: REQUIRED FOR KERNEL TRACING (DPC/ISR), SECURITY SCAN & PAGEFILE INFO</b>
            <b style="color:var(--warn)" data-lang="ru">[!] ПРАВА АДМИНИСТРАТОРА: НУЖНЫ ДЛЯ ТРАССИРОВКИ ЯДРА И СКАНА ВИРУСОВ</b>
            <b style="color:var(--warn)" data-lang="ua">[!] ПРАВА АДМІНІСТРАТОРА: ПОТРІБНІ ДЛЯ ТРАСУВАННЯ ЯДРА ТА СКАНУ ВІРУСІВ</b>
            <b style="color:var(--warn)" data-lang="tr">[!] YÖNETİCİ YETKİSİ: ÇEKİRDEK İZLEME VE GÜVENLİK TARAMASI İÇİN GEREKLİDİR</b>
        </div>
    </div>

    <div class="action-row">
        <a href="https://github.com/Adiru3/ALPA/releases/download/V1.5/ALPA_Setup.exe" class="btn">DOWNLOAD_INSTALLER_V1.5</a>
        <a href="https://github.com/Adiru3/ALPA/releases/download/V1.5/ALPA.zip" class="btn">DOWNLOAD_PORTABLE_ZIP</a>

        <a href="https://github.com/Adiru3/ALPA" style="border: 1px solid var(--border); padding: 10px 25px; color: #fff; font-size: 13px; font-weight: bold; text-decoration: none;">VIEW_SOURCE_CODE</a>
    </div>

    <div class="social-badges">
        <a href="https://www.youtube.com/@adiruaim"><img src="https://img.shields.io/badge/YouTube-@adiruaim-FF0000?style=for-the-badge&logo=youtube" alt="YouTube"></a>
        <a href="https://www.tiktok.com/@adiruhs"><img src="https://img.shields.io/badge/TikTok-@adiruhs-000000?style=for-the-badge&logo=tiktok" alt="TikTok"></a>
        <a href="https://donatello.to/Adiru3"><img src="https://img.shields.io/badge/Support-Donatello-orange?style=for-the-badge" alt="Donatello"></a>
    </div>

    <footer>
        [ENV]: C# // .NET 4.8 // ETW KERNEL TRACE<br>
        &copy; 2026 ALPA BY amazingb01 (Adiru). ALL SYSTEMS NOMINAL.
    </footer>
</div>

<script>
    function setLang(lang) {
        document.querySelectorAll('.lang-btn').forEach(btn => {
            btn.classList.remove('active');
            if(btn.innerText.toLowerCase() === lang) btn.classList.add('active');
        });
        document.querySelectorAll('[data-lang]').forEach(el => {
            el.classList.remove('visible');
            if(el.getAttribute('data-lang') === lang) el.classList.add('visible');
        });
    }
</script>

</body>
</html>
