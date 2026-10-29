/* Reference gallery and the framed picture viewer. Notes live in data/references.json. */

function mediaSrc(ref) {
  return "media/" + ref.path.split("/").map(encodeURIComponent).join("/");
}

Atlas.referenceCard = function (ref) {
  const card = el("article", { class: "thumb-card" });
  if (ref.path) {
    const src = mediaSrc(ref);
    const button = el("button", {
      type: "button",
      class: "thumb",
      "data-action": "open-lightbox",
      "data-src": src,
      "data-title": ref.title,
      "data-note": ref.note || "",
    });
    button.append(el("img", { src: src, alt: "" }));
    card.append(button);
  }
  card.append(el("h3", { text: ref.title }));
  card.append(el("p", { text: ref.note }));
  if (ref.url) {
    card.append(el("p", {}, [el("a", { href: ref.url, target: "_blank", rel: "noreferrer", text: ref.url })]));
  }
  const links = el("p", { class: "links" });
  (ref.systemIds || []).forEach((id) => {
    const system = Atlas.system(id);
    if (!system) return;
    if (links.childNodes.length) links.append(document.createTextNode(", "));
    links.append(el("a", { href: "#/system/" + id, text: system.name }));
  });
  if (links.childNodes.length) card.append(links);
  card.append(el("p", { class: "meta", text: "Референс не становится требованием и не становится принятым решением." }));
  return card;
};

Atlas.renderReferences = function (main) {
  const page = el("div", { class: "gallery-page" });
  page.append(el("h1", { text: "Референсы" }));
  page.append(el("p", { class: "lede", text: "Миниатюра открывает изображение в рамке. Двойной клик увеличивает его в этой точке, колесо меняет масштаб, перетаскивание сдвигает увеличенную картинку. Клик по тёмному полю закрывает просмотр." }));
  const grid = el("div", { class: "gallery" });
  Atlas.data.references.forEach((ref) => grid.append(Atlas.referenceCard(ref)));
  page.append(grid);
  main.append(page);
};

