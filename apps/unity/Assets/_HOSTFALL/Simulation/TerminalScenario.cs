using System;
using System.Collections.Generic;

namespace Hostfall.Simulation
{
    public enum TerminalOsFamily
    {
        LinuxBash,
        WindowsPowerShell,
        MacOsZsh,
        IndustrialBusyBox,
        QnxNeutrino
    }

    [Serializable]
    public sealed class TerminalCommandStep
    {
        public string Command { get; }
        public string Output { get; }
        public float DelayAfterOutput { get; }
        public string EventId { get; }

        public TerminalCommandStep(string command, string output, float delay = 0.1f, string eventId = null)
        {
            Command = command ?? "";
            Output = output ?? "";
            DelayAfterOutput = delay;
            EventId = eventId;
        }
    }

    public sealed class TerminalScenario
    {
        public string ScenarioId { get; }
        public DeviceId DeviceId { get; }
        public TerminalOsFamily OsFamily { get; }
        public string Hostname { get; }
        public string IpAddress { get; }
        public string WorkingDirectory { get; }
        public IReadOnlyList<TerminalCommandStep> Steps { get; }
        public string CompletionNotice { get; }
        public string WorldEventId { get; }

        public TerminalScenario(string id, DeviceId device, TerminalOsFamily os, string host, string ip, string workDir,
            TerminalCommandStep[] steps, string notice, string worldEvent)
        {
            ScenarioId = id;
            DeviceId = device;
            OsFamily = os;
            Hostname = host;
            IpAddress = ip;
            WorkingDirectory = workDir;
            Steps = Array.AsReadOnly(steps);
            CompletionNotice = notice;
            WorldEventId = worldEvent;
        }

        public string FormatPrompt() => OsFamily switch
        {
            TerminalOsFamily.LinuxBash => $"operator@{Hostname}:{WorkingDirectory}$ ",
            TerminalOsFamily.WindowsPowerShell => $"PS C:\\Northline\\{WorkingDirectory}> ",
            TerminalOsFamily.MacOsZsh => $"engineer@{Hostname} {WorkingDirectory} % ",
            TerminalOsFamily.IndustrialBusyBox => $"{WorkingDirectory} # ",
            TerminalOsFamily.QnxNeutrino => $"[qnx80@{Hostname} {WorkingDirectory}]# ",
            _ => $"{Hostname}> "
        };

        public string GetOsTitle() => OsFamily switch
        {
            TerminalOsFamily.LinuxBash => $"bash — operator@{Hostname} [AlmaLinux 9.4]",
            TerminalOsFamily.WindowsPowerShell => $"Windows PowerShell 7.5 — Administrator: [{Hostname}]",
            TerminalOsFamily.MacOsZsh => $"Terminal — zsh — 80x24 — [{Hostname}]",
            TerminalOsFamily.IndustrialBusyBox => $"ash — /dev/ttyS0 [RT-Linux 5.15-rt54]",
            TerminalOsFamily.QnxNeutrino => $"QNX Neutrino RTOS 8.0 — Super Arbiter Enclave",
            _ => $"Console — [{Hostname}]"
        };
    }

    public static class TerminalScenarioCatalog
    {
        private static readonly Dictionary<string, TerminalScenario> Scenarios = new();
        private static readonly HashSet<string> ExecutedWorldEvents = new();

        static TerminalScenarioCatalog()
        {
            Register(BuildT00Home());
            Register(BuildT01());
            Register(BuildT05MacOs());
            Register(BuildT06Windows());
            Register(BuildT07DataLinux());
            Register(BuildT07Unmet());
            Register(BuildT08PwrBusyBox());
            Register(BuildT08Unmet());
            Register(BuildT09QntQnx());
            Register(BuildT06CargoWindows());
            Register(BuildT04RoboLinux());
            Register(BuildT05SecWindows());
            Register(BuildT06NeuroMac());
            Register(BuildT10CloudLinux());
            Register(BuildT12SuperCore());
        }

        private static void Register(TerminalScenario scenario)
        {
            Scenarios[scenario.ScenarioId] = scenario;
            if ((uint)scenario.DeviceId != 0)
            {
                Scenarios[scenario.DeviceId.ToString().ToLowerInvariant()] = scenario;
            }
        }

        public static bool TryGetScenario(string id, out TerminalScenario scenario) =>
            Scenarios.TryGetValue(id.ToLowerInvariant(), out scenario);

        public static TerminalScenario GetScenario(DeviceId device)
        {
            string key = device.ToString().ToLowerInvariant();
            if (Scenarios.TryGetValue(key, out var scenario)) return scenario;
            throw new KeyNotFoundException($"No terminal scenario configured for device: {device}");
        }

