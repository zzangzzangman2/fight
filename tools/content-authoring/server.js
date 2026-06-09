const fs = require("fs");
const http = require("http");
const path = require("path");
const { URL } = require("url");

const repoRoot = path.resolve(__dirname, "..", "..");
const publicRoot = __dirname;
const resourcesRoot = path.join(repoRoot, "UnityScaffold", "Assets", "JoseonMurimTactics", "Resources");
const outputRoot = path.join(resourcesRoot, "AuthoringContent");
const mediaRoot = path.join(outputRoot, "Media");
const manifestPath = path.join(outputRoot, "content_manifest.json");
const backupRoot = path.join(outputRoot, "Backups");
const mapAssetCatalogPath = path.join(resourcesRoot, "MapAssets", "map_asset_catalog.json");
const port = Number(process.env.PORT || 5178);

const mediaFolders = {
  backgrounds: "Backgrounds",
  portraits: "Portraits",
  props: "Props"
};

const mimeTypes = {
  ".html": "text/html; charset=utf-8",
  ".css": "text/css; charset=utf-8",
  ".js": "application/javascript; charset=utf-8",
  ".json": "application/json; charset=utf-8",
  ".png": "image/png",
  ".jpg": "image/jpeg",
  ".jpeg": "image/jpeg",
  ".webp": "image/webp",
  ".gif": "image/gif",
  ".mp3": "audio/mpeg",
  ".wav": "audio/wav",
  ".ogg": "audio/ogg"
};

function ensureDirs() {
  fs.mkdirSync(outputRoot, { recursive: true });
  fs.mkdirSync(backupRoot, { recursive: true });
  for (const folder of Object.values(mediaFolders)) {
    fs.mkdirSync(path.join(mediaRoot, folder), { recursive: true });
  }
}

function send(res, status, body, type = "application/json; charset=utf-8") {
  const payload = Buffer.isBuffer(body) ? body : Buffer.from(body);
  res.writeHead(status, {
    "Content-Type": type,
    "Content-Length": payload.length,
    "Access-Control-Allow-Origin": "*",
    "Access-Control-Allow-Methods": "GET, POST, OPTIONS",
    "Access-Control-Allow-Headers": "Content-Type"
  });
  res.end(payload);
}

function sendJson(res, status, data) {
  send(res, status, JSON.stringify(data), "application/json; charset=utf-8");
}

function readBody(req, limitBytes = 80 * 1024 * 1024) {
  return new Promise((resolve, reject) => {
    const chunks = [];
    let size = 0;
    req.on("data", chunk => {
      size += chunk.length;
      if (size > limitBytes) {
        reject(new Error("payload too large"));
        req.destroy();
        return;
      }

      chunks.push(chunk);
    });
    req.on("end", () => resolve(Buffer.concat(chunks)));
    req.on("error", reject);
  });
}

function safeJoin(root, requestPath) {
  const target = path.resolve(root, requestPath.replace(/^[/\\]+/, ""));
  if (!target.startsWith(path.resolve(root))) {
    return null;
  }

  return target;
}

function slug(value, fallback = "asset") {
  const ascii = String(value || "")
    .normalize("NFKD")
    .replace(/[^\w.-]+/g, "_")
    .replace(/^_+|_+$/g, "")
    .toLowerCase();

  return ascii || `${fallback}_${Date.now()}`;
}

const seededSceneIds = new Set([
  "chapter1_prologue",
  "companion_baek_ryeon_talk",
  "companion_do_arin_talk",
  "companion_jin_seoyul_talk",
  "companion_seo_a_talk",
  "companion_han_biyeon_talk"
]);

function entry(id, speakerId, line, options = {}) {
  return {
    id,
    speakerId,
    line,
    mood: options.mood || "",
    backgroundId: options.backgroundId || "",
    choices: options.choices || []
  };
}

function choice(text, disposition, targetEntryId, options = {}) {
  return {
    id: slug(`${targetEntryId || "end"}_${text.slice(0, 10)}`, "choice"),
    text,
    disposition,
    targetEntryId: targetEntryId || "",
    flagAdded: options.flagAdded || "",
    approvalId: options.approvalId || "",
    approvalDelta: options.approvalDelta || 0,
    factionId: options.factionId || "",
    factionDelta: options.factionDelta || 0,
    battleKey: options.battleKey || "",
    battleValue: options.battleValue || 0,
    romanticIntent: !!options.romanticIntent,
    sceneCommand: options.sceneCommand || ""
  };
}

