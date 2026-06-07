const statLabels = {
  strength: "근력",
  agility: "민첩",
  innerPower: "내공",
  spirit: "정신",
  insight: "통찰",
  charm: "매력"
};

const terrainLabels = {
  stone: "마당",
  wall: "잔해",
  wood: "목재",
  water: "물가",
  bridge: "나무다리",
  roof: "지붕",
  bamboo: "대나무숲",
  reed: "갈대밭",
  cliff: "벼랑",
  altar: "제단"
};

const coverLabels = { none: "엄폐 없음", light: "약한 엄폐", heavy: "강한 엄폐" };
const slotLabels = { main: "주행동", bonus: "보조행동", reaction: "반응", free: "특수" };

const nodes = [
  { id: "westGate", name: "서쪽 문루", x: 10, y: 67, terrain: "stone", elevation: 1, cover: "light" },
  { id: "moonGate", name: "월문 잔해", x: 20, y: 49, terrain: "wall", elevation: 1, cover: "heavy" },
  { id: "wineTables", name: "객잔 술상", x: 29, y: 65, terrain: "wood", elevation: 0, cover: "light" },
  { id: "shallowWater", name: "압록강 여울", x: 32, y: 84, terrain: "water", elevation: 0, cover: "none", hazard: "slippery" },
  { id: "northReeds", name: "북쪽 갈대밭", x: 40, y: 28, terrain: "reed", elevation: 0, cover: "heavy" },
  { id: "bridgeWest", name: "부서진 다리 서단", x: 44, y: 72, terrain: "bridge", elevation: 0, cover: "none" },
  { id: "bridgeEast", name: "부서진 다리 동단", x: 57, y: 68, terrain: "bridge", elevation: 0, cover: "none" },
  { id: "courtyard", name: "폐사당 마당", x: 49, y: 53, terrain: "stone", elevation: 0, cover: "none" },
  { id: "incenseHall", name: "향로가 남은 법당", x: 56, y: 42, terrain: "stone", elevation: 0, cover: "light" },
  { id: "altar", name: "무너진 불상 제단", x: 66, y: 75, terrain: "altar", elevation: 1, cover: "heavy" },
  { id: "bambooGrove", name: "대나무숲", x: 70, y: 31, terrain: "bamboo", elevation: 0, cover: "heavy" },
  { id: "pavilionRoof", name: "누각 지붕", x: 74, y: 54, terrain: "roof", elevation: 2, cover: "light" },
  { id: "eastLantern", name: "동쪽 등불길", x: 83, y: 64, terrain: "stone", elevation: 0, cover: "none" },
  { id: "cliffEdge", name: "벼랑 끝", x: 86, y: 43, terrain: "cliff", elevation: 2, cover: "light", hazard: "fall" },
  { id: "eastFormation", name: "정도맹 진영", x: 89, y: 27, terrain: "stone", elevation: 1, cover: "light" },
  { id: "southBoat", name: "묶인 나룻배", x: 52, y: 90, terrain: "water", elevation: 0, cover: "light" }
];

const terrainObjects = [
  { id: "objTable", node: "wineTables", name: "객잔 술상", icon: "桌", kind: "table", stat: "strength", dc: 11, slot: "bonus", desc: "걷어차서 임시 엄폐를 만들고 인접 적에게 민첩 내성을 요구한다." },
  { id: "objIncense", node: "incenseHall", name: "꺼져가는 향로", icon: "煙", kind: "incense", stat: "insight", dc: 12, slot: "main", desc: "연막을 일으켜 해당 지점과 주변에 강엄폐를 만든다." },
  { id: "objBamboo", node: "bambooGrove", name: "대나무숲", icon: "竹", kind: "bamboo", stat: "agility", dc: 13, slot: "main", desc: "대나무를 베어 시야를 끊거나 적을 넘어뜨린다. 암기/잠행과 연계." },
  { id: "objLantern", node: "eastLantern", name: "흔들리는 등불", icon: "燈", kind: "lantern", stat: "agility", dc: 12, slot: "main", desc: "등불을 베어 화염 지대를 만든다. 물가에서는 피해가 감소한다." },
  { id: "objBridge", node: "bridgeEast", name: "낡은 나무다리", icon: "橋", kind: "bridge", stat: "strength", dc: 14, slot: "main", desc: "다리를 끊어 길을 막고 다리 위 유닛을 물가로 떨어뜨린다." },
  { id: "objAltar", node: "altar", name: "무너진 불상", icon: "佛", kind: "altar", stat: "innerPower", dc: 15, slot: "main", desc: "내공을 실어 파편을 터뜨린다. 주변 적에게 파훼를 크게 누적한다." }
];