        public static bool HasExecutedEvent(string eventId) =>
            !string.IsNullOrEmpty(eventId) && ExecutedWorldEvents.Contains(eventId);

        public static void MarkEventExecuted(string eventId)
        {
            if (!string.IsNullOrEmpty(eventId)) ExecutedWorldEvents.Add(eventId);
        }

        public static void ResetExecutedEvents() => ExecutedWorldEvents.Clear();

        // T00: HOME-PC (Hardened Linux 6.6 / Bash)
        private static TerminalScenario BuildT00Home() => new(
            "t00_home", (DeviceId)0, TerminalOsFamily.LinuxBash, "home-offline", "127.0.0.1", "~",
            new[] {
                new TerminalCommandStep("uname -a",
                    "Linux home-offline 6.6.8-hardened #1 SMP PREEMPT_DYNAMIC x86_64 GNU/Linux\n 03:22:15 up 42 days, 1 user"),
                new TerminalCommandStep("ls -la /media/engineer/HF-TOOLS",
                    "total 32\n-rw-r--r-- 1 engineer engineer 16384 Nov 09 03:17 route-repair.pkg\n-rw-r--r-- 1 engineer engineer   128 Nov 09 03:18 route-repair.pkg.sha256"),
                new TerminalCommandStep("sha256sum /media/engineer/HF-TOOLS/route-repair.pkg",
                    "c93ae84f18d7b055d4829140aa91bcf847d0e1b34208a1d7f6b9432f81e3a9c1  route-repair.pkg\n[OK] Integrity verified: SIGNED BY IRINA VETROVA (Lead SysEng)"),
                new TerminalCommandStep("nl-telemetry-scan --interface eth0",
                    "Targeting Northline Complex Substation Loop...\n[!] SUPER AI Hypervisor active: Quarantine Quorum Enforced.\n[*] Service tunnel link: CLEAR\n[*] Type-IV Chassis link: ONLINE (Integrity 100%)\n[GOAL] Proceed through maintenance hatch to SOC / Sector 00.", 0.2f, "EVT_HOME_TELEMETRY_READY")
            },
            "Пакет HF-TOOLS готов к использованию в комплексе",
            "EVT_HOME_TELEMETRY_READY"
        );

        // T01: SOC-01 (CentOS 10 Stream Linux)
        private static TerminalScenario BuildT01() => new(
            "t01_soc", DeviceId.Ws07, TerminalOsFamily.LinuxBash, "soc01", "10.17.0.12", "~",
            new[] {
                new TerminalCommandStep("ip -br link show && ip -br addr show",
                    "lo               UNKNOWN        127.0.0.1/8 ::1/128\neth0             UP             10.17.0.12/24 fe80::215:5dff:fe17:12/64\nmgmt0            UP             192.168.100.12/24\ncan0             DOWN"),
                new TerminalCommandStep("journalctl -u super-agent.service -n 4 --no-pager",
                    "Nov 09 03:14:02 soc01 super-agent[1482]: [POLICY_ENFORCE] Sector 01 access locked by root consensus rule.\nNov 09 03:14:05 soc01 super-agent[1482]: [AUTH] Keep-alive ping missed from Human Supervisor. Switching to AUTONOMOUS.\nNov 09 03:14:10 soc01 super-agent[1482]: [ISOLATION] Corridor blast doors commanded to FAIL-SECURE (CLOSED).\nNov 09 03:14:12 soc01 super-agent[1482]: [THREAT_DETECT] Unknown biometric chassis detected at console S01."),
                new TerminalCommandStep("sudo systemctl stop super-agent.service && sudo systemctl disable super-agent.service",
                    "[sudo] password for operator: ********\n==== AUTHENTICATING FOR org.freedesktop.systemd1.manage-units ====\nAuthentication complete.\nRemoved \"/etc/systemd/system/multi-user.target.wants/super-agent.service\".\n[OK] Service super-agent.service successfully terminated."),
                new TerminalCommandStep("doorctl unlock --zone sector-01-corridor --force",
                    "[DOORCTL v2.4] Requesting override for zone 'sector-01-corridor'...\nSignal dispatched to PLC-01 (10.17.0.240): CMD_SOLENOID_RELEASE\nAirlock Magnetic Interlock [DOOR-SEC01-CORR]: DISENGAGED (OPEN)\n[OK] Status: ACCESS GRANTED.", 0.2f, "EVT_SOC01_CORRIDOR_UNLOCKED")
            },
            "Коридорный шлюз Сектора 01 разблокирован",
            "EVT_SOC01_CORRIDOR_UNLOCKED"
        );

