/**
 * Sentinel Tizen Player - runtime configuration loader.
 *
 * Reads content/device-config.json, which tells this physical screen which
 * building/zone it represents and whether it should read local JSON files
 * ("standalone" mode) or pull live from the Sentinel CMS API ("live" mode).
 */
window.SentinelConfig = (function () {
    const DEFAULTS = {
        deviceName: "Unconfigured Player",
        buildingId: null,
        zoneId: null,
        mode: "standalone",
        cmsBaseUrl: null,
        contentBasePath: "content",
    };

    const POLL_INTERVALS = {
        emergencyAlertMs: 5000, // check for emergency overrides every 5s
        contentRefreshMs: 60000, // re-pull the zone playlist every 60s
    };

    let current = { ...DEFAULTS };

    async function load() {
        try {
            const response = await fetch("content/device-config.json", { cache: "no-store" });
            if (!response.ok) {
                throw new Error(`device-config.json returned ${response.status}`);
            }
            const data = await response.json();
            current = { ...DEFAULTS, ...data };
        } catch (err) {
            console.error("[SentinelConfig] Could not load device-config.json, using defaults.", err);
        }
        return current;
    }

    function get() {
        return current;
    }

    return { load, get, POLL_INTERVALS };
})();
