const dispositionLabels = ["협도", "왕도", "패도", "풍류"];

let content = null;
let activeTab = "dialogue";
let activeSceneId = "";

const $ = selector => document.querySelector(selector);
const $$ = selector => Array.from(document.querySelectorAll(selector));

function uid(prefix) {
  return `${prefix}_${Date.now().toString(36)}_${Math.random().toString(36).slice(2, 6)}`;
}

function slug(value, fallback) {
  const safe = String(value || "")
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9가-힣_-]+/g, "_")
    .replace(/^_+|_+$/g, "");
  return safe || uid(fallback || "id");
}

function setSaveState(text, strong) {
  $("#saveState").textContent = text;
  if (strong) {
    $("#targetPath").textContent = strong;
  }
}

async function requestJson(url, options) {
  const response = await fetch(url, options);
  const data = await response.json();
  if (!response.ok || data.ok === false) {
    throw new Error(data.error || response.statusText);
  }

  return data;
}

function normalize(data) {
  const normalized = data || {};
  normalized.project ||= { title: "조선 무협 SRPG", note: "" };
  normalized.characters ||= [];
  normalized.backgrounds ||= [];
  normalized.portraits ||= [];
  normalized.props ||= [];
  normalized.dialogueScenes ||= [];

  for (const scene of normalized.dialogueScenes) {
    scene.id ||= uid("scene");
    scene.title ||= "새 대사 씬";
    scene.location ||= "";
    scene.backgroundId ||= "";
    scene.entries ||= [];
    for (const entry of scene.entries) {
      entry.id ||= uid("line");
      entry.speakerId ||= "";
      entry.line ||= "";
      entry.mood ||= "";
      entry.backgroundId ||= "";
      entry.choices ||= [];
      for (const choice of entry.choices) {
        choice.id ||= uid("choice");
        choice.text ||= "";
        choice.disposition = Number.isFinite(Number(choice.disposition)) ? Number(choice.disposition) : -1;
        choice.targetEntryId ||= "";
        choice.flagAdded ||= "";
      }
    }
  }

  if (normalized.dialogueScenes.length === 0) {
    normalized.dialogueScenes.push(newScene());
  }

  return normalized;
}

function newScene() {
  const id = uid("scene");
  return {
    id,
    title: "새 대사 씬",
    location: "",
    backgroundId: "",
    startNodeId: `${id}_001`,
    entries: [newEntry()],
    nodes: []
  };
}

function newEntry() {
  return {
    id: uid("line"),
    speakerId: content?.characters?.[0]?.id || "",
    line: "",
    mood: "",
    backgroundId: "",
    choices: []
  };
}

function newCharacter() {
  return {
    id: uid("character"),
    displayName: "새 인물",
    role: "",
    portraitId: "",
    portraitResource: "",
    notes: ""
  };
}

async function loadContent() {
  setSaveState("불러오는 중");
  content = normalize(await requestJson("/api/content"));
  activeSceneId = content.dialogueScenes[0]?.id || "";
  bindProject();
  render();
  setSaveState("편집 가능", "저장하면 Unity Resources에 반영");
}

function bindProject() {
  $("#projectTitle").value = content.project.title || "";
  $("#projectNote").value = content.project.note || "";
}

function readProject() {
  content.project.title = $("#projectTitle").value.trim();
  content.project.note = $("#projectNote").value.trim();
}

function activeScene() {
  return content.dialogueScenes.find(scene => scene.id === activeSceneId) || content.dialogueScenes[0];
}

function fillCharacterSelect(select, value) {
  select.innerHTML = "";
  for (const character of content.characters) {
    const option = document.createElement("option");
    option.value = character.id;
    option.textContent = character.displayName || character.id;
    select.append(option);
  }
  select.value = value || "";
}

function fillBackgroundSelect(select, value, includeEmpty = true) {
  select.innerHTML = "";
  if (includeEmpty) {
    const empty = document.createElement("option");
    empty.value = "";
    empty.textContent = "변경 없음";
    select.append(empty);
  }
  for (const background of content.backgrounds) {
    const option = document.createElement("option");
    option.value = background.id;
    option.textContent = background.title || background.id;
    select.append(option);
  }
  select.value = value || "";
}

function render() {
  renderTabs();
  renderScenes();
  renderCharacters();
  renderMedia();
}

function renderTabs() {
  $$(".tab").forEach(button => {
    button.classList.toggle("active", button.dataset.tab === activeTab);
  });
  $$(".view").forEach(view => view.classList.remove("active"));
  $(`#${activeTab}Tab`).classList.add("active");
}

