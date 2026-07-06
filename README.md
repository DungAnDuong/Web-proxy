# Web Server Manager

A WinForms (.NET Framework 4.8) application for managing many independent local web
applications (Node.js, Next.js, Express, ...) on a single Windows host, each bound to a private
`localhost` port, fronted by a single [Caddy](https://caddyserver.com) reverse proxy that
terminates HTTPS on port 443 and routes by host header. No website ever binds 443 directly.

```
https://ade-aibom.ddns.net  ---\
https://erp247.ddns.net     ----+---> Caddy (:443) ---> localhost:3001 / :3002 / ...
https://mes.company.vn      ---/
```

## Solution layout

Clean Architecture, four projects under `WebServerManagement/WebServerManagement.sln`:

| Project | Responsibility |
|---|---|
| `WebServerManagement.Core` | Domain models, interfaces (ports), validation, and pure business logic (Caddyfile generation, restart policy). No I/O, no third-party dependencies. |
| `WebServerManagement.Infrastructure` | Concrete implementations: LiteDB persistence, process management (start/stop/pause/restart, crash auto-restart, CPU/RAM sampling), Caddy process supervision + reload, per-site/app file logging, port checking, HTTP health checks, Windows Startup registration. |
| `WebServerManagement.UI` | WinForms presentation: main grid, Add/Edit Website dialog, Settings dialog, log viewer, dark theme, system tray. Composition root lives in `Program.cs`. |
| `WebServerManagement.Tests` | NUnit + Moq unit tests for the Core/Infrastructure business logic listed above. |

## Prerequisites

- Windows 10/11.
- Visual Studio 2017 (15.9+) or later, **or** just the .NET Framework 4.8 Developer Pack + MSBuild, if building from the command line.
- [Caddy](https://caddyserver.com/download) -- **not bundled**. Download `caddy.exe` and either drop it in `Tools/caddy/` or point Settings at wherever you put it.
- Node.js (or whatever runtime your managed sites need) installed on the host; Web Server Manager launches your sites' own start commands, it does not bundle a runtime either.

## Build

Open `WebServerManagement/WebServerManagement.sln` in Visual Studio and build (NuGet packages -- LiteDB, Newtonsoft.Json, NUnit, Moq -- restore automatically). Or from a command prompt:

```powershell
msbuild WebServerManagement\WebServerManagement.sln /t:restore
msbuild WebServerManagement\WebServerManagement.sln /p:Configuration=Release
```

Set `WebServerManagement.UI` as the startup project if running from Visual Studio.

## Run the tests

```powershell
vstest.console.exe WebServerManagement\WebServerManagement.Tests\bin\Debug\WebServerManagement.Tests.dll
```

(or use the Visual Studio Test Explorer).

## First run

1. Launch `WebServerManagement.UI.exe`. On first launch it creates `Data/` (LiteDB config store), `Logs/` (per-site + app logs), and `ReverseProxy/` (generated `Caddyfile`) next to the executable.
2. Open **Tools > Settings** and set the **Caddy Executable Path** (see `Tools/caddy/README.md`).
3. Click **Add Website** and fill in:
   - Website Name, Source Folder, Domain, Internal Port
   - Node Executable (e.g. `C:\Program Files\nodejs\node.exe`), Working Directory
   - Command (e.g. `npm run start` or `node server.js`)
   - Environment (Development/Production), Auto Start, Enable SSL (+ cert/key if not using Caddy's automatic HTTPS)
4. Click **Start**, then **Reload Reverse Proxy** to regenerate and apply the Caddyfile. The grid's Status/PID/CPU/RAM columns update every second.
5. Repeat for additional sites -- each gets its own internal port and domain; Caddy fans all of them out over port 443.

## Deployment notes

- **Port 443**: binding it typically requires elevation. Either run Web Server Manager (and therefore its supervised Caddy process) elevated, or run Caddy separately as a properly-permissioned Windows Service and point the app at it.
- **Run at Windows Startup**: toggle in Settings; this creates a shortcut in the current user's Startup folder (no registry writes).
- **Backup / move to another machine**: use **Export** to write `config.json`, and **Import** to load it elsewhere. LiteDB (`Data/webservermanager.db`) remains the live source of truth at runtime.
- **Extending to other runtimes** (Python/Flask, PHP, ASP.NET Core, ...): implement `IRuntimeAdapter` in the Infrastructure project and register it in `RuntimeAdapterFactory` -- no existing code needs to change.

## Known limitations

- Pausing/resuming a website suspends its OS threads (via `NtSuspendProcess`/`NtResumeProcess`); this holds the process in memory rather than freeing its port, which is the correct semantics for "pause" but is not the same as stopping it.
- The health check service and auto-restart policy operate per-site based on `HealthCheckPath`/`MaxRestartCount`/`RestartWindowSeconds` set on each website; leaving `HealthCheckPath` empty disables HTTP health polling for that site (crash-based auto-restart still applies).
