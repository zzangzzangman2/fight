const dispositionLabels = ["협도", "왕도", "패도", "풍류"];
const factionOptions = [
  ["", "없음"],
  ["JOSEON_SECTS", "조선문파연합"],
  ["ZHONGYUAN_ALLIANCE", "중원무림맹"],
  ["MURIM_INSPECTORS", "무림맹 감찰단"],
  ["ROYAL_COURT", "조정"],
  ["DEMONIC_CULT", "마교"],
  ["BLACK_HAT_GUILD", "흑립방"]
];

let content = null;
let activeTab = "dialogue";
let activeSceneId = "";
let dirty = false;
let serverStatus = null;

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
  $("#saveButton").classList.toggle("dirty", dirty);
}

async function requestJson(url, options) {
  if (window.location.protocol === "file:") {
    throw new Error("기존 대사 자동 불러오기는 server.js로 실행해야 합니다. node server.js 후 http://127.0.0.1:5178 로 열어주세요.");
  }

  const response = await fetch(url, options);
  const data = await response.json();
  if (!response.ok || data.ok === false) {
    throw new Error(data.error || response.statusText);
  }

  return data;
}

function markDirty(message = "수정됨") {
  dirty = true;
  setSaveState(message, "저장하면 Unity Resources에 바로 반영");
}

function clearDirty(message, strong) {
  dirty = false;
  setSaveState(message, strong);
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
        choice.approvalId ||= choice.approvalChanges?.[0]?.id || "";
        choice.approvalDelta = Number.isFinite(Number(choice.approvalDelta))
          ? Number(choice.approvalDelta)
          : Number(choice.approvalChanges?.[0]?.delta || 0);
        choice.factionId ||= choice.factionChanges?.[0]?.id || "";
        choice.factionDelta = Number.isFinite(Number(choice.factionDelta))
          ? Number(choice.factionDelta)
          : Number(choice.factionChanges?.[0]?.delta || 0);
        choice.battleKey ||= choice.battleModifiers?.[0]?.id || "";
        choice.battleValue = Number.isFinite(Number(choice.battleValue))
          ? Number(choice.battleValue)
          : Number(choice.battleModifiers?.[0]?.delta || 0);
        choice.romanticIntent = !!choice.romanticIntent;
        choice.sceneCommand ||= "";
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
  serverStatus = await requestJson("/api/status").catch(() => null);
  bindProject();
  render();
  clearDirty(`자동 불러오기 완료 · ${content.dialogueScenes.length}개 씬`, serverStatus?.manifestPath || "저장하면 Unity Resources에 반영");
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
  renderApplyStatus();
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
  renderScenePreview(scene);
  renderLines(scene);
}

function renderScenePreview(scene) {
  const preview = $("#scenePreview");
  const background = content.backgrounds.find(item => item.id === scene.backgroundId);
  const firstLine = scene.entries.find(item => item.line && item.line.trim()) || scene.entries[0];
  const speaker = content.characters.find(item => item.id === firstLine?.speakerId);
  const image = background?.previewUrl
    ? `<img src="${escapeAttr(background.previewUrl)}" alt="">`
    : "<span>배경 미지정</span>";
  preview.innerHTML = `
    <div class="scene-preview-image">${image}</div>
    <div class="scene-preview-copy">
      <strong>${escapeHtml(scene.title || "대사 씬")}</strong>
      <span>${escapeHtml(scene.location || "장소 미정")} · ${scene.entries.length}개 대사 · 선택지 ${countChoices(scene)}개</span>
      <p>${escapeHtml(speaker?.displayName || firstLine?.speakerId || "서술")} ${firstLine?.line ? "— " + escapeHtml(firstLine.line) : "— 첫 대사를 입력하세요."}</p>
    </div>
  `;
}

function countChoices(scene) {
  return scene.entries.reduce((sum, item) => sum + (item.choices?.length || 0), 0);
}

