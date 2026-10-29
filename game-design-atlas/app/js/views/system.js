/* System article page. The words for one system live in data/articles/<id>.json. */

Atlas.renderSystem = function (main) {
  const system = Atlas.system(Atlas.route.id);
  const page = el("article", { class: "page reading" });
  if (!system) {
    page.append(el("h1", { text: "Система не найдена" }));
    main.append(page);
    return;
  }
  const edges = Atlas.edgesFor(system.id);

  page.append(el("p", { class: "crumbs" }, [
    el("a", { href: "#/map", text: "Карта" }),
    " · ",
    el("a", { href: "#/list", text: "Список" }),
  ]));
  page.append(el("p", { class: "panel-kicker", text: Atlas.labels.category[system.category] }));
  page.append(el("h1", { text: system.name }));
  page.append(el("p", { class: "lede", text: system.title }));

  const chips = el("div", { class: "chips" }, [
    maturityChip(system),
    chip("Решения: " + Atlas.labels.decision[system.decisionState], system.decisionState === "accepted" ? "ok" : "warn"),
    chip("Реализация: " + Atlas.labels.implementation[system.implementationMaturity]),
  ]);
  if (system.documentStatus) chips.append(chip("Документ: " + system.documentStatus));
  if (system.proposed) chips.append(chip("Предложенный пробел", "bad"));
  page.append(chips);
  page.append(el("p", { class: "quiet", text: "Текст ниже — аннотация атласа, не цитата канона. Утверждения ведут к документам, связи — к другим системам." }));

  const toc = el("nav", { class: "toc", "aria-label": "Разделы страницы" });
  [
    ["sec-about", "О системе"],
    ["sec-recorded", "Что записано"],
    ["sec-gaps", "Пробелы"],
    ["sec-scenario", "Сценарий"],
    ["sec-links", "Связи"],
    ["sec-questions", "Вопросы"],
    ["sec-decisions", "Решения"],
    ["sec-audit", "Аудит"],
  ].forEach((item) => {
    toc.append(el("button", {
      type: "button",
      class: "toc-link",
      "data-action": "scroll-to",
      "data-target": item[0],
      text: item[1],
    }));
  });
  page.append(toc);

  if (edges.length) {
    const jumps = el("nav", { class: "jumps", "aria-label": "Переходы к связанным системам" });
    edges.forEach((edge) => jumps.append(jumpLink(edge, system)));
    page.append(jumps);
  }

  page.append(section("О системе", el("div", { class: "prose" }, [
    el("p", { text: system.summary }),
  ]), "sec-about"));

  const points = el("div", { class: "prose" });
  system.points.forEach((point) => {
    const paragraph = el("p");
    paragraph.append(document.createTextNode(point.text + " "));
    paragraph.append(sourceLinks([point.sourceId]));
    points.append(paragraph);
  });
  page.append(section(
    "Что записано в документах",
    points.childNodes.length ? points : el("p", { class: "meta", text: "Отдельных утверждений с источником нет." }),
    "sec-recorded"
  ));

  const gaps = el("ul", { class: "prose-list" });
  (system.gaps || []).forEach((gap) => gaps.append(el("li", { text: gap })));
  page.append(section(
    "Чего в источниках нет",
    gaps.childNodes.length ? gaps : el("p", { class: "meta", text: "Отдельный список пробелов не записан." }),
    "sec-gaps"
  ));

  page.append(section(
    "Как это проживает игрок",
    el("div", { class: "prose" }, [
      el("p", { text: system.scenarioNote || "Отдельный игровой сценарий в импорт не входил." }),
    ]),
    "sec-scenario"
  ));

  const rels = el("div");
  edges.forEach((edge) => rels.append(relationshipArticle(edge, system)));
  page.append(section(
    "Связи и переходы",
    edges.length ? rels : el("p", { class: "meta", text: "Связей нет. Пустая связь не добавлялась ради картинки." }),
    "sec-links"
  ));

  const questions = Atlas.questionsFor(system.id);
  const qList = el("div", { class: "card-list" });
  questions.forEach((question) => qList.append(questionArticle(question, system.id)));
  page.append(section(
    "Открытые вопросы",
    questions.length ? qList : el("p", { class: "meta", text: "В импортированном списке вопросов этой системы нет. Это не доказательство полноты." }),
    "sec-questions"
  ));

  const decisions = Atlas.decisionsFor(system.id);
  const dList = el("div");
  decisions.forEach((decision) => dList.append(decisionArticle(decision, system.id)));
  page.append(section(
    "Решения",
    decisions.length ? dList : el("p", { class: "meta", text: "Отдельной записи решения для этой системы нет." }),
    "sec-decisions"
  ));

  const refs = Atlas.refsFor(system.id);
  if (refs.length) {
    const refBox = el("div", { class: "gallery gallery-inline" });
    refs.forEach((ref) => refBox.append(Atlas.referenceCard(ref)));
    page.append(section("Референсы", refBox, "sec-refs"));
  }

  page.append(section("Аудит полноты", auditBlock(system), "sec-audit"));
  page.append(section("Следующий разговор с агентом", nextTaskBlock(system), "sec-next"));

  const src = el("ul", { class: "prose-list" });
  system.sourceIds.forEach((id) => {
    const source = Atlas.source(id);
    src.append(el("li", {}, [
      sourceLinks([id]),
      source ? el("div", { class: "meta", text: source.path }) : null,
    ]));
  });
  page.append(section("Документы", src, "sec-sources"));
  main.append(page);
};