function renderScenes() {
  const list = $("#sceneList");
  list.innerHTML = "";
  for (const scene of content.dialogueScenes) {
    const item = document.createElement("li");
    const button = document.createElement("button");
    button.className = scene.id === activeSceneId ? "active" : "";
    button.type = "button";
    button.innerHTML = `<strong>${escapeHtml(scene.title || scene.id)}</strong><span>${scene.entries.length}개 대사 · ${escapeHtml(scene.location || "장소 미정")}</span>`;
    button.addEventListener("click", () => {
      activeSceneId = scene.id;
      renderScenes();
    });
    item.append(button);
    list.append(item);
  }

  const scene = activeScene();
  if (!scene) {
    return;
  }

  $("#sceneHeading").textContent = scene.title || "대사 씬";
  $("#sceneTitle").value = scene.title || "";
  $("#sceneLocation").value = scene.location || "";
  fillBackgroundSelect($("#sceneBackground"), scene.backgroundId, false);
  renderLines(scene);
}

function renderLines(scene) {
  const list = $("#lineList");
  list.innerHTML = "";
  scene.entries.forEach((entry, index) => {
    const node = $("#lineTemplate").content.firstElementChild.cloneNode(true);
    node.querySelector(".line-number").textContent = `${index + 1}. 대사`;
    fillCharacterSelect(node.querySelector(".line-speaker"), entry.speakerId);
    fillBackgroundSelect(node.querySelector(".line-background"), entry.backgroundId);
    node.querySelector(".line-mood").value = entry.mood || "";
    node.querySelector(".line-text").value = entry.line || "";

    node.querySelector(".line-speaker").addEventListener("change", event => {
      entry.speakerId = event.target.value;
    });
    node.querySelector(".line-background").addEventListener("change", event => {
      entry.backgroundId = event.target.value;
    });
    node.querySelector(".line-mood").addEventListener("input", event => {
      entry.mood = event.target.value;
    });
    node.querySelector(".line-text").addEventListener("input", event => {
      entry.line = event.target.value;
    });
    node.querySelector(".move-up").addEventListener("click", () => moveEntry(scene, index, -1));
    node.querySelector(".move-down").addEventListener("click", () => moveEntry(scene, index, 1));
    node.querySelector(".duplicate-line").addEventListener("click", () => {
      const clone = JSON.parse(JSON.stringify(entry));
      clone.id = uid("line");
      scene.entries.splice(index + 1, 0, clone);
      renderScenes();
    });
    node.querySelector(".delete-line").addEventListener("click", () => {
      if (scene.entries.length <= 1) {
        entry.line = "";
      } else {
        scene.entries.splice(index, 1);
      }
      renderScenes();
    });
    node.querySelector(".add-choice").addEventListener("click", () => {
      entry.choices.push({
        id: uid("choice"),
        text: "",
        disposition: -1,
        targetEntryId: "",
        flagAdded: ""
      });
      renderScenes();
    });

    renderChoices(scene, entry, node.querySelector(".choice-list"));
    list.append(node);
  });
}

function renderChoices(scene, entry, list) {
  list.innerHTML = "";
  entry.choices.forEach((choice, index) => {
    const node = $("#choiceTemplate").content.firstElementChild.cloneNode(true);
    node.querySelector(".choice-text").value = choice.text || "";
    node.querySelector(".choice-disposition").value = String(choice.disposition ?? -1);
    fillChoiceTarget(node.querySelector(".choice-target"), scene, choice.targetEntryId);
    node.querySelector(".choice-flag").value = choice.flagAdded || "";
    node.querySelector(".choice-text").addEventListener("input", event => {
      choice.text = event.target.value;
    });
    node.querySelector(".choice-disposition").addEventListener("change", event => {
      choice.disposition = Number(event.target.value);
    });
    node.querySelector(".choice-target").addEventListener("change", event => {
      choice.targetEntryId = event.target.value;
    });
    node.querySelector(".choice-flag").addEventListener("input", event => {
      choice.flagAdded = event.target.value.trim();
    });
    node.querySelector(".delete-choice").addEventListener("click", () => {
      entry.choices.splice(index, 1);
      renderScenes();
    });
    list.append(node);
  });
}

function fillChoiceTarget(select, scene, value) {
  select.innerHTML = "";
  const next = document.createElement("option");
  next.value = "";
  next.textContent = "다음 대사";
  select.append(next);

  const end = document.createElement("option");
  end.value = "__END__";
  end.textContent = "대화 종료";
  select.append(end);

  scene.entries.forEach((entry, index) => {
    const option = document.createElement("option");
    option.value = entry.id;
    option.textContent = `${index + 1}번 대사로 이동`;
    select.append(option);
  });
  select.value = value || "";
}

function moveEntry(scene, index, direction) {
  const next = index + direction;
  if (next < 0 || next >= scene.entries.length) {
    return;
  }

  const [entry] = scene.entries.splice(index, 1);
  scene.entries.splice(next, 0, entry);
  renderScenes();
}

