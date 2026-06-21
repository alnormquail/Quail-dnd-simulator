# Hosting the Quail D&D Simulator

A step-by-step guide to running this app on a cheap Linux VPS so your party
can reach it from anywhere. Written for someone who hasn't done this before —
follow it top to bottom and copy-paste the commands.

## The plan (and why)

- **A DigitalOcean droplet** (a small always-on Linux server, ~$6/mo). A plain
  VPS is the best fit for this app: the SQLite database lives on a normal
  persistent disk (no data-loss surprises on redeploy), and the server never
  "sleeps," so nobody's live session gets dropped mid-game.
- **Caddy** sits in front as a reverse proxy. It gives you automatic HTTPS (a
  real padlock, no certificate wrangling) and a **shared password gate**.
- **A `systemd` service** keeps the app running and restarts it on crash or reboot.

```
  Player's browser ──HTTPS──▶  Caddy (port 443)  ──HTTP──▶  the app (127.0.0.1:5179)
                               • TLS certificate                • runs as a systemd service
                               • shared-password gate           • SQLite file on local disk
```

> ⚠️ **Important security note.** This app has **no login system** — anyone who
> can open the page can view and edit every character. That's why we put a
> shared password in front of it with Caddy. Treat that password like the key
> to your group's data. (If you'd rather it never be on the public internet at
> all, see "Alternative: Tailscale" at the bottom.)

---

## 1. What you need first

- A **DigitalOcean account** and a new **Droplet**:
  - Image: **Ubuntu 24.04 LTS**
  - Size: **Basic, 1 GB RAM / 1 vCPU** (the ~$6/mo option — plenty for a party)
  - Add your **SSH key** during creation (DigitalOcean walks you through it)