function defaultCharacters() {
  return [
    { id: "park_sungjun", displayName: "박성준", role: "백두천광검문 소문주 · 빛/검", age: 20, sectId: "baekdu_light_sword", sectName: "백두천광검문", portraitId: "", portraitResource: "", notes: "20세. 주인공." },
    { id: "park_mugyeom", displayName: "박무겸", role: "병든 문주", age: 0, sectId: "baekdu_light_sword", sectName: "백두천광검문", portraitId: "", portraitResource: "", notes: "백두천광검문의 현 문주." },
    { id: "yeon_ok", displayName: "연옥", role: "엄격한 사범", age: 0, sectId: "baekdu_light_sword", sectName: "백두천광검문", portraitId: "", portraitResource: "", notes: "성준을 단련시키는 사범." },
    { id: "cho_hui", displayName: "초희", role: "소백촌 약방", age: 0, sectId: "sobaek_village", sectName: "소백약방", portraitId: "", portraitResource: "", notes: "초반 생계와 약재 루프의 연결 인물." },
    { id: "baek_ryeon", displayName: "백련", role: "설악창문 · 서리/창", age: 17, sectId: "seorak_spear", sectName: "설악창문", portraitId: "", portraitResource: "", notes: "강원 설악창문." },
    { id: "do_arin", displayName: "도아린", role: "화왕도문 · 불/도", age: 18, sectId: "hwawang_blade", sectName: "화왕도문", portraitId: "", portraitResource: "", notes: "경상 화왕도문." },
    { id: "jin_seoyul", displayName: "진서율", role: "천뢰봉문 · 전기/봉", age: 16, sectId: "cheonroe_staff", sectName: "천뢰봉문", portraitId: "", portraitResource: "", notes: "경성 천뢰봉문." },
    { id: "seo_a", displayName: "신서아", role: "화접풍류문 · 바람/꽃/부채", age: 13, sectId: "hwajeop_fan", sectName: "화접풍류문", portraitId: "", portraitResource: "", notes: "13세. 전라 화접풍류문." },
    { id: "han_biyeon", displayName: "한비연", role: "흑련암문 · 어둠/독/암기", age: 18, sectId: "heukryeon_shadow", sectName: "흑련암문", portraitId: "", portraitResource: "", notes: "황해 흑련암문." }
  ];
}

function defaultBackgrounds() {
  return [
    {
      id: "joseon_murim_game_map",
      title: "조선-중원 강호도",
      resourcePath: "WorldMap/joseon_murim_game_map",
      previewUrl: "/resources/WorldMap/joseon_murim_game_map.png",
      notes: "통합 월드맵"
    },
    {
      id: "jido_1",
      title: "전략 지도 1",
      resourcePath: "WorldMap/jido_1",
      previewUrl: "/resources/WorldMap/jido_1.png",
      notes: "기존 테스트 지도"
    },
    {
      id: "jido_2",
      title: "전략 지도 2",
      resourcePath: "WorldMap/jido_2",
      previewUrl: "/resources/WorldMap/jido_2.png",
      notes: "기존 테스트 지도"
    }
  ];
}

function defaultMapAssets() {
  if (!fs.existsSync(mapAssetCatalogPath)) {
    return [];
  }

  try {
    const catalog = JSON.parse(fs.readFileSync(mapAssetCatalogPath, "utf8"));
    return (catalog.assets || []).map(asset => ({
      ...asset,
      previewUrl: asset.previewUrl || `/resources/${asset.file || `${asset.resourcePath}.png`}`
    }));
  } catch {
    return [];
  }
}

