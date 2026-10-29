const Atlas = {
  data: null,
  route: { view: "map", id: null },
  selectedId: null,
  selectedEdgeId: null,
  query: "",
  filters: {
    category: new Set(),
    maturity: new Set(),
    decision: new Set(),
    implementation: new Set(),
    relType: new Set(),
    evidence: new Set(),
  },
  questionFilters: { status: "open", priority: "", system: "", sourceKind: "" },
  graph: { x: 0, y: 0, k: 1, baseK: 1, home: true },
  message: "",
  messageError: false,
};

Atlas.labels = {
  category: {
    foundation: "Основание",
    world: "Мир",
    exploration: "Исследование",
    choice: "Выбор",
    combat: "Confront",
    character: "Персонаж",
    society: "Общество",
    scope: "Рамки среза",
    proposed: "Пробел",
  },
  maturity: {
    "not-audited": "Не аудировано",
    "idea-only": "Только идея",
    "partially-specified": "Частично описано",
    "specification-documented": "Спецификация есть",
    "decision-pending": "Решение не принято",
    "conflicting-sources": "Источники спорят",
  },
  decision: {
    proposed: "Предложено",
    accepted: "Принято",
    rejected: "Отклонено",
    superseded: "Заменено",
    unresolved: "Не закрыто",
  },
  implementation: {
    unknown: "Неизвестно",
    "not-implemented": "Не реализовано",
    "partially-implemented": "Частично",
    implemented: "Реализовано",
    verified: "Сверено со спецификацией",
  },
  relType: {
    dependency: "Зависимость",
    "data-flow": "Данные и ресурсы",
    event: "Событие",
    influence: "Влияние",
    "shared-rule": "Общее правило",
  },
  evidence: {
    confirmed: "Подтверждено",
    inferred: "Выведено",
    unresolved: "Не разрешено",
    conflicting: "Конфликт",
  },
  question: {
    open: "Открыт",
    "under-discussion": "В разговоре",
    resolved: "Закрыт",
    deferred: "Отложен",
  },
  sourceKind: {
    "gameplay-design": "Дизайн игры",
    "technical-research": "Техническое исследование",
    scope: "Рамки среза",
  },
  audit: {
    "not-assessed": "Не оценено",
    missing: "Нет",
    "partially-covered": "Частично",
    covered: "Покрыто",
    "not-applicable": "Не применяется",
  },
  relColor: {
    dependency: "#7dcaa6",
    "data-flow": "#e0a15a",
    event: "#c58bd0",
    influence: "#7eb6e0",
    "shared-rule": "#e4c48a",
  },
};

Atlas.byId = function (list, id) {
  return (list || []).find((item) => item.id === id) || null;
};

Atlas.system = function (id) {
  return Atlas.byId(Atlas.data.systems, id);
};

Atlas.source = function (id) {
  return Atlas.byId(Atlas.data.sources, id);
};

Atlas.auditFor = function (systemId) {
  return Atlas.data.audits.find((item) => item.systemId === systemId) || null;
};

Atlas.questionsFor = function (systemId) {
  return Atlas.data.questions.filter((item) => item.systemIds.indexOf(systemId) !== -1);
};

Atlas.decisionsFor = function (systemId) {
  return Atlas.data.decisions.filter((item) => item.systemIds.indexOf(systemId) !== -1);
};

Atlas.edgesFor = function (systemId) {
  return Atlas.data.relationships.filter((item) => item.fromId === systemId || item.toId === systemId);
};

Atlas.refsFor = function (systemId) {
  return Atlas.data.references.filter((item) => item.systemIds.indexOf(systemId) !== -1);
};

Atlas.githubHref = function (source, anchor) {
  if (!source || !source.path) return "";
  const encoded = source.path.split("/").map(encodeURIComponent).join("/");
  let href = "https://github.com/fargust16/game-dev/blob/main/" + encoded;
  if (anchor) href += "#" + anchor;
  return href;
};

Atlas.systemVisible = function (system) {
  const filters = Atlas.filters;
  if (filters.category.size && !filters.category.has(system.category)) return false;
  if (filters.maturity.size && !filters.maturity.has(system.designMaturity)) return false;
  if (filters.decision.size && !filters.decision.has(system.decisionState)) return false;
  if (filters.implementation.size && !filters.implementation.has(system.implementationMaturity)) return false;
  return true;
};

Atlas.edgeVisible = function (edge) {
  if (!Atlas.systemVisible(Atlas.system(edge.fromId)) || !Atlas.systemVisible(Atlas.system(edge.toId))) return false;
  if (Atlas.filters.relType.size && !Atlas.filters.relType.has(edge.type)) return false;
  if (Atlas.filters.evidence.size && !Atlas.filters.evidence.has(edge.evidence)) return false;
  return true;
};

Atlas.auditScore = function (systemId) {
  const audit = Atlas.auditFor(systemId);
  if (!audit) return null;
  const profile = Atlas.data.profiles[audit.profile];
  let applicable = 0;
  let covered = 0;
  let partial = 0;
  let missing = 0;
  let unassessed = 0;
  profile.criteria.forEach((criterion) => {
    const item = audit.criteria[criterion.id];
    if (item.state === "not-applicable") return;
    applicable += 1;
    if (item.state === "covered") covered += 1;
    else if (item.state === "partially-covered") partial += 1;
    else if (item.state === "missing") missing += 1;
    else unassessed += 1;
  });
  return { applicable, covered, partial, missing, unassessed, profile: audit.profile };
};

Atlas.nextTasks = function (system) {
  const tasks = [];
  const score = Atlas.auditScore(system.id);
  const audit = Atlas.auditFor(system.id);
  const profile = Atlas.data.profiles[audit.profile];
  profile.criteria.forEach((criterion) => {
    const item = audit.criteria[criterion.id];
    if (item.state === "missing" || item.state === "not-assessed" || item.state === "partially-covered") {
      tasks.push(criterion.label + ": " + Atlas.labels.audit[item.state]);
    }
  });
  Atlas.questionsFor(system.id).forEach((question) => {
    if (question.status === "open" || question.status === "under-discussion") {
      if (question.sourceKind === "technical-research") return;
      tasks.push("Вопрос: " + question.text);
    }
  });
  return tasks.slice(0, 6);
};

Atlas.searchHits = function (query) {
  const needle = query.trim().toLowerCase();
  if (!needle) return [];
  const hits = [];
  Atlas.data.systems.forEach((system) => {
    const hay = (system.name + " " + system.title + " " + system.summary).toLowerCase();
    if (hay.indexOf(needle) !== -1) hits.push({ href: "#/system/" + system.id, kind: "Система", title: system.name });
  });
  Atlas.data.sources.forEach((source) => {
    const hay = (source.title + " " + source.path).toLowerCase();
    if (hay.indexOf(needle) !== -1) hits.push({ href: "#/sources", kind: "Документ", title: source.title });
  });
  Atlas.data.questions.forEach((question) => {
    if (question.text.toLowerCase().indexOf(needle) !== -1) {
      hits.push({ href: "#/questions", kind: "Вопрос", title: question.text });
    }
  });
  Atlas.data.decisions.forEach((decision) => {
    if (decision.statement.toLowerCase().indexOf(needle) !== -1) {
      hits.push({ href: "#/decisions", kind: "Решение", title: decision.statement });
    }
  });
  Atlas.data.references.forEach((ref) => {
    if ((ref.title + " " + ref.note).toLowerCase().indexOf(needle) !== -1) {
      hits.push({ href: "#/references", kind: "Референс", title: ref.title });
    }
  });
  return hits.slice(0, 12);
};