Atlas.openLightbox = function (src, title, note) {
  Atlas.closeLightbox();
  const overlay = el("div", {
    class: "lightbox",
    role: "dialog",
    "aria-modal": "true",
    "aria-label": title || "Изображение",
    "data-action": "close-lightbox",
  });
  const frame = el("div", { class: "lightbox-frame" });
  const viewer = el("div", { class: "viewer" });
  const img = el("img", { src: src, alt: title || "", draggable: "false" });
  viewer.append(img);

  const noteBox = el("div", { class: "viewer-note", hidden: "hidden" });
  if (title) noteBox.append(el("strong", { text: title }));
  if (note) noteBox.append(el("p", { text: note }));
  viewer.append(noteBox);

  const tools = el("div", { class: "viewer-tools" });
  tools.append(el("button", { type: "button", class: "viewer-btn", "data-zoom": "out", text: "−", "aria-label": "Уменьшить" }));
  tools.append(el("button", { type: "button", class: "viewer-btn", "data-zoom": "in", text: "+", "aria-label": "Увеличить" }));
  if (note) {
    tools.append(el("button", { type: "button", class: "viewer-btn viewer-note-btn", "data-zoom": "note", text: "Описание" }));
  }
  frame.append(viewer, tools);
  overlay.append(frame);
  document.body.append(overlay);

  const view = { s: 1, x: 0, y: 0, drag: null };

  function frameSize() {
    return viewer.getBoundingClientRect();
  }

  function layoutBase() {
    const nw = img.naturalWidth;
    const nh = img.naturalHeight;
    if (!nw || !nh) return;
    const maxW = window.innerWidth * 0.76;
    const maxH = window.innerHeight * 0.76;
    const contain = Math.min(maxW / nw, maxH / nh);
    const w = Math.max(40, Math.round(nw * contain));
    const h = Math.max(40, Math.round(nh * contain));
    frame.style.width = w + "px";
    frame.style.height = h + "px";
    img.style.width = w + "px";
    img.style.height = h + "px";
  }

  function clamp() {
    const rect = frameSize();
    const w = img.offsetWidth * view.s;
    const h = img.offsetHeight * view.s;
    const limitX = Math.max(0, (w - rect.width) / 2);
    const limitY = Math.max(0, (h - rect.height) / 2);
    view.x = Math.min(limitX, Math.max(-limitX, view.x));
    view.y = Math.min(limitY, Math.max(-limitY, view.y));
  }

  function apply() {
    clamp();
    img.style.transform = "translate(calc(-50% + " + view.x + "px), calc(-50% + " + view.y + "px)) scale(" + view.s + ")";
    viewer.classList.toggle("zoomed", view.s > 1.02);
    viewer.classList.toggle("panning", !!(view.drag && view.drag.moved && view.s > 1.02));
  }

  function zoomAt(px, py, next) {
    const s2 = Math.min(6, Math.max(1, next));
    if (s2 === 1) {
      view.s = 1;
      view.x = 0;
      view.y = 0;
    } else {
      view.x = px - (px - view.x) * (s2 / view.s);
      view.y = py - (py - view.y) * (s2 / view.s);
      view.s = s2;
    }
    apply();
  }

  function pointerCenter(event) {
    const rect = frameSize();
    return {
      x: event.clientX - rect.left - rect.width / 2,
      y: event.clientY - rect.top - rect.height / 2,
    };
  }

  img.addEventListener("load", () => {
    layoutBase();
    apply();
  });
  if (img.complete) {
    layoutBase();
    apply();
  }

  viewer.addEventListener("wheel", (event) => {
    event.preventDefault();
    const point = pointerCenter(event);
    const factor = event.deltaY < 0 ? 1.12 : 1 / 1.12;
    zoomAt(point.x, point.y, view.s * factor);
  }, { passive: false });

  viewer.addEventListener("dblclick", (event) => {
    event.preventDefault();
    const point = pointerCenter(event);
    if (view.s >= 5.5) zoomAt(point.x, point.y, 1);
    else zoomAt(point.x, point.y, view.s * 2);
  });

  viewer.addEventListener("pointerdown", (event) => {
    if (event.target.closest("button")) return;
    view.drag = {
      x: event.clientX,
      y: event.clientY,
      ox: view.x,
      oy: view.y,
      moved: false,
    };
    try { viewer.setPointerCapture(event.pointerId); } catch (error) { /* pointer already gone */ }
    apply();
  });
  viewer.addEventListener("pointermove", (event) => {
    if (!view.drag) return;
    const dx = event.clientX - view.drag.x;
    const dy = event.clientY - view.drag.y;
    if (Math.hypot(dx, dy) > 3) view.drag.moved = true;
    if (view.s <= 1.02) return;
    view.x = view.drag.ox + dx;
    view.y = view.drag.oy + dy;
    apply();
  });
  function endDrag() {
    if (!view.drag) return;
    view.drag = null;
    apply();
  }
  viewer.addEventListener("pointerup", endDrag);
  viewer.addEventListener("pointercancel", endDrag);

  tools.addEventListener("click", (event) => {
    const button = event.target.closest("[data-zoom]");
    if (!button) return;
    event.stopPropagation();
    const kind = button.dataset.zoom;
    if (kind === "note") {
      const open = noteBox.hasAttribute("hidden");
      if (open) noteBox.removeAttribute("hidden");
      else noteBox.setAttribute("hidden", "hidden");
      button.classList.toggle("is-on", open);
      return;
    }
    if (kind === "in") zoomAt(0, 0, view.s * 1.25);
    else zoomAt(0, 0, view.s / 1.25);
  });

  frame.addEventListener("click", (event) => event.stopPropagation());
  window.addEventListener("resize", () => {
    view.s = 1;
    view.x = 0;
    view.y = 0;
    layoutBase();
    apply();
  });
  overlay._viewerCleanup = function () {
    window.removeEventListener("resize", layoutBase);
  };
};

Atlas.closeLightbox = function () {
  const node = document.querySelector(".lightbox");
  if (!node) return;
  if (node._viewerCleanup) node._viewerCleanup();
  node.remove();
};