function gameDefaultScenes() {
  const map = "joseon_murim_game_map";
  return [
    {
      id: "chapter1_prologue",
      title: "제1장 · 꺼져가는 천광",
      location: "백두산 백두천광검문",
      backgroundId: map,
      startNodeId: "",
      entries: [
        entry("c1_000", "", "백두산 중턱. 눈은 쌓였고, 검각은 기울었고, 백두천광검문의 깃발은 바람에 ‘나 아직 안 죽었다’는 듯 겨우 펄럭인다.", { backgroundId: map, mood: "서술" }),
        entry("c1_010", "", "한때는 북방의 명문. 지금은 병든 문주, 무서운 사범, 그리고 지붕 위에서 낮잠 자는 소문주 하나가 전 재산이다.", { backgroundId: map, mood: "서술" }),
        entry("c1_020", "yeon_ok", "박성준. 내려와라. 지붕은 연무장이 아니고, 네 이불도 아니다.", { mood: "엄격" }),
        entry("c1_030", "park_sungjun", "사범님, 낮잠이 아닙니다. 하늘의 기운을 받아 천광심법을... 음, 누워서 받는 중이었습니다.", { mood: "능청" }),
        entry("c1_040", "yeon_ok", "좋다. 그럼 누운 채로 장작도 패고, 목인도 서른 번 치거라.", { mood: "차갑게" }),
        entry("c1_050", "park_sungjun", "(어떻게 둘러댈까?)", {
          mood: "선택",
          choices: [
            choice("하늘 기운이 아직 덜 내려왔습니다.", 3, "c1_060", { flagAdded: "CH1_JOKED_DREAM" }),
            choice("검도 사람도 휴식이 있어야 빛납니다.", 1, "c1_060", { flagAdded: "CH1_JOKED_BLADE_REST" }),
            choice("사범님 발소리 듣고 이미 마음은 내려갔습니다.", 0, "c1_060", { flagAdded: "CH1_JOKED_WAITING" })
          ]
        }),
        entry("c1_060", "yeon_ok", "입공만 보면 벌써 천하제일이다. 몸도 내려와라. 문주님이 찾으신다.", { mood: "단호" }),
        entry("c1_070", "", "성준이 뛰어내리자 기와 세 장이 먼저 하산했다. 연옥의 눈썹도 같이 올라갔다.", { mood: "서술" }),
        entry("c1_080", "park_sungjun", "보셨죠? 제 경공이 아니라 검각이 먼저 움직였습니다. 건물이 아주 적극적이에요.", { mood: "농담" }),
        entry("c1_090", "yeon_ok", "다음엔 네 용돈이 먼저 하산할 것이다.", { mood: "꾸짖음" }),
        entry("c1_100", "park_mugyeom", "성준아. 검은 폼으로 드는 게 아니다. 지켜야 할 것이 있을 때 비로소 손에 붙는다.", { mood: "조용함" }),
        entry("c1_110", "park_sungjun", "아버지, 저는 폼도 지키고 문파도 지키고 밥상도 지키고 싶은데요. 셋 중 밥상이 제일 위태롭습니다.", { mood: "능청" }),
        entry("c1_120", "park_mugyeom", "그래서 부른 것이다. 중원 문파들이 백두산 영맥을 눈독 들인다는 말이 돈다.", { mood: "걱정" }),
        entry("c1_130", "park_mugyeom", "천광심법과 백야검결, 이제 네 차례다. 문파가 작아졌다고 이름까지 작게 부르지는 마라.", { mood: "당부" }),
        entry("c1_140", "park_sungjun", "걱정 마세요. 제가 가벼운 건 말투뿐입니다. 건드리면, 꽤 무겁게 받아칠 겁니다.", { mood: "다짐" }),
        entry("c1_150", "", "낮이 되자 성준은 소백촌으로 내려갔다. 마을 사람들은 그를 ‘사고뭉치 도련님’이라 부르면서도, 은근히 길을 터준다.", { mood: "서술" }),
        entry("c1_160", "cho_hui", "왔네, 백두산 대표 한량. 오늘은 지붕 말고 땅 밟고 다니네?", { mood: "핀잔" }),
        entry("c1_170", "park_sungjun", "초희야, 그 말투... 나를 기다린 사람의 향기가 난다.", { mood: "풍류" }),
        entry("c1_180", "cho_hui", "기다렸지. 장작더미가. 약초 바구니도. 그리고 네가 미룬 외상 장부도.", { mood: "현실적" }),
        entry("c1_190", "", "문파 부흥은 거창한 비급에서 시작되지 않았다. 장작, 약초, 지붕 수리. 듣기엔 초라해도 은전은 거짓말을 하지 않는다.", { mood: "서술" }),
        entry("c1_200", "park_sungjun", "(오늘은 뭘 해야 덜 혼나고 더 벌까?)", {
          mood: "선택",
          choices: [
            choice("장작부터 패자. 백야검결로 나무도 감동시켜보자.", 1, "c1_210", { flagAdded: "CH1_VILLAGE_WORK_STARTED", factionId: "JOSEON_SECTS", factionDelta: 1 }),
            choice("약초를 캐자. 초희 잔소리도 조금은 줄겠지.", 0, "c1_220", { flagAdded: "CH1_VILLAGE_WORK_STARTED", factionId: "JOSEON_SECTS", factionDelta: 2 }),
            choice("검각부터 고치자. 적보다 비가 먼저 쳐들어온다.", 2, "c1_230", { flagAdded: "CH1_VILLAGE_WORK_STARTED", factionId: "JOSEON_SECTS", factionDelta: 1 })
          ]
        }),
        entry("c1_210", "park_sungjun", "좋아. 백야검결 제일식, 장작분광. 나무야 미안하다, 우리 집이 가난하다.", { mood: "농담" }),
        entry("c1_220", "park_sungjun", "산길? 내가 전문이지. 어릴 때 길 잃은 경험이 이렇게 경력이 될 줄이야.", { mood: "능청" }),
        entry("c1_230", "park_sungjun", "비 새는 검각에서 천하제일을 꿈꾸면 감기부터 천하제일이 된다. 지붕부터 살리자.", { mood: "현실적" }),
        entry("c1_240", "", "밤이 되자, 성준은 다시 찢어진 깃발 아래 섰다. 낮엔 웃었고, 손에는 물집이 잡혔다.", { mood: "서술" }),
        entry("c1_250", "park_sungjun", "좋아. 중원인지 뭔지, 우리 집 앞마당까지 들어오면 손님 대접은 못 하지.", { mood: "다짐" }),
        entry("c1_260", "", "꺼져가던 천광은 아직 남아 있었다. 조금 시끄럽고, 조금 가난하고, 이상하게 포기할 마음은 안 드는 빛이었다.", { mood: "서술" })
      ],
      nodes: []
    },
    companionScene("companion_baek_ryeon_talk", "백련 첫 대화", "baek_ryeon", [
      entry("t0", "baek_ryeon", "“창끝은 차갑게 둘게요. 대신 사람 마음까지 얼리라는 명령은... 조금 곤란합니다.”", { mood: "차분" }),
      entry("t1", "park_sungjun", "(어떻게 답할까?)", {
        mood: "선택",
        choices: [
          choice("좋아. 먼저 사람부터 살리자.", 0, "t2a", { approvalId: "baek_ryeon", approvalDelta: 3 }),
          choice("전열부터 잡자. 그래도 사람은 버리지 않는다.", 2, "t2b", { approvalId: "baek_ryeon", approvalDelta: -2 })
        ]
      }),
      entry("t2a", "baek_ryeon", "백련이 아주 작게 웃는다. “그 순서라면, 제 창도 덜 차가워질 것 같네요.”", { mood: "안도" }),
      entry("t2b", "baek_ryeon", "백련이 눈을 내리깐다. “말씀은 차갑지만... 버리지 않겠다는 쪽을 믿겠습니다.”", { mood: "서늘" })
    ]),
    companionScene("companion_do_arin_talk", "도아린 첫 대화", "do_arin", [
      entry("t0", "do_arin", "“문주, 길게 말하면 불 꺼져. 저놈들 오면 내가 앞에서 확 열어버릴게.”", { mood: "호쾌" }),
      entry("t1", "park_sungjun", "(어떻게 답할까?)", {
        mood: "선택",
        choices: [
          choice("좋다. 대신 한 발만 앞서라.", 1, "t2a", { approvalId: "do_arin", approvalDelta: 2 }),
          choice("가라. 오늘 길은 네 불로 낸다.", 2, "t2b", { approvalId: "do_arin", approvalDelta: 4 })
        ]
      }),
      entry("t2a", "do_arin", "도아린이 히죽 웃고 도집을 친다. “한 발. 음... 큰 한 발은 괜찮지?”", { mood: "씩씩" }),
      entry("t2b", "do_arin", "도아린의 눈이 반짝인다. “그 말, 내가 제일 좋아하는 종류야.”", { mood: "전의" })
    ]),
    companionScene("companion_jin_seoyul_talk", "진서율 첫 대화", "jin_seoyul", [
      entry("t0", "jin_seoyul", "“문주님, 저 물받이 보이죠? 저기에 번개 한 줄만 흘리면 감찰단이 춤추듯 넘어질걸요. 물론... 이론상요!”", { mood: "흥분" })
    ]),
    companionScene("companion_seo_a_talk", "신서아 첫 대화", "seo_a", [
      entry("t0", "seo_a", "“나도 할 수 있어요! 작다고 얕보면 안 돼요. 꽃바람은 원래 빈틈으로 쏙 들어가는 법이라구요!”", { mood: "밝음" })
    ]),
    companionScene("companion_han_biyeon_talk", "한비연 첫 대화", "han_biyeon", [
      entry("t0", "han_biyeon", "“정면승부? 멋은 있지. 내 취향은 아니고. 나는 그림자길로 돌아가서, 저쪽 허리부터 톡 건드릴게.”", { mood: "낮게" })
    ])
  ];
}