const skills = {
  parkGentlemanSword: {
    id: "parkGentlemanSword",
    name: "사군자검",
    ownerHint: "박성준",
    slot: "main",
    target: "enemy",
    range: 2,
    stat: "agility",
    attackBonus: 2,
    damage: "1d8",
    breakGain: 10,
    tags: ["검법", "해동문"],
    desc: "매화·난초·국화·대나무의 궤를 섞는 해동문 기본 검법. 인접 엄폐를 무시하지는 않지만 파훼가 안정적으로 쌓인다.",
    cue: "Park_GentlemanSword"
  },
  parkMoonCharm: {
    id: "parkMoonCharm",
    name: "농월추파",
    ownerHint: "박성준",
    slot: "bonus",
    target: "enemy",
    range: 5,
    stat: "charm",
    uses: 2,
    social: true,
    dcMod: "spirit",
    moraleDamage: 13,
    tags: ["심리전", "풍류", "위험한 농담"],
    desc: "호색한 기믹을 전투 심리전으로 사용한다. 성공 시 적 기세를 꺾지만, 실패하면 동료 승인도와 아군 기세에 페널티가 생긴다.",
    cue: "Park_MoonCharm"
  },
  parkStep: {
    id: "parkStep",
    name: "태평농월보",
    ownerHint: "박성준",
    slot: "bonus",
    target: "self",
    stat: "agility",
    innerCost: 1,
    cooldown: 1,
    gainMovement: 2,
    statusSelf: { name: "기회", duration: 1 },
    tags: ["경공", "보법"],
    desc: "한 턴 동안 이동 거리를 늘리고 다음 공격 또는 심리전 판정에 기회를 얻는다.",
    cue: "Park_MoonStep"
  },
  parkCommand: {
    id: "parkCommand",
    name: "문주호령",
    ownerHint: "박성준",
    slot: "main",
    target: "allyAll",
    stat: "charm",
    uses: 1,
    moraleHeal: 10,
    statusTeam: { name: "기회", duration: 1 },
    tags: ["지휘", "문파"],
    desc: "아군 전체 기세를 회복하고 1턴간 기회를 부여한다. 1전투 1회.",
    cue: "Park_Command"
  },

  yunMoonReturn: {
    id: "yunMoonReturn",
    name: "월하반조검",
    ownerHint: "윤서화",
    slot: "main",
    target: "enemy",
    range: 1,
    stat: "agility",
    attackBonus: 3,
    innerCost: 1,
    cooldown: 1,
    damage: "1d10",
    breakGain: 16,
    tags: ["예검", "반격검"],
    desc: "상대 초식을 거울처럼 되비추는 근접 검법. 파훼 누적량이 높다.",
    cue: "Yun_MoonReturn"
  },
  yunMeasureLine: {
    id: "yunMeasureLine",
    name: "검로재기",
    ownerHint: "윤서화",
    slot: "main",
    target: "enemy",
    range: 4,
    stat: "insight",
    uses: 3,
    social: true,
    fixedDc: 12,
    breakDamage: 24,
    statusTarget: { name: "간파", duration: 2 },
    tags: ["간파", "파훼"],
    desc: "피해 대신 상대 검로를 읽는다. 성공하면 파훼를 크게 누적하고 간파 상태를 건다.",
    cue: "Yun_MeasureLine"
  },
  yunEtiquetteCounter: {
    id: "yunEtiquetteCounter",
    name: "예검반격 예약",
    ownerHint: "윤서화",
    slot: "reaction",
    target: "self",
    stat: "insight",
    innerCost: 1,
    tags: ["반응", "검법"],
    desc: "근접 공격을 받으면 d20+통찰로 반격. 성공 시 피해를 절반으로 줄이고 공격자 파훼를 올린다.",
    reactionMode: "parry",
    cue: "Yun_CounterReady"
  },

  baekIcePalm: {
    id: "baekIcePalm",
    name: "빙백장",
    ownerHint: "백련",
    slot: "main",
    target: "enemy",
    range: 4,
    stat: "innerPower",
    attackBonus: 2,
    innerCost: 2,
    damage: "1d8",
    breakGain: 9,
    statusTarget: { name: "둔화", duration: 2 },
    tags: ["빙공", "장법"],
    desc: "물가/여울의 적에게 기회를 얻는다. 명중 시 둔화.",
    cue: "Baek_IcePalm"
  },
  baekSnowBreath: {
    id: "baekSnowBreath",
    name: "설화심법",
    ownerHint: "백련",
    slot: "main",
    target: "ally",
    range: 5,
    stat: "innerPower",
    innerCost: 1,
    heal: "1d8",
    cooldown: 1,
    tags: ["심법", "회복"],
    desc: "아군 하나의 체력을 회복하고 중독/화상 1개를 정화한다.",
    cue: "Baek_Heal"
  },
  baekFrostSeal: {
    id: "baekFrostSeal",
    name: "한설봉로",
    ownerHint: "백련",
    slot: "main",
    target: "node",
    range: 4,
    stat: "innerPower",
    innerCost: 2,
    uses: 2,
    tags: ["지형", "빙공"],
    desc: "선택 지점을 빙판으로 만들어 이동을 방해하고 물가 지형에서는 추가 파훼를 건다.",
    cue: "Baek_FrostSeal"
  },

  hanPoisonNeedle: {
    id: "hanPoisonNeedle",
    name: "비화독침",
    ownerHint: "한비연",
    slot: "main",
    target: "enemy",
    range: 6,
    stat: "agility",
    attackBonus: 3,
    innerCost: 1,
    uses: 5,
    damage: "1d6",
    breakGain: 6,
    statusTarget: { name: "중독", duration: 3 },
    tags: ["암기", "독공", "원거리"],
    desc: "긴 사거리 암기. 강엄폐 대상에게는 불리하지만 대나무숲/연막에서 기회를 얻는다.",
    cue: "Han_PoisonNeedle"
  },
  hanSmokeStep: {
    id: "hanSmokeStep",
    name: "흑립잠행",
    ownerHint: "한비연",
    slot: "bonus",
    target: "self",
    stat: "agility",
    innerCost: 1,
    cooldown: 1,
    gainMovement: 3,
    statusSelf: { name: "은신", duration: 1 },
    tags: ["잠행", "경공"],
    desc: "이동력을 늘리고 은신 상태가 된다. 다음 암기 공격에 기회.",
    cue: "Han_StealthStep"
  },
  hanNeedleRain: {
    id: "hanNeedleRain",
    name: "만천화우",
    ownerHint: "한비연",
    slot: "main",
    target: "node",
    range: 5,
    stat: "agility",
    innerCost: 2,
    uses: 1,
    areaDamage: "1d6",
    tags: ["암기", "범위", "1전투1회"],
    desc: "지점 주변에 암기를 뿌린다. 다수전에서 강력하지만 1전투 1회.",
    cue: "Han_NeedleRain"
  },

  doMountainPalm: {
    id: "doMountainPalm",
    name: "파산권",
    ownerHint: "도아린",
    slot: "main",
    target: "enemy",
    range: 1,
    stat: "strength",
    attackBonus: 2,
    innerCost: 1,
    damage: "1d10",
    breakGain: 13,
    push: 1,
    tags: ["권장", "밀치기"],
    desc: "근접 제압기. 벼랑/물가/다리에서 밀치기 연계가 좋다.",
    cue: "Do_MountainPalm"
  },
  doIronBody: {
    id: "doIronBody",
    name: "철산고",
    ownerHint: "도아린",
    slot: "main",
    target: "enemy",
    range: 1,
    stat: "strength",
    innerCost: 2,
    cooldown: 2,
    attackBonus: 1,
    damage: "2d6",
    breakGain: 18,
    push: 2,
    tags: ["권장", "돌파"],
    desc: "몸으로 들이받아 큰 파훼와 밀치기를 만든다. 엄폐물 뒤의 적을 끌어내기 좋다.",
    cue: "Do_IronBody"
  },
  doQinna: {
    id: "doQinna",
    name: "금나수",
    ownerHint: "도아린",
    slot: "main",
    target: "enemy",
    range: 1,
    stat: "strength",
    innerCost: 1,
    social: true,
    fixedDc: 13,
    breakDamage: 20,
    statusTarget: { name: "무장해제", duration: 1 },
    tags: ["제압", "비살상"],
    desc: "무기를 잡아 비틀어 제압한다. 피해는 낮지만 비살상 승리 조건을 크게 돕는다.",
    cue: "Do_Qinna"
  },

  enemyOrthodoxSword: {
    id: "enemyOrthodoxSword",
    name: "중원정도검",
    ownerHint: "정도맹",
    slot: "main",
    target: "enemy",
    range: 1,
    stat: "agility",
    attackBonus: 2,
    damage: "1d8",
    breakGain: 8,
    tags: ["중원검", "정파"],
    desc: "정도맹 표준 검술. 고지에서 명중 보너스.",
    cue: "Enemy_OrthodoxSword"
  },
  enemyCultureEdict: {
    id: "enemyCultureEdict",
    name: "문명교화령",
    ownerHint: "정도맹 사절",
    slot: "main",
    target: "enemy",
    range: 6,
    stat: "charm",
    social: true,
    fixedDc: 13,
    moraleDamage: 10,
    tags: ["사기압박", "사절"],
    desc: "중원식 예법과 언어를 강요해 기세를 깎는다. 통찰/정신이 높은 대상에게 약하다.",
    cue: "Enemy_Edict"
  },
  enemyPalm: {
    id: "enemyPalm",
    name: "복호장",
    ownerHint: "정도맹 장법가",
    slot: "main",
    target: "enemy",
    range: 1,
    stat: "strength",
    attackBonus: 1,
    damage: "1d10",
    breakGain: 10,
    push: 1,
    tags: ["장법", "밀치기"],
    desc: "밀치기 장법. 지형 낙하를 노린다.",
    cue: "Enemy_Palm"
  },
  enemyInkTaunt: {
    id: "enemyInkTaunt",
    name: "기록관 조롱",
    ownerHint: "기록관",
    slot: "main",
    target: "enemy",
    range: 6,
    stat: "charm",
    social: true,
    fixedDc: 12,
    moraleDamage: 8,
    tags: ["심리전", "기록관"],
    desc: "조선 무공을 변방 잡기로 깎아내려 사기를 낮춘다.",
    cue: "Enemy_InkTaunt"
  },
  enemyPiercingArrow: {
    id: "enemyPiercingArrow",
    name: "관통수전",
    ownerHint: "호위궁수",
    slot: "main",
    target: "enemy",
    range: 7,
    stat: "agility",
    attackBonus: 2,
    damage: "1d8",
    breakGain: 5,
    tags: ["원거리", "고지"],
    desc: "지붕과 벼랑에서 강한 원거리 공격.",
    cue: "Enemy_Arrow"
  }
};

