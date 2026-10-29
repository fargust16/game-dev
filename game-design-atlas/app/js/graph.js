Atlas.mountGraph = function (host) {
  host.innerHTML = "";
  const tools = el("div", { class: "stage-tools" }, [
    el("button", { type: "button", text: "−", "data-action": "zoom-out" }),
    el("button", { type: "button", id: "zoom-label", text: "100%" }),
    el("button", { type: "button", text: "+", "data-action": "zoom-in" }),
    el("button", { type: "button", text: "Вместить", "data-action": "zoom-fit" }),
  ]);
  const svg = document.createElementNS("http://www.w3.org/2000/svg", "svg");
  svg.setAttribute("role", "img");
  svg.setAttribute("aria-label", "Карта игровых систем");
  const world = document.createElementNS("http://www.w3.org/2000/svg", "g");
  svg.append(world);
  const legend = el("div", { class: "legend" });
  Object.keys(Atlas.labels.relType).forEach((type) => {
    legend.append(el("span", {}, [
      el("i", { class: "swatch", style: "background:" + Atlas.labels.relColor[type] }),
      Atlas.labels.relType[type],
    ]));
  });
  legend.append(el("span", {}, [el("i", { class: "swatch dashed" }), "Выведено или не разрешено"]));
  host.append(tools, svg, legend);

  const ns = "http://www.w3.org/2000/svg";
  const categoryColor = {
    foundation: "#e4c48a",
    world: "#7dcaa6",
    exploration: "#79b7d6",
    choice: "#d7a15a",
    combat: "#e07a5f",
    character: "#c4a2de",
    society: "#d6c36a",
    scope: "#9aafc4",
    proposed: "#b7a89a",
  };
  const systems = Atlas.data.systems;
  const edges = Atlas.data.relationships;
  const selected = Atlas.selectedId;
  const neighbors = new Set();
  if (selected) {
    neighbors.add(selected);
    edges.forEach((edge) => {
      if (edge.fromId === selected || edge.toId === selected) {
        neighbors.add(edge.fromId);
        neighbors.add(edge.toId);
      }
    });
  }

  function nodePos(system) {
    return { x: system.x, y: system.y };
  }

  const pairCount = {};
  edges.forEach((edge) => {
    const key = [edge.fromId, edge.toId].sort().join("|");
    pairCount[key] = (pairCount[key] || 0) + 1;
  });
  const pairIndex = {};

  edges.forEach((edge) => {
    const from = Atlas.system(edge.fromId);
    const to = Atlas.system(edge.toId);
    const a = nodePos(from);
    const b = nodePos(to);
    const key = [edge.fromId, edge.toId].sort().join("|");
    const index = pairIndex[key] || 0;
    pairIndex[key] = index + 1;
    const count = pairCount[key];
    const mx = (a.x + b.x) / 2;
    const my = (a.y + b.y) / 2;
    const dx = b.x - a.x;
    const dy = b.y - a.y;
    const len = Math.hypot(dx, dy) || 1;
    const bend = (index - (count - 1) / 2) * 42;
    const cx = mx + (-dy / len) * bend;
    const cy = my + (dx / len) * bend;
    const path = "M " + a.x + " " + a.y + " Q " + cx + " " + cy + " " + b.x + " " + b.y;
    const visible = Atlas.edgeVisible(edge);
    const focused = !selected || edge.fromId === selected || edge.toId === selected;
    const hit = document.createElementNS(ns, "path");
    hit.setAttribute("d", path);
    hit.setAttribute("class", "edge-hit");
    hit.dataset.edge = edge.id;
    const line = document.createElementNS(ns, "path");
    line.setAttribute("d", path);
    line.setAttribute("class", "edge");
    line.setAttribute("stroke", Atlas.labels.relColor[edge.type]);
    if (edge.evidence === "inferred") line.setAttribute("stroke-dasharray", "6 4");
    if (edge.evidence === "unresolved") line.setAttribute("stroke-dasharray", "2 3");
    if (edge.evidence === "conflicting") line.setAttribute("stroke-dasharray", "5 2 1 2");
    let opacity = 0.85;
    if (!visible) opacity = 0.05;
    else if (selected && !focused) opacity = 0.12;
    else if (Atlas.selectedEdgeId === edge.id) opacity = 1;
    line.setAttribute("opacity", String(opacity));
    if (Atlas.selectedEdgeId === edge.id) line.setAttribute("stroke-width", "3");
    line.style.pointerEvents = "none";
    world.append(line, hit);
  });

  systems.forEach((system) => {
    const visible = Atlas.systemVisible(system);
    const focused = !selected || neighbors.has(system.id);
    const group = document.createElementNS(ns, "g");
    group.dataset.system = system.id;
    group.setAttribute("transform", "translate(" + system.x + " " + system.y + ")");
    let opacity = 1;
    if (!visible) opacity = 0.08;
    else if (selected && !focused) opacity = 0.18;
    group.setAttribute("opacity", String(opacity));
    const circle = document.createElementNS(ns, "circle");
    circle.setAttribute("r", system.id === selected ? "26" : "22");
    circle.setAttribute("fill", "#101820");
    circle.setAttribute("stroke", categoryColor[system.category] || "#e4c48a");
    circle.setAttribute("stroke-width", system.id === selected ? "3.5" : "2");
    if (system.proposed) circle.setAttribute("stroke-dasharray", "4 3");
    const label = document.createElementNS(ns, "text");
    label.setAttribute("class", "node-label");
    label.setAttribute("text-anchor", "middle");
    label.setAttribute("y", "40");
    label.textContent = system.name;
    group.append(circle, label);
    group.style.cursor = "pointer";
    world.append(group);
  });

  function applyTransform() {
    const g = Atlas.graph;
    world.setAttribute("transform", "translate(" + g.x + " " + g.y + ") scale(" + g.k + ")");
    const label = document.getElementById("zoom-label");
    const base = g.baseK || g.k || 1;
    if (label) label.textContent = Math.round(g.k / base * 100) + "%";
  }

  function fitView() {
    const width = svg.clientWidth;
    const height = svg.clientHeight;
    if (width < 80 || height < 80) return;
    const box = world.getBBox();
    if (!box.width || !box.height) return;
    const padLeft = 48;
    const padRight = 48;
    const padTop = 72;
    const padBottom = 120;
    const innerW = Math.max(40, width - padLeft - padRight);
    const innerH = Math.max(40, height - padTop - padBottom);
    const k = Math.min(innerW / box.width, innerH / box.height);
    const spareX = innerW - box.width * k;
    const spareY = innerH - box.height * k;
    Atlas.graph.baseK = k;
    Atlas.graph.k = k;
    Atlas.graph.x = padLeft + spareX / 2 - box.x * k;
    Atlas.graph.y = padTop + spareY / 2 - box.y * k;
    applyTransform();
  }

  if (Atlas.graph.home) fitView();
  else applyTransform();

  if (typeof ResizeObserver !== "undefined") {
    const observer = new ResizeObserver(() => {
      if (Atlas.graph.home) fitView();
    });
    observer.observe(svg);
  }

  let drag = null;
  svg.addEventListener("pointerdown", (event) => {
    const systemNode = event.target.closest("[data-system]");
    const edgeNode = event.target.closest("[data-edge]");
    drag = {
      x: event.clientX,
      y: event.clientY,
      moved: false,
      system: systemNode ? systemNode.dataset.system : null,
      edge: edgeNode ? edgeNode.dataset.edge : null,
    };
    svg.setPointerCapture(event.pointerId);
    svg.classList.add("dragging");
  });
  svg.addEventListener("pointermove", (event) => {
    if (!drag) return;
    const dx = event.clientX - drag.x;
    const dy = event.clientY - drag.y;
    if (Math.hypot(dx, dy) > 4) drag.moved = true;
    if (drag.moved && !drag.system && !drag.edge) {
      Atlas.graph.home = false;
      Atlas.graph.x += dx;
      Atlas.graph.y += dy;
      drag.x = event.clientX;
      drag.y = event.clientY;
      applyTransform();
    }
  });
  svg.addEventListener("pointerup", () => {
    if (!drag) return;
    svg.classList.remove("dragging");
    if (!drag.moved && drag.system) {
      Atlas.selectedId = drag.system;
      Atlas.selectedEdgeId = null;
      Atlas.render();
    } else if (!drag.moved && drag.edge) {
      Atlas.selectedEdgeId = drag.edge;
      Atlas.selectedId = null;
      Atlas.render();
    } else if (!drag.moved) {
      Atlas.selectedId = null;
      Atlas.selectedEdgeId = null;
      Atlas.render();
    }
    drag = null;
  });
  svg.addEventListener("wheel", (event) => {
    event.preventDefault();
    const rect = svg.getBoundingClientRect();
    const px = event.clientX - rect.left;
    const py = event.clientY - rect.top;
    Atlas.graph.home = false;
    const base = Atlas.graph.baseK || Atlas.graph.k || 1;
    const next = event.deltaY < 0 ? Atlas.graph.k * 1.08 : Atlas.graph.k / 1.08;
    const k = Math.min(base * 4, Math.max(base * 0.75, next));
    const worldX = (px - Atlas.graph.x) / Atlas.graph.k;
    const worldY = (py - Atlas.graph.y) / Atlas.graph.k;
    Atlas.graph.k = k;
    Atlas.graph.x = px - worldX * k;
    Atlas.graph.y = py - worldY * k;
    applyTransform();
  }, { passive: false });
};


