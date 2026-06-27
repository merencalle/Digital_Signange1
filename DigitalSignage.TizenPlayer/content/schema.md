# Content Schema

Three kinds of JSON files drive the player. All paths are relative to `content/`.

## 1. Building file — `content/buildings/<building-id>.json`

```jsonc
{
  "buildingId": "bldg-a",
  "buildingName": "Building A — Headquarters",
  "command": "1st Infantry Division HQ",
  "installation": "Fort Example",
  "zones": [
    {
      "zoneId": "bldg-a-floor1-lobby",
      "zoneName": "Main Lobby",
      "zoneType": "lobby",        // lobby | elevator | hallway | breakroom | command
      "floor": 1,
      "rotationDefaultDuration": 15,
      "playlist": [
        {
          "id": "welcome-1",
          "type": "message",      // message | image | video | directory | weather | menu | training-board | recognition-board | network-status | html
          "title": "Welcome to Building A",
          "body": "Headquarters, 1st Infantry Division",
          "icon": "shield",
          "duration": 15
        }
      ]
    }
  ]
}
```

## 2. Global emergency alert — `content/global/emergency-alerts.json`

Polled continuously (every few seconds) regardless of which zone/building a screen belongs to. When `active` is `true`, the player immediately interrupts normal rotation and displays this full-screen, on every screen, until it's cleared.

```jsonc
{
  "active": false,
  "level": "FPCON-CHARLIE",   // INFORMATION | CAUTION | WARNING | FPCON-CHARLIE | LOCKDOWN | EVACUATE
  "title": "LOCKDOWN IN EFFECT",
  "message": "Shelter in place. Await further instructions.",
  "issuedBy": "Installation Network Department",
  "issuedAt": "2026-06-27T12:00:00Z",
  "expiresAt": null,
  "evacuationMaps": {
    "1": "content/media/images/evac-bldg-a-floor1.svg"
  }
}
```

## 3. Device assignment — `content/device-config.json` (or per-device, see `js/config.js`)

Tells one physical screen which building/zone it is, and whether it reads local JSON files or pulls live from the Sentinel CMS.

```jsonc
{
  "deviceName": "BldgA-Floor1-Lobby-TV1",
  "buildingId": "bldg-a",
  "zoneId": "bldg-a-floor1-lobby",
  "mode": "standalone",          // "standalone" (local JSON) | "live" (Sentinel CMS API)
  "cmsBaseUrl": null,             // only used when mode = "live"
  "contentBasePath": "content"
}
```

## Content item types and how they render

| type | required fields | notes |
|---|---|---|
| `message` | `title`, `body` | Full-screen card, optional `icon` |
| `image` | `src` | `src` relative to `content/` or an absolute URL |
| `video` | `src` | Auto-plays muted, advances on end or `duration`, whichever first |
| `directory` | `body` (HTML list) or `items[]` | Floor directory listing |
| `weather` | `body` | Static text for now (live weather feed is a future integration) |
| `menu` | `body` | Cafeteria/break room menu text |
| `training-board` / `recognition-board` | `title`, `body` | Same rendering as `message`, distinct styling |
| `network-status` | `title`, `body` | IT/cyber status, styled with a status color via `status: "ok"|"degraded"|"down"` |
| `html` | `body` | Raw HTML injected into the slide (only use for trusted, locally-authored content) |
