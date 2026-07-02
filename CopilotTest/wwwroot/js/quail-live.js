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
