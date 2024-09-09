# Shadow

Monitor or control remote user sessions.

## Description

Shadow is a lightweight Windows application designed for IT professionals and system administrators. It allows seamless monitoring and control of remote user sessions without requiring end-user interaction.

## Features

- Connect via IPv4, IPv6, or hostname
- Monitor mode for passive observation
- Control mode for active assistance
- User-friendly interface for session selection
- Automatic discovery of active remote sessions

## Installation

1. Download the latest release from the [Releases](https://github.com/soc-otter/shadow/releases) page.
2. Extract the ZIP file to your desired location.
3. Run `Shadow.exe` to start the application.

## Usage

1. Launch Shadow.
2. Enter the target machine's IPv4, IPv6, or hostname.
3. Click "OK" to discover active sessions.
4. Select a session from the list.
5. Choose "Monitor" or "Control" to start shadowing.

## Requirements

- Windows 10 or later
- .NET Framework 4.7.2 or higher
- Administrative privileges on both local and target machines

## Notes

- Ensure that the Windows Firewall on the target machine allows incoming connections for remote administration.
- The application modifies registry settings on the target machine to enable shadowing. These changes are reverted after the session ends.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/soc-otter/Shadow/blob/master/LICENSE.txt) file for details.

## Disclaimer

This tool is intended for legitimate system administration and support purposes only. Always ensure you have the necessary permissions and comply with relevant policies and regulations when using Shadow.
