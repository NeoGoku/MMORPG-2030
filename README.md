# MMORPG-2030

Android-first 3D MMORPG foundation. Game direction: [GDD](docs/GDD.md). Actual progress and limitations: [implementation status](docs/IMPLEMENTATION_STATUS.md).

## Cloud builds from Android
Open a pull request or run **Actions → Client exports → Run workflow**. Open the completed run and download `MMORPG-Android-debug`, `MMORPG-Windows` or `MMORPG-Web` under Artifacts. Android contains a debug APK; Windows requires both EXE and PCK in the same folder; serve the complete Web folder over HTTP(S). Artifact retention is 14 days. New Android CI builds use a new debug key, so uninstalling the old APK may be required. These are test builds, not store-signed releases.

**Server build** compiles the backend and runs a real two-client transport test. All development/builds can run in the cloud; a local PC is not required.

## Connect two players
Start the server in a cloud development environment with .NET 8:

```sh
dotnet run --project Server --urls http://0.0.0.0:5080
```

Run the Godot 4.3 project or an exported client. Enter the reachable server endpoint (`ws://127.0.0.1:5080/ws` only when client and server share a machine), then Connect. On a phone, use a server hostname reachable from that phone. HTTPS browser clients require `wss://…/ws` through an approved TLS endpoint. No public server is provisioned by this repository.

Use WASD/arrows or the on-screen directional buttons. Each connection gets a temporary capsule; another connection can see it move. Reconnect creates a new identity. There are no accounts, persistence or combat yet. The unauthenticated server is for controlled development testing.

## Focused checks

```sh
godot --headless --path Client --editor --quit
godot --headless --path Client --script res://tests/scene_smoke.gd
dotnet build Server/MMORPG.Server.csproj --configuration Release
# With server running on 127.0.0.1:5080:
python -m pip install -r tests/requirements.txt
python tests/multiplayer.py
godot --headless --path Client --script res://tests/network_smoke.gd
```

Protocol and security boundaries: [architecture](docs/ARCHITECTURE.md). No paid hosting or production deployment is configured.
