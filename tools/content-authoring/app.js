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
const sectOptions = [
  { id: "", label: "문파 미정", aliases: [] },
  { id: "baekdu_light_sword", label: "백두천광검문", aliases: ["백두천광검문", "백두산"] },
  { id: "seorak_spear", label: "설악창문", aliases: ["설악창문", "강원"] },
  { id: "hwawang_blade", label: "화왕도문", aliases: ["화왕도문", "경상"] },
  { id: "cheonroe_staff", label: "천뢰봉문", aliases: ["천뢰봉문", "경성"] },
  { id: "hwajeop_fan", label: "화접풍류문", aliases: ["화접풍류문", "전라"] },
  { id: "heukryeon_shadow", label: "흑련암문", aliases: ["흑련암문", "황해"] },
  { id: "shaolin", label: "소림사", aliases: ["소림사", "소림"] },
  { id: "wudang", label: "무당파", aliases: ["무당파", "무당"] },
  { id: "huashan", label: "화산파", aliases: ["화산파", "화산"] },
  { id: "emei", label: "아미파", aliases: ["아미파", "아미"] },
  { id: "kunlun", label: "곤륜파", aliases: ["곤륜파", "곤륜"] },
  { id: "kongtong", label: "공동파", aliases: ["공동파", "공동"] },
  { id: "zhongnan", label: "종남파", aliases: ["종남파", "종남"] },
  { id: "qingcheng", label: "청성파", aliases: ["청성파", "청성"] },
  { id: "dianchang", label: "점창파", aliases: ["점창파", "점창"] },
  { id: "gaibang", label: "개방", aliases: ["개방"] },
  { id: "namgung", label: "남궁세가", aliases: ["남궁세가", "남궁"] },
  { id: "jegalsega", label: "제갈세가", aliases: ["제갈세가", "제갈"] },
  { id: "moyong", label: "모용세가", aliases: ["모용세가", "모용"] },
  { id: "hwangbo", label: "황보세가", aliases: ["황보세가", "황보"] },
  { id: "sacheon_dangmun", label: "사천당문", aliases: ["사천당문", "당문", "사천당가"] },
  { id: "habuk_paengga", label: "하북팽가", aliases: ["하북팽가", "팽가"] },
  { id: "haenam", label: "해남파", aliases: ["해남파", "해남"] },
  { id: "sungsan", label: "숭산파", aliases: ["숭산파", "숭산"] },
  { id: "hyungsan", label: "형산파", aliases: ["형산파", "형산"] },
  { id: "hangsan", label: "항산파", aliases: ["항산파", "항산"] },
  { id: "taesan", label: "태산파", aliases: ["태산파", "태산"] },
  { id: "demonic_cult", label: "마교", aliases: ["마교"] },
  { id: "blood_cult", label: "혈교", aliases: ["혈교"] },
  { id: "black_hat_guild", label: "흑립방", aliases: ["흑립방"] },
  { id: "sobaek_village", label: "소백약방", aliases: ["소백약방", "소백촌"] }
];
const characterMetaDefaults = {
  park_sungjun: { age: 20, sectId: "baekdu_light_sword", romanceEligible: false },
  park_mugyeom: { age: 0, sectId: "baekdu_light_sword", romanceEligible: false },
  yeon_ok: { age: 0, sectId: "baekdu_light_sword", romanceEligible: false },
  cho_hui: { age: 0, sectId: "sobaek_village", romanceEligible: false },
  baek_ryeon: { age: 17, sectId: "seorak_spear", romanceEligible: false },
  do_arin: { age: 18, sectId: "hwawang_blade", romanceEligible: false },
  jin_seoyul: { age: 16, sectId: "cheonroe_staff", romanceEligible: false },
  seo_a: { age: 13, sectId: "hwajeop_fan", romanceEligible: false },
  han_biyeon: { age: 18, sectId: "heukryeon_shadow", romanceEligible: false }
};
const apiFallbackBases = ["http://127.0.0.1:5179", "http://127.0.0.1:5178"];

let content = null;
let activeTab = initialTab();
let activeSceneId = "";
let dirty = false;
let serverStatus = null;
let apiBase = window.location.protocol === "file:" ? null : "";

const $ = selector => document.querySelector(selector);
const $$ = selector => Array.from(document.querySelectorAll(selector));
const offlineResourcesRoot = "../../UnityScaffold/Assets/JoseonMurimTactics/Resources/";