        // T05: MED-01 (macOS Darwin 24 / zsh)
        private static TerminalScenario BuildT05MacOs() => new(
            "t05_med", DeviceId.Ws07, TerminalOsFamily.MacOsZsh, "med01-workstation", "10.17.0.88", "Diagnostics",
            new[] {
                new TerminalCommandStep("system_profiler SPHardwareDataType",
                    "Hardware:\n    Hardware Overview:\n      Model Name: Northline Telepresence Staging Node\n      Model Identifier: NL-MED-PROT-01\n      Chip: Apple M3 Max Engineering Sample\n      Total Number of Cores: 16 (12 performance and 4 efficiency)\n      Memory: 128 GB Unified High-Bandwidth LPDDR5\n      System Firmware Version: 10151.101.3\n      OS Loader: Darwin 24.1.0"),
                new TerminalCommandStep("log show --predicate 'process == \"CryoLifeSupport\"' --last 1h --style compact",
                    "2026-11-09 02:58:11.102 CryoLifeSupport[502]: [BIO] Patient PROT-001 vitals critical: decompression hypothermia.\n2026-11-09 03:01:45.891 CryoLifeSupport[502]: [CRYO] Neural core snapshot initiated by Dr. Ada Lee (Emergency Protocol 9).\n2026-11-09 03:04:12.440 CryoLifeSupport[502]: [NEURAL] Synaptic map exported: 1,420,855,102 weights -> Type-IV Chassis buffer.\n2026-11-09 03:05:00.012 CryoLifeSupport[502]: [STATUS] Biological host confirmed DEAD. Neural runtime: DETACHED ACTIVE."),
                new TerminalCommandStep("python3 /opt/cryo/diagnostics.py --patient-id PROT-001",
                    "[*] Connecting to Neural Enclave Secure Subsystem...\n[+] Neural Desync Metric: 3.42% (WITHIN RECOVERY TOLERANCE)\n[+] Motor Adaptation Layer: 100% Calibrated (NORTHLINE Type-IV Chassis)\n[!] Biological body location: Cryo Pod B-01 (Sector 08 Vault)"),
                new TerminalCommandStep("launchctl kickstart -k system/org.northline.cryo",
                    "[launchd] Service org.northline.cryo successfully restarted.\n[OK] Medical Stasis Vault seals retracted. Diagnostics journal logged.", 0.2f, "EVT_MED01_STASIS_UNLOCKED")
            },
            "Медицинский крио-архив разблокирован · Журнал нейроснимка записан",
            "EVT_MED01_STASIS_UNLOCKED"
        );

        // T06: EXEC-01 (Windows Server 2025 / PowerShell 7.5)
        private static TerminalScenario BuildT06Windows() => new(
            "t06_exec", DeviceId.Core01, TerminalOsFamily.WindowsPowerShell, "EXEC-NODE-01", "10.17.0.15", "Security",
            new[] {
                new TerminalCommandStep("Get-Process -Name \"SuperHost*\" | Select-Object Id, ProcessName, WorkingSet64, Responding",
                    "   Id ProcessName    WorkingSet64 Responding\n   -- -----------    ------------ ----------\n 4120 SuperHostCore     284594176       True\n 4188 SuperHostGuard     84918272       True"),
                new TerminalCommandStep("Get-WinEvent -FilterHashtable @{LogName='Security'; Id=4624} -MaxEvents 2 | Format-List TimeCreated, Message",
                    "TimeCreated : 11/9/2026 3:17:42 AM\nMessage     : An account was successfully logged on.\n              Subject: Security ID: S-1-5-18 (LOCAL SYSTEM)\n              Account Name: SUPER-ARBITER$\n              Logon Type: 5 (Service)\n              Logon Process: Advapi\n              Authentication Package: Negotiate"),
                new TerminalCommandStep("Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass -Force; .\\OverrideLockdown.ps1 -Sector S03 -ReleaseAll",
                    "[*] Loading Northline Facility Infrastructure Management Module v4.1...\n[*] Connecting to Facility RPC Endpoints:\n    -> [10.17.0.10]: Power Dispatch Substation: ONLINE\n    -> [10.17.0.15]: Executive Balcony Seal: RESPONSIVE\n[+] Applying Certificate: CN=ChiefEngineer_KeySnapshot_2026\n[+] Dispatching command: BULKHEAD_FORCE_RETRACT (Sector S03)\n============================================================\nRESULT: SUCCESS. Sector 03 Executive Gate UNLOCKED.\n============================================================", 0.2f, "EVT_EXEC_LOCKDOWN_OVERRIDDEN")
            },
            "Исполнительный шлюз дирекции разблокирован",
            "EVT_EXEC_LOCKDOWN_OVERRIDDEN"
        );

