/* List, questions, documents, decisions, and the audit overview. */

Atlas.renderList = function (main) {
  const wrap = el("div", { class: "table-wrap" });
  const table = el("table");
  table.append(el("thead", {}, [el("tr", {}, [
    el("th", { text: "Система" }),
    el("th", { text: "Категория" }),
    el("th", { text: "Дизайн" }),
    el("th", { text: "Решения" }),
    el("th", { text: "Аудит" }),
  ])]));
  const body = el("tbody");
  Atlas.data.systems.filter(Atlas.systemVisible).forEach((system) => {
    const score = Atlas.auditScore(system.id);
    const row = el("tr", { class: "clickable", "data-href": "#/system/" + system.id }, [
      el("td", {}, [el("strong", { text: system.name }), el("div", { class: "meta", text: system.title })]),
      el("td", { text: Atlas.labels.category[system.category] }),
      el("td", { text: Atlas.labels.maturity[system.designMaturity] }),
      el("td", { text: Atlas.labels.decision[system.decisionState] }),
      el("td", { text: score.covered + " / " + score.applicable }),
    ]);
    body.append(row);
  });
  table.append(body);
  wrap.append(table);
  main.append(wrap);
};

Atlas.renderQuestions = function (main) {
  const filters = Atlas.questionFilters;
  const page = el("div", { class: "page" });
  page.append(el("h1", { text: "Открытые вопросы" }));
  page.append(el("p", { class: "lede", text: "Текст вопросов взят из 99 Open Questions. Вопросы рельефа помечены как техническое исследование. Их можно отфильтровать по типу источника, и они не считаются дырой правил игры." }));
  const list = el("div", { class: "card-list" });
  Atlas.data.questions.filter((question) => {
    if (filters.status && question.status !== filters.status) return false;
    if (filters.priority && question.priority !== filters.priority) return false;
    if (filters.sourceKind && question.sourceKind !== filters.sourceKind) return false;
    if (filters.system && question.systemIds.indexOf(filters.system) === -1) return false;
    return true;
  }).forEach((question) => {
    const systems = question.systemIds.map((id) => el("a", { href: "#/system/" + id, text: Atlas.system(id).name }));
    const line = el("p", { class: "meta" });
    systems.forEach((link, index) => {
      if (index) line.append(document.createTextNode(", "));
      line.append(link);
    });
    if (question.sourceId) {
      line.append(document.createTextNode(" · "));
      line.append(sourceLinks([question.sourceId]));
    }
    const card = questionCard(question);
    card.append(line);
    list.append(card);
  });
  page.append(list);
  main.append(page);
};

Atlas.renderSources = function (main) {
  const page = el("div", { class: "page" });
  page.append(el("h1", { text: "Документы" }));
  page.append(el("p", { class: "lede", text: "Атлас не копирует канон. Ссылки открывают файлы в репозитории. Локальный сервер сами Markdown не публикует." }));
  const statuses = ["Canonical", "Draft", "Deprecated", "No status field"];
  Atlas.data.sources.forEach((source) => {
    if (statuses.indexOf(source.documentStatus) === -1) statuses.push(source.documentStatus);
  });
  statuses.forEach((status) => {
    const group = Atlas.data.sources.filter((source) => source.documentStatus === status);
    if (!group.length) return;
    page.append(el("h2", { text: status }));
    const list = el("ul", { class: "clean" });
    group.forEach((source) => {
      list.append(el("li", {}, [
        el("a", { href: Atlas.githubHref(source, source.anchor), target: "_blank", rel: "noreferrer", text: source.title }),
        el("div", { class: "meta", text: source.path + (source.retired ? " — не использовать как источник дизайна" : "") }),
        source.note ? el("div", { text: source.note }) : null,
      ]));
    });
    page.append(list);
  });
  main.append(page);
};

Atlas.renderDecisions = function (main) {
  const page = el("div", { class: "page" });
  page.append(el("h1", { text: "Журнал решений" }));
  page.append(el("p", { class: "lede", text: "Импортированные записи повторяют то, что уже написано в документах. Новые записи этой рабочей среды помечены отдельно и не подменяют канон, пока их отдельно не перенесут в Markdown." }));
  const list = el("div", { class: "card-list" });
  Atlas.data.decisions.forEach((decision) => {
    const card = el("article", { class: "card" }, [
      el("p", {}, [el("strong", { text: decision.statement })]),
      chip(Atlas.labels.decision[decision.status], decision.status === "accepted" ? "ok" : "warn"),
      decision.origin === "new" ? chip("Новое, записано в атласе", "gold") : chip("Импорт из документов"),
    ]);
    if (decision.rationale) card.append(el("p", { text: decision.rationale }));
    if (decision.alternatives) card.append(el("p", { class: "meta", text: "Рассматривалось: " + decision.alternatives }));
    const meta = el("p", { class: "meta" });
    if (decision.date) meta.append(document.createTextNode(decision.date + " · "));
    meta.append(document.createTextNode(decision.dateNote || ""));
    card.append(meta);
    const links = el("p");
    decision.systemIds.forEach((id, index) => {
      if (index) links.append(document.createTextNode(", "));
      links.append(el("a", { href: "#/system/" + id, text: Atlas.system(id).name }));
    });
    card.append(links, sourceLinks(decision.sourceIds));
    list.append(card);
  });
  page.append(list);
  main.append(page);
};

Atlas.renderAudit = function (main) {
  const page = el("div", { class: "page" });
  page.append(el("h1", { text: "Аудит полноты" }));
  page.append(el("p", { class: "lede", text: "У каждой категории свой профиль. Общего процента игры нет: закрытый Core Loop не закрывает алхимию, а найденный абзац не значит, что правила дописаны." }));
  const groups = [
    { title: "Ещё не проверено до конца", test: (score) => score.unassessed > 0 },
    { title: "Есть явные дыры", test: (score) => score.missing > 0 },
    { title: "Есть незакрытые решения дизайна", test: (system) => system.decisionState === "unresolved" || system.decisionState === "proposed" },
    { title: "По своему профилю критерии покрыты", test: (score) => score.applicable > 0 && score.covered === score.applicable },
  ];
  groups.forEach((group) => {
    const rows = Atlas.data.systems.filter((system) => {
      const score = Atlas.auditScore(system.id);
      if (group.title.indexOf("решени") !== -1) return group.test(system);
      return group.test(score);
    });
    page.append(el("h2", { text: group.title }));
    if (!rows.length) {
      page.append(el("p", { class: "meta", text: "Сейчас пусто." }));
      return;
    }
    const list = el("ul", { class: "clean" });
    rows.forEach((system) => {
      const score = Atlas.auditScore(system.id);
      list.append(el("li", {}, [
        el("a", { href: "#/system/" + system.id, text: system.name }),
        " — покрыто " + score.covered + " из " + score.applicable + ", частично " + score.partial + ", нет " + score.missing + ", не оценено " + score.unassessed,
      ]));
    });
    page.append(list);
  });
  main.append(page);
};

