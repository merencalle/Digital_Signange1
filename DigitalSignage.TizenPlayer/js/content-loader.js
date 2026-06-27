/**
 * Sentinel Tizen Player - content loading.
 *
 * Two modes:
 *  - "standalone": reads JSON files under content/ on this same device/server.
 *  - "live": pulls from the existing Sentinel CMS HTTP API (the same
 *    /api/devices/{id}/playlist endpoint the WindowsPlayer uses), so this
 *    Tizen player can become just another player type against the same
 *    backend without any CMS changes other than enabling CORS for that
 *    origin (see README "Live mode" section - NOT enabled by default).
 *
 * Every successful fetch is cached in localStorage. If a fetch fails
 * (network drop, CMS unreachable), the last good cached copy is served so
 * the screen keeps looping instead of going blank.
 */
window.SentinelContentLoader = (function () {
    const CACHE_KEYS = {
        zone: "sentinel.cache.zone",
        emergency: "sentinel.cache.emergency",
    };

    let lastFetchFailed = false;

    function isOffline() {
        return lastFetchFailed;
    }

    async function loadZonePlaylist(config) {
        try {
            const result = config.mode === "live"
                ? await loadZoneFromCms(config)
                : await loadZoneFromStandalone(config);
            localStorage.setItem(CACHE_KEYS.zone, JSON.stringify(result));
            lastFetchFailed = false;
            return result;
        } catch (err) {
            console.error("[SentinelContentLoader] Failed to load zone playlist, falling back to cache.", err);
            lastFetchFailed = true;
            const cached = localStorage.getItem(CACHE_KEYS.zone);
            return cached ? JSON.parse(cached) : { buildingName: "", zoneName: "", playlist: [] };
        }
    }

    async function loadZoneFromStandalone(config) {
        const base = config.contentBasePath || "content";
        const response = await fetch(`${base}/buildings/${config.buildingId}.json`, { cache: "no-store" });
        if (!response.ok) {
            throw new Error(`building file returned ${response.status}`);
        }
        const building = await response.json();
        const zone = building.zones.find((z) => z.zoneId === config.zoneId);
        if (!zone) {
            throw new Error(`zone '${config.zoneId}' not found in building '${config.buildingId}'`);
        }

        return {
            buildingName: building.buildingName,
            zoneName: zone.zoneName,
            defaultDuration: zone.rotationDefaultDuration || 15,
            playlist: zone.playlist,
        };
    }

    async function loadZoneFromCms(config) {
        if (!config.cmsBaseUrl || !config.cmsDeviceId) {
            throw new Error("live mode requires cmsBaseUrl and cmsDeviceId in device-config.json");
        }

        const response = await fetch(`${config.cmsBaseUrl}/api/devices/${config.cmsDeviceId}/playlist`, {
            cache: "no-store",
        });

        if (response.status === 204) {
            return { buildingName: config.buildingId || "", zoneName: config.zoneId || "", defaultDuration: 15, playlist: [] };
        }
        if (!response.ok) {
            throw new Error(`CMS playlist endpoint returned ${response.status}`);
        }

        const dto = await response.json();

        // Map Sentinel's ContentItem shape (Image/Video/HTML/Widget + filePath)
        // onto this player's richer slide schema (type/title/body/src/duration).
        const playlist = (dto.items || []).map((item) => {
            const mediaUrl = `${config.cmsBaseUrl}/${String(item.filePath || "").replace(/^\//, "")}`;
            if (item.contentType === "Image") {
                return { id: String(item.id), type: "image", src: mediaUrl, duration: 15 };
            }
            if (item.contentType === "Video") {
                return { id: String(item.id), type: "video", src: mediaUrl, duration: 0 };
            }
            // HTML/Widget content isn't rendered by the CMS-side pipeline yet -
            // fall back to a plain message slide so nothing silently disappears.
            return { id: String(item.id), type: "message", title: item.name, body: item.contentType, duration: 12 };
        });

        return { buildingName: config.buildingId || "", zoneName: dto.playlistName || "", defaultDuration: 15, playlist };
    }

    async function loadEmergencyAlert(config) {
        try {
            const base = config.contentBasePath || "content";
            const response = await fetch(`${base}/global/emergency-alerts.json`, { cache: "no-store" });
            if (!response.ok) {
                throw new Error(`emergency-alerts.json returned ${response.status}`);
            }
            const data = await response.json();
            localStorage.setItem(CACHE_KEYS.emergency, JSON.stringify(data));
            return data;
        } catch (err) {
            const cached = localStorage.getItem(CACHE_KEYS.emergency);
            return cached ? JSON.parse(cached) : { active: false };
        }
    }

    return { loadZonePlaylist, loadEmergencyAlert, isOffline };
})();