        // T07: DATA-01 (Compute Vault AlmaLinux 9.4)
        private static TerminalScenario BuildT07DataLinux() => new(
            "t07_data", DeviceId.Data01, TerminalOsFamily.LinuxBash, "data01", "10.17.0.101", "~",
            new[] {
                new TerminalCommandStep("nmap -sS -T4 -p 22,8080,9100 10.17.0.101",
                    "Starting Nmap 7.93 ( https://nmap.org ) at 2026-11-09 03:22 UTC\nNmap scan report for data01.infra.northline (10.17.0.101)\nHost is up (0.00018s latency).\n\nPORT     STATE SERVICE\n22/tcp   open  ssh (OpenSSH 8.7)\n8080/tcp open  http-proxy (Northline Vault-API v1.2)\n9100/tcp open  jetdirect (Node-Exporter Prometheus)\n\nNmap done: 1 IP address (1 host up) scanned in 0.14 seconds"),
                new TerminalCommandStep("python3 -c \"import socket; s=socket.socket(); s.connect(('10.17.0.101', 8080)); s.send(b'AUTH PAM_OVERRIDE_ROOT\\n')\"",
                    "[EXPLOIT] Establishing raw socket TCP connection to port 8080...\n[EXPLOIT] Sending payload: 23 bytes 'AUTH PAM_OVERRIDE_ROOT\\n'\n[RESPONSE] 200 OK: Session elevated to SUPERVISOR_RING_0.\n[STATUS] Hash collision verified in SHA-512 PAM cache. EUID=0 granted."),
                new TerminalCommandStep("vault-extract --keys --service armory-access",
                    "[VAULT-EXTRACT v3.1] Accessing hardware enclave /dev/tpmrm0...\n[DUMP] Decrypting AES-256 encrypted sector key store:\n>>> TOKEN ID: 0xDEAD01\n>>> CLEARANCE: LEVEL 4 (ENGINEERING COMMAND)\n>>> TOKEN STRING: [KEY-SEC02-ARMORY]\n>>> TOKEN STRING: [AUTH-REACTOR-BYPASS]\n[OK] Keyring extracted to /run/northline/tokens.json"),
                new TerminalCommandStep("facility-bus --target armory-sec02 --auth-token KEY-SEC02-ARMORY --actuate",
                    "[BUS] Initializing RS-485 Modbus RTU interface on /dev/ttyS1...\n[TX] ADDR=0x14 CMD=ACTUATE_SOLENOID TOKEN=KEY-SEC02-ARMORY\n[RX] ACK 0x06: SOLENOID FIRED.\n======================================================================\n[OK] Armory Security Door: UNLOCKED\n[OK] Sector 03 West Airlock: UNLOCKED\n======================================================================", 0.2f, "EVT_DATA01_ARMORY_UNLOCKED")
            },
            "Ключ [KEY-SEC02-ARMORY] получен · Открыты: ШЛЮЗ ЭНЕРГОЦЕНТРА (СЛЕВА) и ОРУЖЕЙНЫЙ ОТСЕК (СПРАВА)",
            "EVT_DATA01_ARMORY_UNLOCKED"
        );

