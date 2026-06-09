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
        entry("c1_000", "", "백두산 중턱, 낡은 검각. 눈은 아직 처마 끝에 걸려 있고, 찢어진 백두천광검문의 깃발은 새벽바람에 힘없이 흔들린다.", { backgroundId: map, mood: "서술" }),
        entry("c1_010", "", "예전에는 북방의 명문이라 불렸던 문파. 이제 남은 것은 병든 문주 박무겸, 엄격한 사범 연옥, 그리고 오늘도 수련을 빼먹은 외동아들 박성준뿐이다.", { backgroundId: map, mood: "서술" }),
        entry("c1_020", "yeon_ok", "박성준. 검각 지붕 위가 연무장이더냐?", { mood: "엄격" }),
        entry("c1_030", "park_sungjun", "사범님, 오해십니다. 저는 지금 고도의 심상 수련 중이었습니다. 꿈속의 저는 이미 천광검문을 세 번이나 부흥시켰고요.", { mood: "능청" }),
        entry("c1_040", "yeon_ok", "그럼 네 꿈속의 박성준을 불러오너라. 현실의 박성준은 오늘 장작 패기와 목인 삼십 합이다.", { mood: "차갑게" }),
        entry("c1_050", "park_sungjun", "(어떻게 둘러댈까?)", {
          mood: "선택",
          choices: [
            choice("수련 중이었습니다. 꿈속에서.", 3, "c1_060", { flagAdded: "CH1_JOKED_DREAM" }),
            choice("검도 쉬어야 날이 섭니다.", 1, "c1_060", { flagAdded: "CH1_JOKED_BLADE_REST" }),
            choice("사범님이 찾으실 줄 알고 기다렸죠.", 0, "c1_060", { flagAdded: "CH1_JOKED_WAITING" })
          ]
        }),
        entry("c1_060", "yeon_ok", "말은 늘었고, 검은 줄었구나. 내려와라. 네 아버지께서 부르신다.", { mood: "단호" }),
        entry("c1_070", "", "박성준이 지붕에서 뛰어내리자 낡은 기와 몇 장이 와르르 미끄러진다. 연옥의 눈썹이 올라가고, 성준은 아무 일 없었다는 듯 먼 산을 본다.", { mood: "서술" }),
        entry("c1_080", "park_sungjun", "역시 우리 검각은 바람도 잘 통하고, 지붕도 잘 내려오는군요.", { mood: "농담" }),
        entry("c1_090", "yeon_ok", "그 입이 지붕보다 먼저 무너지기 전에 가라.", { mood: "꾸짖음" }),
        entry("c1_100", "park_mugyeom", "성준아. 검은 재주로 드는 것이 아니다. 짊어질 것이 있어야 드는 것이다.", { mood: "조용함" }),
        entry("c1_110", "park_sungjun", "아버지, 검은 무겁고 밥값은 더 무겁고 잔소리는 제일 무겁습니다. 셋 다 들라 하시면 아들이 좀 휘청입니다.", { mood: "능청" }),
        entry("c1_120", "park_mugyeom", "네가 웃는 건 좋다. 다만 웃음 뒤에 숨지는 마라. 중원 문파들이 백두산 영맥을 노린다는 소문이 돈다.", { mood: "걱정" }),
        entry("c1_130", "park_mugyeom", "천광심법과 백야검결은 이제 네가 이어야 한다. 문파가 작아졌다고, 네 어깨까지 작아지는 것은 아니다.", { mood: "당부" }),
        entry("c1_140", "park_sungjun", "제가 좀 가볍게 보여도 말이죠. 우리 문파 이름까지 가볍게 넘기진 않습니다.", { mood: "다짐" }),
        entry("c1_150", "", "그날 낮, 성준은 소백촌으로 내려간다. 마을 사람들은 그를 아직도 사고뭉치 도련님이라 부르지만, 문파를 믿는 눈빛만은 완전히 꺼지지 않았다.", { mood: "서술" }),
        entry("c1_160", "cho_hui", "또 수련 빼먹고 내려왔어? 약방 앞에서 폼 잡을 시간 있으면 장작이나 패.", { mood: "핀잔" }),
        entry("c1_170", "park_sungjun", "초희야, 오늘따라 약초보다 네가 더 향기롭다? 백두산에도 봄이 오긴 오는구나.", { mood: "풍류" }),
        entry("c1_180", "cho_hui", "그 입에 붙일 약초는 없으니 그냥 가서 일이나 해. 마을도, 너희 검각도, 지금 농담만 먹고 살 수는 없어.", { mood: "현실적" }),
        entry("c1_190", "", "소백촌의 일감은 작다. 장작을 패고, 약초를 캐고, 길목을 살피고, 무너진 검각을 고친다. 하지만 그 작은 일들이 백두천광검문을 다시 세우는 첫 돌이 된다.", { mood: "서술" }),
        entry("c1_200", "park_sungjun", "(오늘은 무엇부터 시작할까?)", {
          mood: "선택",
          choices: [
            choice("장작부터 패자. 은전이 있어야 밥도 먹고 지붕도 고친다.", 1, "c1_210", { flagAdded: "CH1_VILLAGE_WORK_STARTED", factionId: "JOSEON_SECTS", factionDelta: 1 }),
            choice("약초를 캐서 약방을 돕자. 다친 사람부터 챙겨야지.", 0, "c1_220", { flagAdded: "CH1_VILLAGE_WORK_STARTED", factionId: "JOSEON_SECTS", factionDelta: 2 }),
            choice("검각 수리부터다. 집이 무너지면 이름도 무너진다.", 2, "c1_230", { flagAdded: "CH1_VILLAGE_WORK_STARTED", factionId: "JOSEON_SECTS", factionDelta: 1 })
          ]
        }),
        entry("c1_210", "park_sungjun", "좋아. 오늘의 백야검결 첫 초식은 장작더미 상대다. 나무야, 영광으로 알아라.", { mood: "농담" }),
        entry("c1_220", "park_sungjun", "산길은 내가 좀 안다. 길 잃은 척하며 놀던 세월이 여기서 빛을 보는구만.", { mood: "능청" }),
        entry("c1_230", "park_sungjun", "검각이 집 같아야 제자도 돌아오지. 일단 비 새는 곳부터 막아보자.", { mood: "현실적" }),
        entry("c1_240", "", "그날 밤, 성준은 무너진 검각 앞에 다시 선다. 찢어진 깃발 아래로 새벽빛이 아주 얇게 스며든다.", { mood: "서술" }),
        entry("c1_250", "park_sungjun", "네놈들이 건드릴 건 낡은 문파가 아니야. 내 집이고, 내 사람들이다.", { mood: "다짐" }),
        entry("c1_260", "", "꺼져가던 천광이 아직 완전히 사라지지 않았다. 백두천광검문의 하루가, 이제 플레이어의 손에서 시작된다.", { mood: "서술" })
      ],
      nodes: []
    },
    companionScene("companion_baek_ryeon_talk", "백련 첫 대화", "baek_ryeon", [
      entry("t0", "baek_ryeon", "“창끝은 차갑게 두겠습니다. 다만, 사람을 살릴 길까지 얼리지는 말아 주세요.”", { mood: "차분" }),
      entry("t1", "park_sungjun", "(어떻게 답할까?)", {
        mood: "선택",
        choices: [
          choice("다친 제자들부터 살피자.", 0, "t2a", { approvalId: "baek_ryeon", approvalDelta: 3 }),
          choice("냉정하게 — 지금은 전열이 먼저다.", 2, "t2b", { approvalId: "baek_ryeon", approvalDelta: -2 })
        ]
      }),
      entry("t2a", "baek_ryeon", "백련이 조용히 고개를 숙인다. “…네. 그 말이면 충분합니다.”", { mood: "안도" }),
      entry("t2b", "baek_ryeon", "백련의 눈빛이 잠시 얼어붙는다. “그 냉정함이 사람을 버리지 않길 바랍니다.”", { mood: "서늘" })
    ]),
    companionScene("companion_do_arin_talk", "도아린 첫 대화", "do_arin", [
      entry("t0", "do_arin", "“문주, 복잡하게 재지 말자. 저놈들이 밀고 오면, 내가 먼저 불길 열게.”", { mood: "호쾌" }),
      entry("t1", "park_sungjun", "(어떻게 답할까?)", {
        mood: "선택",
        choices: [
          choice("좋다. 단, 혼자 앞서지 마라.", 1, "t2a", { approvalId: "do_arin", approvalDelta: 2 }),
          choice("앞장서라. 길은 힘으로 연다.", 2, "t2b", { approvalId: "do_arin", approvalDelta: 4 })
        ]
      }),
      entry("t2a", "do_arin", "도아린이 도집을 툭 친다. “알았어. 한 발만 먼저 간다, 한 발만.”", { mood: "씩씩" }),
      entry("t2b", "do_arin", "도아린이 씩 웃는다. “그 말 기다렸어.”", { mood: "전의" })
    ]),
    companionScene("companion_jin_seoyul_talk", "진서율 첫 대화", "jin_seoyul", [
      entry("t0", "jin_seoyul", "“문주님, 방금 지붕 물받이 봤어요? 저기 전기 흘리면 감찰단 발이 딱 멈출걸요?”", { mood: "흥분" })
    ]),
    companionScene("companion_seo_a_talk", "신서아 첫 대화", "seo_a", [
      entry("t0", "seo_a", "“나도 할 수 있어요! 작다고 얕보면 안 된다구요. 꽃바람은 낮게 불 때 더 잘 스며들어요.”", { mood: "밝음" })
    ]),
    companionScene("companion_han_biyeon_talk", "한비연 첫 대화", "han_biyeon", [
      entry("t0", "han_biyeon", "“정면으로 부딪히는 건 취향이 아니야. 대신, 구월산 그림자길은 내가 볼게.”", { mood: "낮게" })
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
      updatedAt: manifest.updatedAt || ""
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