const unitTemplates = [
  {
    id: "park",
    name: "박성준",
    faction: "ally",
    role: "해동문 문주 / 풍류검",
    assetSlot: "박성준\n캐릭터 에셋 자리",
    hp: 48,
    inner: 7,
    ac: 16,
    morale: 68,
    movement: 4,
    level: 3,
    approvalRisk: true,
    stats: { strength: 13, agility: 15, innerPower: 13, spirit: 12, insight: 12, charm: 18 },
    skills: ["parkGentlemanSword", "parkMoonCharm", "parkStep", "parkCommand"],
    position: "westGate"
  },
  {
    id: "yun",
    name: "윤서화",
    faction: "ally",
    role: "예검 반격수",
    assetSlot: "윤서화\n캐릭터 에셋 자리",
    hp: 38,
    inner: 8,
    ac: 17,
    morale: 70,
    movement: 5,
    level: 3,
    stats: { strength: 11, agility: 18, innerPower: 14, spirit: 14, insight: 17, charm: 11 },
    skills: ["yunMoonReturn", "yunMeasureLine", "yunEtiquetteCounter"],
    position: "moonGate"
  },
  {
    id: "baek",
    name: "백련",
    faction: "ally",
    role: "빙백심법 / 치유",
    assetSlot: "백련\n캐릭터 에셋 자리",
    hp: 34,
    inner: 10,
    ac: 14,
    morale: 64,
    movement: 4,
    level: 3,
    stats: { strength: 9, agility: 12, innerPower: 18, spirit: 16, insight: 14, charm: 13 },
    skills: ["baekIcePalm", "baekSnowBreath", "baekFrostSeal"],
    position: "northReeds"
  },
  {
    id: "han",
    name: "한비연",
    faction: "ally",
    role: "흑립방 암기 / 독공",
    assetSlot: "한비연\n캐릭터 에셋 자리",
    hp: 32,
    inner: 7,
    ac: 15,
    morale: 62,
    movement: 5,
    level: 3,
    stats: { strength: 10, agility: 19, innerPower: 13, spirit: 12, insight: 16, charm: 10 },
    skills: ["hanPoisonNeedle", "hanSmokeStep", "hanNeedleRain"],
    position: "wineTables"
  },
  {
    id: "do",
    name: "도아린",
    faction: "ally",
    role: "파산권 / 비살상 제압",
    assetSlot: "도아린\n캐릭터 에셋 자리",
    hp: 44,
    inner: 6,
    ac: 15,
    morale: 66,
    movement: 4,
    level: 3,
    stats: { strength: 18, agility: 12, innerPower: 14, spirit: 15, insight: 11, charm: 12 },
    skills: ["doMountainPalm", "doIronBody", "doQinna"],
    position: "shallowWater"
  },
  {
    id: "envoy",
    name: "정도맹 사절 주홍문",
    faction: "enemy",
    role: "문화 강요 사절 / 지휘",
    assetSlot: "주홍문\n적 에셋 자리",
    hp: 42,
    inner: 7,
    ac: 17,
    morale: 76,
    movement: 4,
    level: 3,
    stats: { strength: 12, agility: 16, innerPower: 14, spirit: 15, insight: 14, charm: 17 },
    skills: ["enemyOrthodoxSword", "enemyCultureEdict"],
    position: "eastFormation"
  },
  {
    id: "swordA",
    name: "청성검수 갑",
    faction: "enemy",
    role: "검진 전열",
    assetSlot: "청성검수\n적 에셋 자리",
    hp: 34,
    inner: 4,
    ac: 16,
    morale: 60,
    movement: 5,
    level: 2,
    stats: { strength: 13, agility: 15, innerPower: 11, spirit: 12, insight: 12, charm: 9 },
    skills: ["enemyOrthodoxSword"],
    position: "pavilionRoof"
  },
  {
    id: "swordB",
    name: "청성검수 을",
    faction: "enemy",
    role: "대나무숲 매복",
    assetSlot: "청성검수\n적 에셋 자리",
    hp: 34,
    inner: 4,
    ac: 16,
    morale: 60,
    movement: 5,
    level: 2,
    stats: { strength: 13, agility: 15, innerPower: 11, spirit: 12, insight: 12, charm: 9 },
    skills: ["enemyOrthodoxSword"],
    position: "bambooGrove"
  },
  {
    id: "palmEnemy",
    name: "복호장 고수",
    faction: "enemy",
    role: "밀치기 장법가",
    assetSlot: "복호장 고수\n적 에셋 자리",
    hp: 40,
    inner: 5,
    ac: 15,
    morale: 64,
    movement: 4,
    level: 2,
    stats: { strength: 17, agility: 12, innerPower: 13, spirit: 13, insight: 11, charm: 9 },
    skills: ["enemyPalm"],
    position: "altar"
  },
  {
    id: "scribe",
    name: "예법기록관",
    faction: "enemy",
    role: "사기 교란 / 기록 왜곡",
    assetSlot: "기록관\n적 에셋 자리",
    hp: 27,
    inner: 5,
    ac: 14,
    morale: 55,
    movement: 4,
    level: 2,
    stats: { strength: 9, agility: 12, innerPower: 12, spirit: 14, insight: 16, charm: 15 },
    skills: ["enemyInkTaunt", "enemyPiercingArrow"],
    position: "cliffEdge"
  }
];

const state = {
  seed: 20260607,
  rng: null,
  units: [],
  objects: [],
  turnOrder: [],
  currentIndex: 0,
  round: 1,
  selectedUnitId: null,
  selectedSkillId: null,
  mode: null,
  showNodes: false,
  busy: false,
  battleOver: false,
  log: []
};

const els = {};

document.addEventListener("DOMContentLoaded", () => {
  ["battleMap", "nodesLayer", "objectsLayer", "unitsLayer", "roundCounter", "activeName", "turnTrack", "selectedPanel", "actionBar", "terrainActions", "combatLog", "seedLabel", "restartButton", "gridToggle"].forEach((id) => {
    els[id] = document.getElementById(id);
  });

  els.restartButton.addEventListener("click", () => resetBattle(Date.now() % 100000000));
  els.gridToggle.addEventListener("click", () => {
    state.showNodes = !state.showNodes;
    els.gridToggle.setAttribute("aria-pressed", String(state.showNodes));
    render();
  });

  resetBattle(state.seed);
});

function resetBattle(seed) {
  state.seed = seed;
  state.rng = createRng(seed);
  state.units = unitTemplates.map(createUnit);
  state.objects = terrainObjects.map((obj) => ({ ...structuredClone(obj), used: false }));
  state.turnOrder = [];
  state.currentIndex = 0;
  state.round = 1;
  state.selectedUnitId = null;
  state.selectedSkillId = null;
  state.mode = null;
  state.busy = false;
  state.battleOver = false;
  state.log = [];
  rollInitiative();
  logEvent("System", `seed ${seed}. 압록강 폐사당 전투 개시.`, "system");
  logEvent("World", "정도맹 사절단은 조선 문파에게 중원식 언어와 예법을 따르라 강요한다. 박성준은 여성 고수들과 함께 이에 맞선다.", "narration");
  beginTurn();
  render();
}

function createUnit(template) {
  const unit = structuredClone(template);
  unit.maxHp = unit.hp;
  unit.maxInner = unit.inner;
  unit.maxMorale = 100;
  unit.breakGauge = 0;
  unit.prof = unit.level >= 3 ? 2 : 1;
  unit.defeated = false;
  unit.surrendered = false;
  unit.cooldowns = {};
  unit.uses = {};
  unit.statuses = [];
  unit.reactionMode = null;
  unit.actions = { main: true, bonus: true, reaction: true, movement: unit.movement };
  for (const skillId of unit.skills) {
    const skill = skills[skillId];
    if (skill.uses) unit.uses[skillId] = skill.uses;
  }
  return unit;
}

function createRng(seed) {
  let t = seed >>> 0;
  return function rng() {
    t += 0x6D2B79F5;
    let v = t;
    v = Math.imul(v ^ (v >>> 15), v | 1);
    v ^= v + Math.imul(v ^ (v >>> 7), v | 61);
    return ((v ^ (v >>> 14)) >>> 0) / 4294967296;
  };
}

function rollInitiative() {
  state.turnOrder = state.units.map((unit) => {
    const d20 = rollD20("normal");
    const terrain = getNode(unit.position);
    const high = terrain.elevation > 0 ? 1 : 0;
    unit.initiative = d20.total + mod(unit.stats.agility) + high;
    logEvent("Init", `${unit.name}: d20 ${d20.text} + 민첩 ${mod(unit.stats.agility)} + 지형 ${high} = ${unit.initiative}`, "system");
    return unit.id;
  }).sort((a, b) => getUnit(b).initiative - getUnit(a).initiative);
}

function beginTurn() {
  if (state.battleOver) return;
  const active = getActiveUnit();
  if (!active || active.defeated || active.surrendered) {
    advanceTurn();
    return;
  }
  active.actions = { main: true, bonus: true, reaction: true, movement: active.movement };
  active.reactionMode = null;
  tickStatuses(active);
  reduceCooldowns(active);
  applyStartHazards(active);
  state.selectedUnitId = active.id;
  state.selectedSkillId = null;
  state.mode = null;
  logEvent("Turn", `${active.name} 차례. 위치: ${getNode(active.position).name}.`, active.faction === "ally" ? "system" : "miss");
  render();

  if (active.faction === "enemy" && !state.battleOver) {
    state.busy = true;
    setTimeout(() => enemyTakeTurn(active), 520);
  }
}

function advanceTurn() {
  if (state.battleOver) return;
  state.selectedSkillId = null;
  state.mode = null;
  state.currentIndex += 1;
  if (state.currentIndex >= state.turnOrder.length) {
    state.currentIndex = 0;
    state.round += 1;
    tickObjects();
  }
  beginTurn();
}

function enemyTakeTurn(enemy) {
  if (state.battleOver || enemy.defeated || enemy.surrendered) return;
  const candidates = aliveUnits("ally").map((ally) => ({ ally, d: nodeDistance(enemy.position, ally.position) })).sort((a, b) => a.d - b.d);
  const target = chooseEnemyTarget(enemy, candidates);
  const usable = enemy.skills.map((id) => skills[id]).filter((skill) => canUseSkill(enemy, skill).ok && skill.target === "enemy");
  let skill = usable.find((s) => nodeDistance(enemy.position, target.position) <= s.range) || usable[0];

  if (!skill) {
    logEvent("AI", `${enemy.name} 사용할 무공이 없어 방어한다.`, "system");
    endActiveTurn();
    return;
  }

  if (nodeDistance(enemy.position, target.position) > skill.range) {
    const nearestNode = nearestNodeToward(enemy, target);
    if (nearestNode) moveUnit(enemy, nearestNode.id, true);
  }

  if (nodeDistance(enemy.position, target.position) <= skill.range && canUseSkill(enemy, skill).ok) {
    executeSkill(enemy, skill, target);
  } else {
    logEvent("AI", `${enemy.name}이 ${target.name}에게 접근하며 자세를 잡는다.`, "system");
  }
  state.busy = false;
  setTimeout(() => endActiveTurn(), 360);
}