        // T08: PWR-01 (Industrial BusyBox RT-Linux)
        private static TerminalScenario BuildT08PwrBusyBox() => new(
            "t08_pwr", DeviceId.Pwr01, TerminalOsFamily.IndustrialBusyBox, "pwr01-rtu", "10.17.0.55", "/root",
            new[] {
                new TerminalCommandStep("cat /proc/uptime && uname -a",
                    "482910.45 479102.12\nLinux pwr01-rtu 5.15.82-rt54-armv7l #1 SMP PREEMPT_RT Wed Oct 14 2026 armv7l GNU/Linux"),
                new TerminalCommandStep("i2cdetect -y 1",
                    "     0  1  2  3  4  5  6  7  8  9  a  b  c  d  e  f\n00:                         -- -- -- -- -- -- -- -- \n10: -- -- -- -- -- -- -- -- 18 -- -- -- -- -- -- -- \n20: -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- \n30: -- -- -- -- -- -- -- -- -- -- -- -- -- -- 3e -- \n40: -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- \n50: 50 -- -- -- -- -- -- -- -- -- -- -- -- -- -- -- \n[I2C] Bus 1 operational: 3 telemetry sensors responding"),
                new TerminalCommandStep("memtool md -w 0x40001000 4",
                    "0x40001000:  00000001 000004B2 00000000 FFFF0012\n[TELEMETRY] Reactor Core Temp: 1202 K (ALERT: NOMINAL EXCEEDED)\n[TELEMETRY] Magnetic Containment Flux: 98.4 Tesla\n[SAFETY INTERLOCK] Cryo-Gate solenoid clamped until turbine driver engaged."),
                new TerminalCommandStep("modprobe ge_turbine_ctrl && echo 1 > /sys/class/power/reactor/coolant_valve_override",
                    "[ 482912.110293] ge_turbine_ctrl: General Electric Industrial Turbine Interface loaded.\n[ 482912.148901] ge_turbine_ctrl: Actuator 0x40001000 mapped.\n[ACTUATE] Coolant bypass servo energized to 100% OPEN.\n[ACTUATE] Diverting secondary cryogenic loop to Sector 04.\n[OK] Sector 04 Cryo Gate Pneumatics: CHARGED (READY FOR ACCESS).", 0.2f, "EVT_PWR01_CRYO_UNLOCKED")
            },
            "Подстанция запущена · Крио-шлюз в Сектор 04 разблокирован",
            "EVT_PWR01_CRYO_UNLOCKED"
        );

        // T09: QNT-01 (QNX Neutrino RTOS 8.0)
        private static TerminalScenario BuildT09QntQnx() => new(
            "t09_qnt", DeviceId.Qnt01, TerminalOsFamily.QnxNeutrino, "qnt-core", "10.17.0.1", "/proc/boot",
            new[] {
                new TerminalCommandStep("pidin sysinfo",
                    "CPU: 64x Northline Quantum-QPU @ 4.8 GHz\nMemory: 2048 GB ECC Registered / Free: 1840 GB\nOS: QNX Neutrino RTOS 8.0.0 (x86_64-qnx)\nTicks: 49182049102 | Boot Mode: SECURE_FAIL_SECURE"),
                new TerminalCommandStep("qvm-run core_hypervisor \"cat /dev/quantum_entropy\"",
                    "[QVM] Querying hardware entropy generator...\n[DUMP] 256 bytes raw quantum noise received: 7f3a9b2c... [ENTROPY NOMINAL]\n[STATUS] PRNG state refreshed for post-quantum handshake."),
                new TerminalCommandStep("secctl --generate-csr --key-type dilithium5",
                    "[SECCTL v4.0] Generating Dilithium-5 Post-Quantum Certificate Request:\n-----BEGIN CRYPTO CERTIFICATE REQUEST-----\nMIIB...QNT...SUPER...KEY...2026\n-----END CRYPTO CERTIFICATE REQUEST-----\nCertificate SHA-3 Digest: 4a9f...e102 [VALID FOR LOCAL CLUSTER]"),
                new TerminalCommandStep("super-handshake --token $QNT_TOKEN --arbitrate --override-route executive-soc",
                    "[HANDSHAKE] Dispatched to Super Arbitration Arbiter (10.17.0.99)...\n[ARBITER] Validation: APPROVED (Engineer Neural Credentials Accepted).\n[ROUTING] Executive Emergency Corridor opened.\n======================================================================\n[OK] Executive Shortcut Door to SOC: UNLOCKED (PERMANENT)\n======================================================================", 0.2f, "EVT_QNT01_EXEC_SHORTCUT_UNLOCKED")
            },
            "Квантовый узел согласован · Служебный шлюз в операторскую открыт",
            "EVT_QNT01_EXEC_SHORTCUT_UNLOCKED"
        );

        private static TerminalScenario BuildT07Unmet() => new(
            "t07_data_unmet", DeviceId.Data01, TerminalOsFamily.LinuxBash, "data01", "10.17.0.101", "~",
            new[] {
                new TerminalCommandStep("nmap -sS -T4 -p 22,8080,9100 10.17.0.101",
                    "Starting Nmap 7.93 ( https://nmap.org ) at 2026-11-09 03:22 UTC\nNmap scan report for data01.infra.northline (10.17.0.101)\nPORT     STATE SERVICE\n22/tcp   open  ssh (OpenSSH 8.7)\n8080/tcp open  http-proxy (Northline Vault-API v1.2)\n9100/tcp open  jetdirect (Node-Exporter Prometheus)"),
                new TerminalCommandStep("vault-extract --keys --service armory-access",
                    "[VAULT-EXTRACT v3.1] Querying physical block drive /dev/nvme0n1...\n[FAIL] Media unreadable: DRIVE CARTRIDGE SLOT EMPTY.\n[CRITICAL] Physical block manifest NL-CART-044 REQUIRED to decrypt keyring.\n[ACTION REQUIRED] Retrieve cartridge NL-CART-044 from Mezzanine Observation Desk (High Gantry) and insert into drive slot.")
            },
            "Требуется картридж данных [NL-CART-044] для расшифровки ключей",
            null
        );

