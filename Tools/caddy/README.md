# Caddy binary goes here

Web Server Manager supervises Caddy as a child process, but it does not bundle the Caddy
binary itself. Download the Windows build for your architecture from
https://caddyserver.com/download (or https://github.com/caddyserver/caddy/releases) and place
`caddy.exe` in this folder -- or anywhere else, as long as you point **Settings > Caddy
Executable Path** at it.

Once configured, the app will:

1. Generate a `Caddyfile` from your enabled websites (`domain { reverse_proxy localhost:port }`).
2. Run `caddy validate` against it before applying anything.
3. Start Caddy if it isn't running yet, or `caddy reload` it if it already is.

No manual editing of the Caddyfile is required or expected.