Atlas.zoomGraph = function (factor) {
  const base = Atlas.graph.baseK || Atlas.graph.k || 1;
  const next = Math.min(base * 4, Math.max(base * 0.75, Atlas.graph.k * factor));
  const svg = document.querySelector(".stage svg");
  if (svg && Atlas.graph.k) {
    const rect = svg.getBoundingClientRect();
    const px = rect.width / 2;
    const py = rect.height / 2;
    const worldX = (px - Atlas.graph.x) / Atlas.graph.k;
    const worldY = (py - Atlas.graph.y) / Atlas.graph.k;
    Atlas.graph.x = px - worldX * next;
    Atlas.graph.y = py - worldY * next;
  }
  Atlas.graph.k = next;
  Atlas.graph.home = false;
  Atlas.render();
};


function el(tag, attrs, children) {
  const node = document.createElement(tag);
  if (attrs) {
    Object.keys(attrs).forEach((key) => {
      const value = attrs[key];
      if (value == null || value === false) return;
      if (key === "class") node.className = value;
      else if (key === "text") node.textContent = value;
      else node.setAttribute(key, value);
    });
  }
  const list = children == null ? [] : [].concat(children);
  list.forEach((child) => {
    if (child == null || child === false) return;
    node.append(child.nodeType ? child : document.createTextNode(String(child)));
  });
  return node;
}
