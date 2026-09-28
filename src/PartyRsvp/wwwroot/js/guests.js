// Live guest list: the server pushes "rsvpChanged" over SignalR whenever anyone replies,
// and we re-fetch the public guest list so the page never needs a reload.
(() => {
  "use strict";

  const list = document.querySelector("[data-guest-list]");
  if (!list || !window.signalR) return;

  const empty = document.querySelector("[data-empty]");
  const stats = document.querySelectorAll("[data-stat]");

  const renderStats = (data) => {
    stats.forEach((el) => {
      const value = String(data[el.dataset.stat]);
      if (el.textContent !== value) {
        el.textContent = value;
        el.classList.remove("bump");
        void el.offsetWidth; // restart the animation
        el.classList.add("bump");
      }
    });
  };

  // Built with textContent, never innerHTML, so a guest's name or wish can't inject markup.
  const renderGuests = (guests) => {
    const known = new Set([...list.children].map((li) => li.dataset.key));
    const items = guests.map((guest) => {
      const key = `${guest.name}|${guest.partySize}|${guest.message ?? ""}`;
      const li = document.createElement("li");
      li.dataset.key = key;
      if (known.size > 0 && !known.has(key)) li.classList.add("is-new");

      const name = document.createElement("span");
      name.className = "guest-name";
      name.textContent = guest.name;
      li.append(name);

      if (guest.partySize > 1) {
        const plus = document.createElement("span");
        plus.className = "guest-plus";
        plus.textContent = `+${guest.partySize - 1}`;
        li.append(plus);
      }
      if (guest.message) {
        const wish = document.createElement("q");
        wish.className = "guest-wish";
        wish.textContent = guest.message;
        li.append(wish);
      }
      return li;
    });
    list.replaceChildren(...items);
    empty.hidden = guests.length > 0;
  };

  const refresh = async () => {
    const response = await fetch("/api/guests", { headers: { Accept: "application/json" } });
    if (response.ok) renderGuests(await response.json());
  };

  // Tag the server-rendered rows so the first live update can tell which rows are new.
  [...list.children].forEach((li) => {
    const name = li.querySelector(".guest-name")?.textContent ?? "";
    const plus = li.querySelector(".guest-plus")?.textContent.slice(1);
    const wish = li.querySelector(".guest-wish")?.textContent ?? "";
    li.dataset.key = `${name}|${plus ? Number(plus) + 1 : 1}|${wish}`;
  });

  const connection = new signalR.HubConnectionBuilder()
    .withUrl("/hubs/rsvp")
    .withAutomaticReconnect()
    .build();

  connection.on("rsvpChanged", (latest) => {
    renderStats(latest);
    refresh();
  });
  // Catch up on anything we missed while disconnected.
  connection.onreconnected(() => {
    refresh();
    fetch("/api/stats").then((r) => (r.ok ? r.json() : null)).then((s) => s && renderStats(s));
  });

  connection.start().catch((err) => console.warn("Live updates unavailable:", err));
})();