function companionScene(id, title, speakerId, entries) {
  return {
    id,
    title,
    location: "백두산 검각",
    backgroundId: "joseon_murim_game_map",
    startNodeId: "",
    entries,
    nodes: []
  };
}

function buildNodesForScene(scene, characters) {
  const nodeIds = scene.entries.map((_, index) => `${scene.id}_${String(index + 1).padStart(3, "0")}`);
  scene.startNodeId = nodeIds[0] || "";
  scene.nodes = scene.entries.map((line, index) => {
    const speaker = characters.find(item => item.id === line.speakerId);
    const nextNodeId = index + 1 < nodeIds.length ? nodeIds[index + 1] : "";
    return {
      nodeId: nodeIds[index],
      speakerId: line.speakerId || "",
      speakerName: speaker?.displayName || "",
      line: line.line || "",
      mood: line.mood || "",
      backgroundId: line.backgroundId || scene.backgroundId || "",
      portraitId: speaker?.portraitId || "",
      portraitResource: speaker?.portraitResource || "",
      nextNodeId,
      choices: (line.choices || []).filter(item => item.text && item.text.trim()).map(item => ({
        text: item.text.trim(),
        disposition: Number(item.disposition ?? -1),
        nextNodeId: item.targetEntryId === "__END__"
          ? ""
          : item.targetEntryId
            ? nodeIds[scene.entries.findIndex(entryItem => entryItem.id === item.targetEntryId)] || nextNodeId
            : nextNodeId,
        requiredFlags: [],
        flagsAdded: item.flagAdded ? [item.flagAdded] : [],
        approvalChanges: item.approvalId ? [{ id: item.approvalId, delta: Number(item.approvalDelta || 0) }] : [],
        factionChanges: item.factionId ? [{ id: item.factionId, delta: Number(item.factionDelta || 0) }] : [],
        battleModifiers: item.battleKey ? [{ id: item.battleKey, delta: Number(item.battleValue || 0) }] : [],
        romanticIntent: !!item.romanticIntent,
        sceneCommand: item.sceneCommand || ""
      }))
    };
  });
}