function initialTab() {
  const requested = new URLSearchParams(window.location.search).get("tab");
  return ["dialogue", "characters", "media", "mapAssets", "guide"].includes(requested) ? requested : "dialogue";
}

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
  let lastError = null;
  for (const base of apiCandidates(url)) {
    try {
      const response = await fetch(withApiBase(url, base), options);
      const data = await response.json();
      if (!response.ok || data.ok === false) {
        const error = new Error(data.error || response.statusText);
        error.fromServer = true;
        throw error;
      }

      apiBase = base;
      return data;
    } catch (error) {
      if (error.fromServer) {
        throw error;
      }
      lastError = error;
    }
  }

  const error = new Error(window.location.protocol === "file:"
    ? "로컬 저장 서버가 실행 중이 아닙니다. tools/content-authoring/server.js를 백그라운드로 실행해야 게임 파일에 바로 저장됩니다."
    : lastError?.message || "로컬 저장 서버에 연결하지 못했습니다.");
  error.networkUnavailable = true;
  throw error;
}

function apiCandidates(url) {
  if (/^https?:\/\//i.test(url)) {
    return [""];
  }

  const bases = [];
  if (apiBase !== null) {
    bases.push(apiBase);
  }
  if (window.location.protocol === "file:") {
    bases.push(...apiFallbackBases);
  } else {
    bases.push("");
  }

  return [...new Set(bases)];
}

function withApiBase(url, base) {
  if (/^https?:\/\//i.test(url) || !base) {
    return url;
  }

  return `${base}${url}`;
}

function markDirty(message = "수정됨") {
  dirty = true;
  setSaveState(message, "저장하면 Unity Resources에 바로 반영");
}

function clearDirty(message, strong) {
  dirty = false;
  setSaveState(message, strong);
}

function cloneEmbeddedDefaults() {
  if (!window.JOSEON_AUTHORING_DEFAULTS) {
    return null;
  }

  return JSON.parse(JSON.stringify(window.JOSEON_AUTHORING_DEFAULTS));
}

function embeddedMapAssets() {
  const assets = window.JOSEON_MAP_ASSET_CATALOG?.assets || [];
  return JSON.parse(JSON.stringify(assets));
}

function mergeMapAssets(items) {
  const output = Array.isArray(items) ? [...items] : [];
  for (const asset of embeddedMapAssets()) {
    if (!output.some(item => item.id === asset.id)) {
      output.push(asset);
    }
  }

  return output;
}

function normalizeAge(value) {
  const age = Number.parseInt(value, 10);
  return Number.isFinite(age) && age >= 1 && age <= 150 ? age : 0;
}

function canUseRomanticIntent(choice, characters) {
  if (!choice?.romanticIntent || !choice.approvalId) {
    return false;
  }

  const character = (characters || []).find(item => item.id === choice.approvalId);
  return !!character && !!character.romanceEligible;
}

function inferAge(character) {
  const text = `${character?.notes || ""} ${character?.role || ""}`;
  const match = text.match(/(\d{1,3})\s*세/);
  return match ? normalizeAge(match[1]) : 0;
}

function validSectId(value) {
  return sectOptions.some(option => option.id === value);
}

function sectLabel(value) {
  return sectOptions.find(option => option.id === value)?.label || "";
}

function inferSectId(character) {
  const text = `${character?.role || ""} ${character?.notes || ""} ${character?.displayName || ""}`;
  for (const option of sectOptions) {
    if (!option.id) {
      continue;
    }
    if (option.aliases.some(alias => alias && text.includes(alias))) {
      return option.id;
    }
  }

  return "";
}

function applyEmbeddedDefaults({ message, dirtyState = false } = {}) {
  const fallback = cloneEmbeddedDefaults();
  if (!fallback) {
    throw new Error("내장 기본 대사를 찾지 못했습니다. defaults.js를 확인해주세요.");
  }

  content = normalize(fallback);
  activeSceneId = content.dialogueScenes[0]?.id || "";
  serverStatus = null;
  bindProject();
  render();

  const statusText = message || `오프라인 기본 대사 로드 · ${content.dialogueScenes.length}개 씬`;
  const targetText = "서버 없이 불러옴 · 직접 게임 적용 저장은 node server.js 실행 필요";
  if (dirtyState) {
    markDirty(statusText);
    $("#targetPath").textContent = targetText;
  } else {
    clearDirty(statusText, targetText);
  }
}