function jumpLink(edge, system) {
  const otherId = edge.fromId === system.id ? edge.toId : edge.fromId;
  const other = Atlas.system(otherId);
  const outward = edge.fromId === system.id;
  const uncertain = edge.evidence !== "confirmed";
  return el("a", { class: "jump" + (uncertain ? " dashed" : ""), href: "#/system/" + otherId }, [
    el("span", { class: "jump-kicker", text: (outward ? "ведёт к" : "ведёт сюда") + " · " + Atlas.labels.relType[edge.type] }),
    el("strong", { text: other ? other.name : otherId }),
    el("span", { class: "jump-note", text: Atlas.labels.evidence[edge.evidence] }),
  ]);
}

function relationshipArticle(edge, system) {
  const otherId = edge.fromId === system.id ? edge.toId : edge.fromId;
  const other = Atlas.system(otherId);
  const from = Atlas.system(edge.fromId);
  const to = Atlas.system(edge.toId);
  const article = el("article", { class: "rel" });
  article.append(el("p", { class: "arrow", text: (from ? from.name : edge.fromId) + " → " + (to ? to.name : edge.toId) }));
  article.append(el("h3", {}, [
    el("a", { href: "#/system/" + otherId, text: other ? other.name : otherId }),
  ]));
  article.append(el("p", { class: "meta", text: Atlas.labels.relType[edge.type] + " · " + Atlas.labels.evidence[edge.evidence] }));
  article.append(el("div", { class: "prose" }, [el("p", { text: edge.explanation })]));
  if (edge.sourceIds && edge.sourceIds.length) article.append(sourceLinks(edge.sourceIds));
  return article;
}

function questionArticle(question, currentId) {
  const card = questionCard(question);
  const line = el("p", { class: "links" });
  question.systemIds.forEach((id) => {
    if (id === currentId) return;
    const other = Atlas.system(id);
    if (!other) return;
    if (line.childNodes.length) line.append(document.createTextNode(", "));
    line.append(el("a", { href: "#/system/" + id, text: other.name }));
  });
  if (question.sourceId) {
    if (line.childNodes.length) line.append(document.createTextNode(" · "));
    line.append(sourceLinks([question.sourceId]));
  }
  if (line.childNodes.length) card.append(line);
  return card;
}

