using CommandSystem;
using Exiled.API.Features;

namespace CardLogger.Commands
{
    [CommandHandler(typeof(ClientCommandHandler))]
    public class TimeCommand : ICommand
    {
        public string Command => "time";
        public string[] Aliases => Array.Empty<string>();
        public string Description => "Shows the current in-game time.";

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

            string time = CardLogger.Singleton.GetInGameTime();
            player.ShowHint($"Current Facility Time: {time}", 5f);
            response = $"Current Facility Time: {time}";
            return true;
        }
    }
} 