function renderApplyStatus() {
  const panel = $("#applyStatus");
  if (!panel || !content) {
    return;
  }

  const status = serverStatus || {};
  panel.innerHTML = `
    <article><strong>${content.dialogueScenes.length}</strong><span>대사 씬</span></article>
    <article><strong>${content.characters.length}</strong><span>인물</span></article>
    <article><strong>${(content.backgrounds.length + content.portraits.length + content.props.length)}</strong><span>배경/에셋</span></article>
    <article><strong>${status.updatedAt ? new Date(status.updatedAt).toLocaleTimeString() : "-"}</strong><span>최근 저장</span></article>
  `;
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
      markDirty("대사 복제됨");
      renderScenes();
    });
    node.querySelector(".delete-line").addEventListener("click", () => {
      if (scene.entries.length <= 1) {
        entry.line = "";
      } else {
        scene.entries.splice(index, 1);
      }
      markDirty("대사 삭제됨");
      renderScenes();
    });
    node.querySelector(".add-choice").addEventListener("click", () => {
      entry.choices.push({
        id: uid("choice"),
        text: "",
        disposition: -1,
        targetEntryId: "",
        flagAdded: "",
        approvalId: "",
        approvalDelta: 0,
        factionId: "",
        factionDelta: 0,
        battleKey: "",
        battleValue: 0,
        romanticIntent: false,
        sceneCommand: ""
      });
      markDirty("선택지 추가됨");
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
    fillApprovalSelect(node.querySelector(".choice-approval"), choice.approvalId);
    fillFactionSelect(node.querySelector(".choice-faction"), choice.factionId);
    node.querySelector(".choice-approval-delta").value = String(choice.approvalDelta || 0);
    node.querySelector(".choice-faction-delta").value = String(choice.factionDelta || 0);
    node.querySelector(".choice-romantic").value = String(!!choice.romanticIntent);
    node.querySelector(".choice-command").value = choice.sceneCommand || "";
    node.querySelector(".choice-battle-key").value = choice.battleKey || "";
    node.querySelector(".choice-battle-value").value = String(choice.battleValue || 0);
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
    node.querySelector(".choice-approval").addEventListener("change", event => {
      choice.approvalId = event.target.value;
    });
    node.querySelector(".choice-approval-delta").addEventListener("input", event => {
      choice.approvalDelta = Number(event.target.value || 0);
    });
    node.querySelector(".choice-faction").addEventListener("change", event => {
      choice.factionId = event.target.value;
    });
    node.querySelector(".choice-faction-delta").addEventListener("input", event => {
      choice.factionDelta = Number(event.target.value || 0);
    });
    node.querySelector(".choice-romantic").addEventListener("change", event => {
      choice.romanticIntent = event.target.value === "true";
    });
    node.querySelector(".choice-command").addEventListener("input", event => {
      choice.sceneCommand = event.target.value.trim();
    });
    node.querySelector(".choice-battle-key").addEventListener("input", event => {
      choice.battleKey = event.target.value.trim();
    });
    node.querySelector(".choice-battle-value").addEventListener("input", event => {
      choice.battleValue = Number(event.target.value || 0);
    });
    node.querySelector(".delete-choice").addEventListener("click", () => {
      entry.choices.splice(index, 1);
      markDirty("선택지 삭제됨");
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

function fillApprovalSelect(select, value) {
  select.innerHTML = '<option value="">없음</option>';
  for (const character of content.characters) {
    const option = document.createElement("option");
    option.value = character.id;
    option.textContent = character.displayName || character.id;
    select.append(option);
  }
  select.value = value || "";
}

function fillFactionSelect(select, value) {
  select.innerHTML = "";
  for (const [id, label] of factionOptions) {
    const option = document.createElement("option");
    option.value = id;
    option.textContent = label;
    select.append(option);
  }
  select.value = value || "";
}

function moveEntry(scene, index, direction) {
  const next = index + direction;
  if (next < 0 || next >= scene.entries.length) {
    return;
  }

  const [entry] = scene.entries.splice(index, 1);
  scene.entries.splice(next, 0, entry);
  markDirty("대사 순서 변경됨");
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
      markDirty("인물 삭제됨");
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
      markDirty("에셋 삭제됨");
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
  markDirty("업로드 완료 · 저장 필요");
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
          approvalChanges: choice.approvalId ? [{ id: choice.approvalId, delta: Number(choice.approvalDelta || 0) }] : [],
          factionChanges: choice.factionId ? [{ id: choice.factionId, delta: Number(choice.factionDelta || 0) }] : [],
          battleModifiers: choice.battleKey ? [{ id: choice.battleKey, delta: Number(choice.battleValue || 0) }] : [],
          romanticIntent: !!choice.romanticIntent,
          sceneCommand: choice.sceneCommand || ""
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
  serverStatus = await requestJson("/api/status").catch(() => serverStatus);
  render();
  clearDirty(`게임 적용 완료 ${new Date(result.updatedAt).toLocaleTimeString()}`, result.manifestPath);
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
  $("#importDefaultsButton").addEventListener("click", () => importDefaults().catch(error => setSaveState(`가져오기 실패: ${error.message}`)));
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
    markDirty("새 씬 추가됨");
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
    markDirty("씬 삭제됨");
    render();
  });
  $("#sceneTitle").addEventListener("input", event => {
    activeScene().title = event.target.value;
    $("#sceneHeading").textContent = event.target.value || "대사 씬";
    renderScenePreview(activeScene());
  });
  $("#sceneLocation").addEventListener("input", event => {
    activeScene().location = event.target.value;
    renderScenePreview(activeScene());
  });
  $("#sceneBackground").addEventListener("change", event => {
    activeScene().backgroundId = event.target.value;
    renderScenePreview(activeScene());
  });
  $("#addLineButton").addEventListener("click", () => {
    activeScene().entries.push(newEntry());
    markDirty("대사 추가됨");
    renderScenes();
  });
  $("#addCharacterButton").addEventListener("click", () => {
    content.characters.push(newCharacter());
    markDirty("인물 추가됨");
    render();
  });
  $("#backgroundUpload").addEventListener("change", event => uploadFiles("backgrounds", event.target.files).catch(error => setSaveState(`업로드 실패: ${error.message}`)));
  $("#portraitUpload").addEventListener("change", event => uploadFiles("portraits", event.target.files).catch(error => setSaveState(`업로드 실패: ${error.message}`)));
  $("#propUpload").addEventListener("change", event => uploadFiles("props", event.target.files).catch(error => setSaveState(`업로드 실패: ${error.message}`)));
  document.addEventListener("input", event => {
    if (event.target.matches("input:not([type=file]), textarea")) {
      markDirty("수정됨");
    }
  });
  document.addEventListener("change", event => {
    if (event.target.matches("select")) {
      markDirty("수정됨");
    }
  });
}

async function importDefaults() {
  const replace = window.confirm("게임 기본 프롤로그/동료 대화를 다시 가져올까요? 같은 기본 씬은 최신 기본값으로 교체하고, 직접 만든 씬은 유지합니다.");
  if (!replace) {
    return;
  }

  setSaveState("게임 기본 대사 가져오는 중");
  const result = await requestJson("/api/import-defaults", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ replace: true })
  });
  content = normalize(result.manifest);
  activeSceneId = content.dialogueScenes[0]?.id || "";
  serverStatus = await requestJson("/api/status").catch(() => serverStatus);
  bindProject();
  render();
  clearDirty(`기본 대사 적용 완료 ${new Date(result.updatedAt).toLocaleTimeString()}`, result.manifestPath);
}

bindEvents();
loadContent().catch(error => setSaveState(`불러오기 실패: ${error.message}`));