function chooseEnemyTarget(enemy, candidates) {
  if (enemy.id === "envoy") {
    return getUnit("park") && !getUnit("park").defeated ? getUnit("park") : candidates[0].ally;
  }
  if (enemy.id === "scribe") {
    return candidates.map((c) => c.ally).sort((a, b) => b.morale - a.morale)[0];
  }
  return candidates[0].ally;
}

function nearestNodeToward(unit, target) {
  const occupied = new Set(state.units.filter((u) => !u.defeated && !u.surrendered && u.id !== unit.id).map((u) => u.position));
  return nodes
    .filter((node) => !occupied.has(node.id))
    .map((node) => ({ ...node, move: nodeDistance(unit.position, node.id), target: nodeDistance(node.id, target.position) }))
    .filter((node) => node.move <= unit.actions.movement && node.target < nodeDistance(unit.position, target.position))
    .sort((a, b) => a.target - b.target)[0];
}

function endActiveTurn() {
  const active = getActiveUnit();
  if (active) {
    active.actions.main = false;
    active.actions.bonus = false;
  }
  checkBattleEnd();
  if (!state.battleOver) advanceTurn();
}

function render() {
  els.battleMap.classList.toggle("show-nodes", state.showNodes || state.mode === "move" || state.mode === "target-node");
  renderNodes();
  renderObjects();
  renderUnits();
  renderTop();
  renderTurnTrack();
  renderSelectedPanel();
  renderActionBar();
  renderTerrainActions();
  renderLog();
}

function renderNodes() {
  els.nodesLayer.innerHTML = nodes.map((node) => {
    const classes = ["map-node"];
    if (state.mode === "move" && isReachable(getActiveUnit(), node)) classes.push("valid-move");
    if (state.mode === "target-node" && selectedSkill() && isNodeTargetable(getActiveUnit(), selectedSkill(), node.id)) classes.push("valid-target");
    return `<button class="${classes.join(" ")}" style="left:${node.x}%;top:${node.y}%" data-node-id="${node.id}" data-title="${escapeHtml(node.name)}" type="button" aria-label="${escapeHtml(node.name)}"></button>`;
  }).join("");

  els.nodesLayer.querySelectorAll(".map-node").forEach((btn) => {
    btn.addEventListener("click", () => handleNodeClick(btn.dataset.nodeId));
  });
}

function renderObjects() {
  els.objectsLayer.innerHTML = state.objects.map((obj) => {
    const node = getNode(obj.node);
    const active = getActiveUnit();
    const close = active && nodeDistance(active.position, obj.node) <= 2 && !obj.used;
    return `<button class="object-token ${obj.used ? "used" : ""} ${close ? "highlight" : ""}" style="left:${node.x}%;top:${node.y}%" data-object-id="${obj.id}" data-title="${escapeHtml(obj.name)}" type="button" aria-label="${escapeHtml(obj.name)}">${obj.icon}</button>`;
  }).join("");
  els.objectsLayer.querySelectorAll(".object-token").forEach((btn) => {
    btn.addEventListener("click", () => handleObjectClick(btn.dataset.objectId));
  });
}

function renderUnits() {
  const selectedSkillObj = selectedSkill();
  els.unitsLayer.innerHTML = state.units.map((unit) => {
    const node = getNode(unit.position);
    const active = getActiveUnit();
    const isActive = active && active.id === unit.id;
    const targetable = selectedSkillObj && isUnitTargetable(active, selectedSkillObj, unit);
    const classes = ["unit-card", unit.faction, isActive ? "active" : "", unit.defeated ? "defeated" : "", unit.surrendered ? "surrendered" : "", targetable ? "targetable" : ""].filter(Boolean).join(" ");
    return `<button class="${classes}" style="left:${node.x}%;top:${node.y}%" data-unit-id="${unit.id}" type="button" aria-label="${escapeHtml(unit.name)}">
      <div class="portrait-slot">${escapeHtml(unit.assetSlot).replace(/\n/g, "<br>")}</div>
      <div class="unit-name">${escapeHtml(unit.name)}</div>
      <div class="unit-role">${escapeHtml(unit.role)}</div>
      <div class="mini-bars">
        <div class="mini-bar hp"><span style="width:${pct(unit.hp, unit.maxHp)}%"></span></div>
        <div class="mini-bar inner"><span style="width:${pct(unit.inner, unit.maxInner)}%"></span></div>
        <div class="mini-bar break"><span style="width:${unit.breakGauge}%"></span></div>
      </div>
    </button>`;
  }).join("");
  els.unitsLayer.querySelectorAll(".unit-card").forEach((btn) => {
    btn.addEventListener("click", () => handleUnitClick(btn.dataset.unitId));
  });
}

function renderTop() {
  const active = getActiveUnit();
  els.roundCounter.textContent = String(state.round);
  els.activeName.textContent = active ? active.name : "-";
  els.seedLabel.textContent = `seed ${state.seed}`;
}

function renderTurnTrack() {
  els.turnTrack.innerHTML = state.turnOrder.map((id, index) => {
    const unit = getUnit(id);
    const dead = unit.defeated ? "전투불능" : unit.surrendered ? "항복" : `속도 ${unit.initiative}`;
    return `<li class="${index === state.currentIndex ? "current" : ""}">
      <span class="turn-dot ${unit.faction}"></span>
      <strong>${escapeHtml(unit.name)}</strong>
      <small>${dead}</small>
    </li>`;
  }).join("");
}

function renderSelectedPanel() {
  const unit = getUnit(state.selectedUnitId) || getActiveUnit();
  if (!unit) {
    els.selectedPanel.innerHTML = `<h2>선택 없음</h2><p class="selected-empty">캐릭터를 선택하면 상세 정보가 표시됩니다.</p>`;
    return;
  }
  const node = getNode(unit.position);
  els.selectedPanel.innerHTML = `<h2>선택 캐릭터</h2>
    <div class="selected-card">
      <div class="selected-head">
        <div class="portrait-slot">${escapeHtml(unit.assetSlot).replace(/\n/g, "<br>")}</div>
        <div class="selected-title"><strong>${escapeHtml(unit.name)}</strong><span>${escapeHtml(unit.role)}</span><span>${escapeHtml(node.name)} · ${terrainLabels[node.terrain]} · ${coverLabels[node.cover]}</span></div>
      </div>
      <div class="meters">
        ${meter("체력", unit.hp, unit.maxHp, "hp")}
        ${meter("내공", unit.inner, unit.maxInner, "inner")}
        ${meter("기세", unit.morale, 100, "morale")}
        ${meter("파훼", unit.breakGauge, 100, "break")}
      </div>
      <div class="stat-grid">
        ${Object.entries(unit.stats).map(([key, value]) => `<div class="stat-box"><span class="stat-label">${statLabels[key]}</span><strong>${value} / ${mod(value) >= 0 ? "+" : ""}${mod(value)}</strong></div>`).join("")}
      </div>
      <div class="status-list">${unit.statuses.length ? unit.statuses.map((s) => `<span class="status-chip">${escapeHtml(s.name)} ${s.duration || 1}턴</span>`).join("") : `<span class="status-chip">상태 이상 없음</span>`}</div>
    </div>`;
}