function rebuildNodes(manifest) {
  for (const scene of manifest.dialogueScenes || []) {
    scene.entries ||= [];
    if (scene.entries.length === 0 && Array.isArray(scene.nodes) && scene.nodes.length > 0) {
      scene.entries = entriesFromNodes(scene);
    }
    buildNodesForScene(scene, manifest.characters || []);
  }

  return manifest;
}

function entriesFromNodes(scene) {
  const nodeToEntryId = new Map((scene.nodes || []).map((node, index) => [
    node.nodeId,
    `line_${String(index + 1).padStart(3, "0")}`
  ]));

  return scene.nodes.map((node, index) => ({
    id: `line_${String(index + 1).padStart(3, "0")}`,
    speakerId: node.speakerId || "",
    line: node.line || "",
    mood: node.mood || "",
    backgroundId: node.backgroundId || "",
    choices: (node.choices || []).map(choiceItem => ({
      id: slug(choiceItem.text, "choice"),
      text: choiceItem.text || "",
      disposition: Number(choiceItem.disposition ?? -1),
      targetEntryId: choiceItem.nextNodeId ? nodeToEntryId.get(choiceItem.nextNodeId) || "" : "__END__",
      flagAdded: (choiceItem.flagsAdded || [])[0] || "",
      approvalId: (choiceItem.approvalChanges || [])[0]?.id || "",
      approvalDelta: (choiceItem.approvalChanges || [])[0]?.delta || 0,
      factionId: (choiceItem.factionChanges || [])[0]?.id || "",
      factionDelta: (choiceItem.factionChanges || [])[0]?.delta || 0,
      battleKey: (choiceItem.battleModifiers || [])[0]?.id || "",
      battleValue: (choiceItem.battleModifiers || [])[0]?.delta || 0,
      romanticIntent: !!choiceItem.romanticIntent,
      sceneCommand: choiceItem.sceneCommand || ""
    }))
  }));
}

