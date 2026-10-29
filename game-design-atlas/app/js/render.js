/* Shell: sidebar, map detail, and which view to draw. */

function chip(text, kind) {
  return el("span", { class: "chip" + (kind ? " " + kind : ""), text: text });
}

function sourceLinks(ids) {
  const wrap = el("span");
  (ids || []).forEach((id, index) => {
    const source = Atlas.source(id);
    if (!source) return;
    if (index) wrap.append(document.createTextNode(" · "));
    const href = Atlas.githubHref(source, source.anchor);
    wrap.append(el("a", { href: href, target: "_blank", rel: "noreferrer", text: source.title }));
  });
  return wrap;
}

function maturityChip(system) {
  const label = Atlas.labels.maturity[system.designMaturity];
  const kind = system.designMaturity === "specification-documented" ? "ok" : "warn";
  return chip(label, kind);
}

Atlas.renderSidebar = function () {
  const host = document.getElementById("sidebar");
  host.innerHTML = "";
  const data = Atlas.data;
  const openQuestions = data.questions.filter((item) => item.status === "open").length;
  host.append(el("p", { class: "side-title", text: "Обзор проекта" }));
  const stats = el("div", { class: "stats" }, [
    el("div", { class: "stat" }, [el("b", { text: String(data.systems.length) }), el("span", { text: "Систем" })]),
    el("div", { class: "stat" }, [el("b", { text: String(data.relationships.length) }), el("span", { text: "Связей" })]),
    el("div", { class: "stat" }, [el("b", { text: String(data.sources.length) }), el("span", { text: "Документов" })]),
    el("div", { class: "stat" }, [el("b", { text: String(openQuestions) }), el("span", { text: "Открытых вопросов" })]),
  ]);
  host.append(stats);
  host.append(el("button", { class: "linkish", type: "button", text: "Сбросить фильтры", "data-action": "reset-filters" }));

  if (Atlas.route.view === "questions") {
    host.append(questionFilterForm());
    return;
  }

  host.append(filterGroup("Категории", "category", Atlas.labels.category, data.systems, "category"));
  host.append(filterGroup("Полнота дизайна", "maturity", Atlas.labels.maturity, data.systems, "designMaturity"));
  host.append(filterGroup("Состояние решений", "decision", Atlas.labels.decision, data.systems, "decisionState"));
  host.append(filterGroup("Реализация", "implementation", Atlas.labels.implementation, data.systems, "implementationMaturity"));
  host.append(filterGroup("Тип связи", "relType", Atlas.labels.relType, data.relationships, "type"));
  host.append(filterGroup("Статус связи", "evidence", Atlas.labels.evidence, data.relationships, "evidence"));
  host.append(el("p", { class: "meta", text: "Реализация в этой версии не выводится из документов. Почти везде стоит «неизвестно»." }));
};

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

function filterGroup(title, key, labels, records, field) {
  const block = el("div", { class: "filter-block" }, [el("h3", { text: title })]);
  Object.keys(labels).forEach((id) => {
    const count = records.filter((record) => record[field] === id).length;
    if (!count) return;
    const checked = Atlas.filters[key].has(id);
    const box = el("input", { type: "checkbox" });
    box.checked = checked;
    box.dataset.filter = key;
    box.dataset.value = id;
    const row = el("label", { class: "filter-row" }, [
      box,
      el("i", { class: "dot", style: "background:" + (Atlas.labels.relColor[id] || categoryColor[id] || "#8aa0b5") }),
      el("span", { text: labels[id] }),
      el("span", { class: "count", text: String(count) }),
    ]);
    block.append(row);
  });
  return block;
}

function questionFilterForm() {
  const block = el("div", { class: "filter-block" }, [el("h3", { text: "Фильтр вопросов" })]);
  block.append(selectFilter("Статус", "status", Atlas.labels.question, true));
  block.append(selectFilter("Приоритет", "priority", { high: "Высокий", medium: "Средний" }, true));
  block.append(selectFilter("Тип источника", "sourceKind", Atlas.labels.sourceKind, true));
  const systemSelect = el("select", { "data-qfilter": "system" }, [el("option", { value: "", text: "Все системы" })]);
  Atlas.data.systems.forEach((system) => {
    const option = el("option", { value: system.id, text: system.name });
    if (Atlas.questionFilters.system === system.id) option.selected = true;
    systemSelect.append(option);
  });
  block.append(el("label", { text: "Система" }), systemSelect);
  return block;
}

function selectFilter(label, key, options, includeAll) {
  const select = el("select", { "data-qfilter": key });
  if (includeAll) select.append(el("option", { value: "", text: "Все" }));
  Object.keys(options).forEach((id) => {
    const option = el("option", { value: id, text: options[id] });
    if (Atlas.questionFilters[key] === id) option.selected = true;
    select.append(option);
  });
  return el("div", {}, [el("label", { text: label }), select]);
}