function renderActionBar() {
  const active = getActiveUnit();
  if (!active || active.faction !== "ally" || state.busy || state.battleOver) {
    els.actionBar.innerHTML = `<p class="selected-empty">${state.battleOver ? "전투 종료." : "적 턴 진행 중입니다."}</p>`;
    return;
  }

  const skillButtons = active.skills.map((skillId) => {
    const skill = skills[skillId];
    const can = canUseSkill(active, skill);
    const selected = state.selectedSkillId === skillId;
    return `<button class="action-button ${selected ? "selected" : ""} ${!can.ok ? "disabled" : ""}" type="button" data-skill-id="${skillId}" ${!can.ok ? "disabled" : ""}>
      <div class="action-title"><strong>${escapeHtml(skill.name)}</strong><span>${slotLabels[skill.slot]}</span></div>
      <div class="skill-meta-row">${skillTags(active, skill).map((tag) => `<span class="cost-pill">${escapeHtml(tag)}</span>`).join("")}</div>
      <p>${escapeHtml(skill.desc)}</p>
      ${!can.ok ? `<p>사용 불가: ${escapeHtml(can.reason)}</p>` : ""}
    </button>`;
  }).join("");

  els.actionBar.innerHTML = `
    <button class="action-button move-button ${state.mode === "move" ? "selected" : ""}" type="button" id="moveButton">
      <div class="action-title"><strong>경공 이동</strong><span>이동</span></div>
      <p>현재 이동력 ${active.actions.movement}. 전술 노드를 클릭해 이동한다. 실제 Unity에서는 그리드/타일맵을 숨기고 경로만 표시.</p>
    </button>
    ${skillButtons}
    <button class="end-button" type="button" id="endTurnButton">턴 종료</button>`;

  els.actionBar.querySelector("#moveButton").addEventListener("click", () => {
    state.mode = state.mode === "move" ? null : "move";
    state.selectedSkillId = null;
    render();
  });
  els.actionBar.querySelectorAll("[data-skill-id]").forEach((btn) => {
    btn.addEventListener("click", () => chooseSkill(btn.dataset.skillId));
  });
  els.actionBar.querySelector("#endTurnButton").addEventListener("click", () => endActiveTurn());
}

function renderTerrainActions() {
  const active = getActiveUnit();
  if (!active || active.faction !== "ally" || state.busy || state.battleOver) {
    els.terrainActions.innerHTML = `<p class="selected-empty">아군 턴에만 지형지물을 사용할 수 있습니다.</p>`;
    return;
  }
  const nearby = state.objects.map((obj) => ({ obj, d: nodeDistance(active.position, obj.node) })).filter(({ obj, d }) => d <= 2 || !obj.used).sort((a, b) => a.d - b.d).slice(0, 6);
  if (!nearby.length) {
    els.terrainActions.innerHTML = `<p class="selected-empty">주변에 상호작용할 지형지물이 없습니다.</p>`;
    return;
  }
  els.terrainActions.innerHTML = nearby.map(({ obj, d }) => {
    const can = canUseObject(active, obj, d);
    return `<button class="terrain-button ${!can.ok ? "disabled" : ""}" data-object-id="${obj.id}" type="button" ${!can.ok ? "disabled" : ""}>
      <strong>${obj.icon} ${escapeHtml(obj.name)} <small>${slotLabels[obj.slot]} · 거리 ${d}</small></strong>
      <span>${escapeHtml(obj.desc)}${can.ok ? "" : ` / 불가: ${escapeHtml(can.reason)}`}</span>
    </button>`;
  }).join("");
  els.terrainActions.querySelectorAll("[data-object-id]").forEach((btn) => btn.addEventListener("click", () => useTerrainObject(active, btn.dataset.objectId)));
}

function renderLog() {
  els.combatLog.innerHTML = state.log.slice(-80).map((entry) => `<li class="log-${entry.level}"><span class="log-type">${escapeHtml(entry.type)}</span><span class="log-text">${escapeHtml(entry.text)}</span></li>`).join("");
  els.combatLog.scrollTop = els.combatLog.scrollHeight;
}

function chooseSkill(skillId) {
  const active = getActiveUnit();
  const skill = skills[skillId];
  const can = canUseSkill(active, skill);
  if (!can.ok) return;
  state.selectedSkillId = skillId;
  state.mode = skill.target === "node" ? "target-node" : "target-unit";
  if (skill.target === "self" || skill.target === "allyAll") {
    executeSkill(active, skill, active);
    state.selectedSkillId = null;
    state.mode = null;
  }
  render();
}

function handleUnitClick(unitId) {
  const active = getActiveUnit();
  const unit = getUnit(unitId);
  if (!unit) return;
  if (state.mode === "target-unit" && selectedSkill() && active && isUnitTargetable(active, selectedSkill(), unit)) {
    executeSkill(active, selectedSkill(), unit);
    state.selectedSkillId = null;
    state.mode = null;
  } else {
    state.selectedUnitId = unit.id;
  }
  render();
}

function handleNodeClick(nodeId) {
  const active = getActiveUnit();
  if (!active || active.faction !== "ally" || state.busy) return;
  if (state.mode === "move") {
    if (isReachable(active, getNode(nodeId))) {
      moveUnit(active, nodeId, false);
      state.mode = null;
      render();
    }
    return;
  }
  if (state.mode === "target-node" && selectedSkill() && isNodeTargetable(active, selectedSkill(), nodeId)) {
    executeSkill(active, selectedSkill(), getNode(nodeId));
    state.selectedSkillId = null;
    state.mode = null;
    render();
  }
}

function handleObjectClick(objectId) {
  const active = getActiveUnit();
  if (!active || active.faction !== "ally" || state.busy) return;
  useTerrainObject(active, objectId);
}

function canUseSkill(unit, skill) {
  if (!unit || unit.defeated || unit.surrendered) return { ok: false, reason: "전투불능" };
  if (skill.slot === "main" && !unit.actions.main) return { ok: false, reason: "주행동 소모" };
  if (skill.slot === "bonus" && !unit.actions.bonus) return { ok: false, reason: "보조행동 소모" };
  if (skill.slot === "reaction" && !unit.actions.reaction) return { ok: false, reason: "반응 소모" };
  if ((skill.innerCost || 0) > unit.inner) return { ok: false, reason: "내공 부족" };
  if ((unit.cooldowns[skill.id] || 0) > 0) return { ok: false, reason: `쿨다운 ${unit.cooldowns[skill.id]}턴` };
  if (skill.uses && (unit.uses[skill.id] || 0) <= 0) return { ok: false, reason: "사용횟수 없음" };
  return { ok: true };
}

function isReachable(unit, node) {
  if (!unit || !node || unit.defeated || unit.surrendered) return false;
  if (state.units.some((other) => other.id !== unit.id && !other.defeated && !other.surrendered && other.position === node.id)) return false;
  return nodeDistance(unit.position, node.id) <= unit.actions.movement;
}

function isUnitTargetable(actor, skill, target) {
  if (!actor || !skill || !target || target.defeated || target.surrendered) return false;
  const d = nodeDistance(actor.position, target.position);
  if (d > skill.range && skill.target !== "allyAll") return false;
  if (skill.target === "enemy") return actor.faction !== target.faction;
  if (skill.target === "ally") return actor.faction === target.faction;
  if (skill.target === "self") return actor.id === target.id;
  return false;
}

function isNodeTargetable(actor, skill, nodeId) {
  if (!actor || !skill) return false;
  return nodeDistance(actor.position, nodeId) <= skill.range;
}

function moveUnit(unit, nodeId, silent) {
  const d = nodeDistance(unit.position, nodeId);
  unit.position = nodeId;
  unit.actions.movement = Math.max(0, unit.actions.movement - d);
  const node = getNode(nodeId);
  if (!silent) logEvent("Move", `${unit.name} → ${node.name}. 이동력 ${unit.actions.movement} 남음.`, "system");
  if (node.hazard === "slippery" && !hasStatus(unit, "은신")) {
    const check = rollCheck(unit, "agility", 12, getAdvantageMode(unit, null, node));
    if (!check.success) {
      upsertStatus(unit, { name: "넘어짐", duration: 1 });
      logEvent("Terrain", `${unit.name}이 젖은 돌바닥에서 미끄러짐: ${check.text} vs DC 12.`, "miss");
    }
  }
}

function executeSkill(actor, skill, target) {
  const can = canUseSkill(actor, skill);
  if (!can.ok) {
    logEvent("System", `${skill.name} 사용 불가: ${can.reason}.`, "miss");
    return;
  }

  spendAction(actor, skill);
  actor.inner = Math.max(0, actor.inner - (skill.innerCost || 0));
  if (skill.cooldown) actor.cooldowns[skill.id] = skill.cooldown + 1;
  if (skill.uses) actor.uses[skill.id] = Math.max(0, (actor.uses[skill.id] || 0) - 1);

  if (skill.reactionMode) {
    actor.reactionMode = skill.reactionMode;
    upsertStatus(actor, { name: "반격준비", duration: 1 });
    logEvent("Reaction", `${actor.name}이 ${skill.name}을 준비한다. 다음 근접 공격에 반응 가능.`, "system");
    mockNarration(actor, skill, actor, "ready");
    checkBattleEnd();
    return;
  }

  if (skill.target === "allyAll") {
    aliveUnits(actor.faction).forEach((ally) => {
      ally.morale = clamp(ally.morale + (skill.moraleHeal || 0), 0, 100);
      if (skill.statusTeam) upsertStatus(ally, structuredClone(skill.statusTeam));
    });
    logEvent("Skill", `${actor.name} ${skill.name}: 아군 전체 기세 +${skill.moraleHeal || 0}, 기회 1턴.`, "hit");
    mockNarration(actor, skill, actor, "support");
    checkBattleEnd();
    return;
  }

  if (skill.target === "self") {
    actor.actions.movement += skill.gainMovement || 0;
    if (skill.statusSelf) upsertStatus(actor, structuredClone(skill.statusSelf));
    logEvent("Skill", `${actor.name} ${skill.name}: 이동력 +${skill.gainMovement || 0}.`, "hit");
    mockNarration(actor, skill, actor, "support");
    checkBattleEnd();
    return;
  }

  if (skill.target === "ally" && skill.heal) {
    const healRoll = rollDice(skill.heal);
    const amount = healRoll.total + Math.max(0, mod(actor.stats[skill.stat]));
    target.hp = clamp(target.hp + amount, 0, target.maxHp);
    removeOneOf(target, ["중독", "화상"]);
    logEvent("Heal", `${actor.name} ${skill.name}: ${target.name} HP +${amount} (${healRoll.text}).`, "hit");
    mockNarration(actor, skill, target, "heal");
    checkBattleEnd();
    return;
  }

  if (skill.target === "node") {
    executeNodeSkill(actor, skill, target);
    checkBattleEnd();
    return;
  }

  if (skill.social) {
    executeSocialSkill(actor, skill, target);
    checkBattleEnd();
    return;
  }

  executeAttack(actor, skill, target);
  checkBattleEnd();
}