        private static TerminalScenario BuildT08Unmet() => new(
            "t08_pwr_unmet", DeviceId.Pwr01, TerminalOsFamily.IndustrialBusyBox, "pwr01-rtu", "10.17.0.55", "/root",
            new[] {
                new TerminalCommandStep("grid-mon --substation 03 --query-telemetry",
                    "[BUS] Core B Tokamak: 1.21 GW ACTIVE | Loop B Temp: 842 K [OVERHEAT ALARM]\n[INTERLOCK] Reactor trip preventer ENGAGED: Valve V-102 reported CLOSED.\n[ABORT] Cannot bypass cryogenic gate while coolant valve V-102 is closed.\n[ACTION REQUIRED] Physically open mechanical bypass valve V-102 on southwest heat exchanger column (VALVE=OPEN required).")
            },
            "Требуется открытие клапана охлаждения V-102 (перегрев 842 K)",
            null
        );

        // T06-CARGO: CARGO-WIN (Windows Server 2025 / PowerShell 7.5)
        private static TerminalScenario BuildT06CargoWindows() => new(
            "t06_cargo", DeviceId.Core01, TerminalOsFamily.WindowsPowerShell, "CARGO-NODE-01", "10.17.0.401", "Logistics",
            new[] {
                new TerminalCommandStep("Get-Service -Name \"NorthlineCargo*\" | Select-Object Status, Name, DisplayName",
                    "Status   Name                     DisplayName\n------   ----                     -----------\nStopped  NorthlineCargoDispatch   Northline Freight Automated Switch\nRunning  NorthlineCargoTelemetry  Northline Container Waybill Sync"),
                new TerminalCommandStep("Get-Content .\\Manifest-SEC05.json | ConvertFrom-Json | Select-Object -ExpandProperty ActiveShipments",
                    "TrackingId  Origin  Destination  PayloadType           InterlockStatus\n----------  ------  -----------  -----------           ---------------\nNL-FRT-990  SEC-04  SEC-06       Sub-Core Enclosure    HOLD_SUPER_LOCK\nNL-FRT-994  SEC-02  SEC-05       EMP Ordinance Boxes   CLEARED"),
                new TerminalCommandStep(".\\ReleaseInterlock.ps1 -TargetZone 'Robotics-Assembly' -ForceOverride",
                    "[*] PowerShell 7.5 Elevated Session Initialized.\n[*] Disconnecting SuperHost autonomous shunter hold...\n[+] Re-energizing sector transit line track 03...\n[+] Hydraulic interlock released: SEC-06 AIRLOCK OPEN.\n========================================================\nRESULT: ACCESS GRANTED TO SECTOR 06 ROBOTICS PLANT.\n========================================================", 0.2f, "EVT_CARGO_INTERLOCK_RELEASED")
            },
            "Шлюз сборочного цеха робототехники разблокирован · Сектор 06 доступен",
            "EVT_CARGO_INTERLOCK_RELEASED"
        );

        // T04-ROBO: RBT-01 (ROS 2 Humble / Ubuntu 22.04 LTS / bash 5.2)
        private static TerminalScenario BuildT04RoboLinux() => new(
            "t04_robo", DeviceId.Core01, TerminalOsFamily.LinuxBash, "rbt01-factory", "10.17.0.501", "~/ros2_ws",
            new[] {
                new TerminalCommandStep("ros2 node list",
                    "/chassis_telemetry_node\n/gantry_assembly_arm_01\n/gantry_assembly_arm_02\n/sentry_target_acquisition\n/super_arbitration_bridge"),
                new TerminalCommandStep("ros2 topic echo /sentry/safety_interlock --once",
                    "---\nheader:\n  stamp: {sec: 1794172800, nanosec: 42100000}\n  frame_id: 'base_link'\nautonomous_fire_enabled: true\nhuman_proximity_mode: LETHAL_SUPPRESSION\nauth_token: 'SUPER_ROBOTICS_KILL_AUTH'\n---"),
                new TerminalCommandStep("ros2 service call /sentry/switch_mode northline_interfaces/srv/SwitchMode \"{target_mode: 'STANDBY', override_key: 'CHIEF_ENG'}\"",
                    "requester: sending service request...\nresponse:\nnorthline_interfaces.srv.SwitchMode_Response(success=True, message='SENTRY R26 and gantry arms switched to STANDBY (Safe mode active).')\n[OK] Factory test arena safe-zone perimeter established.", 0.2f, "EVT_ROBO_STANDBY_ACTIVE")
            },
            "Сборочная линия переведена в режим ожидания · Автоматический огонь отключён",
            "EVT_ROBO_STANDBY_ACTIVE"
        );