function defaultManifest() {
  const manifest = {
    version: 1,
    updatedAt: new Date().toISOString(),
    project: {
      title: "조선 무협 SRPG",
      note: "콘텐츠 편집기에서 저장하면 Unity Resources의 게임 대사에 즉시 반영됩니다."
    },
    characters: defaultCharacters(),
    backgrounds: defaultBackgrounds(),
    portraits: [],
    props: [],
    mapAssets: defaultMapAssets(),
    dialogueScenes: gameDefaultScenes()
  };
  return rebuildNodes(manifest);
}

function isPlaceholderManifest(manifest) {
  const scenes = manifest?.dialogueScenes || [];
  if (scenes.length === 0) {
    return true;
  }

  return scenes.length === 1 && scenes[0].id === "prologue_placeholder";
}

function mergeById(current, defaults, replaceExisting = false) {
  const output = Array.isArray(current) ? [...current] : [];
  for (const item of defaults) {
    const index = output.findIndex(existing => existing.id === item.id);
    if (index < 0) {
      output.push(item);
    } else if (replaceExisting) {
      output[index] = { ...output[index], ...item };
    }
  }
  return output;
}

function mergeGameDefaults(manifest, replaceSeededScenes = false) {
  const seed = defaultManifest();
  if (!manifest || isPlaceholderManifest(manifest)) {
    return { manifest: seed, changed: true };
  }

  let changed = false;
  manifest.version ||= 1;
  manifest.project ||= seed.project;
  manifest.characters = mergeById(manifest.characters, seed.characters);
  manifest.backgrounds = mergeById(manifest.backgrounds, seed.backgrounds);
  manifest.portraits ||= [];
  manifest.props ||= [];
  manifest.mapAssets = mergeById(manifest.mapAssets, seed.mapAssets);
  manifest.dialogueScenes ||= [];

  if (replaceSeededScenes) {
    manifest.dialogueScenes = manifest.dialogueScenes.filter(scene => !seededSceneIds.has(scene.id));
    changed = true;
  }

  for (const scene of seed.dialogueScenes) {
    if (!manifest.dialogueScenes.some(existing => existing.id === scene.id)) {
      manifest.dialogueScenes.push(scene);
      changed = true;
    }
  }

  rebuildNodes(manifest);
  return { manifest, changed };
}

function writeManifest(content) {
  content.updatedAt = new Date().toISOString();
  rebuildNodes(content);
  fs.writeFileSync(manifestPath, `${JSON.stringify(content, null, 2)}\n`, "utf8");
}

function loadManifest() {
  ensureDirs();
  if (!fs.existsSync(manifestPath)) {
    const seeded = defaultManifest();
    writeManifest(seeded);
    return seeded;
  }

  try {
    const parsed = JSON.parse(fs.readFileSync(manifestPath, "utf8"));
    const merged = mergeGameDefaults(parsed);
    if (merged.changed) {
      backupManifest();
      writeManifest(merged.manifest);
    }

    return merged.manifest;
  } catch (error) {
    const fallback = defaultManifest();
    fallback.project.note = `기존 매니페스트를 읽지 못했습니다: ${error.message}`;
    return fallback;
  }
}

function backupManifest() {
  if (!fs.existsSync(manifestPath)) {
    return;
  }

  const stamp = new Date().toISOString().replace(/[:.]/g, "-");
  fs.copyFileSync(manifestPath, path.join(backupRoot, `content_manifest_${stamp}.json`));
}