function executeAttack(actor, skill, target) {
  const targetNode = getNode(target.position);
  const actorNode = getNode(actor.position);
  const mode = getAdvantageMode(actor, target, targetNode, skill);
  const d20 = rollD20(mode);
  const statMod = mod(actor.stats[skill.stat]);
  const terrainBonus = terrainAttackBonus(actorNode, targetNode, skill);
  const statusBonus = statusAttackBonus(actor, target);
  const total = d20.total + statMod + actor.prof + (skill.attackBonus || 0) + terrainBonus + statusBonus;
  const ac = defenseValue(target, targetNode, skill);
  const hit = d20.nat === 20 || (d20.nat !== 1 && total >= ac);
  logEvent("Dice", `${actor.name} ${skill.name}: ${d20.text} + ${statLabels[skill.stat]} ${signed(statMod)} + 숙련 ${actor.prof} + 무공 ${skill.attackBonus || 0} + 지형/상태 ${signed(terrainBonus + statusBonus)} = ${total} vs 방어 ${ac}.`, hit ? "hit" : "miss");

  if (!hit) {
    actor.morale = clamp(actor.morale - 3, 0, 100);
    target.morale = clamp(target.morale + 2, 0, 100);
    logEvent("Miss", `${target.name}이 ${skill.name}을 피하거나 막았다.`, "miss");
    mockNarration(actor, skill, target, "miss");
    return;
  }

  const crit = d20.nat === 20;
  const damageRoll = rollDice(skill.damage, crit ? 2 : 1);
  let damage = Math.max(1, damageRoll.total + Math.max(0, statMod));
  const reactionResult = tryReaction(target, actor, skill, damage);
  damage = reactionResult.damage;
  target.hp = clamp(target.hp - damage, 0, target.maxHp);
  target.breakGauge = clamp(target.breakGauge + (skill.breakGain || 0) + (crit ? 12 : 0), 0, 100);
  target.morale = clamp(target.morale - (crit ? 11 : 5), 0, 100);
  actor.morale = clamp(actor.morale + (crit ? 8 : 4), 0, 100);

  if (skill.statusTarget && !target.defeated) upsertStatus(target, structuredClone(skill.statusTarget));
  if (skill.push && !target.defeated) pushTarget(actor, target, skill.push);
  if (target.breakGauge >= 100 && !hasStatus(target, "파훼")) {
    upsertStatus(target, { name: "파훼", duration: 2 });
    target.morale = clamp(target.morale - 14, 0, 100);
    logEvent("Break", `${target.name}의 초식이 완전히 읽혔다. 파훼 상태, 기세 -14.`, "hit");
  }
  logEvent("Hit", `${target.name} 피해 ${damage}${crit ? " / 대성공" : ""}. HP ${target.hp}/${target.maxHp}.`, "hit");
  if (target.hp <= 0) defeatUnit(target, actor);
  mockNarration(actor, skill, target, crit ? "critical" : "hit");
}

function executeSocialSkill(actor, skill, target) {
  const defenderStat = skill.dcMod ? target.stats[skill.dcMod] : target.stats.spirit;
  const dc = skill.fixedDc || (10 + mod(defenderStat) + Math.floor(target.level / 2));
  const targetNode = getNode(target.position);
  const mode = getAdvantageMode(actor, target, targetNode, skill);
  const check = rollCheck(actor, skill.stat, dc, mode);
  logEvent("Dice", `${actor.name} ${skill.name}: ${check.text} vs DC ${dc}.`, check.success ? "hit" : "miss");
  if (check.success) {
    if (skill.moraleDamage) {
      target.morale = clamp(target.morale - skill.moraleDamage, 0, 100);
      target.breakGauge = clamp(target.breakGauge + 8, 0, 100);
      logEvent("Social", `${target.name} 기세 -${skill.moraleDamage}, 파훼 +8.`, "hit");
    }
    if (skill.breakDamage) {
      target.breakGauge = clamp(target.breakGauge + skill.breakDamage, 0, 100);
      logEvent("Insight", `${target.name} 파훼 +${skill.breakDamage}.`, "hit");
    }
    if (skill.statusTarget) upsertStatus(target, structuredClone(skill.statusTarget));
    if (target.morale <= 15 && target.faction === "enemy") surrenderUnit(target, actor);
    mockNarration(actor, skill, target, "socialSuccess");
  } else {
    actor.morale = clamp(actor.morale - 5, 0, 100);
    if (actor.id === "park" && skill.id === "parkMoonCharm") {
      aliveUnits("ally").filter((u) => u.id !== "park").forEach((ally) => ally.morale = clamp(ally.morale - 2, 0, 100));
      logEvent("Risk", "박성준의 방자한 농담이 빗나가 동료들의 시선이 차가워졌다. 아군 기세 소폭 감소.", "miss");
    }
    mockNarration(actor, skill, target, "socialFail");
  }
}

function executeNodeSkill(actor, skill, node) {
  const check = rollCheck(actor, skill.stat, 12, getAdvantageMode(actor, null, node, skill));
  logEvent("Dice", `${actor.name} ${skill.name}: ${check.text} vs DC 12 at ${node.name}.`, check.success ? "hit" : "miss");
  const victims = unitsWithinNode(node.id, 1).filter((u) => u.faction !== actor.faction && !u.defeated && !u.surrendered);
  if (skill.id === "baekFrostSeal") {
    node.hazard = "frozen";
    node.cover = node.cover === "none" ? "light" : node.cover;
    victims.forEach((unit) => {
      unit.breakGauge = clamp(unit.breakGauge + (node.terrain === "water" ? 20 : 10), 0, 100);
      upsertStatus(unit, { name: "둔화", duration: 2 });
    });
    logEvent("Terrain", `${node.name}이 빙판으로 변했다. 적 ${victims.length}명 둔화.`, check.success ? "hit" : "system");
  } else if (skill.id === "hanNeedleRain") {
    victims.forEach((unit) => {
      const dmg = rollDice(skill.areaDamage).total + mod(actor.stats.agility);
      unit.hp = clamp(unit.hp - dmg, 0, unit.maxHp);
      upsertStatus(unit, { name: "중독", duration: 2 });
      unit.morale = clamp(unit.morale - 5, 0, 100);
      logEvent("Area", `${unit.name} 만천화우 피해 ${dmg}, 중독.`, "hit");
      if (unit.hp <= 0) defeatUnit(unit, actor);
    });
  }
  mockNarration(actor, skill, actor, "terrain");
}

function canUseObject(actor, obj, d = nodeDistance(actor.position, obj.node)) {
  if (obj.used) return { ok: false, reason: "이미 사용됨" };
  if (d > 2) return { ok: false, reason: "거리 2 밖" };
  if (obj.slot === "main" && !actor.actions.main) return { ok: false, reason: "주행동 소모" };
  if (obj.slot === "bonus" && !actor.actions.bonus) return { ok: false, reason: "보조행동 소모" };
  return { ok: true };
}