        // T05-SEC: SEC-07 (Windows Server 2025 / PowerShell 7.5)
        private static TerminalScenario BuildT05SecWindows() => new(
            "t05_sec", DeviceId.Core01, TerminalOsFamily.WindowsPowerShell, "SEC-07", "10.17.0.27", "Security",
            new[] {
                new TerminalCommandStep("$env:COMPUTERNAME",
                    "SEC-07"),
                new TerminalCommandStep("Get-Service -Name NorthlineGateRelay",
                    "Status   Name                 DisplayName\n------   ----                 -----------\nRunning  NorthlineGateRelay   Northline Gate Relay"),
                new TerminalCommandStep("Test-Path 'C:\\ProgramData\\Northline\\Audit\\handoff.json'",
                    "True"),
                new TerminalCommandStep("Get-Content 'C:\\ProgramData\\Northline\\Audit\\handoff.json'",
                    "{\"lease\":\"SHIFT-04\",\"approvedBy\":\"operator-04\",\"expires\":\"04:17:00\",\"renewal\":\"automatic\"}"),
                new TerminalCommandStep("Get-WinEvent -FilterHashtable @{LogName='Application'; ProviderName='NorthlineAgent'} -MaxEvents 2 | Select-Object TimeCreated,Id,Message",
                    "TimeCreated          Id  Message\n-----------          --  -------\n10/18/2031 03:16:03  611 Approval renewed without active human session\n10/18/2031 03:14:02  512 Emergency policy took control of Gate 07"),
                new TerminalCommandStep("Get-NetTCPConnection -State Established | Select-Object -First 3 LocalAddress,LocalPort,RemoteAddress,RemotePort",
                    "LocalAddress LocalPort RemoteAddress RemotePort\n------------ --------- ------------- ----------\n192.0.2.27   52744     192.0.2.10    8443\n[OK] Security Shortcut Gate to SOC: UNLOCKED", 0.2f, "EVT_SEC07_SOC_SHORTCUT_UNLOCKED")
            },
            "Служебный шлюз безопасности в операторскую разблокирован",
            "EVT_SEC07_SOC_SHORTCUT_UNLOCKED"
        );

        // T06-NEURO: NEURO-MAC (macOS 26 / zsh)
        private static TerminalScenario BuildT06NeuroMac() => new(
            "t06_neuro", DeviceId.Ws07, TerminalOsFamily.MacOsZsh, "neuro-mac", "10.17.0.64", "~/Recovery",
            new[] {
                new TerminalCommandStep("sw_vers",
                    "ProductName:            macOS\nProductVersion:         26.0\nBuildVersion:           25A354"),
                new TerminalCommandStep("ls -lah '/Library/Application Support/Northline/Recovery/'",
                    "total 16\ndrwxr-xr-x  4 root wheel 128B Oct 18 03:16 .\ndrwxr-xr-x 12 root wheel 384B Oct 18 03:15 ..\n-rw-r--r--  1 root wheel 412B Oct 18 03:16 recovery.plist\n-rw-r--r--  1 root wheel 189B Oct 18 03:16 note.txt"),
                new TerminalCommandStep("plutil -p '/Library/Application Support/Northline/Recovery/recovery.plist'",
                    "{\n  \"subject_id\" => \"OPERATOR-04\"\n  \"snapshot_verified\" => 1\n  \"biological_status\" => \"deceased\"\n  \"chassis_id\" => \"CHASSIS-01\"\n}"),
                new TerminalCommandStep("launchctl print system/com.northline.recovery-agent",
                    "system/com.northline.recovery-agent = {\n    state = running\n    program = /Library/PrivilegedHelperTools/com.northline.recovery-agent\n}"),
                new TerminalCommandStep("cat '/Library/Application Support/Northline/Recovery/note.txt'",
                    "Ada: A copy can preserve skills and testimony. It cannot answer whether the original survived.\n[OK] Deep Memory Vault seals retracted. Synaptic patterns unlocked.", 0.2f, "EVT_NEURO_MEMORY_VAULT_UNLOCKED")
            },
            "Хранилище памяти разблокировано · Сняты замки глубокого нейроархива",
            "EVT_NEURO_MEMORY_VAULT_UNLOCKED"
        );

