using Exiled.API.Features;
using Exiled.API.Interfaces;
using Exiled.Events.EventArgs.Player;
using Exiled.API.Features.Items;
using System.Text;

namespace CardLogger
{
    public class CardLogger : Plugin<Config>
    {
        public static CardLogger Singleton;
        public override string Name => "CardLogger";
        public override string Author => "Ducstii";
        public override Version Version => new Version(1, 0, 0);

        private readonly LogManager _logManager;
        private static readonly Dictionary<string, string> DoorAliases = new()
        {
            { "079_FIRST", "SCP-079 First Door" },
            { "079_SECOND", "SCP-079 Second Door" },
            { "096", "SCP-096 Chamber" },
            { "049_ARMORY", "SCP-049 Armory" },
            { "079_ARMORY", "SCP-079 Armory" },
            { "173_ARMORY", "SCP-173 Armory" },
            { "173_CONNECTOR", "SCP-173 Connector" },
            { "173_GATE", "SCP-173 Gate" },
            { "914", "SCP-914" },
            { "939_CRYO", "SCP-939 Cryo" },
            { "ESCAPE_FINAL", "Final Escape" },
            { "ESCAPE_PRIMARY", "Primary Escape" },
            { "ESCAPE_SECONDARY", "Secondary Escape" },
            { "GATE_A", "Gate A" },
            { "GATE_B", "Gate B" },
            { "GR18", "GR18" },
            { "GR18_INNER", "GR18 Inner" },
            { "HCZ_127_LAB", "HCZ 127 Lab" },
            { "HCZ_ARMORY", "HCZ Armory" },
            { "HID_CHAMBER", "HID Chamber" },
            { "HID_LAB", "HID Lab" },
            { "INTERCOM", "Intercom" },
            { "LCZ_ARMORY", "LCZ Armory" },
            { "LCZ_WC", "LCZ Restroom" },
            { "SURFACE_GATE", "Surface Gate" },
            { "SURFACE_NUKE", "Surface Nuke" },
            { "106_PRIMARY", "SCP-106 Primary" },
            { "106_SECONDARY", "SCP-106 Secondary" },
            { "330", "SCP-330" },
            { "330_CHAMBER", "SCP-330 Chamber" },
            { "CHECKPOINT_EZ_HCZ_A", "EZ-HCZ Checkpoint A" },
            { "CHECKPOINT_LCZ_A", "LCZ Checkpoint A" },
            { "CHECKPOINT_LCZ_B", "LCZ Checkpoint B" },
        };

        private DateTime _roundStartTime;

        public CardLogger()
        {
            Singleton = this;
            _logManager = new LogManager();
        }

        public override void OnEnabled()
        {
            Exiled.Events.Handlers.Player.InteractingDoor += OnInteractingDoor;
            Exiled.Events.Handlers.Player.InteractingLocker += OnInteractingLocker;
            Exiled.Events.Handlers.Server.RoundStarted += OnRoundStarted;
            base.OnEnabled();
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Player.InteractingDoor -= OnInteractingDoor;
            Exiled.Events.Handlers.Player.InteractingLocker -= OnInteractingLocker;
            Exiled.Events.Handlers.Server.RoundStarted -= OnRoundStarted;
            _logManager.ClearLogs();
            base.OnDisabled();
        }

        private void OnRoundStarted()
        {
            _logManager.ClearLogs();
            _roundStartTime = DateTime.UtcNow;
        }

        public string GetInGameTime()
        {
            TimeSpan elapsed = DateTime.UtcNow - _roundStartTime;
            int totalInGameMinutes = (int)(elapsed.TotalSeconds / 30.0 * 2);
            int hour = 8 + (totalInGameMinutes / 60);
            int minute = totalInGameMinutes % 60;
            if (hour >= 24) hour = hour % 24;
            return $"{hour:00}:{minute:00}";
        }

        private void OnInteractingDoor(InteractingDoorEventArgs ev)
        {
            if (ev.Player.CurrentItem is not Keycard keycard) return;

            string doorName = ev.Door.Name;
            if (doorName is "LCZ" or "HCZ" or "EZ") return;

            doorName = DoorAliases.GetValueOrDefault(doorName, doorName);
            string time = GetInGameTime();
            string action = ev.Door.IsOpen ? "closed" : "opened";
            
            _logManager.AddLog(keycard.Serial, time, doorName, action, ev.Player.Nickname, ev.IsAllowed);
        }

        private void OnInteractingLocker(InteractingLockerEventArgs ev)
        {
            if (ev.Player.CurrentItem is not Keycard keycard) return;

            string time = GetInGameTime();
            _logManager.AddLog(keycard.Serial, time, "SCP-330 Cabinet", "accessed", ev.Player.Nickname, ev.IsAllowed);
        }

        public string GetKeycardLogs(ushort serial)
        {
            return _logManager.GetFormattedLogs(serial);
        }
    }

    public class LogManager
    {
        private readonly Dictionary<ushort, List<string>> _keycardLogs = new();

        public void AddLog(ushort serial, string time, string location, string action, string playerName, bool isAllowed)
        {
            string log = isAllowed
                ? $"[{time}] {location} {action} by {playerName}"
                : $"<color=red>[{time}] {location} attempted to be {action} by {playerName}</color>";

            if (!_keycardLogs.ContainsKey(serial))
                _keycardLogs[serial] = new List<string>();
            
            _keycardLogs[serial].Add(log);
        }

        public void ClearLogs()
        {
            _keycardLogs.Clear();
        }

        public string GetFormattedLogs(ushort serial)
        {
            if (!_keycardLogs.ContainsKey(serial) || _keycardLogs[serial].Count == 0)
                return "No logs found for this keycard.";

            string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var sb = new StringBuilder();
            sb.AppendLine("[Keycard Access Log]");
            sb.AppendLine($"Serial: {serial}");
            sb.AppendLine($"Date: {today}");
            sb.AppendLine();
            sb.AppendLine(string.Join("\n", _keycardLogs[serial]));
            sb.AppendLine("[End of Log]");
            
            return sb.ToString();
        }

        public bool HasLogs(ushort serial)
        {
            return _keycardLogs.ContainsKey(serial) && _keycardLogs[serial].Count > 0;
        }
    }

    public class Config : IConfig
    {
        public bool IsEnabled { get; set; } = true;
        public bool Debug { get; set; } = false;
        public bool LogFailedAttempts { get; set; } = true;
        public int MaxLogsPerCard { get; set; } = 100;
        public bool ShowRealTimeInLogs { get; set; } = true;

        public List<string> AllowedLogRoles { get; set; } = new()
        {
            "FacilityGuard",
            "NtfPrivate",
            "NtfSergeant",
            "NtfSpecialist",
            "NtfCaptain",
            "Tutorial"
        };
    }
} 