function decisionArticle(decision, currentId) {
  const article = el("article", { class: "rel" });
  article.append(el("div", { class: "prose" }, [el("p", { text: decision.statement })]));
  if (decision.rationale) article.append(el("p", { class: "rationale", text: decision.rationale }));
  const chips = el("div", { class: "chips" }, [
    chip(Atlas.labels.decision[decision.status], decision.status === "accepted" ? "ok" : "warn"),
  ]);
  if (decision.origin === "new") chips.append(chip("Новое, записано в атласе", "gold"));
  article.append(chips);
  if (decision.date || decision.dateNote) {
    article.append(el("p", { class: "meta", text: [decision.date, decision.dateNote].filter(Boolean).join(" · ") }));
  }
  const line = el("p", { class: "links" });
  (decision.systemIds || []).forEach((id) => {
    if (id === currentId) return;
    const other = Atlas.system(id);
    if (!other) return;
    if (line.childNodes.length) line.append(document.createTextNode(", "));
    line.append(el("a", { href: "#/system/" + id, text: other.name }));
  });
  if (line.childNodes.length) article.append(line);
  if (decision.sourceIds && decision.sourceIds.length) article.append(sourceLinks(decision.sourceIds));
  return article;
}

function section(title, body, id) {
  const node = el("section", { class: "section" }, [el("h2", { text: title }), body]);
  if (id) node.id = id;
  return node;
}

function questionCard(question) {
  const card = el("article", { class: "card" }, [
    el("p", { class: "prose-line", text: question.text }),
    el("p", { class: "meta" }, [
      Atlas.labels.question[question.status],
      " · ",
      Atlas.labels.sourceKind[question.sourceKind],
      question.priority ? " · приоритет в 99: " + question.priority : " · приоритет в источнике не задан",
      question.sectionTitle ? " · " + question.sectionTitle : "",
      question.origin === "new" ? " · новая запись атласа" : "",
      question.origin === "atlas-annotation" ? " · пометка атласа, не дословный вопрос" : "",
    ]),
  ]);
  return card;
}

function auditBlock(system) {
  const audit = Atlas.auditFor(system.id);
  const profile = Atlas.data.profiles[audit.profile];
  const score = Atlas.auditScore(system.id);
  const box = el("div", { class: "audit-read" });
  box.append(el("p", { class: "prose-line", text: "Профиль: " + profile.label + "." }));
  box.append(el("p", { class: "prose-line", text: "Покрыто " + score.covered + " из " + score.applicable + ". Знаменатель — критерии профиля без «не применяется». В числителе только «покрыто». Частичное покрытие и непроверенные пункты туда не входят. Это не процент готовности игры." }));
  const width = score.applicable ? Math.round(100 * score.covered / score.applicable) : 0;
  box.append(el("div", { class: "bar" }, [el("span", { style: "width:" + width + "%" })]));
  profile.criteria.forEach((criterion) => {
    const item = audit.criteria[criterion.id];
    box.append(el("div", { class: "audit-line" }, [
      el("div", {}, [
        el("strong", { text: criterion.label }),
        item.evidence ? el("p", { class: "meta", text: item.evidence }) : null,
      ]),
      chip(Atlas.labels.audit[item.state], item.state === "covered" ? "ok" : item.state === "missing" ? "bad" : "warn"),
    ]));
  });
  return box;
}

function nextTaskBlock(system) {
  const tasks = Atlas.nextTasks(system);
  const list = el("ol", { class: "prose-list" });
  tasks.forEach((task) => list.append(el("li", { text: task })));
  return el("div", {}, [
    el("p", { class: "note", text: "Список собран из незакрытых критериев аудита и открытых вопросов дизайна. Это повестка следующего разговора с агентом, не утверждённый бэклог и не поручение на код." }),
    tasks.length ? list : el("p", { text: "По текущему профилю нечего предложить. Это всё ещё не значит, что дизайн закончен." }),
  ]);
}


