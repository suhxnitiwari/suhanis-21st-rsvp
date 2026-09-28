// Small progressive enhancements. Every page works without JavaScript.
(() => {
  "use strict";

  const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;

  // Countdown to the party on the invitation page.
  const countdown = document.querySelector("[data-countdown]");
  if (countdown) {
    const target = Date.parse(countdown.dataset.countdown);
    const units = {
      days: countdown.querySelector('[data-unit="days"]'),
      hours: countdown.querySelector('[data-unit="hours"]'),
      minutes: countdown.querySelector('[data-unit="minutes"]'),
      seconds: countdown.querySelector('[data-unit="seconds"]'),
    };
    const pad = (n) => String(n).padStart(2, "0");

    const tick = () => {
      const left = Math.max(0, target - Date.now());
      if (left === 0) {
        countdown.classList.add("is-done");
        countdown.querySelector(".countdown-done").hidden = false;
        clearInterval(timer);
        return;
      }
      const s = Math.floor(left / 1000);
      units.days.textContent = Math.floor(s / 86400);
      units.hours.textContent = pad(Math.floor(s / 3600) % 24);
      units.minutes.textContent = pad(Math.floor(s / 60) % 60);
      units.seconds.textContent = pad(s % 60);
    };
    const timer = setInterval(tick, 1000);
    tick();
  }

  // RSVP form: stop double-submits while the reply is saving.
  // (Showing party size only for "yes" is handled in CSS with :has().)
  const form = document.querySelector("[data-rsvp-form]");
  form?.addEventListener("submit", () => {
    form.querySelector('button[type="submit"]').classList.add("is-busy");
  });

  // A soft shower of petals when someone says yes.
  const petalHost = document.querySelector("[data-petals]");
  if (petalHost && !reducedMotion) {
    const colors = ["#f3c2c5", "#e89aa3", "#f7dcd6", "#cd6f75", "#dfe6cf"];
    const layer = document.createElement("div");
    layer.className = "petals";
    layer.setAttribute("aria-hidden", "true");
    for (let i = 0; i < 36; i++) {
      const petal = document.createElement("span");
      petal.style.setProperty("--x", `${Math.random() * 100}vw`);
      petal.style.setProperty("--drift", `${(Math.random() - 0.5) * 30}vw`);
      petal.style.setProperty("--spin", `${(Math.random() - 0.5) * 900}deg`);
      petal.style.setProperty("--size", `${8 + Math.random() * 10}px`);
      petal.style.setProperty("--delay", `${Math.random() * 1.8}s`);
      petal.style.setProperty("--fall", `${3.5 + Math.random() * 3}s`);
      petal.style.background = colors[i % colors.length];
      layer.append(petal);
    }
    document.body.append(layer);
    setTimeout(() => layer.remove(), 9000);
  }
})();
