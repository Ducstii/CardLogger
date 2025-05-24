using CommandSystem;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using PlayerRoles;

namespace CardLogger.Commands
{
    [CommandHandler(typeof(ClientCommandHandler))]
    public class LogsCommand : ICommand
    {
        public string Command => "logs";
        public string[] Aliases => Array.Empty<string>();
        public string Description => "Shows logs for the keycard you are holding.";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (sender is not CommandSender playerSender)
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

            if (player.Role.Type != RoleTypeId.FacilityGuard && !player.Role.Type.ToString().StartsWith("Ntf"))
            {
                response = "Only Facility Guards and MTF can use this command.";
                return false;
            }

            if (player.CurrentItem is not Keycard keycard)
            {
                response = "You must be holding a keycard to use this command.";
                return false;
            }

            if (!CardLogger.Singleton.Config.AllowedLogRoles.Contains(player.Role.Type.ToString()))
            {
                response = "You do not have permission to use this command.";
                return false;
            }

            response = CardLogger.Singleton.GetKeycardLogs(keycard.Serial);
            return true;
        }
    }
} 