/**
 * Sentinel Tizen Player - rotation engine and slide rendering.
 */
(function () {
    const ICONS = {
        shield: "\u{1F6E1}\u{FE0F}",
        star: "⭐",
        "exclamation-triangle": "⚠️",
        flag: "\u{1F6A9}",
        lock: "\u{1F512}",
        "person-badge": "\u{1FAAA}",
        people: "\u{1F465}",
    };

    let config = null;
    let playlist = [];
    let defaultDuration = 15;
    let currentIndex = 0;
    let rotationTimer = null;
    let currentEl = null;

    let slideArea, zoneNameLabel, buildingNameLabel, offlineIndicator, clockEl;

    document.addEventListener("DOMContentLoaded", init);

    async function init() {
        slideArea = document.getElementById("slide-area");
        zoneNameLabel = document.getElementById("zone-name-label");
        buildingNameLabel = document.getElementById("building-name-label");
        offlineIndicator = document.getElementById("offline-indicator");
        clockEl = document.getElementById("clock");

        config = await window.SentinelConfig.load();
        window.SentinelTizen.init();

        startClock();
        await refreshContent();
        startContentRefreshLoop();
        startEmergencyPollingLoop();
    }

    function startClock() {
        const tick = () => {
            clockEl.textContent = new Date().toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });
        };
        tick();
        setInterval(tick, 1000 * 30);
    }

    async function refreshContent() {
        const result = await window.SentinelContentLoader.loadZonePlaylist(config);
        playlist = result.playlist || [];
        defaultDuration = result.defaultDuration || 15;
        zoneNameLabel.textContent = result.zoneName || config.deviceName;
        buildingNameLabel.textContent = result.buildingName || "";

        offlineIndicator.classList.toggle("show", window.SentinelContentLoader.isOffline());

        if (!rotationTimer && !currentEl) {
            currentIndex = 0;
            playCurrentSlide();
        }
    }

    function startContentRefreshLoop() {
        setInterval(refreshContent, config.POLL_INTERVALS?.contentRefreshMs || window.SentinelConfig.POLL_INTERVALS.contentRefreshMs);
    }

    function startEmergencyPollingLoop() {
        const poll = async () => {
            const alert = await window.SentinelContentLoader.loadEmergencyAlert(config);
            applyEmergencyState(alert);
        };
        poll();
        setInterval(poll, window.SentinelConfig.POLL_INTERVALS.emergencyAlertMs);
    }

    function applyEmergencyState(alert) {
        const overlay = document.getElementById("emergency-overlay");
        if (alert && alert.active) {
            document.getElementById("emergency-level").textContent = alert.level || "";
            document.getElementById("emergency-title").textContent = alert.title || "";
            document.getElementById("emergency-message").textContent = alert.message || "";
            document.getElementById("emergency-issued-by").textContent = alert.issuedBy ? `Issued by ${alert.issuedBy}` : "";
            overlay.classList.add("show");
        } else {
            overlay.classList.remove("show");
        }
    }

    function playCurrentSlide() {
        clearTimeout(rotationTimer);

        if (playlist.length === 0) {
            showSlide({ type: "message", title: "No Content Assigned", body: "This screen has no playlist yet. Contact your content manager." });
            return;
        }

        const item = playlist[currentIndex % playlist.length];
        showSlide(item);
        scheduleAdvance(item);
    }

    function scheduleAdvance(item) {
        if (item.type === "video") {
            const videoEl = currentEl.querySelector("video");
            if (videoEl) {
                videoEl.addEventListener("ended", advance, { once: true });
            }
            if (item.duration && item.duration > 0) {
                rotationTimer = setTimeout(advance, item.duration * 1000);
            }
            return;
        }

        const durationMs = (item.duration || defaultDuration) * 1000;
        rotationTimer = setTimeout(advance, durationMs);
    }

    function advance() {
        currentIndex = (currentIndex + 1) % Math.max(playlist.length, 1);
        playCurrentSlide();
    }

    function showSlide(item) {
        const newEl = buildSlideElement(item);
        slideArea.appendChild(newEl);
        requestAnimationFrame(() => newEl.classList.add("active"));

        if (currentEl) {
            const oldEl = currentEl;
            oldEl.classList.remove("active");
            setTimeout(() => oldEl.remove(), 700);
        }
        currentEl = newEl;
    }

    function buildSlideElement(item) {
        const el = document.createElement("div");
        el.className = `slide type-${item.type || "message"}`;

        if (item.type === "html") {
            el.innerHTML = item.body || "";
            return el;
        }

        if (item.type === "image") {
            el.innerHTML = `<img class="slide-media" src="${resolveMediaSrc(item.src)}" alt="${escapeAttr(item.title || "")}" />`;
            return el;
        }

        if (item.type === "video") {
            el.innerHTML = `<video class="slide-media" autoplay muted playsinline src="${resolveMediaSrc(item.src)}"></video>`;
            return el;
        }

        let html = "";
        if (item.type === "network-status") {
            const status = item.status || "ok";
            html += `<div class="status-pill status-${status}">${status.toUpperCase()}</div>`;
        } else if (item.icon && ICONS[item.icon]) {
            html += `<div class="slide-icon">${ICONS[item.icon]}</div>`;
        }
        if (item.title) {
            html += `<h1>${item.title}</h1>`;
        }
        if (item.body) {
            html += `<div class="body-text">${item.body}</div>`;
        }
        el.innerHTML = html;
        return el;
    }

    function resolveMediaSrc(src) {
        if (!src) return "";
        if (/^https?:\/\//i.test(src)) {
            return src;
        }
        const base = config?.contentBasePath || "content";
        return src.startsWith(base) ? src : `${base}/${src.replace(/^\//, "")}`;
    }

    function escapeAttr(value) {
        return String(value).replace(/"/g, "&quot;");
    }
})();