function normalize(data) {
  const normalized = data || {};
  normalized.project ||= { title: "조선 무협 SRPG", note: "" };
  normalized.characters ||= [];
  normalized.backgrounds ||= [];
  normalized.portraits ||= [];
  normalized.props ||= [];
  normalized.mapAssets = mergeMapAssets(normalized.mapAssets);
  normalized.dialogueScenes ||= [];

  for (const character of normalized.characters) {
    const meta = characterMetaDefaults[character.id] || {};
    character.age = normalizeAge(character.age) || normalizeAge(meta.age) || inferAge(character);
    character.romanceEligible = !!character.romanceEligible || !!meta.romanceEligible;
    character.sectId = validSectId(character.sectId) ? character.sectId : meta.sectId || inferSectId(character);
    character.sectName = sectLabel(character.sectId);
  }

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
        choice.romanticIntent = canUseRomanticIntent(choice, normalized.characters);
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
    age: 0,
    sectId: "",
    sectName: "",
    portraitId: "",
    portraitResource: "",
    notes: ""
  };
}

async function loadContent() {
  setSaveState("불러오는 중");
  try {
    content = normalize(await requestJson("/api/content"));
    activeSceneId = content.dialogueScenes[0]?.id || "";
    serverStatus = await requestJson("/api/status").catch(() => null);
    bindProject();
    render();
    clearDirty(`자동 불러오기 완료 · ${content.dialogueScenes.length}개 씬`, serverStatus?.manifestPath || "저장하면 Unity Resources에 반영");
  } catch (error) {
    if (error.fromServer) {
      throw error;
    }
    applyEmbeddedDefaults();
  }
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
  renderMapAssets();
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
  const image = previewImageMarkup(mediaPreviewUrl(background), background?.title || "배경 미지정");
  preview.innerHTML = `
    <div class="scene-preview-image">${image}</div>
    <div class="scene-preview-copy">
      <strong>${escapeHtml(scene.title || "대사 씬")}</strong>
      <span>${escapeHtml(scene.location || "장소 미정")} · ${scene.entries.length}개 대사 · 선택지 ${countChoices(scene)}개</span>
      <p>${escapeHtml(speaker?.displayName || firstLine?.speakerId || "서술")} ${firstLine?.line ? "— " + escapeHtml(firstLine.line) : "— 첫 대사를 입력하세요."}</p>
    </div>
  `;
}

function mediaPreviewUrl(item) {
  if (!item?.previewUrl) {
    return "";
  }

  if (window.location.protocol === "file:" && item.previewUrl.startsWith("/resources/")) {
    return offlineResourcesRoot + item.previewUrl.slice("/resources/".length);
  }

  if (window.location.protocol === "file:" && item.previewUrl.startsWith("/media/")) {
    return offlineResourcesRoot + "AuthoringContent/Media/" + item.previewUrl.slice("/media/".length);
  }

  return item.previewUrl;
}