Atlas.renderDetail = function () {
  const host = document.getElementById("detail");
  host.innerHTML = "";
  if (Atlas.route.view !== "map" && Atlas.route.view !== "list") return;
  if (Atlas.selectedEdgeId) {
    const edge = Atlas.byId(Atlas.data.relationships, Atlas.selectedEdgeId);
    if (!edge) return;
    host.append(el("p", { class: "panel-kicker", text: "Связь" }));
    host.append(el("h2", {}, [
      el("a", { href: "#/system/" + edge.fromId, text: Atlas.system(edge.fromId).name }),
      " → ",
      el("a", { href: "#/system/" + edge.toId, text: Atlas.system(edge.toId).name }),
    ]));
    host.append(chip(Atlas.labels.relType[edge.type], "gold"));
    host.append(document.createTextNode(" "));
    host.append(chip(Atlas.labels.evidence[edge.evidence], edge.evidence === "confirmed" ? "ok" : "warn"));
    host.append(el("p", { text: edge.explanation }));
    host.append(el("p", { class: "meta", text: "Источники" }));
    host.append(sourceLinks(edge.sourceIds));
    return;
  }
  const system = Atlas.selectedId ? Atlas.system(Atlas.selectedId) : null;
  if (!system) {
    host.append(el("p", { class: "panel-kicker", text: "Карта" }));
    host.append(el("h2", { text: "Выберите систему или связь" }));
    host.append(el("p", { class: "meta", text: "Колесо меняет масштаб, перетаскивание фона двигает карту. Пунктир на связи значит, что она выведена или ещё не разрешена. Пунктир на узле значит, что системы в каноне нет." }));
    return;
  }
  host.append(el("p", { class: "panel-kicker", text: Atlas.labels.category[system.category] }));
  host.append(el("h2", { text: system.name }));
  host.append(el("p", { class: "meta", text: system.title }));
  const chips = el("div", { class: "chips" }, [
    maturityChip(system),
    chip(Atlas.labels.decision[system.decisionState], system.decisionState === "accepted" ? "ok" : "warn"),
    chip("Реализация: " + Atlas.labels.implementation[system.implementationMaturity]),
  ]);
  if (system.proposed) chips.append(chip("Предложено, не утверждено", "bad"));
  host.append(chips);
  host.append(el("p", { text: system.summary }));
  const score = Atlas.auditScore(system.id);
  host.append(el("p", { class: "meta", text: "Аудит: покрыто " + score.covered + " из " + score.applicable + " применимых. Частично и «не оценено» в числитель не входят." }));
  host.append(el("p", {}, [el("a", { href: "#/system/" + system.id, text: "Открыть страницу системы" })]));
  const edges = Atlas.edgesFor(system.id).filter(Atlas.edgeVisible);
  if (edges.length) {
    host.append(el("h3", { text: "Связи" }));
    edges.forEach((edge) => {
      const other = edge.fromId === system.id ? edge.toId : edge.fromId;
      host.append(el("p", {}, [
        el("a", { href: "#/system/" + other, text: Atlas.system(other).name }),
        document.createTextNode(" · " + Atlas.labels.relType[edge.type] + " · " + Atlas.labels.evidence[edge.evidence]),
      ]));
    });
  }
};

Atlas.renderMap = function (main) {
  const stage = el("div", { class: "stage" });
  main.append(stage);
  Atlas.mountGraph(stage);
};


Atlas.render = function () {
  const view = Atlas.route.view;
  document.querySelectorAll(".nav a").forEach((link) => {
    link.classList.toggle("active", link.dataset.view === view || (view === "system" && link.dataset.view === "list"));
  });
  const workspace = document.querySelector(".workspace");
  const showDetail = view === "map" || view === "list";
  workspace.classList.toggle("no-detail", !showDetail);
  workspace.classList.toggle("reading-view", view === "system");
  Atlas.renderSidebar();
  Atlas.renderDetail();
  const main = document.getElementById("main");
  main.innerHTML = "";
  const views = {
    map: Atlas.renderMap,
    list: Atlas.renderList,
    system: Atlas.renderSystem,
    questions: Atlas.renderQuestions,
    sources: Atlas.renderSources,
    references: Atlas.renderReferences,
    decisions: Atlas.renderDecisions,
    audit: Atlas.renderAudit,
  };
  (views[view] || Atlas.renderMap)(main);
  const banner = document.getElementById("banner");
  banner.textContent = Atlas.message || "Название The Unseen Order в канонических документах ещё не записано. Здесь это имя рабочей среды, не цитата из канона.";
  banner.classList.toggle("error", Atlas.messageError);
};