function renderCharacters() {
  const grid = $("#characterGrid");
  grid.innerHTML = "";
  for (const character of content.characters) {
    const card = document.createElement("article");
    card.className = "character-card";
    card.innerHTML = `
      ${portraitPreview(character)}
      <label>이름<input class="char-name" type="text" value="${escapeAttr(character.displayName || "")}"></label>
      <label>역할<input class="char-role" type="text" value="${escapeAttr(character.role || "")}"></label>
      <label>초상화<select class="char-portrait"></select></label>
      <label>메모<textarea class="char-notes" rows="3">${escapeHtml(character.notes || "")}</textarea></label>
      <button class="danger delete-character" type="button">인물 삭제</button>
    `;
    const portraitSelect = card.querySelector(".char-portrait");
    fillPortraitSelect(portraitSelect, character.portraitId);
    card.querySelector(".char-name").addEventListener("input", event => {
      character.displayName = event.target.value;
      renderScenes();
    });
    card.querySelector(".char-role").addEventListener("input", event => {
      character.role = event.target.value;
    });
    portraitSelect.addEventListener("change", event => {
      character.portraitId = event.target.value;
      const portrait = content.portraits.find(item => item.id === character.portraitId);
      character.portraitResource = portrait?.resourcePath || "";
      renderCharacters();
    });
    card.querySelector(".char-notes").addEventListener("input", event => {
      character.notes = event.target.value;
    });
    card.querySelector(".delete-character").addEventListener("click", () => {
      content.characters = content.characters.filter(item => item !== character);
      render();
    });
    grid.append(card);
  }
}

function fillPortraitSelect(select, value) {
  select.innerHTML = '<option value="">초상화 없음</option>';
  for (const portrait of content.portraits) {
    const option = document.createElement("option");
    option.value = portrait.id;
    option.textContent = portrait.title || portrait.id;
    select.append(option);
  }
  select.value = value || "";
}

function portraitPreview(character) {
  const portrait = content.portraits.find(item => item.id === character.portraitId);
  if (portrait?.previewUrl) {
    return `<div class="portrait-thumb"><img src="${escapeAttr(portrait.previewUrl)}" alt=""></div>`;
  }

  return '<div class="portrait-thumb">초상화 없음</div>';
}

function renderMedia() {
  renderMediaGrid("#backgroundGrid", content.backgrounds, "backgrounds");
  renderMediaGrid("#portraitGrid", content.portraits, "portraits");
  renderMediaGrid("#propGrid", content.props, "props");
}

function renderMediaGrid(selector, items, kind) {
  const grid = $(selector);
  grid.innerHTML = "";
  for (const item of items) {
    const card = document.createElement("article");
    card.className = "media-card";
    const preview = item.previewUrl
      ? `<div class="media-thumb"><img src="${escapeAttr(item.previewUrl)}" alt=""></div>`
      : '<div class="media-thumb">미리보기 없음</div>';
    card.innerHTML = `
      ${preview}
      <label>표시 이름<input class="media-title" type="text" value="${escapeAttr(item.title || "")}"></label>
      <label>메모<textarea class="media-notes" rows="2">${escapeHtml(item.notes || "")}</textarea></label>
      <small>${escapeHtml(item.resourcePath || "")}</small>
      <button class="danger delete-media" type="button">삭제</button>
    `;
    card.querySelector(".media-title").addEventListener("input", event => {
      item.title = event.target.value;
      renderScenes();
    });
    card.querySelector(".media-notes").addEventListener("input", event => {
      item.notes = event.target.value;
    });
    card.querySelector(".delete-media").addEventListener("click", () => {
      const list = mediaList(kind);
      const index = list.indexOf(item);
      if (index >= 0) {
        list.splice(index, 1);
      }
      render();
    });
    grid.append(card);
  }
}

function mediaList(kind) {
  if (kind === "backgrounds") return content.backgrounds;
  if (kind === "portraits") return content.portraits;
  return content.props;
}

async function uploadFiles(kind, files) {
  for (const file of files) {
    const dataUrl = await readFileAsDataUrl(file);
    const result = await requestJson(`/api/upload?kind=${kind}`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ name: file.name, type: file.type, dataUrl })
    });
    mediaList(kind).push({
      id: uniqueMediaId(kind, result.id),
      title: result.title,
      resourcePath: result.resourcePath,
      previewUrl: result.previewUrl,
      notes: ""
    });
  }
  render();
  setSaveState("업로드 완료. 저장하면 게임 매니페스트에 반영됩니다.");
}

function uniqueMediaId(kind, id) {
  const list = mediaList(kind);
  let candidate = slug(id, kind);
  let suffix = 2;
  while (list.some(item => item.id === candidate)) {
    candidate = `${slug(id, kind)}_${suffix++}`;
  }
  return candidate;
}

