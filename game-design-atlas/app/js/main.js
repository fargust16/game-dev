function parseRoute() {
  const raw = (location.hash || "#/map").replace(/^#\/?/, "");
  const parts = raw.split("/").filter(Boolean);
  const view = parts[0] || "map";
  const known = ["map", "list", "system", "questions", "sources", "references", "decisions", "audit"];
  return { view: known.indexOf(view) === -1 ? "map" : view, id: parts[1] || null };
}

document.addEventListener("click", (event) => {
  const action = event.target.closest("[data-action]");
  if (action) {
    const name = action.dataset.action;
    if (name === "reset-filters") {
      Object.keys(Atlas.filters).forEach((key) => Atlas.filters[key].clear());
      Atlas.render();
    } else if (name === "zoom-in") {
      Atlas.zoomGraph(1.12);
    } else if (name === "zoom-out") {
      Atlas.zoomGraph(1 / 1.12);
    } else if (name === "zoom-fit") {
      Atlas.graph.home = true;
      Atlas.render();
    } else if (name === "select-edge") {
      Atlas.selectedEdgeId = action.dataset.id;
      Atlas.selectedId = null;
      Atlas.render();
    } else if (name === "scroll-to") {
      const target = document.getElementById(action.dataset.target);
      if (target) target.scrollIntoView({ behavior: "smooth", block: "start" });
    } else if (name === "open-lightbox") {
      Atlas.openLightbox(action.dataset.src, action.dataset.title, action.dataset.note);
    } else if (name === "close-lightbox") {
      if (event.target.closest(".lightbox-frame")) return;
      Atlas.closeLightbox();
    }
  }
  const row = event.target.closest("[data-href]");
  if (row && !event.target.closest("a, button, input, textarea, select")) {
    location.hash = row.dataset.href.replace(/^#/, "");
  }
});

document.addEventListener("change", (event) => {
  const box = event.target.closest("[data-filter]");
  if (box) {
    const bag = Atlas.filters[box.dataset.filter];
    if (box.checked) bag.add(box.dataset.value);
    else bag.delete(box.dataset.value);
    Atlas.render();
    return;
  }
  const qfilter = event.target.closest("[data-qfilter]");
  if (qfilter) {
    Atlas.questionFilters[qfilter.dataset.qfilter] = qfilter.value;
    Atlas.render();
  }
});

document.getElementById("search").addEventListener("input", (event) => {
  const host = document.getElementById("search-results");
  const hits = Atlas.data ? Atlas.searchHits(event.target.value) : [];
  host.innerHTML = "";
  if (!hits.length) {
    host.hidden = true;
    return;
  }
  hits.forEach((hit) => {
    host.append(el("a", { href: hit.href }, [hit.title, el("small", { text: hit.kind })]));
  });
  host.hidden = false;
});

document.addEventListener("keydown", (event) => {
  if (event.key === "Escape" && Atlas.closeLightbox) Atlas.closeLightbox();
});

window.addEventListener("hashchange", () => {
  if (Atlas.closeLightbox) Atlas.closeLightbox();
  Atlas.route = parseRoute();
  if (Atlas.route.view === "system") Atlas.selectedId = Atlas.route.id;
  document.getElementById("search-results").hidden = true;
  Atlas.render();
});

async function boot() {
  Atlas.route = parseRoute();
  try {
    await Atlas.load();
  } catch (error) {
    document.getElementById("main").append(el("p", { class: "empty", text: "Атлас открывается только через локальный сервер: python server.py в папке game-design-atlas." }));
    return;
  }
  if (Atlas.route.view === "system") Atlas.selectedId = Atlas.route.id;
  Atlas.render();
}

boot();