async function handleApi(req, res, url) {
  if (req.method === "GET" && url.pathname === "/api/content") {
    sendJson(res, 200, loadManifest());
    return true;
  }

  if (req.method === "POST" && url.pathname === "/api/content") {
    ensureDirs();
    const body = await readBody(req);
    const content = JSON.parse(body.toString("utf8"));
    backupManifest();
    writeManifest(content);
    sendJson(res, 200, {
      ok: true,
      manifestPath: path.relative(repoRoot, manifestPath).replace(/\\/g, "/"),
      updatedAt: content.updatedAt
    });
    return true;
  }

  if (req.method === "POST" && url.pathname === "/api/import-defaults") {
    ensureDirs();
    const body = await readBody(req);
    const options = body.length > 0 ? JSON.parse(body.toString("utf8")) : {};
    const current = loadManifest();
    const merged = mergeGameDefaults(current, options.replace === true);
    backupManifest();
    writeManifest(merged.manifest);
    sendJson(res, 200, {
      ok: true,
      manifest: merged.manifest,
      manifestPath: path.relative(repoRoot, manifestPath).replace(/\\/g, "/"),
      updatedAt: merged.manifest.updatedAt
    });
    return true;
  }

  if (req.method === "GET" && url.pathname === "/api/status") {
    const manifest = loadManifest();
    sendJson(res, 200, {
      ok: true,
      manifestPath: path.relative(repoRoot, manifestPath).replace(/\\/g, "/"),
      absoluteManifestPath: manifestPath,
      sceneCount: manifest.dialogueScenes?.length || 0,
      characterCount: manifest.characters?.length || 0,
      mediaCount: (manifest.backgrounds?.length || 0) + (manifest.portraits?.length || 0) + (manifest.props?.length || 0),
      mapAssetCount: manifest.mapAssets?.length || 0,
      updatedAt: manifest.updatedAt || ""
    });
    return true;
  }

  if (req.method === "GET" && url.pathname === "/api/map-assets") {
    sendJson(res, 200, {
      ok: true,
      assets: defaultMapAssets()
    });
    return true;
  }

  if (req.method === "POST" && url.pathname === "/api/upload") {
    ensureDirs();
    const kind = url.searchParams.get("kind") || "props";
    const folder = mediaFolders[kind];
    if (!folder) {
      sendJson(res, 400, { ok: false, error: "unknown media kind" });
      return true;
    }

    const body = JSON.parse((await readBody(req)).toString("utf8"));
    const originalName = body.name || "asset";
    const ext = path.extname(originalName).toLowerCase() || ".png";
    const baseName = slug(path.basename(originalName, ext), kind.slice(0, -1));
    const fileName = `${baseName}${ext}`;
    const targetPath = path.join(mediaRoot, folder, fileName);
    const comma = String(body.dataUrl || "").indexOf(",");
    const encoded = comma >= 0 ? body.dataUrl.slice(comma + 1) : body.dataUrl;
    fs.writeFileSync(targetPath, Buffer.from(encoded, "base64"));

    const resourcePath = `AuthoringContent/Media/${folder}/${baseName}`;
    sendJson(res, 200, {
      ok: true,
      id: baseName,
      title: path.basename(originalName, ext),
      fileName,
      resourcePath,
      previewUrl: `/media/${folder}/${fileName}`
    });
    return true;
  }

  return false;
}

function serveFile(res, filePath) {
  if (!filePath || !fs.existsSync(filePath) || !fs.statSync(filePath).isFile()) {
    send(res, 404, "Not found", "text/plain; charset=utf-8");
    return;
  }

  const ext = path.extname(filePath).toLowerCase();
  send(res, 200, fs.readFileSync(filePath), mimeTypes[ext] || "application/octet-stream");
}

const server = http.createServer(async (req, res) => {
  try {
    if (req.method === "OPTIONS") {
      send(res, 204, "");
      return;
    }

    const url = new URL(req.url, `http://${req.headers.host}`);
    if (await handleApi(req, res, url)) {
      return;
    }

    if (req.method !== "GET") {
      send(res, 405, "Method not allowed", "text/plain; charset=utf-8");
      return;
    }

    if (url.pathname.startsWith("/media/")) {
      serveFile(res, safeJoin(mediaRoot, decodeURIComponent(url.pathname.slice("/media/".length))));
      return;
    }

    if (url.pathname.startsWith("/resources/")) {
      serveFile(res, safeJoin(resourcesRoot, decodeURIComponent(url.pathname.slice("/resources/".length))));
      return;
    }

    const requestPath = url.pathname === "/" ? "index.html" : decodeURIComponent(url.pathname.slice(1));
    serveFile(res, safeJoin(publicRoot, requestPath));
  } catch (error) {
    sendJson(res, 500, { ok: false, error: error.message });
  }
});

ensureDirs();
server.listen(port, "127.0.0.1", () => {
  console.log(`Content authoring UI: http://127.0.0.1:${port}`);
  console.log(`Saving to: ${manifestPath}`);
});
