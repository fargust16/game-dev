Atlas.load = async function () {
  const urls = ["api/atlas", "api/atlas.json"];
  for (let i = 0; i < urls.length; i++) {
    const response = await fetch(urls[i]);
    if (!response.ok) continue;
    Atlas.data = await response.json();
    return;
  }
  throw new Error("Сервер атласа не ответил.");
};