function previewImageMarkup(url, fallbackText) {
  const fallback = escapeHtml(fallbackText || "미리보기 없음");
  if (!url) {
    return `<span>${fallback}</span>`;
  }

  return `<img src="${escapeAttr(url)}" alt="" onerror="this.hidden=true;this.nextElementSibling.hidden=false"><span hidden>${fallback}</span>`;
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
    <article><strong>${content.mapAssets?.length || 0}</strong><span>MAP 에셋</span></article>
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
      <div class="character-meta-grid">
        <label>나이<select class="char-age"></select></label>
        <label>문파<select class="char-sect"></select></label>
      </div>
      <label class="inline-check"><input class="char-romance-eligible" type="checkbox"> 로맨스 효과 허용</label>
      <label>초상화<select class="char-portrait"></select></label>
      <label>메모<textarea class="char-notes" rows="3">${escapeHtml(character.notes || "")}</textarea></label>
      <button class="danger delete-character" type="button">인물 삭제</button>
    `;
    const ageSelect = card.querySelector(".char-age");
    const sectSelect = card.querySelector(".char-sect");
    const portraitSelect = card.querySelector(".char-portrait");
    const romanceEligibleInput = card.querySelector(".char-romance-eligible");
    fillAgeSelect(ageSelect, character.age);
    fillSectSelect(sectSelect, character.sectId);
    fillPortraitSelect(portraitSelect, character.portraitId);
    romanceEligibleInput.checked = !!character.romanceEligible;
    card.querySelector(".char-name").addEventListener("input", event => {
      character.displayName = event.target.value;
      renderScenes();
    });
    card.querySelector(".char-role").addEventListener("input", event => {
      character.role = event.target.value;
    });
    ageSelect.addEventListener("change", event => {
      character.age = normalizeAge(event.target.value);
      renderCharacters();
    });
    romanceEligibleInput.addEventListener("change", event => {
      character.romanceEligible = event.target.checked;
    });
    sectSelect.addEventListener("change", event => {
      character.sectId = event.target.value;
      character.sectName = sectLabel(character.sectId);
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

function fillAgeSelect(select, value) {
  select.innerHTML = '<option value="0">나이 미정</option>';
  for (let age = 1; age <= 150; age += 1) {
    const option = document.createElement("option");
    option.value = String(age);
    option.textContent = `${age}세`;
    select.append(option);
  }
  select.value = String(normalizeAge(value));
}

function fillSectSelect(select, value) {
  select.innerHTML = "";
  for (const optionData of sectOptions) {
    const option = document.createElement("option");
    option.value = optionData.id;
    option.textContent = optionData.label;
    select.append(option);
  }
  select.value = validSectId(value) ? value : "";
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
  return `<div class="portrait-thumb">${previewImageMarkup(mediaPreviewUrl(portrait), "초상화 없음")}</div>`;
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
    const preview = `<div class="media-thumb">${previewImageMarkup(mediaPreviewUrl(item), "미리보기 없음")}</div>`;
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

function renderMapAssets() {
  const stats = $("#mapAssetStats");
  const assets = content?.mapAssets || [];
  const tiles = assets.filter(item => item.category === "terrain");
  const objects = assets.filter(item => item.category !== "terrain");

  if (stats) {
    stats.innerHTML = `
      <article><strong>${tiles.length}</strong><span>타일</span></article>
      <article><strong>${objects.length}</strong><span>오브젝트</span></article>
      <article><strong>${assets.length}</strong><span>전체</span></article>
    `;
  }

  renderMapAssetGrid("#mapTileGrid", tiles);
  renderMapAssetGrid("#mapObjectGrid", objects);
}

function renderMapAssetGrid(selector, items) {
  const grid = $(selector);
  if (!grid) {
    return;
  }

  grid.innerHTML = "";
  for (const item of items) {
    const card = document.createElement("article");
    card.className = "map-asset-card";
    const tags = (item.tags || []).slice(0, 4).map(tag => `<span>${escapeHtml(tag)}</span>`).join("");
    card.innerHTML = `
      <div class="map-asset-thumb">${previewImageMarkup(mediaPreviewUrl(item), item.title || item.id)}</div>
      <div class="map-asset-copy">
        <strong>${escapeHtml(item.title || item.id)}</strong>
        <span>${escapeHtml(item.subtype || item.category || "")}</span>
        <p>${escapeHtml(item.notes || "")}</p>
        <small>${escapeHtml(item.resourcePath || "")}</small>
        <div class="map-asset-tags">${tags}</div>
      </div>
    `;
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
    character.age = normalizeAge(character.age);
    character.romanceEligible = !!character.romanceEligible;
    character.sectId = validSectId(character.sectId) ? character.sectId : "";
    character.sectName = sectLabel(character.sectId);
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
          romanticIntent: canUseRomanticIntent(choice, output.characters),
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
  try {
    const result = await requestJson("/api/content", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(output)
    });
    content = normalize(output);
    serverStatus = await requestJson("/api/status").catch(() => serverStatus);
    render();
    clearDirty(`게임 적용 완료 ${new Date(result.updatedAt).toLocaleTimeString()}`, result.manifestPath);
  } catch (error) {
    if (error.fromServer) {
      throw error;
    }
    content = normalize(output);
    serverStatus = null;
    render();
    dirty = true;
    setSaveState("저장 실패 · 로컬 저장 서버 필요", "JSON 내려받기 대신 tools/content-authoring/server.js가 실행 중일 때 게임 파일에 바로 저장합니다.");
  }
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
  try {
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
  } catch (error) {
    if (error.fromServer) {
      throw error;
    }
    applyEmbeddedDefaults({ message: "게임 기본 대사 가져옴 · 저장 필요", dirtyState: true });
  }
}

bindEvents();
loadContent().catch(error => setSaveState(`불러오기 실패: ${error.message}`));
