/**
 * Sentinel Tizen Player - Samsung Tizen TV integration shims.
 *
 * Everything here is defensively wrapped: this file runs fine in a normal
 * desktop browser (for development/testing) and only engages real Tizen
 * APIs when running inside an actual Tizen WebKit container.
 */
window.SentinelTizen = (function () {
    const isTizen = typeof window.tizen !== "undefined";

    function init() {
        if (!isTizen) {
            console.info("[SentinelTizen] Not running under Tizen - skipping TV-specific integration.");
            return;
        }

        preventAccidentalExit();
        keepScreenAwake();
    }

    // Samsung TV remotes send a "Return" key that, by default, exits the app.
    // Signage should never exit on a stray remote press.
    function preventAccidentalExit() {
        document.addEventListener("tizenhwkey", function (event) {
            if (event.keyName === "back") {
                event.preventDefault?.();
                console.info("[SentinelTizen] Ignored Return/Back key - signage stays running.");
            }
        });
    }

    function keepScreenAwake() {
        try {
            window.tizen.power.request("SCREEN", "SCREEN_NORMAL");
        } catch (err) {
            console.warn("[SentinelTizen] tizen.power.request not available on this profile.", err);
        }
    }

    function getNetworkStatus() {
        if (!isTizen) {
            return navigator.onLine ? "connected" : "disconnected";
        }
        try {
            return window.tizen.systeminfo.getCapability("http://tizen.org/feature/network.wifi")
                ? "connected"
                : "disconnected";
        } catch (err) {
            return navigator.onLine ? "connected" : "disconnected";
        }
    }

    return { isTizen, init, getNetworkStatus };
})();