function readFileAsDataUrl(file) {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => resolve(reader.result);
    reader.onerror = reject;
    reader.readAsDataURL(file);
  });
}

function prepareForSave() {
  readProject();
  const output = JSON.parse(JSON.stringify(content));
  output.version = 1;
  output.characters.forEach(character => {
    character.id = slug(character.id || character.displayName, "character");
  });

  for (const scene of output.dialogueScenes) {
    scene.id = slug(scene.id || scene.title, "scene");
    const nodeIds = scene.entries.map((_, index) => `${scene.id}_${String(index + 1).padStart(3, "0")}`);
    scene.startNodeId = nodeIds[0] || "";
    scene.nodes = scene.entries.map((entry, index) => {
      const speaker = output.characters.find(item => item.id === entry.speakerId);
      const nextNodeId = index + 1 < nodeIds.length ? nodeIds[index + 1] : "";
      return {
        nodeId: nodeIds[index],
        speakerId: entry.speakerId || "",
        speakerName: speaker?.displayName || "",
        line: entry.line || "",
        mood: entry.mood || "",
        backgroundId: entry.backgroundId || scene.backgroundId || "",
        portraitId: speaker?.portraitId || "",
        portraitResource: speaker?.portraitResource || "",
        nextNodeId,
        choices: (entry.choices || []).filter(choice => choice.text?.trim()).map(choice => ({
          text: choice.text.trim(),
          disposition: Number(choice.disposition ?? -1),
          nextNodeId: choice.targetEntryId === "__END__"
            ? ""
            : choice.targetEntryId
              ? nodeIds[scene.entries.findIndex(item => item.id === choice.targetEntryId)] || nextNodeId
              : nextNodeId,
          requiredFlags: [],
          flagsAdded: choice.flagAdded ? [choice.flagAdded] : [],
          approvalChanges: [],
          factionChanges: [],
          battleModifiers: [],
          sceneCommand: ""
        }))
      };
    });
  }

  return output;
}

async function saveContent() {
  setSaveState("저장 중");
  const output = prepareForSave();
  const result = await requestJson("/api/content", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(output)
  });
  content = normalize(output);
  render();
  setSaveState(`저장 완료 ${new Date(result.updatedAt).toLocaleTimeString()}`, result.manifestPath);
}

function escapeHtml(value) {
  return String(value ?? "")
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;");
}

function escapeAttr(value) {
  return escapeHtml(value).replace(/"/g, "&quot;");
}

function bindEvents() {
  $$(".tab").forEach(button => {
    button.addEventListener("click", () => {
      activeTab = button.dataset.tab;
      render();
    });
  });
  $("#reloadButton").addEventListener("click", loadContent);
  $("#saveButton").addEventListener("click", () => saveContent().catch(error => setSaveState(`저장 실패: ${error.message}`)));
  $("#projectTitle").addEventListener("input", () => {
    content.project.title = $("#projectTitle").value;
  });
  $("#projectNote").addEventListener("input", () => {
    content.project.note = $("#projectNote").value;
  });
  $("#addSceneButton").addEventListener("click", () => {
    const scene = newScene();
    content.dialogueScenes.push(scene);
    activeSceneId = scene.id;
    render();
  });
  $("#deleteSceneButton").addEventListener("click", () => {
    if (content.dialogueScenes.length <= 1) {
      activeScene().entries = [newEntry()];
      activeScene().title = "새 대사 씬";
    } else {
      content.dialogueScenes = content.dialogueScenes.filter(scene => scene.id !== activeSceneId);
      activeSceneId = content.dialogueScenes[0].id;
    }
    render();
  });
  $("#sceneTitle").addEventListener("input", event => {
    activeScene().title = event.target.value;
    $("#sceneHeading").textContent = event.target.value || "대사 씬";
  });
  $("#sceneLocation").addEventListener("input", event => {
    activeScene().location = event.target.value;
  });
  $("#sceneBackground").addEventListener("change", event => {
    activeScene().backgroundId = event.target.value;
  });
  $("#addLineButton").addEventListener("click", () => {
    activeScene().entries.push(newEntry());
    renderScenes();
  });
  $("#addCharacterButton").addEventListener("click", () => {
    content.characters.push(newCharacter());
    render();
  });
  $("#backgroundUpload").addEventListener("change", event => uploadFiles("backgrounds", event.target.files).catch(error => setSaveState(`업로드 실패: ${error.message}`)));
  $("#portraitUpload").addEventListener("change", event => uploadFiles("portraits", event.target.files).catch(error => setSaveState(`업로드 실패: ${error.message}`)));
  $("#propUpload").addEventListener("change", event => uploadFiles("props", event.target.files).catch(error => setSaveState(`업로드 실패: ${error.message}`)));
}

bindEvents();
loadContent().catch(error => setSaveState(`불러오기 실패: ${error.message}`));