function useTerrainObject(actor, objectId) {
  const obj = state.objects.find((o) => o.id === objectId);
  if (!obj || state.busy || state.battleOver) return;
  const d = nodeDistance(actor.position, obj.node);
  const can = canUseObject(actor, obj, d);
  if (!can.ok) {
    logEvent("Terrain", `${obj.name} 사용 불가: ${can.reason}.`, "miss");
    return;
  }
  if (obj.slot === "main") actor.actions.main = false;
  if (obj.slot === "bonus") actor.actions.bonus = false;
  obj.used = true;
  const check = rollCheck(actor, obj.stat, obj.dc, getAdvantageMode(actor, null, getNode(obj.node)));
  logEvent("Dice", `${actor.name} ${obj.name} 활용: ${check.text} vs DC ${obj.dc}.`, check.success ? "hit" : "miss");
  applyObjectEffect(actor, obj, check.success);
  mockNarration(actor, { name: obj.name, cue: `Terrain_${obj.kind}` }, actor, "terrain");
  checkBattleEnd();
  render();
}

function applyObjectEffect(actor, obj, success) {
  const node = getNode(obj.node);
  if (obj.kind === "table") {
    node.cover = "heavy";
    const enemies = unitsWithinNode(obj.node, 1).filter((u) => u.faction !== actor.faction);
    enemies.forEach((unit) => {
      if (success) {
        upsertStatus(unit, { name: "넘어짐", duration: 1 });
        unit.morale = clamp(unit.morale - 5, 0, 100);
        logEvent("Terrain", `${unit.name}이 술상에 걸려 넘어짐.`, "hit");
      }
    });
    logEvent("Terrain", `${node.name}에 강엄폐가 생겼다.`, "system");
  }
  if (obj.kind === "incense") {
    node.cover = "heavy";
    node.hazard = "smoke";
    aliveUnits().filter((u) => nodeDistance(u.position, node.id) <= 1).forEach((unit) => upsertStatus(unit, { name: "연막", duration: 2 }));
    logEvent("Terrain", "연막이 퍼져 강엄폐와 은신 연계를 만든다.", "system");
  }
  if (obj.kind === "bamboo") {
    node.cover = success ? "none" : "heavy";
    const enemies = unitsWithinNode(obj.node, 1).filter((u) => u.faction !== actor.faction);
    enemies.forEach((unit) => {
      if (success) {
        unit.hp = clamp(unit.hp - rollDice("1d6").total, 0, unit.maxHp);
        upsertStatus(unit, { name: "넘어짐", duration: 1 });
        logEvent("Terrain", `${unit.name}이 쓰러지는 대나무에 휘말림.`, "hit");
        if (unit.hp <= 0) defeatUnit(unit, actor);
      }
    });
  }
  if (obj.kind === "lantern") {
    node.hazard = "burning";
    node.cover = "none";
    unitsWithinNode(obj.node, 1).forEach((unit) => {
      const damage = success ? rollDice("1d6").total : 1;
      unit.hp = clamp(unit.hp - damage, 0, unit.maxHp);
      upsertStatus(unit, { name: "화상", duration: 2 });
      logEvent("Terrain", `${unit.name} 화염 피해 ${damage}, 화상.`, "hit");
      if (unit.hp <= 0) defeatUnit(unit, actor);
    });
  }
  if (obj.kind === "bridge") {
    node.hazard = "brokenBridge";
    node.terrain = "water";
    unitsWithinNode(obj.node, 0).forEach((unit) => {
      unit.position = "shallowWater";
      upsertStatus(unit, { name: "넘어짐", duration: 1 });
      logEvent("Terrain", `${unit.name}이 다리와 함께 여울로 추락했다.`, "hit");
    });
  }
  if (obj.kind === "altar") {
    unitsWithinNode(obj.node, 1).filter((u) => u.faction !== actor.faction).forEach((unit) => {
      const damage = success ? rollDice("1d8").total + mod(actor.stats.innerPower) : 2;
      unit.hp = clamp(unit.hp - damage, 0, unit.maxHp);
      unit.breakGauge = clamp(unit.breakGauge + 18, 0, 100);
      logEvent("Terrain", `${unit.name} 제단 파편 피해 ${damage}, 파훼 +18.`, "hit");
      if (unit.hp <= 0) defeatUnit(unit, actor);
    });
  }
}

function spendAction(actor, skill) {
  if (skill.slot === "main") actor.actions.main = false;
  if (skill.slot === "bonus") actor.actions.bonus = false;
  if (skill.slot === "reaction") actor.actions.reaction = false;
}

function tryReaction(target, attacker, skill, damage) {
  const melee = nodeDistance(target.position, attacker.position) <= 1;
  if (!melee || !target.actions.reaction || target.defeated || target.surrendered) return { damage };
  if (!(target.reactionMode === "parry" || hasStatus(target, "반격준비"))) return { damage };
  target.actions.reaction = false;
  target.reactionMode = null;
  removeStatus(target, "반격준비");
  const check = rollCheck(target, "insight", 14, getAdvantageMode(target, attacker, getNode(attacker.position)));
  if (check.success) {
    const reduced = Math.ceil(damage / 2);
    attacker.breakGauge = clamp(attacker.breakGauge + 12, 0, 100);
    attacker.morale = clamp(attacker.morale - 4, 0, 100);
    logEvent("Reaction", `${target.name} 예검반격 성공: ${check.text}. 피해 ${damage - reduced} 감소, ${attacker.name} 파훼 +12.`, "hit");
    mockNarration(target, { name: "예검반격", cue: "Reaction_Parry" }, attacker, "reaction");
    return { damage: reduced };
  }
  logEvent("Reaction", `${target.name} 반응 실패: ${check.text}.`, "miss");
  return { damage };
}

function terrainAttackBonus(actorNode, targetNode, skill) {
  let bonus = 0;
  if (actorNode.elevation > targetNode.elevation) bonus += 2;
  if (targetNode.terrain === "water" && skill.tags?.includes("빙공")) bonus += 2;
  if ((actorNode.terrain === "bamboo" || hasStatus(getActiveUnit(), "은신")) && skill.tags?.includes("암기")) bonus += 2;
  if (targetNode.hazard === "burning" && skill.tags?.includes("공포")) bonus += 1;
  return bonus;
}

function defenseValue(target, targetNode, skill) {
  let ac = target.ac;
  if (targetNode.cover === "light") ac += 1;
  if (targetNode.cover === "heavy") ac += skill.tags?.includes("근접") ? 1 : 3;
  if (hasStatus(target, "넘어짐")) ac -= 2;
  if (hasStatus(target, "간파")) ac -= 1;
  if (hasStatus(target, "은신")) ac += 2;
  return ac;
}

function statusAttackBonus(actor, target) {
  let bonus = 0;
  if (hasStatus(actor, "기회")) bonus += 2;
  if (hasStatus(target, "파훼")) bonus += 3;
  if (hasStatus(target, "간파")) bonus += 1;
  if (hasStatus(actor, "넘어짐")) bonus -= 2;
  return bonus;
}

function getAdvantageMode(actor, target, targetNode, skill = null) {
  let adv = 0;
  if (actor && hasStatus(actor, "기회")) adv += 1;
  if (actor && hasStatus(actor, "은신") && skill?.tags?.includes("암기")) adv += 1;
  const actorNode = actor ? getNode(actor.position) : null;
  if (actorNode && targetNode && actorNode.elevation > targetNode.elevation) adv += 1;
  if (targetNode?.cover === "heavy" && skill?.target === "enemy" && !skill.tags?.includes("근접")) adv -= 1;
  if (actorNode?.hazard === "slippery" && !skill?.tags?.includes("경공")) adv -= 1;
  if (target && hasStatus(target, "파훼")) adv += 1;
  if (adv > 0) return "advantage";
  if (adv < 0) return "disadvantage";
  return "normal";
}

function pushTarget(actor, target, distance) {
  const from = getNode(actor.position);
  const targetNode = getNode(target.position);
  const candidates = nodes
    .filter((node) => !state.units.some((u) => u.id !== target.id && !u.defeated && !u.surrendered && u.position === node.id))
    .map((node) => ({ node, score: Math.hypot(node.x - from.x, node.y - from.y) - Math.hypot(targetNode.x - from.x, targetNode.y - from.y), d: nodeDistance(target.position, node.id) }))
    .filter((item) => item.d <= Math.max(1, distance + 1))
    .sort((a, b) => b.score - a.score)[0];
  if (!candidates) return;
  target.position = candidates.node.id;
  logEvent("Push", `${target.name}이 ${candidates.node.name}(으)로 밀려났다.`, "hit");
  if (candidates.node.hazard === "fall") {
    const fall = rollDice("1d8").total;
    target.hp = clamp(target.hp - fall, 0, target.maxHp);
    upsertStatus(target, { name: "넘어짐", duration: 1 });
    logEvent("Terrain", `${target.name} 벼랑 낙하 피해 ${fall}.`, "hit");
    if (target.hp <= 0) defeatUnit(target, actor);
  }
}

