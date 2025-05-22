# CardLogger Plugin for SCP: Secret Laboratory

## Overview

CardLogger is an Exiled plugin for SCP: Secret Laboratory that logs keycard interactions with doors and SCP cabinets. It provides players with a command to view these logs for the keycard they are currently holding.

## Features

- Logs successful and failed keycard access attempts on doors.
- Logs keycard interactions with SCP cabinets (with a generic name due to API limitations in some Exiled versions).
- Provides an in-game command `.logs` for players to view the access history of their held keycard.
- Restricts the `.logs` command usage to Facility Guards and MTF roles.
- Timestamps logs based on an in-game clock (starting at 08:00, with 2 minutes passing every 30 seconds).
- Includes the real-world date in the `.logs` command response.
- Provides an in-game command `.time` to display the current in-game facility time.

## Installation

1. Ensure you have EXILED installed on your SCP:SL server.
2. Place the `CardLogger.dll` file in your EXILED plugins folder.
3. (Optional) Configure the plugin settings in the generated configuration file.

## Usage

- Players with Facility Guard or MTF roles can use the `.logs` command while holding a keycard to view its access history.
- Any player can use the `.time` command to see the current in-game facility time.

## Building from Source

1. Clone this repository:
   ```bash
   git clone https://github.com/Ducstii/CardLogger.git
   ```
2. Open the `CardLogger.sln` solution in Visual Studio or a compatible C# IDE.
3. Ensure you have the correct Exiled dependencies referenced in the project.
4. Build the solution in Release mode to generate the `CardLogger.dll` file.

## Configuration

The plugin generates a configuration file (usually `CardLogger.yml`) in your EXILED config folder. You can adjust settings such as enabling/disabling the plugin or debug mode.

```yaml
is_enabled: true
debug: false
```

## Contributing

If you find any issues or have suggestions for improvements, feel free to open an issue or submit a pull request on the GitHub repository.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details. 