// Short two-note chime played when it becomes your turn in live combat.
// WebAudio only (no audio file); browsers allow it because the player has
// already interacted with the page (claiming a seat).
window.quailTurnDing = function () {
    try {
        const ctx = new (window.AudioContext || window.webkitAudioContext)();
        const note = (freq, start, dur) => {
            const osc = ctx.createOscillator();
            const gain = ctx.createGain();
            osc.type = "sine";
            osc.frequency.value = freq;
            gain.gain.setValueAtTime(0.0001, ctx.currentTime + start);
            gain.gain.exponentialRampToValueAtTime(0.3, ctx.currentTime + start + 0.02);
            gain.gain.exponentialRampToValueAtTime(0.0001, ctx.currentTime + start + dur);
            osc.connect(gain).connect(ctx.destination);
            osc.start(ctx.currentTime + start);
            osc.stop(ctx.currentTime + start + dur + 0.05);
        };
        note(660, 0, 0.35);      // E5
        note(880, 0.18, 0.5);    // A5
        setTimeout(() => ctx.close(), 1500);
    } catch (e) { /* audio unavailable — the on-screen banner still shows */ }
};

// Auto-recover from a server restart: Blazor toggles CSS classes on
// #components-reconnect-modal as the circuit drops and reconnect attempts run.
// Once attempts fail (or the server rejects the dead circuit), poll until the
// server answers again, then reload — the seat and encounter restore themselves.
(function () {
    const modal = document.getElementById("components-reconnect-modal");
    if (!modal) return;
    let polling = false;
    function pollAndReload() {
        if (polling) return;
        polling = true;
        const tick = setInterval(function () {
            fetch(window.location.href, { method: "HEAD", cache: "no-store" })
                .then(function (r) { if (r.ok) { clearInterval(tick); window.location.reload(); } })
                .catch(function () { /* still down — keep polling */ });
        }, 2000);
    }
    new MutationObserver(function () {
        const cls = modal.className || "";
        if (cls.includes("components-reconnect-failed") || cls.includes("components-reconnect-rejected"))
            pollAndReload();
    }).observe(modal, { attributes: true, attributeFilter: ["class"] });
})();

// Flash the browser tab title while it's your turn, so players who switched
// tabs/apps notice. quailTitleAlert(true) starts, quailTitleAlert(false) stops.
(function () {
    let timer = null;
    const original = document.title;
    window.quailTitleAlert = function (on) {
        if (timer) { clearInterval(timer); timer = null; }
        if (on) {
            let flip = false;
            timer = setInterval(function () {
                document.title = flip ? original : "🎲 YOUR TURN!";
                flip = !flip;
            }, 1000);
            document.title = "🎲 YOUR TURN!";
        } else {
            document.title = original;
        }
    };
})();
