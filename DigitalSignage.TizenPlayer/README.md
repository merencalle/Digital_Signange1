# Sentinel Tizen Player

**Mission-Ready Digital Signage — Built for the Army**

A web-based (HTML5/CSS/JS) digital signage player built to run on Samsung Smart TVs
(Tizen OS), either as a packaged Tizen Web App or simply opened in the Samsung TV
browser. It is a **standalone, additive sibling** to the existing `DigitalSignage.CMS`
and `DigitalSignage.WindowsPlayer` projects — nothing in those was changed to build
this. See "Relationship to Sentinel CMS" below for how the two can eventually meet.

## What this is for

A multi-building Army installation where each building belongs to a different
command and typically has 3-4 floors. This player is the thing actually running on
the TV in the lobby, elevator bank, hallway, break room, or command suite — looping
through that zone's content, and immediately interrupting for an installation-wide
emergency alert when one is issued.

## Folder structure

```
DigitalSignage.TizenPlayer/
├── config.xml                  Tizen Web App manifest (required to package as .wgt)
├── index.html                  The player itself
├── css/styles.css              Dark, high-contrast military theme
├── js/
│   ├── config.js               Loads content/device-config.json at startup
│   ├── tizen-integration.js    Samsung TV remote / power / network shims
│   ├── content-loader.js       Fetches + caches content (standalone or live mode)
│   └── player.js               Rotation engine, slide rendering, emergency overlay
├── content/
│   ├── schema.md                       Full JSON schema reference
│   ├── device-config.json              Which building/zone THIS screen is
│   ├── buildings/
│   │   ├── bldg-a.json                 Sample: Building A, 5 zones across 3 floors
│   │   └── bldg-b.json                 Sample: Building B, 3 zones across 2 floors
│   ├── global/
│   │   ├── emergency-alerts.json               Default (inactive) global alert
│   │   └── emergency-alerts.SAMPLE-ACTIVE.json Example of an active lockdown alert
│   └── media/images/                   Drop building-specific images/evac maps here
├── admin/index.html             Standalone, file-based JSON editor (see below)
└── assets/icons/                 App icon for Tizen packaging
```

## Running it locally (any browser, for development)

No build step — it's static files. From this folder:

```bash
# Python 3
python -m http.server 8080

# or Node
npx serve .
```

Open `http://localhost:8080`. It loads `content/device-config.json`, which by
default points at `bldg-a` / `bldg-a-floor1-lobby` in standalone mode — you should
see the Building A lobby rotation immediately.

**To see the emergency override in action**: copy
`content/global/emergency-alerts.SAMPLE-ACTIVE.json` over
`content/global/emergency-alerts.json` while the player is running. Within 5 seconds
it interrupts and shows the full-screen lockdown alert. Restore the original file (or
set `"active": false`) to clear it.

## Deploying to a real Samsung Smart TV

### Option A — just point the TV browser at it (fastest, good for many TV models)

Most commercial Samsung signage-capable TVs have a built-in web browser /
URL-launcher mode. Host this folder on a server reachable by the TV (the existing
Sentinel CMS server works fine as a static file host, or any web server on your
installation's LAN) and point the TV's browser/URL launcher at
`http://<server>/DigitalSignage.TizenPlayer/index.html?device=<name>`. Set the TV to
auto-launch that URL on boot (via the TV's own settings or a MDC/RMS provisioning
profile, depending on your Samsung commercial display model).

### Option B — package as a real Tizen Web App (.wgt) via Tizen Studio

This is the right path for Tizen-certified Smart TVs (not just commercial displays)
and gives you proper app lifecycle, auto-launch, and remote-control integration.

1. Install **Tizen Studio** (free, from the Tizen Developers site) with the **TV
   Extension SDK**.
2. `File → Import → Tizen Project` and select this `DigitalSignage.TizenPlayer`
   folder (it already has a valid `config.xml`).
3. Put the TV in **Developer Mode**: on the TV, open the Apps screen, press `1 2 3
   4 5` on the remote (or via Smart Hub settings on newer models) → Developer Mode
   → On → enter your PC's IP address → restart the TV.
4. In Tizen Studio, **Device Manager** → connect to the TV by its IP (same network).
5. Right-click the project → **Run As → Tizen Web Application** to push and launch
   it directly, or **Build Signed Package** → **.wgt** file, then use the Device
   Manager's "Install" to side-load that .wgt for a persistent install that survives
   reboot.
6. Set the installed app as the TV's auto-launch app (commercial Samsung
   displays expose this under their signage/MDC settings; consumer Tizen TVs do this
   via Smart Hub app management).