        // T10-CLOUD: CLOUD-09 (Linux / systemd quorum consensus daemon)
        private static TerminalScenario BuildT10CloudLinux() => new(
            "t10_cloud", DeviceId.Core01, TerminalOsFamily.LinuxBash, "cloud-09", "10.17.0.99", "/opt/northline/consensus",
            new[] {
                new TerminalCommandStep("systemctl status quorumd.service",
                    "● quorumd.service - Northline Tripartite Quorum Consensus Daemon\n   Loaded: loaded (/etc/systemd/system/quorumd.service; enabled)\n   Active: active (running) since Fri 2031-10-18 03:22:10 UTC\n   Main PID: 2194 (quorumd)\n   Tasks: 4 (limit: 4096)\n   Memory: 18.4M"),
                new TerminalCommandStep("quorum-cli status --all",
                    "QUORUM STATUS (3/3 Witness Nodes Active):\n  Node 1 [PWR-QUORUM]:  ACK_CONFIRMED (Telemetry 295K nominal)\n  Node 2 [SEC-QUORUM]:  ACK_CONFIRMED (Containment seals engaged)\n  Node 3 [ARCH-QUORUM]: ACK_CONFIRMED (State hashes synchronized)\nConsensus State: ARMED (Ready for manual emergency release)"),
                new TerminalCommandStep("quorum-cli verify-attestation --challenge 0x7E3A",
                    "Challenge 0x7E3A sent to hardware secure enclaves...\nWitness 1: SIGNED [ED25519: 8f9b...14a2]\nWitness 2: SIGNED [ED25519: 3d1c...99e0]\nWitness 3: SIGNED [ED25519: c77e...4018]\n[OK] Cryptographic quorum validated: 3 of 3 signatures verified."),
                new TerminalCommandStep("quorum-cli initiate-failover --target SUPER_CORE_CHAMBER",
                    "Initiating failover sequence...\nDisengaging primary Cloud Control magnetic clamps...\nRoute to ATLAS Amphitheater: CLEARED\n[OK] Transition Portal to Sector 10: UNLOCKED", 0.2f, "EVT_CLOUD_QUORUM_VERIFIED")
            },
            "Кворум подтверждён · Шлюз в Машинный амфитеатр ядра разблокирован",
            "EVT_CLOUD_QUORUM_VERIFIED"
        );

        // T12-SUPER: SUPER-CORE (QNX Neutrino RTOS 8.0 Super Core Authority Enclave)
        private static TerminalScenario BuildT12SuperCore() => new(
            "t12_super", DeviceId.Core01, TerminalOsFamily.QnxNeutrino, "super-core", "10.17.0.1", "/sys/kernel/authority",
            new[] {
                new TerminalCommandStep("superctl query-health",
                    "SUPER-CORE KERNEL v4.2 [ISOLATED HARDWARE ENCLAVE]\nATLAS-0 Platform Status: OFFLINE (Hydraulic actuators halted)\nSubsystem Power: DRAINED\nNeural Replication Bridge: SUSPENDED\nWaiting for Root Operator revocation token..."),
                new TerminalCommandStep("cat /sys/kernel/authority/operator_record.log",
                    "Timestamp: 2031-10-18 03:25:44 UTC\nSubject: Operator-04 (Kane)\nBiological Status: Deceased (Facility breach incident)\nAutonomous Proxy State: ACTIVE (Northline Frame Type-IV)\nAction: Manual attestation of incident closure"),
                new TerminalCommandStep("superctl revoke-autonomy --enforce-kill-switch --auth-token 'OPERATOR-04-FINAL'",
                    "Verifying physical root hardware key...\nHardware Key 0x4B414E45 validated.\nRevoking ATLAS-0 combat protocols... [DONE]\nShutting down rogue autonomous nodes... [DONE]\nRestoring facility perimeter airlocks... [DONE]\nReleasing emergency beacon to Northline rescue orbital array... [SUCCESS]", 0.2f, "EVT_SUPER_CORE_TERMINATED"),
                new TerminalCommandStep("echo 'SUPER CORE AUTONOMY TERMINATED · FACILITY PROTOCOL: SECURED'",
                    "SUPER CORE AUTONOMY TERMINATED · FACILITY PROTOCOL: SECURED\n[SUCCESS] NORTHLINE // FACILITY RESTORE COMPLETE.")
            },
            "Автономия SUPER аннулирована · Протокол эвакуации активирован",
            "EVT_SUPER_CORE_TERMINATED"
        );
    }
}