function applyStartHazards(unit) {
  const node = getNode(unit.position);
  if (node.hazard === "burning") {
    const damage = rollDice("1d4").total;
    unit.hp = clamp(unit.hp - damage, 0, unit.maxHp);
    upsertStatus(unit, { name: "화상", duration: 1 });
    logEvent("Hazard", `${unit.name} 화염 지대 피해 ${damage}.`, "miss");
    if (unit.hp <= 0) defeatUnit(unit, null);
  }
  if (node.hazard === "frozen") {
    unit.actions.movement = Math.max(1, unit.actions.movement - 2);
    logEvent("Hazard", `${unit.name} 빙판으로 이동력 -2.`, "system");
  }
}

function tickStatuses(unit) {
  for (const status of unit.statuses) status.duration -= 1;
  unit.statuses = unit.statuses.filter((status) => status.duration > 0);
  if (hasStatus(unit, "중독")) {
    const poison = 2;
    unit.hp = clamp(unit.hp - poison, 0, unit.maxHp);
    logEvent("Status", `${unit.name} 중독 피해 ${poison}.`, "miss");
    if (unit.hp <= 0) defeatUnit(unit, null);
  }
}

function reduceCooldowns(unit) {
  for (const [skillId, value] of Object.entries(unit.cooldowns)) {
    unit.cooldowns[skillId] = Math.max(0, value - 1);
  }
}

function tickObjects() {
  for (const node of nodes) {
    if (node.hazard === "smoke" && Math.random() < 0.33) node.hazard = null;
  }
}

function defeatUnit(unit, source) {
  if (unit.defeated) return;
  unit.defeated = true;
  unit.hp = 0;
  unit.morale = 0;
  logEvent("Defeat", `${unit.name} 전투불능${source ? ` (${source.name})` : ""}.`, unit.faction === "enemy" ? "hit" : "miss");
}

function surrenderUnit(unit, source) {
  if (unit.surrendered || unit.defeated) return;
  unit.surrendered = true;
  logEvent("Surrender", `${unit.name}이 무기를 내려놓았다${source ? ` (${source.name}의 기세)` : ""}.`, "hit");
}

function checkBattleEnd() {
  const alliesAlive = aliveUnits("ally").length;
  const enemiesAlive = aliveUnits("enemy").length;
  if (enemiesAlive === 0) {
    state.battleOver = true;
    logEvent("Victory", "조선 문파 연합 승리. 정도맹 사절단의 강압 명분이 무너졌다.", "hit");
    logEvent("AI Cue", "Gemini: 전투 후 소문, 동료 반응, 정도맹 후속 사절 대사 생성 요청 가능.", "narration");
  } else if (alliesAlive === 0) {
    state.battleOver = true;
    logEvent("Defeat", "조선 문파 연합 패배. 정도맹의 흡수 압박이 거세진다.", "miss");
  }
}

function rollD20(mode = "normal") {
  const a = 1 + Math.floor(state.rng() * 20);
  const b = 1 + Math.floor(state.rng() * 20);
  if (mode === "advantage") return { total: Math.max(a, b), nat: Math.max(a, b), text: `기회 d20(${a}, ${b})→${Math.max(a, b)}` };
  if (mode === "disadvantage") return { total: Math.min(a, b), nat: Math.min(a, b), text: `불리 d20(${a}, ${b})→${Math.min(a, b)}` };
  return { total: a, nat: a, text: `d20 ${a}` };
}

function rollDice(formula, multiplier = 1) {
  const match = String(formula).match(/(\d+)d(\d+)/i);
  if (!match) return { total: 0, text: formula };
  const count = Number(match[1]) * multiplier;
  const sides = Number(match[2]);
  const rolls = Array.from({ length: count }, () => 1 + Math.floor(state.rng() * sides));
  return { total: rolls.reduce((a, b) => a + b, 0), text: `${formula}${multiplier > 1 ? `x${multiplier}` : ""} [${rolls.join("+")}]` };
}

function rollCheck(unit, stat, dc, mode = "normal") {
  const d20 = rollD20(mode);
  const total = d20.total + mod(unit.stats[stat]) + unit.prof;
  return { success: d20.nat === 20 || (d20.nat !== 1 && total >= dc), total, text: `${d20.text} + ${statLabels[stat]} ${signed(mod(unit.stats[stat]))} + 숙련 ${unit.prof} = ${total}` };
}

function mockNarration(actor, skill, target, outcome) {
  const cue = skill.cue || "GenericCue";
  const targetName = target?.name || target?.name || "전장";
  const templates = {
    hit: `${actor.name}의 ${skill.name}이 전장의 흐름을 가른다. ${targetName}의 초식에 빈틈이 생겼다. cue=${cue}`,
    critical: `${actor.name}의 ${skill.name}이 완벽하게 꽂힌다. 검기와 먼지가 갈라지며 전장이 잠시 멎는다. cue=${cue}`,
    miss: `${actor.name}의 ${skill.name}이 빗나간다. ${targetName}은 한 끗 차이로 무공의 결을 비켜냈다. cue=${cue}`,
    support: `${actor.name}이 ${skill.name}으로 아군의 흐름을 바꾼다. cue=${cue}`,
    heal: `${actor.name}의 내공이 흰 서리처럼 번져 ${targetName}의 숨을 고르게 한다. cue=${cue}`,
    ready: `${actor.name}이 호흡을 가라앉히고 반응 초식을 준비한다. cue=${cue}`,
    terrain: `${actor.name}이 ${skill.name}을 전장의 지형과 엮어 초식의 일부로 삼는다. cue=${cue}`,
    reaction: `${actor.name}의 반응 초식이 번개처럼 끼어든다. cue=${cue}`,
    socialSuccess: `${actor.name}의 말이 칼보다 깊게 박힌다. ${targetName}의 기세가 흔들린다. cue=${cue}`,
    socialFail: `${actor.name}의 말이 역풍을 맞는다. 전장의 공기가 잠시 차가워진다. cue=${cue}`
  };
  logEvent("AI Mock", templates[outcome] || templates.hit, "narration");
}

function skillTags(unit, skill) {
  const tags = [slotLabels[skill.slot]];
  if (skill.innerCost) tags.push(`내공 ${skill.innerCost}`);
  if (skill.range !== undefined) tags.push(`사거리 ${skill.range}`);
  if (skill.cooldown) tags.push(`쿨 ${skill.cooldown}`);
  if (skill.uses) tags.push(`횟수 ${unit.uses[skill.id] ?? 0}/${skill.uses}`);
  if (skill.damage) tags.push(`피해 ${skill.damage}`);
  if (skill.heal) tags.push(`회복 ${skill.heal}`);
  return tags.concat(skill.tags || []);
}

function getActiveUnit() { return getUnit(state.turnOrder[state.currentIndex]); }
function getUnit(id) { return state.units.find((unit) => unit.id === id); }
function selectedSkill() { return skills[state.selectedSkillId]; }
function getNode(id) { return nodes.find((node) => node.id === id); }
function aliveUnits(faction = null) { return state.units.filter((unit) => !unit.defeated && !unit.surrendered && (!faction || unit.faction === faction)); }
function unitsWithinNode(nodeId, radius) { return state.units.filter((unit) => !unit.defeated && !unit.surrendered && nodeDistance(unit.position, nodeId) <= radius); }
function nodeDistance(aId, bId) {
  const a = typeof aId === "string" ? getNode(aId) : aId;
  const b = typeof bId === "string" ? getNode(bId) : bId;
  return Math.max(0, Math.ceil(Math.hypot(a.x - b.x, a.y - b.y) / 13));
}
function mod(value) { return Math.floor((value - 10) / 2); }
function signed(value) { return value >= 0 ? `+${value}` : `${value}`; }
function clamp(value, min, max) { return Math.max(min, Math.min(max, value)); }
function pct(value, max) { return max <= 0 ? 0 : clamp(Math.round((value / max) * 100), 0, 100); }
function meter(label, value, max, cls) { return `<div class="meter"><span class="meter-label">${label}</span><div class="meter-track ${cls}"><span style="width:${pct(value, max)}%"></span></div><strong>${value}/${max}</strong></div>`; }
function escapeHtml(value) { return String(value).replace(/[&<>"]/g, (s) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;" }[s])); }
function logEvent(type, text, level = "system") { state.log.push({ type, text, level }); }
function hasStatus(unit, name) { return !!unit && unit.statuses.some((status) => status.name === name); }
function upsertStatus(unit, status) {
  const existing = unit.statuses.find((s) => s.name === status.name);
  if (existing) existing.duration = Math.max(existing.duration, status.duration || 1);
  else unit.statuses.push({ ...status, duration: status.duration || 1 });
}
function removeStatus(unit, name) { unit.statuses = unit.statuses.filter((s) => s.name !== name); }
function removeOneOf(unit, names) {
  for (const name of names) {
    if (hasStatus(unit, name)) { removeStatus(unit, name); return; }
  }
}