### Configuring each physical screen

Every screen needs its own `content/device-config.json` telling it which
building/zone it is:

```json
{
  "deviceName": "BldgA-Floor1-Lobby-TV1",
  "buildingId": "bldg-a",
  "zoneId": "bldg-a-floor1-lobby",
  "mode": "standalone",
  "cmsBaseUrl": null,
  "contentBasePath": "content"
}
```

The simplest approach for many screens: host one copy of this player per
building (or per screen) with its own `device-config.json`, or serve the same
files but give each TV a different query string and adapt `config.js` to read it
(left as a small extension point — currently `device-config.json` is the single
source of truth).

## Editing content

- **Building/zone content**: edit the JSON files under `content/buildings/`
  directly, or use `admin/index.html` (open it in any browser) to load, edit, and
  re-download one. See `content/schema.md` for the full field reference.
- **Emergency alerts**: edit `content/global/emergency-alerts.json` directly, or use
  the "Emergency Alert" section of `admin/index.html` to generate one and download
  it, then move it into place.

`admin/index.html` is intentionally a thin, **file-based, no-login, no-audit-trail**
tool — it edits JSON files on your own machine and downloads the result, nothing
more. It is **not** meant to replace proper content governance.

## Relationship to Sentinel CMS

This player ships in **standalone mode** by default — it reads local JSON files and
needs nothing from the existing `DigitalSignage.CMS` backend. That's deliberate: it
means this entire project can be demoed, tested, and deployed without touching the
CMS or the WindowsPlayer at all.

If you want this Tizen player to eventually pull live content from the same
Sentinel CMS that the Windows Player uses (centralized control, the approval
workflow, device management UI, etc.), `content-loader.js` already has the live-mode
code path wired to the existing `/api/devices/{id}/playlist` endpoint — set in
`device-config.json`:

```json
{
  "mode": "live",
  "cmsBaseUrl": "https://your-sentinel-host:5110",
  "cmsDeviceId": 7
}
```

**One small CMS-side change would be needed for this to work**, and it has
**not** been made: the Sentinel CMS currently has no CORS policy, so a browser
running this player from a different origin (the Tizen player's own host) would be
blocked from calling the CMS API. The fix is a few lines in `Program.cs` — a CORS
policy scoped *only* to the `/api/devices` group, nothing else touched. Ask for it
specifically when you're ready to wire this up; it's intentionally not bundled into
this delivery.

Also note: Sentinel's content model today only really distinguishes Image and Video
content types. This player's richer schema (message boards, directories, weather,
network status, training/recognition boards) only exists in standalone mode for now
— live mode falls back to a plain message slide for anything that isn't Image/Video.
Extending Sentinel's `ContentItem` model to carry these richer types is a separate,
larger change than this delivery covers.

## Design notes

- Dark navy / red accent theme, matching the Sentinel CMS's own palette (drawn from
  the CPE ST3 command identity) — sans-serif, high contrast, landscape-only.
- Crossfade transitions between slides; per-item rotation duration with a
  zone-level default fallback.
- Offline resilience: every successful content fetch is cached in `localStorage`;
  if a fetch fails (network drop), the last good copy keeps looping and a small
  "Offline" badge appears top-right.
- Emergency alerts are polled independently of the normal content rotation (every 5
  seconds) and take over the entire screen the instant `active: true` is seen,
  regardless of what was playing.
