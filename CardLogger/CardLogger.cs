using Exiled.API.Features;
using Exiled.API.Interfaces;
using Exiled.Events.EventArgs.Player;
using CommandSystem;
using Exiled.API.Features.Items;
using PlayerRoles;

namespace CardLogger
{
    public class CardLogger : Plugin<Config>
    {
        public static CardLogger Singleton;
        public override string Name => "CardLogger";
        public override string Author => "Ducstii";
        public override Version Version => new Version(1, 0, 0);

        internal Dictionary<ushort, List<string>> keycardLogs;
        private Dictionary<string, string> doorAliases;
        private DateTime roundStartTime;

        public CardLogger()
        {
            Singleton = this;
        }

        public override void OnEnabled()
        {
            keycardLogs = new Dictionary<ushort, List<string>>();
            InitializeDoorAliases();
            Exiled.Events.Handlers.Player.InteractingDoor += OnInteractingDoor;
            Exiled.Events.Handlers.Player.InteractingLocker += OnInteractingLocker;
            Exiled.Events.Handlers.Server.RoundStarted += OnRoundStarted;
            base.OnEnabled();
        }

        private void InitializeDoorAliases()
        {
            doorAliases = new Dictionary<string, string>
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
        }

        private string GetDoorAlias(string doorName)
        {
            return doorAliases.TryGetValue(doorName, out string alias) ? alias : doorName;
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Player.InteractingDoor -= OnInteractingDoor;
            Exiled.Events.Handlers.Player.InteractingLocker -= OnInteractingLocker;
            Exiled.Events.Handlers.Server.RoundStarted -= OnRoundStarted;
            keycardLogs.Clear();
            base.OnDisabled();
        }

        private void OnRoundStarted()
        {
            keycardLogs.Clear();
            roundStartTime = DateTime.UtcNow;
        }

        private string GetInGameTime()
        {
            // 2 minutes pass every 30 seconds, starting at 08:00
            TimeSpan elapsed = DateTime.UtcNow - roundStartTime;
            int totalInGameMinutes = (int)(elapsed.TotalSeconds / 30.0 * 2);
            int hour = 8 + (totalInGameMinutes / 60);
            int minute = totalInGameMinutes % 60;
            if (hour >= 24) hour = hour % 24;
            return $"{hour:00}:{minute:00}";
        }

        private void OnInteractingDoor(InteractingDoorEventArgs ev)
        {
            if (ev.Player.CurrentItem is Keycard keycard)
            {
                ushort serial = keycard.Serial;
                string doorName = ev.Door.Name;
                if (doorName == "LCZ" || doorName == "HCZ" || doorName == "EZ")
                    return; // Skip logging for generic zone doors

                doorName = GetDoorAlias(doorName);
                string time = GetInGameTime();
                string log;
                if (!ev.IsAllowed)
                    log = $"<color=red>[{time}] {doorName} attempted to open by {ev.Player.Nickname}</color>";
                else
                    log = $"[{time}] {doorName} accessed by {ev.Player.Nickname}";
                if (!keycardLogs.ContainsKey(serial))
                    keycardLogs[serial] = new List<string>();
                keycardLogs[serial].Add(log);
            }
        }

        private void OnInteractingLocker(InteractingLockerEventArgs ev)
        {
            if (ev.Player.CurrentItem is Keycard keycard)
            {
                ushort serial = keycard.Serial;
                string lockerName = "SCP Cabinet"; // Can't get the real name in your version
                string time = GetInGameTime();
                string log;
                if (!ev.IsAllowed)
                    log = $"<color=red>[{time}] {lockerName} attempted to open by {ev.Player.Nickname}</color>";
                else
                    log = $"[{time}] {lockerName} accessed by {ev.Player.Nickname}";
                if (!keycardLogs.ContainsKey(serial))
                    keycardLogs[serial] = new List<string>();
                keycardLogs[serial].Add(log);
            }
        }

        [CommandHandler(typeof(ClientCommandHandler))]
        public class LogsCommand : ICommand
        {
            public string Command => "logs";
            public string[] Aliases => new string[] { };
            public string Description => "Shows logs for the keycard you are holding.";

            public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
            {
                if (!(sender is CommandSender playerSender))
                {
                    response = "This command can only be used by players.";
                    return false;
                }

                Player player = Player.Get(playerSender);
                if (player == null)
                {
                    response = "Player not found.";
                    return false;
                }

                // Only allow Facility Guard and MTF
                if (player.Role.Type != RoleTypeId.FacilityGuard && !player.Role.Type.ToString().StartsWith("Ntf"))
                {
                    response = "Only Facility Guards and MTF can use this command.";
                    return false;
                }

                if (!(player.CurrentItem is Keycard keycard))
                {
                    response = "You must be holding a keycard to use this command.";
                    return false;
                }

                // Only allow roles specified in the config
                if (!CardLogger.Singleton.Config.AllowedLogRoles.Contains(player.Role.Type.ToString()))
                {
                    response = "You do not have permission to use this command.";
                    return false;
                }

                ushort serial = keycard.Serial;
                if (!CardLogger.Singleton.keycardLogs.ContainsKey(serial) || CardLogger.Singleton.keycardLogs[serial].Count == 0)
                {
                    response = "No logs found for this keycard.";
                    return true;
                }

                // Add the real-world date to the log response
                string today = DateTime.UtcNow.ToString("yyyy-MM-dd");
                response = $"[Keycard Access Log]\nSerial: {serial}\nDate: {today}\n\n" + string.Join("\n", CardLogger.Singleton.keycardLogs[serial]) + "\n[End of Log]";
                return true;
            }
        }

        [CommandHandler(typeof(ClientCommandHandler))]
        public class TimeCommand : ICommand
        {
            public string Command => "time";
            public string[] Aliases => new string[] { };
            public string Description => "Shows the current in-game time.";

            public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
            {
                if (!(sender is CommandSender playerSender))
                {
                    response = "This command can only be used by players.";
                    return false;
                }

                Player player = Player.Get(playerSender);
                if (player == null)
                {
                    response = "Player not found.";
                    return false;
                }

                string time = CardLogger.Singleton.GetInGameTime();
                player.ShowHint($"Current Facility Time: {time}", 5f);
                response = $"Current Facility Time: {time}";
                return true;
            }
        }
    }

    public class Config : IConfig
    {
        public bool IsEnabled { get; set; } = true;
        public bool Debug { get; set; } = false;

        // New configuration option for allowed roles for the .logs command
        public List<string> AllowedLogRoles { get; set; } = new List<string>
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