- A **domain name** pointing at the droplet. Two options:
  - A real domain you own (e.g. `quail.yourdomain.com`), or
  - A **free** subdomain from [DuckDNS](https://www.duckdns.org) (e.g. `quailparty.duckdns.org`)
  - Either way, create a **DNS "A" record** for that name pointing to your
    droplet's public IP address.

You'll do most of the work over SSH:

```bash
ssh root@YOUR_DROPLET_IP
```

Everything in sections 2–6 runs **on the droplet** unless it says "on your Mac."

---

## 2. Install the .NET runtime

The app is built with .NET 10, so the server needs the ASP.NET Core **runtime**
(not the full SDK — you'll build on your Mac and copy the result up).

```bash
# Add Microsoft's package feed
wget https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb -O /tmp/ms-prod.deb
sudo dpkg -i /tmp/ms-prod.deb
rm /tmp/ms-prod.deb

sudo apt-get update
sudo apt-get install -y aspnetcore-runtime-10.0
```

Verify it worked:

```bash
dotnet --list-runtimes        # should list Microsoft.AspNetCore.App 10.0.x
```

> If `apt` can't find `aspnetcore-runtime-10.0`, Ubuntu's own .NET feed may be
> shadowing Microsoft's. As a fallback, install via the official script:
> ```bash
> curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
> sudo bash /tmp/dotnet-install.sh --channel 10.0 --runtime aspnetcore --install-dir /usr/share/dotnet
> sudo ln -sf /usr/share/dotnet/dotnet /usr/bin/dotnet
> ```

---

## 3. Create a home and a user for the app

Running the app as its own unprivileged user is good hygiene.

```bash
sudo useradd --system --no-create-home --shell /usr/sbin/nologin quail
sudo mkdir -p /opt/quail /opt/quail/backups
sudo chown -R quail:quail /opt/quail
```

The database file (`dnd-party.db`) will be created automatically inside
`/opt/quail` the first time the app runs, seeded from the code. It stays there
across restarts and updates.

---

## 4. Build on your Mac and copy it up

Run these **on your Mac**, from the repo folder:

```bash
# Build a release version into ./publish
dotnet publish CopilotTest/CopilotTest.csproj -c Release -o ./publish

# Copy it to the server. NOTE: --exclude protects the live database from
# being overwritten or deleted. Never rsync with --delete here.
rsync -avz --exclude 'dnd-party.db*' ./publish/ root@YOUR_DROPLET_IP:/opt/quail/
```

Then back **on the droplet**, hand ownership to the `quail` user:

```bash
sudo chown -R quail:quail /opt/quail
```

---

## 5. Run it as a service

Create the service definition:

```bash
sudo nano /etc/systemd/system/quail.service
```

Paste this in:

```ini
[Unit]
Description=Quail D&D Simulator
After=network.target

[Service]
WorkingDirectory=/opt/quail
ExecStart=/usr/bin/dotnet /opt/quail/CopilotTest.dll
Restart=always
RestartSec=5
User=quail
# Production turns off dev-only SQL logging and uses the production error page.
Environment=ASPNETCORE_ENVIRONMENT=Production
# Bind to localhost only — Caddy is the public-facing piece.
Environment=ASPNETCORE_URLS=http://127.0.0.1:5179
# Trust the X-Forwarded-* headers Caddy adds, so the app knows it's behind HTTPS.
Environment=ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
Environment=DOTNET_CLI_TELEMETRY_OPTOUT=1

[Install]
WantedBy=multi-user.target
```

Save (`Ctrl+O`, `Enter`, `Ctrl+X`), then start it:

```bash
sudo systemctl daemon-reload
sudo systemctl enable --now quail
sudo systemctl status quail        # should say "active (running)"
```

Quick local check that the app is answering (before HTTPS is set up):

```bash
curl -I http://127.0.0.1:5179      # expect HTTP/1.1 200 OK
```

If something's wrong, the logs are:

```bash
sudo journalctl -u quail -e --no-pager
```

---

## 6. Put Caddy in front (HTTPS + password)

Install Caddy:

```bash
sudo apt-get install -y debian-keyring debian-archive-keyring apt-transport-https curl
curl -1sLf 'https://dl.cloudsmith.io/public/caddy/stable/gpg.key' | sudo gpg --dearmor -o /usr/share/keyrings/caddy-stable-archive-keyring.gpg
curl -1sLf 'https://dl.cloudsmith.io/public/caddy/stable/debian.deb.txt' | sudo tee /etc/apt/sources.list.d/caddy-stable.list
sudo apt-get update
sudo apt-get install -y caddy
```

Generate a password hash for the party's shared login (pick any password):

```bash
caddy hash-password --plaintext 'your-shared-party-password'
# copy the long $2a$... string it prints
```

Edit the Caddy config:

```bash
sudo nano /etc/caddy/Caddyfile
```

Replace its contents with (use **your** domain and the **hash** from above):

```caddyfile
quail.yourdomain.com {
    # One shared login for the whole party. Username is "party".
    basic_auth {
        party PASTE_THE_$2a$_HASH_HERE
    }

    # Hand requests to the app. Caddy handles WebSockets (Blazor needs them)
    # automatically — no extra config required.
    reverse_proxy 127.0.0.1:5179
}
```

> On Caddy versions older than 2.8 the directive is spelled `basicauth`
> (no underscore). Check with `caddy version` if it complains.

Reload Caddy:

```bash
sudo systemctl reload caddy
```

Caddy will automatically obtain an HTTPS certificate the first time someone
visits `https://quail.yourdomain.com` (this needs your DNS A record to be
pointing at the droplet already — see section 1).

---

## 7. Open the firewall

```bash
sudo ufw allow OpenSSH
sudo ufw allow 80
sudo ufw allow 443
sudo ufw enable
```

Port 5179 is intentionally **not** opened — the app only listens on localhost,
and the public reaches it through Caddy on 443.

---

## 8. You're live

Open **https://quail.yourdomain.com** in a browser. You'll be asked for the
shared login (username `party`, plus your password), then the app loads over
HTTPS. Share that address and the password with your players.

---

## Updating the app later

Whenever you change the code, repeat the build-and-copy from your Mac, then
restart the service:

```bash
# on your Mac
dotnet publish CopilotTest/CopilotTest.csproj -c Release -o ./publish
rsync -avz --exclude 'dnd-party.db*' ./publish/ root@YOUR_DROPLET_IP:/opt/quail/

# on the droplet
sudo chown -R quail:quail /opt/quail
sudo systemctl restart quail
```

The `--exclude 'dnd-party.db*'` is what keeps your party's data safe across
updates — it skips the database and its WAL sidecar files.

---

## Backups (do this — character sheets are irreplaceable)

The database uses **WAL mode**, so the newest changes live partly in a
`dnd-party.db-wal` sidecar file. **Don't** back up by copying `dnd-party.db`
alone — you'd miss recent edits. Use SQLite's online backup, which is safe to
run while the app is live:

```bash
sudo apt-get install -y sqlite3

# one backup, named by date
sudo -u quail sqlite3 /opt/quail/dnd-party.db ".backup '/opt/quail/backups/dnd-party-$(date +%F).db'"
```

Automate it daily at 3am with cron:

```bash
sudo crontab -u quail -e
```

Add this line (the `\%` escaping is required inside crontab):

```cron
0 3 * * * sqlite3 /opt/quail/dnd-party.db ".backup '/opt/quail/backups/dnd-party-$(date +\%F).db'"
```

To **restore** a backup: stop the app, swap the file in, clear the stale
sidecars, start again:

```bash
sudo systemctl stop quail
sudo -u quail cp /opt/quail/backups/dnd-party-YYYY-MM-DD.db /opt/quail/dnd-party.db
sudo rm -f /opt/quail/dnd-party.db-wal /opt/quail/dnd-party.db-shm
sudo systemctl start quail
```

Copy backups off the server periodically too (from your Mac):
`rsync -avz root@YOUR_DROPLET_IP:/opt/quail/backups/ ./quail-backups/`

---

## Keep the box healthy

Turn on automatic security updates so you don't have to babysit it:

```bash
sudo apt-get install -y unattended-upgrades
sudo dpkg-reconfigure --priority=low unattended-upgrades   # choose "Yes"
```

---

## Alternative: keep it off the public internet (Tailscale)

If you'd prefer the app never be reachable from the open web, skip Caddy and the
domain. Install [Tailscale](https://tailscale.com) on the droplet and on each
player's device; they reach the app at the droplet's private Tailscale address
(e.g. `http://100.x.y.z:5179`). More private, but every player must install
Tailscale and sign in — more friction for a non-technical group. For that setup,
change `ASPNETCORE_URLS` to `http://0.0.0.0:5179` and open port 5179 only to the
Tailscale interface.
