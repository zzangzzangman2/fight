const GRID_W = 16;
const GRID_H = 9;

const statLabels = {
  strength: "근력",
  agility: "민첩",
  innerPower: "내공",
  spirit: "정신",
  insight: "통찰",
  charm: "매력"
};

const terrainDefs = {
  C: { id: "courtyard", label: "관문 앞마당", short: "마", moveCost: 1, defense: 0, avoid: 0, cover: "없음", height: 0, walkable: true, tags: [] },
  R: { id: "road", label: "산길", short: "길", moveCost: 1, defense: 0, avoid: 0, cover: "없음", height: 0, walkable: true, tags: ["road"] },
  B: { id: "bamboo", label: "설죽림", short: "죽", moveCost: 2, defense: 1, avoid: 2, cover: "약한 엄폐", height: 0, walkable: true, tags: ["bamboo", "cover"] },
  F: { id: "forest", label: "솔숲 풀밭", short: "숲", moveCost: 2, defense: 0, avoid: 2, cover: "약한 엄폐", height: 0, walkable: true, tags: ["cover", "stealth"] },
  W: { id: "water", label: "백두 계류", short: "수", moveCost: 3, defense: 0, avoid: -1, cover: "없음", height: 0, walkable: true, hazard: "slippery", tags: ["water"] },
  P: { id: "bridge", label: "돌다리", short: "교", moveCost: 1, defense: 0, avoid: 0, cover: "없음", height: 0, walkable: true, tags: ["bridge"] },
  H: { id: "roof", label: "너럭바위", short: "암", moveCost: 1, defense: 1, avoid: 1, cover: "약한 엄폐", height: 1, walkable: true, tags: ["high", "roof"] },
  V: { id: "cliff", label: "벼랑 바위", short: "벼", moveCost: 2, defense: 2, avoid: 0, cover: "약한 엄폐", height: 2, walkable: true, hazard: "fall", tags: ["high", "cliff"] },
  M: { id: "ruin", label: "깎아지른 암벽", short: "벽", moveCost: 99, defense: 3, avoid: 0, cover: "강한 엄폐", height: 1, walkable: false, tags: ["block", "ruin"] },
  S: { id: "shrine", label: "설문 제단", short: "단", moveCost: 1, defense: 1, avoid: 1, cover: "약한 엄폐", height: 1, walkable: true, tags: ["shrine"] },
  E: { id: "fire", label: "불붙은 잔해", short: "화", moveCost: 2, defense: 0, avoid: -1, cover: "없음", height: 0, walkable: true, hazard: "fire", tags: ["fire"] }
};

// 백두산 설문 관문전 — baekdu_snow_gate_srpg_ground.png(16:9) 일러스트의 지형을 16x9 그리드로 옮긴 레이아웃.
// 좌측 설죽림(B), 좌상단 설벽(M/V), 중앙 계류(W)와 돌다리(P), 상단 설문/제단(S), 우측 대강(W)과 벼랑(V).
const mapRows = [
  "MVVVCCCCCCSSSMMM",
  "VBBFCCCCCCCRRRMM",
  "BBBFFCCCCCCRVVVM",
  "BBBFFCCCRRRCVVVW",
  "BBWWWWWPCCCCVVWW",
  "BFFCCCCRWWWVVWWW",
  "FFCCRRRRCCVVWWWW",
  "FFCRRCCFFCVWWWWW",
  "CCRRCCCFFVVWWWWW"
];

const terrainProps = [
  { id: "incense", x: 10, y: 2, label: "제단 향로", icon: "煙", stat: "insight", dc: 12, kind: "smoke", desc: "향로를 뒤집어 2라운드 연막과 강엄폐를 만든다." },
  { id: "lanternNorth", x: 13, y: 4, label: "붉은 등불", icon: "燈", stat: "agility", dc: 12, kind: "fire", desc: "벼랑의 등불을 베어 주변에 화염 지대를 만든다." },
  { id: "bridgeRope", x: 7, y: 4, label: "다리 밧줄", icon: "索", stat: "strength", dc: 13, kind: "bridge", desc: "돌다리 밧줄을 끊어 통로를 제한하고 위의 적을 계류로 떨어뜨린다." },
  { id: "wineCart", x: 9, y: 3, label: "술수레", icon: "車", stat: "strength", dc: 11, kind: "cart", desc: "술수레를 밀어 강엄폐를 만들고 인접 적을 노출시킨다." },
  { id: "bambooTrap", x: 1, y: 3, label: "휘어진 대나무", icon: "竹", stat: "agility", dc: 13, kind: "bambooTrap", desc: "설죽을 튕겨 지나가는 적을 넘어뜨릴 덫을 만든다." },
  { id: "shrineBell", x: 11, y: 0, label: "설문 종", icon: "鐘", stat: "spirit", dc: 12, kind: "bell", desc: "관문 종을 울려 조선 문파의 기세를 끌어올린다." }
];

const skills = {
  parkGentlemanSword: {
    id: "parkGentlemanSword", name: "사군자검", icon: "劍", type: "attack", rangeMin: 1, rangeMax: 1,
    stat: "agility", hitBonus: 2, damage: "1d8", breakGain: 8, innerCost: 0, uses: null, cooldown: 0,
    tags: ["검법", "근접"], canCounter: true, desc: "박성준 기본 검법. 안정적인 명중과 파훼 누적."
  },
  parkMoonCharm: {
    id: "parkMoonCharm", name: "농월풍류", icon: "月", type: "social", rangeMin: 1, rangeMax: 3,
    stat: "charm", dcStat: "spirit", moraleDamage: 12, breakGain: 5, innerCost: 0, uses: 2, cooldown: 1,
    tags: ["심리전", "도발"], canCounter: false, desc: "풍류와 허세로 상대의 기세를 꺾는다. 실패하면 박성준 기세가 깎인다."
  },
  parkCommand: {
    id: "parkCommand", name: "문주호령", icon: "令", type: "rally", rangeMin: 0, rangeMax: 5,
    stat: "charm", innerCost: 1, uses: 1, cooldown: 0, moraleHeal: 10,
    tags: ["지휘", "문파"], canCounter: false, desc: "아군 전체 기세 +10, 1전투 1회."
  },
  parkStep: {
    id: "parkStep", name: "태평농월보", icon: "步", type: "self", rangeMin: 0, rangeMax: 0,
    stat: "agility", innerCost: 1, uses: 2, cooldown: 1, moveBuff: 2, status: { name: "경공", duration: 1, avoid: 2 },
    tags: ["경공"], canCounter: false, desc: "이번 페이즈 이동 +2, 회피 +2. 행동 종료 전 사용 가능."
  },

  yunMoonReturn: {
    id: "yunMoonReturn", name: "월하반조검", icon: "月", type: "attack", rangeMin: 1, rangeMax: 1,
    stat: "agility", hitBonus: 3, damage: "1d10", breakGain: 16, innerCost: 1, uses: 4, cooldown: 0,
    tags: ["예검", "검법"], canCounter: true, desc: "윤서화의 주력 검법. 파훼 누적이 높고 반격에도 좋다."
  },
  yunMeasureLine: {
    id: "yunMeasureLine", name: "검로재기", icon: "看", type: "debuff", rangeMin: 1, rangeMax: 3,
    stat: "insight", innerCost: 0, uses: 3, cooldown: 0, fixedDc: 12, breakGain: 28,
    statusTarget: { name: "간파", duration: 2, defense: -2 }, tags: ["간파", "파훼"], canCounter: false, desc: "피해 없이 상대 검로를 읽어 파훼를 크게 누적한다."
  },
  yunCounter: {
    id: "yunCounter", name: "예검반격", icon: "反", type: "attack", rangeMin: 1, rangeMax: 1,
    stat: "insight", hitBonus: 2, damage: "1d6", breakGain: 14, innerCost: 0, uses: null, cooldown: 0,
    tags: ["반격", "검법"], canCounter: true, reactionOnly: true, desc: "반격용 검법. 근접 공격을 받으면 자동 후보가 된다."
  },

  baekIcePalm: {
    id: "baekIcePalm", name: "빙백장", icon: "氷", type: "attack", rangeMin: 1, rangeMax: 2,
    stat: "innerPower", hitBonus: 2, damage: "1d8", breakGain: 9, innerCost: 1, uses: 5, cooldown: 0,
    statusTarget: { name: "둔화", duration: 1, move: -2 }, tags: ["빙공", "장법"], canCounter: true, desc: "1~2칸 빙공. 물가 대상에게 기회를 얻는다."
  },
  baekHeal: {
    id: "baekHeal", name: "설화심법", icon: "息", type: "heal", rangeMin: 1, rangeMax: 3,
    stat: "innerPower", heal: "1d8", innerCost: 1, uses: 4, cooldown: 0,
    tags: ["회복", "심법"], canCounter: false, desc: "아군 회복. 중독/화상 1개 제거."
  },
  baekFreezeFord: {
    id: "baekFreezeFord", name: "한설빙로", icon: "雪", type: "terrainSkill", rangeMin: 1, rangeMax: 3,
    stat: "innerPower", innerCost: 2, uses: 2, cooldown: 1, terrainEffect: "freeze",
    tags: ["지형", "빙공"], canCounter: false, desc: "물가 타일을 얼려 이동비용 1, 회피 +1의 얼음길로 만든다."
  },

  hanPoisonNeedle: {
    id: "hanPoisonNeedle", name: "비화독침", icon: "毒", type: "attack", rangeMin: 2, rangeMax: 4,
    stat: "agility", hitBonus: 3, damage: "1d6", breakGain: 6, innerCost: 1, uses: 5, cooldown: 0,
    statusTarget: { name: "중독", duration: 3 }, tags: ["암기", "독공", "원거리"], canCounter: true, desc: "2~4칸 암기. 대나무숲/갈대숲에서 명중 보너스."
  },
  hanStealth: {
    id: "hanStealth", name: "흑립잠행", icon: "隱", type: "self", rangeMin: 0, rangeMax: 0,
    stat: "agility", innerCost: 1, uses: 3, cooldown: 1, moveBuff: 2, status: { name: "은신", duration: 1, avoid: 3, hit: 2 },
    tags: ["잠행", "경공"], canCounter: false, desc: "이동 +2, 다음 공격 명중 +2, 회피 +3."
  },
  hanNeedleRain: {
    id: "hanNeedleRain", name: "만천화우", icon: "雨", type: "aoe", rangeMin: 2, rangeMax: 4,
    stat: "agility", hitBonus: 1, damage: "1d6", breakGain: 8, innerCost: 2, uses: 1, cooldown: 0, radius: 1,
    tags: ["암기", "범위"], canCounter: false, desc: "지점 주변 1칸 범위 암기. 1전투 1회."
  },

  arinMountainPalm: {
    id: "arinMountainPalm", name: "파산권", icon: "拳", type: "attack", rangeMin: 1, rangeMax: 1,
    stat: "strength", hitBonus: 2, damage: "1d10", breakGain: 12, innerCost: 1, uses: 5, cooldown: 0, push: 1,
    tags: ["권법", "밀치기"], canCounter: true, desc: "근접 권법. 명중 시 1칸 밀치기."
  },
  arinIronShoulder: {
    id: "arinIronShoulder", name: "철산고", icon: "撞", type: "attack", rangeMin: 1, rangeMax: 1,
    stat: "strength", hitBonus: 1, damage: "2d6", breakGain: 18, innerCost: 2, uses: 3, cooldown: 1, push: 2,
    tags: ["돌파", "밀치기"], canCounter: true, desc: "큰 파훼와 2칸 밀치기. 다리/벼랑에서 강력."
  },
  arinQinna: {
    id: "arinQinna", name: "금나수", icon: "擒", type: "debuff", rangeMin: 1, rangeMax: 1,
    stat: "strength", innerCost: 1, uses: 3, cooldown: 0, fixedDc: 13, breakGain: 20,
    statusTarget: { name: "무장해제", duration: 1, defense: -1 }, tags: ["제압", "비살상"], canCounter: false, desc: "무기를 비틀어 비살상 제압을 돕는다."
  },

  enemyOrthodoxSword: {
    id: "enemyOrthodoxSword", name: "중원정도검", icon: "正", type: "attack", rangeMin: 1, rangeMax: 1,
    stat: "agility", hitBonus: 2, damage: "1d8", breakGain: 8, innerCost: 0, uses: null, cooldown: 0,
    tags: ["중원검", "근접"], canCounter: true, desc: "정도맹 표준 검술."
  },
  enemyEdict: {
    id: "enemyEdict", name: "문명교화령", icon: "令", type: "social", rangeMin: 1, rangeMax: 3,
    stat: "charm", dcStat: "spirit", moraleDamage: 10, breakGain: 4, innerCost: 0, uses: 2, cooldown: 1,
    tags: ["사기압박"], canCounter: false, desc: "중원식 예법과 언어를 강요해 기세를 깎는다."
  },
  enemyPalm: {
    id: "enemyPalm", name: "복호장", icon: "掌", type: "attack", rangeMin: 1, rangeMax: 1,
    stat: "strength", hitBonus: 1, damage: "1d10", breakGain: 10, innerCost: 0, uses: null, cooldown: 0, push: 1,
    tags: ["장법", "밀치기"], canCounter: true, desc: "근접 장법."
  },
  enemyArrow: {
    id: "enemyArrow", name: "관통수전", icon: "弓", type: "attack", rangeMin: 2, rangeMax: 5,
    stat: "agility", hitBonus: 2, damage: "1d8", breakGain: 4, innerCost: 0, uses: 8, cooldown: 0,
    tags: ["궁술", "원거리"], canCounter: true, desc: "2~5칸 궁술. 근접 반격 불가."
  },
  enemyTaunt: {
    id: "enemyTaunt", name: "기록관 조롱", icon: "筆", type: "social", rangeMin: 1, rangeMax: 4,
    stat: "charm", dcStat: "spirit", moraleDamage: 8, breakGain: 3, innerCost: 0, uses: 3, cooldown: 0,
    tags: ["심리전"], canCounter: false, desc: "조선 무공을 변방 잡기로 깎아내린다."
  }
};

const appearances = {
  park: {
    hairStyle: "short", weapon: "sword", build: "standard",
    skin: "#f0c097", hair: "#2b2428", hairShade: "#171419",
    outfit: "#27334a", outfitLight: "#f5f0e6", accent: "#b34a39", lower: "#202638", shoe: "#17151a"
  },
  yun: {
    hairStyle: "long", weapon: "sword", build: "slim",
    skin: "#f2c7a4", hair: "#1f2029", hairShade: "#12141b",
    outfit: "#23283b", outfitLight: "#fff2ea", accent: "#9b3149", lower: "#1c2131", shoe: "#17151b"
  },
  baek: {
    hairStyle: "bob", weapon: "palm", build: "small",
    skin: "#f1c8aa", hair: "#d6c3ad", hairShade: "#b59d87",
    outfit: "#dce9ed", outfitLight: "#fff8f0", accent: "#5f93bd", lower: "#303a4b", shoe: "#1b2028"
  },
  han: {
    hairStyle: "pony", weapon: "needle", build: "slim",
    skin: "#e8b98d", hair: "#17191f", hairShade: "#0e1014",
    outfit: "#2b3330", outfitLight: "#e9dfcf", accent: "#80614b", lower: "#171b1d", shoe: "#101214"
  },
  arin: {
    hairStyle: "long", weapon: "fist", build: "sturdy",
    skin: "#e9b47f", hair: "#b87343", hairShade: "#805036",
    outfit: "#49383a", outfitLight: "#f0dfce", accent: "#d5943f", lower: "#2b2224", shoe: "#181316"
  },
  enemyOfficer: {
    hairStyle: "topknot", weapon: "sword", build: "standard",
    skin: "#d6a77d", hair: "#2d2523", hairShade: "#181211",
    outfit: "#6b2d2a", outfitLight: "#eee5d8", accent: "#c88a3d", lower: "#3a2421", shoe: "#17110f"
  },
  enemyScout: {
    hairStyle: "cap", weapon: "bow", build: "slim",
    skin: "#d2a07a", hair: "#2c2320", hairShade: "#17110f",
    outfit: "#5a3830", outfitLight: "#efe2d1", accent: "#8b5a36", lower: "#2f231f", shoe: "#16100e"
  }
};

const unitTemplates = [
  {
    id: "park", name: "박성준", short: "성준", faction: "ally", role: "해동문 문주 / 풍류검",
    hp: 48, inner: 7, guard: 16, movement: 5, level: 3, morale: 68,
    stats: { strength: 13, agility: 15, innerPower: 13, spirit: 12, insight: 12, charm: 18 },
    skills: ["parkGentlemanSword", "parkMoonCharm", "parkCommand", "parkStep"], appearance: appearances.park, x: 2, y: 8
  },
  {
    id: "yun", name: "윤서화", short: "서화", faction: "ally", role: "예검 반격수",
    hp: 38, inner: 8, guard: 17, movement: 6, level: 3, morale: 70,
    stats: { strength: 11, agility: 18, innerPower: 14, spirit: 14, insight: 17, charm: 11 },
    skills: ["yunMoonReturn", "yunMeasureLine", "yunCounter"], appearance: appearances.yun, x: 3, y: 8
  },
  {
    id: "baek", name: "백련", short: "백련", faction: "ally", role: "빙백심법 / 치유",
    hp: 34, inner: 10, guard: 14, movement: 5, level: 3, morale: 64,
    stats: { strength: 9, agility: 12, innerPower: 18, spirit: 16, insight: 14, charm: 13 },
    skills: ["baekIcePalm", "baekHeal", "baekFreezeFord"], appearance: appearances.baek, x: 1, y: 8
  },
  {
    id: "han", name: "한비연", short: "비연", faction: "ally", role: "흑립방 암기 / 독공",
    hp: 32, inner: 7, guard: 15, movement: 6, level: 3, morale: 62,
    stats: { strength: 10, agility: 19, innerPower: 13, spirit: 12, insight: 16, charm: 10 },
    skills: ["hanPoisonNeedle", "hanStealth", "hanNeedleRain"], appearance: appearances.han, x: 1, y: 7
  },
  {
    id: "arin", name: "도아린", short: "아린", faction: "ally", role: "파산권 / 비살상 제압",
    hp: 44, inner: 6, guard: 15, movement: 5, level: 3, morale: 66,
    stats: { strength: 18, agility: 12, innerPower: 14, spirit: 15, insight: 11, charm: 12 },
    skills: ["arinMountainPalm", "arinIronShoulder", "arinQinna"], appearance: appearances.arin, x: 4, y: 8
  },

  { id: "envoy", name: "사절 주홍문", short: "사절", faction: "enemy", role: "정도맹 문화강요 사절", hp: 44, inner: 6, guard: 17, movement: 5, level: 3, morale: 78, stats: { strength: 12, agility: 16, innerPower: 14, spirit: 15, insight: 14, charm: 17 }, skills: ["enemyOrthodoxSword", "enemyEdict"], appearance: appearances.enemyOfficer, x: 11, y: 1 },
  { id: "swordA", name: "청성검수 갑", short: "검갑", faction: "enemy", role: "검진 전열", hp: 32, inner: 3, guard: 15, movement: 5, level: 2, morale: 58, stats: { strength: 13, agility: 15, innerPower: 11, spirit: 12, insight: 12, charm: 9 }, skills: ["enemyOrthodoxSword"], appearance: appearances.enemyOfficer, x: 9, y: 1 },
  { id: "swordB", name: "청성검수 을", short: "검을", faction: "enemy", role: "검진 후열", hp: 32, inner: 3, guard: 15, movement: 5, level: 2, morale: 58, stats: { strength: 13, agility: 15, innerPower: 11, spirit: 12, insight: 12, charm: 9 }, skills: ["enemyOrthodoxSword"], appearance: appearances.enemyOfficer, x: 12, y: 1 },
  { id: "palm", name: "복호장 고수", short: "장법", faction: "enemy", role: "밀치기 장법가", hp: 40, inner: 4, guard: 15, movement: 5, level: 2, morale: 64, stats: { strength: 17, agility: 12, innerPower: 13, spirit: 13, insight: 11, charm: 9 }, skills: ["enemyPalm"], appearance: { ...appearances.enemyOfficer, weapon: "fist", build: "sturdy" }, x: 8, y: 3 },
  { id: "scribe", name: "예법기록관", short: "기록", faction: "enemy", role: "기세 교란", hp: 27, inner: 4, guard: 14, movement: 4, level: 2, morale: 55, stats: { strength: 9, agility: 12, innerPower: 12, spirit: 14, insight: 16, charm: 15 }, skills: ["enemyTaunt"], appearance: { ...appearances.enemyOfficer, weapon: "scroll", hairStyle: "cap" }, x: 10, y: 1 },
  { id: "archer", name: "사천궁수", short: "궁수", faction: "enemy", role: "고지 궁수", hp: 28, inner: 3, guard: 14, movement: 5, level: 2, morale: 55, stats: { strength: 10, agility: 16, innerPower: 10, spirit: 12, insight: 13, charm: 9 }, skills: ["enemyArrow"], appearance: appearances.enemyScout, x: 12, y: 2 },
  { id: "guardA", name: "정도맹 호위", short: "호위", faction: "enemy", role: "돌다리 봉쇄", hp: 30, inner: 3, guard: 15, movement: 4, level: 2, morale: 56, stats: { strength: 14, agility: 13, innerPower: 11, spirit: 12, insight: 11, charm: 8 }, skills: ["enemyOrthodoxSword"], appearance: appearances.enemyOfficer, x: 8, y: 4 },
  { id: "guardB", name: "정도맹 추격대", short: "추격", faction: "enemy", role: "강변 압박", hp: 30, inner: 3, guard: 15, movement: 5, level: 2, morale: 56, stats: { strength: 14, agility: 14, innerPower: 11, spirit: 12, insight: 11, charm: 8 }, skills: ["enemyOrthodoxSword"], appearance: appearances.enemyOfficer, x: 11, y: 4 }
];

const state = {
  seed: 20260607,
  rng: null,
  tiles: [],
  props: [],
  units: [],
  phase: "player",
  round: 1,
  selectedUnitId: null,
  selectedSkillId: null,
  selectedTile: null,
  hoverTile: null,
  mode: "select",
  reachable: new Map(),
  attackable: new Set(),
  interactable: new Set(),
  preMove: null,
  showDanger: false,
  battleOver: false,
  showIntro: true,
  outcome: null,
  resultDismissed: false,
  enemyQueue: [],
  log: []
};

const els = {};

document.addEventListener("DOMContentLoaded", () => {
  ["battlefield", "phaseLabel", "roundCounter", "phaseCard", "dangerToggle", "endPhaseButton", "restartButton", "briefingButton", "guidePanel", "phaseBanner", "introOverlay", "resultOverlay", "selectedPanel", "actionBar", "forecastPanel", "tilePanel", "allyRoster", "enemyRoster", "seedLabel", "combatLog"].forEach((id) => {
    els[id] = document.getElementById(id);
  });

  els.restartButton.addEventListener("click", () => resetBattle(Date.now() % 100000000));
  els.endPhaseButton.addEventListener("click", () => {
    if (state.phase === "player" && !state.battleOver) endPlayerPhase();
  });
  els.dangerToggle.addEventListener("click", () => {
    state.showDanger = !state.showDanger;
    els.dangerToggle.setAttribute("aria-pressed", String(state.showDanger));
    render();
  });
  els.briefingButton.addEventListener("click", () => {
    state.showIntro = true;
    render();
  });

  resetBattle(state.seed);
});

function resetBattle(seed) {
  state.seed = seed;
  state.rng = createRng(seed);
  state.tiles = createTiles();
  state.props = terrainProps.map((prop) => ({ ...structuredClone(prop), used: false }));
  state.units = unitTemplates.map(createUnit);
  state.phase = "player";
  state.round = 1;
  state.selectedUnitId = null;
  state.selectedSkillId = null;
  state.selectedTile = null;
  state.hoverTile = null;
  state.mode = "select";
  state.reachable = new Map();
  state.attackable = new Set();
  state.interactable = new Set();
  state.preMove = null;
  state.showDanger = false;
  state.battleOver = false;
  state.showIntro = true;
  state.outcome = null;
  state.resultDismissed = false;
  state.enemyQueue = [];
  state.log = [];
  els.dangerToggle.setAttribute("aria-pressed", "false");
  logEvent("System", `seed ${seed}. 백두산 설문 관문전 개시.`, "system");
  logEvent("World", "중원 감찰단이 백두산 설문(雪門)을 넘어 조선 문파에 중원식 말과 예법을 강요하려 한다. 해동문이 관문 앞을 막아선다.", "phase");
  startPlayerPhase();
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

function createTiles() {
  const tiles = [];
  for (let y = 0; y < GRID_H; y += 1) {
    for (let x = 0; x < GRID_W; x += 1) {
      const code = mapRows[y][x];
      const def = terrainDefs[code];
      tiles.push({ x, y, code, ...structuredClone(def), smoke: 0, frozen: 0, burning: code === "E" ? 99 : 0, trap: 0 });
    }
  }
  return tiles;
}

function createUnit(template) {
  const unit = structuredClone(template);
  unit.maxHp = unit.hp;
  unit.maxInner = unit.inner;
  unit.maxMorale = 100;
  unit.prof = unit.level >= 3 ? 2 : 1;
  unit.breakGauge = 0;
  unit.statuses = [];
  unit.cooldowns = {};
  unit.uses = {};
  unit.moved = false;
  unit.acted = false;
  unit.defeated = false;
  unit.surrendered = false;
  unit.position = { x: unit.x, y: unit.y };
  delete unit.x;
  delete unit.y;
  unit.skills.forEach((skillId) => {
    const skill = skills[skillId];
    if (Number.isInteger(skill.uses)) unit.uses[skillId] = skill.uses;
  });
  return unit;
}

function startPlayerPhase() {
  state.phase = "player";
  state.mode = "select";
  state.selectedSkillId = null;
  state.selectedUnitId = null;
  state.preMove = null;
  state.units.filter((unit) => unit.faction === "ally" && !unit.defeated && !unit.surrendered).forEach((unit) => {
    unit.acted = false;
    unit.moved = false;
    tickUnitStart(unit);
  });
  logEvent("Phase", `${state.round}라운드 아군 페이즈 시작. 원하는 아군을 클릭해 이동/공격하세요.`, "phase");
  showPhaseBanner(`아군 페이즈 · ${state.round}턴`, "player");
  render();
}

function endPlayerPhase() {
  if (state.battleOver) return;
  clearSelection();
  state.phase = "enemy";
  state.enemyQueue = livingUnits("enemy").slice();
  logEvent("Phase", `${state.round}라운드 적 페이즈 시작.`, "phase");
  showPhaseBanner("적 페이즈", "enemy");
  render();
  window.setTimeout(processEnemyQueue, 420);
}

function endEnemyPhase() {
  if (state.battleOver) return;
  state.round += 1;
  decayMapEffects();
  startPlayerPhase();
}

function tickUnitStart(unit) {
  Object.keys(unit.cooldowns).forEach((key) => {
    unit.cooldowns[key] = Math.max(0, unit.cooldowns[key] - 1);
  });

  unit.statuses = unit.statuses.filter((status) => {
    if (status.name === "중독") {
      const damage = rollDice("1d4").total;
      unit.hp = clamp(unit.hp - damage, 0, unit.maxHp);
      unit.morale = clamp(unit.morale - 3, 0, 100);
      logEvent("Status", `${unit.name} 중독 피해 ${damage}.`, "miss");
      if (unit.hp <= 0) defeatUnit(unit, null);
    }
    if (status.name === "화상") {
      const damage = rollDice("1d4").total;
      unit.hp = clamp(unit.hp - damage, 0, unit.maxHp);
      logEvent("Status", `${unit.name} 화상 피해 ${damage}.`, "miss");
      if (unit.hp <= 0) defeatUnit(unit, null);
    }
    status.duration -= 1;
    return status.duration > 0;
  });
}

function decayMapEffects() {
  state.tiles.forEach((tile) => {
    if (tile.smoke > 0) tile.smoke -= 1;
    if (tile.frozen > 0) {
      tile.frozen -= 1;
      if (tile.frozen === 0 && tile.code === "W") {
        Object.assign(tile, structuredClone(terrainDefs.W), { x: tile.x, y: tile.y, code: "W", smoke: tile.smoke, frozen: 0, burning: 0, trap: tile.trap });
      }
    }
    if (tile.burning > 0 && tile.burning < 99) {
      tile.burning -= 1;
      if (tile.burning === 0 && tile.code !== "E") {
        tile.hazard = null;
      }
    }
    if (tile.trap > 0) tile.trap -= 1;
  });
}

function processEnemyQueue() {
  if (state.battleOver) return;
  const enemy = state.enemyQueue.shift();
  if (!enemy) {
    endEnemyPhase();
    return;
  }
  if (enemy.defeated || enemy.surrendered) {
    window.setTimeout(processEnemyQueue, 160);
    return;
  }
  enemyTakeTurn(enemy);
  checkBattleEnd();
  render();
  window.setTimeout(processEnemyQueue, 650);
}

function enemyTakeTurn(enemy) {
  tickUnitStart(enemy);
  if (enemy.defeated) return;
  const allies = livingUnits("ally");
  if (!allies.length) return;

  const direct = chooseAttackFromPosition(enemy, allies);
  if (direct) {
    executeSkill(enemy, direct.skill, direct.target);
    return;
  }

  const movePlan = chooseEnemyMove(enemy, allies);
  if (movePlan) {
    const from = tileKey(enemy.position);
    enemy.position = { x: movePlan.tile.x, y: movePlan.tile.y };
    logEvent("AI", `${enemy.name} ${from} → ${tileKey(enemy.position)} 이동.`, "ai");
    applyTileEntry(enemy, getTile(enemy.position.x, enemy.position.y));
  }

  const afterMove = chooseAttackFromPosition(enemy, livingUnits("ally"));
  if (afterMove) {
    executeSkill(enemy, afterMove.skill, afterMove.target);
  } else {
    logEvent("AI", `${enemy.name}이 지형을 잡고 대기한다.`, "ai");
  }
}

function chooseAttackFromPosition(unit, targets) {
  const attackSkills = unit.skills.map((id) => skills[id]).filter((skill) => isHostileSkill(skill) && canPaySkill(unit, skill).ok && !skill.reactionOnly);
  const options = [];
  attackSkills.forEach((skill) => {
    targets.forEach((target) => {
      const dist = distance(unit.position, target.position);
      if (dist >= skill.rangeMin && dist <= skill.rangeMax) {
        options.push({ skill, target, score: target.hp + target.morale / 4 - target.breakGauge / 3 });
      }
    });
  });
  if (!options.length) return null;
  return options.sort((a, b) => a.score - b.score)[0];
}

function chooseEnemyMove(unit, targets) {
  const reach = computeReachable(unit, unit.movement);
  const attackSkills = unit.skills.map((id) => skills[id]).filter((skill) => isHostileSkill(skill) && canPaySkill(unit, skill).ok && !skill.reactionOnly);
  let best = null;
  reach.forEach((info, key) => {
    const tile = getTile(info.x, info.y);
    if (!tile || unitAt(tile.x, tile.y)) return;
    targets.forEach((target) => {
      attackSkills.forEach((skill) => {
        const dist = distance(tile, target.position);
        if (dist >= skill.rangeMin && dist <= skill.rangeMax) {
          const score = dist + info.cost * 0.2 - tile.defense * 0.3 - tile.avoid * 0.2 + target.hp * 0.01;
          if (!best || score < best.score) best = { tile, skill, target, score };
        }
      });
    });
  });
  if (best) return best;

  const nearest = targets.slice().sort((a, b) => distance(unit.position, a.position) - distance(unit.position, b.position))[0];
  let fallback = null;
  reach.forEach((info) => {
    const tile = getTile(info.x, info.y);
    if (!tile || unitAt(tile.x, tile.y)) return;
    const score = distance(tile, nearest.position) + info.cost * 0.08;
    if (!fallback || score < fallback.score) fallback = { tile, score };
  });
  return fallback;
}

function handleTileClick(tile) {
  if (state.battleOver || state.phase !== "player") return;
  state.selectedTile = tile;
  const unit = unitAt(tile.x, tile.y);

  if (state.mode === "move" && state.selectedUnitId) {
    moveSelectedUnit(tile);
    return;
  }

  if (state.mode === "target" && state.selectedUnitId && state.selectedSkillId) {
    handleTargetTile(tile, unit);
    return;
  }

  if (state.mode === "interact" && state.selectedUnitId) {
    handleInteractTile(tile);
    return;
  }

  if (unit) {
    if (unit.faction === "ally" && !unit.acted && !unit.defeated) {
      selectUnit(unit.id);
    } else {
      inspectTile(tile);
    }
  } else {
    inspectTile(tile);
  }
}

function selectUnit(unitId) {
  const unit = getUnit(unitId);
  if (!unit || unit.faction !== "ally" || unit.acted || unit.defeated) return;
  state.selectedUnitId = unit.id;
  state.selectedSkillId = null;
  state.selectedTile = getTile(unit.position.x, unit.position.y);
  state.mode = unit.moved ? "command" : "move";
  state.preMove = null;
  state.reachable = unit.moved ? new Map() : computeReachable(unit, effectiveMovement(unit));
  state.attackable = computeAttackableFrom(unit.position, unit);
  state.interactable = computeInteractable(unit);
  logEvent("Select", `${unit.name} 선택. 이동 가능한 칸이 파랗게 표시됩니다.`, "system");
  render();
}

function inspectTile(tile) {
  state.selectedTile = tile;
  render();
}

function moveSelectedUnit(tile) {
  const unit = selectedUnit();
  if (!unit || unit.acted) return;
  if (unit.moved) {
    logEvent("Move", "이미 이동한 유닛입니다. 공격/지형/대기만 선택할 수 있습니다.", "miss");
    state.mode = "command";
    state.reachable = new Map();
    render();
    return;
  }
  const key = tileKey(tile);
  if (!state.reachable.has(key)) {
    logEvent("Move", "이동 범위 밖입니다.", "miss");
    render();
    return;
  }
  if (unitAt(tile.x, tile.y)) {
    logEvent("Move", "이미 유닛이 있는 칸입니다.", "miss");
    render();
    return;
  }
  state.preMove = { x: unit.position.x, y: unit.position.y };
  unit.position = { x: tile.x, y: tile.y };
  unit.moved = true;
  state.mode = "command";
  state.reachable = new Map();
  state.attackable = computeAttackableFrom(unit.position, unit);
  state.interactable = computeInteractable(unit);
  applyTileEntry(unit, tile);
  logEvent("Move", `${unit.name} 이동: ${tileKey(state.preMove)} → ${tileKey(unit.position)}.`, "system");
  render();
}

function handleTargetTile(tile, unit) {
  const actor = selectedUnit();
  const skill = selectedSkill();
  if (!actor || !skill) return;

  if (skill.type === "terrainSkill") {
    if (!state.attackable.has(tileKey(tile))) {
      logEvent("Skill", `${skill.name} 대상 위치가 사거리 밖입니다.`, "miss");
      render();
      return;
    }
    executeSkill(actor, skill, tile);
    finishUnitAction(actor);
    render();
    return;
  }

  if (skill.type === "aoe") {
    if (!state.attackable.has(tileKey(tile))) {
      logEvent("Skill", `${skill.name} 대상 위치가 사거리 밖입니다.`, "miss");
      render();
      return;
    }
    executeSkill(actor, skill, tile);
    finishUnitAction(actor);
    render();
    return;
  }

  if (!unit) {
    logEvent("Skill", "대상이 없습니다.", "miss");
    render();
    return;
  }

  if (!isValidSkillTarget(actor, skill, unit)) {
    logEvent("Skill", `${skill.name} 대상이 아닙니다.`, "miss");
    render();
    return;
  }

  executeSkill(actor, skill, unit);
  finishUnitAction(actor);
  render();
}

function handleInteractTile(tile) {
  const actor = selectedUnit();
  if (!actor) return;
  const prop = propAt(tile.x, tile.y);
  if (!prop || prop.used || !state.interactable.has(prop.id)) {
    logEvent("Terrain", "상호작용할 수 없는 지형지물입니다.", "miss");
    render();
    return;
  }
  executeTerrainInteraction(actor, prop);
  finishUnitAction(actor);
  render();
}

function chooseSkill(skillId) {
  const unit = selectedUnit();
  const skill = skills[skillId];
  if (!unit || !skill) return;
  const can = canPaySkill(unit, skill);
  if (!can.ok) {
    logEvent("Skill", can.reason, "miss");
    render();
    return;
  }

  if (skill.type === "self" || skill.type === "rally") {
    executeSkill(unit, skill, unit);
    if (skill.type === "rally") finishUnitAction(unit);
    render();
    return;
  }

  state.selectedSkillId = skill.id;
  state.mode = "target";
  state.attackable = computeSkillRange(unit.position, skill);
  logEvent("Skill", `${unit.name}: ${skill.name} 대상 선택.`, "system");
  render();
}

function chooseInteractMode() {
  const unit = selectedUnit();
  if (!unit) return;
  state.mode = "interact";
  state.selectedSkillId = null;
  state.interactable = computeInteractable(unit);
  logEvent("Terrain", "인접한 지형지물을 선택하세요.", "system");
  render();
}

function cancelCommand() {
  const unit = selectedUnit();
  if (unit && state.preMove) {
    unit.position = { ...state.preMove };
    unit.moved = false;
  }
  if (unit) {
    state.mode = unit.moved ? "command" : "move";
    state.preMove = null;
    state.reachable = unit.moved ? new Map() : computeReachable(unit, effectiveMovement(unit));
    state.attackable = computeAttackableFrom(unit.position, unit);
    state.interactable = computeInteractable(unit);
  } else {
    clearSelection();
  }
  render();
}

function waitSelected() {
  const unit = selectedUnit();
  if (!unit) return;
  logEvent("Wait", `${unit.name} 대기.`, "system");
  finishUnitAction(unit);
  render();
}

function finishUnitAction(unit) {
  unit.acted = true;
  state.mode = "select";
  state.selectedUnitId = null;
  state.selectedSkillId = null;
  state.reachable = new Map();
  state.attackable = new Set();
  state.interactable = new Set();
  state.preMove = null;
  checkBattleEnd();
  if (!state.battleOver && livingUnits("ally").every((ally) => ally.acted || ally.defeated || ally.surrendered)) {
    logEvent("Phase", "모든 아군이 행동했습니다. 잠시 후 적 페이즈가 자동으로 시작됩니다.", "phase");
    window.setTimeout(() => {
      const allies = livingUnits("ally");
      if (state.phase === "player" && !state.battleOver && !state.showIntro && allies.length && allies.every((ally) => ally.acted || ally.defeated || ally.surrendered)) {
        endPlayerPhase();
      }
    }, 750);
  }
}

function clearSelection() {
  state.mode = "select";
  state.selectedUnitId = null;
  state.selectedSkillId = null;
  state.reachable = new Map();
  state.attackable = new Set();
  state.interactable = new Set();
  state.preMove = null;
}

function executeSkill(actor, skill, target) {
  const can = canPaySkill(actor, skill);
  if (!can.ok) {
    logEvent("Skill", can.reason, "miss");
    return false;
  }

  consumeSkill(actor, skill);

  if (skill.type === "attack") {
    resolveCombat(actor, target, skill, { counter: false });
    return true;
  }

  if (skill.type === "social") {
    resolveSocial(actor, target, skill);
    return true;
  }

  if (skill.type === "debuff") {
    resolveDebuff(actor, target, skill);
    return true;
  }

  if (skill.type === "heal") {
    resolveHeal(actor, target, skill);
    return true;
  }

  if (skill.type === "rally") {
    resolveRally(actor, skill);
    return true;
  }

  if (skill.type === "self") {
    resolveSelf(actor, skill);
    return true;
  }

  if (skill.type === "terrainSkill") {
    resolveTerrainSkill(actor, target, skill);
    return true;
  }

  if (skill.type === "aoe") {
    resolveAoe(actor, target, skill);
    return true;
  }

  return false;
}

function resolveCombat(actor, target, skill, options = {}) {
  if (!target || target.defeated || target.surrendered) return;
  const dist = distance(actor.position, target.position);
  const mode = attackRollMode(actor, target, skill);
  const d20 = rollD20Mode(mode);
  const statMod = mod(actor.stats[skill.stat]);
  const tileBonus = attackTerrainBonus(actor, target, skill);
  const statusHit = statusBonus(actor, "hit");
  const total = d20.total + statMod + actor.prof + (skill.hitBonus || 0) + tileBonus.hit + statusHit;
  const defense = target.guard + defenseTerrainBonus(target) + statusDefenseBonus(target);
  const crit = d20.natural === 20;
  const fumble = d20.natural === 1;
  const hit = crit || (!fumble && total >= defense);
  const modeText = mode === "advantage" ? "기회" : mode === "disadvantage" ? "불리" : "일반";

  logEvent("Dice", `${actor.name} ${skill.name}: ${modeText} d20 ${d20.text} + ${statLabels[skill.stat]} ${statMod} + 숙련 ${actor.prof} + 무공 ${skill.hitBonus || 0} + 지형/상태 ${tileBonus.hit + statusHit} = ${total} vs 방어 ${defense}.`, hit ? "hit" : "miss");

  if (fumble) {
    actor.morale = clamp(actor.morale - 8, 0, 100);
    upsertStatus(actor, { name: "노출", duration: 1, defense: -2 });
    logEvent("Fumble", `${actor.name} 초식이 흐트러짐. 기세 -8, 노출 1턴.`, "miss");
    return;
  }

  if (!hit) {
    target.morale = clamp(target.morale + 2, 0, 100);
    logEvent("Miss", `${target.name}이 ${skill.name}을 피하거나 막아냈다.`, "miss");
    return;
  }

  const dice = rollDice(skill.damage, crit ? 2 : 1);
  let damage = Math.max(1, dice.total + statMod + tileBonus.damage - defenseDamageReduction(target));
  if (hasStatus(actor, "은신") && skill.tags.includes("암기")) damage += 2;
  applyDamage(target, damage, actor);
  target.breakGauge = clamp(target.breakGauge + (skill.breakGain || 0) + (crit ? 12 : 0), 0, 100);
  target.morale = clamp(target.morale - (crit ? 11 : 4), 0, 100);
  actor.morale = clamp(actor.morale + (crit ? 8 : 3), 0, 100);
  logEvent("Hit", `${target.name} 피해 ${damage}${crit ? " · 대성공" : ""}. HP ${target.hp}/${target.maxHp}, 파훼 ${target.breakGauge}.`, "hit");

  if (skill.statusTarget && !target.defeated) {
    upsertStatus(target, structuredClone(skill.statusTarget));
    logEvent("Status", `${target.name} ${skill.statusTarget.name} ${skill.statusTarget.duration}턴.`, "hit");
  }

  if (skill.push && !target.defeated) pushUnit(actor, target, skill.push);
  if (target.breakGauge >= 100 && !hasStatus(target, "파훼")) {
    upsertStatus(target, { name: "파훼", duration: 2, defense: -3 });
    target.morale = clamp(target.morale - 12, 0, 100);
    logEvent("Break", `${target.name}의 초식이 완전히 파훼됨. 방어 -3, 기세 -12.`, "hit");
  }

  if (!options.counter && !target.defeated) {
    const counterSkill = findCounterSkill(target, actor);
    if (counterSkill) {
      logEvent("Counter", `${target.name} 반격: ${counterSkill.name}.`, "phase");
      consumeSkill(target, counterSkill, { counter: true });
      resolveCombat(target, actor, counterSkill, { counter: true });
    } else {
      logEvent("Counter", `${target.name} 반격 불가.`, "system");
    }
  }

  if (!options.counter && !actor.defeated && !target.defeated && canFollowUp(actor, target, skill)) {
    logEvent("Follow", `${actor.name} 추격 발동. 민첩 차이로 한 번 더 공격.`, "phase");
    resolveCombat(actor, target, skill, { counter: true, followUp: true });
  }
}

function resolveSocial(actor, target, skill) {
  if (!target || target.faction === actor.faction || target.defeated) return;
  const d20 = rollD20Mode(attackRollMode(actor, target, skill));
  const total = d20.total + mod(actor.stats[skill.stat]) + actor.prof + statusBonus(actor, "hit");
  const dc = 12 + mod(target.stats[skill.dcStat || "spirit"]) + Math.floor(target.morale / 35);
  const success = d20.natural === 20 || (d20.natural !== 1 && total >= dc);
  logEvent("Dice", `${actor.name} ${skill.name}: d20 ${d20.text} + ${statLabels[skill.stat]} ${mod(actor.stats[skill.stat])} + 숙련 ${actor.prof} = ${total} vs DC ${dc}.`, success ? "hit" : "miss");
  if (success) {
    target.morale = clamp(target.morale - skill.moraleDamage, 0, 100);
    target.breakGauge = clamp(target.breakGauge + (skill.breakGain || 0), 0, 100);
    logEvent("Social", `${target.name} 기세 -${skill.moraleDamage}, 파훼 +${skill.breakGain || 0}.`, "hit");
    if (target.morale <= 12 && target.id === "envoy") {
      livingUnits("enemy").forEach((enemy) => { enemy.surrendered = true; });
      state.battleOver = true;
      state.outcome = "victory";
      logEvent("Victory", "사절 주홍문이 체면을 접고 철수를 받아들였다. 조선 문파 연합의 첫 승리.", "hit");
    }
  } else {
    actor.morale = clamp(actor.morale - 5, 0, 100);
    logEvent("Social", `${actor.name}의 말이 먹히지 않음. ${actor.name} 기세 -5.`, "miss");
  }
}

function resolveDebuff(actor, target, skill) {
  if (!target || target.faction === actor.faction || target.defeated) return;
  const check = rollCheck(actor, skill.stat, skill.fixedDc || 12, attackRollMode(actor, target, skill));
  logEvent("Dice", `${actor.name} ${skill.name}: ${check.text} vs DC ${check.dc}.`, check.success ? "hit" : "miss");
  if (check.success) {
    target.breakGauge = clamp(target.breakGauge + (skill.breakGain || 0), 0, 100);
    if (skill.statusTarget) upsertStatus(target, structuredClone(skill.statusTarget));
    logEvent("Debuff", `${target.name} 파훼 +${skill.breakGain || 0}${skill.statusTarget ? `, ${skill.statusTarget.name}` : ""}.`, "hit");
  } else {
    actor.morale = clamp(actor.morale - 3, 0, 100);
    logEvent("Debuff", `${skill.name} 실패.`, "miss");
  }
}

function resolveHeal(actor, target, skill) {
  if (!target || target.faction !== actor.faction || target.defeated) return;
  const dice = rollDice(skill.heal);
  const amount = Math.max(1, dice.total + mod(actor.stats[skill.stat]));
  target.hp = clamp(target.hp + amount, 0, target.maxHp);
  const before = target.statuses.length;
  target.statuses = target.statuses.filter((status) => !["중독", "화상"].includes(status.name));
  logEvent("Heal", `${actor.name} ${skill.name}: ${target.name} HP +${amount}${before !== target.statuses.length ? ", 상태이상 정화" : ""}.`, "hit");
}

function resolveRally(actor, skill) {
  const check = rollCheck(actor, skill.stat, 13, "normal");
  const amount = check.success ? skill.moraleHeal : Math.ceil(skill.moraleHeal / 2);
  livingUnits(actor.faction).forEach((ally) => { ally.morale = clamp(ally.morale + amount, 0, 100); });
  logEvent("Rally", `${actor.name} ${skill.name}: ${check.text}. 아군 기세 +${amount}.`, check.success ? "hit" : "system");
}

function resolveSelf(actor, skill) {
  if (skill.moveBuff) {
    upsertStatus(actor, structuredClone(skill.status));
    actor.tempMoveBonus = (actor.tempMoveBonus || 0) + skill.moveBuff;
    state.reachable = computeReachable(actor, effectiveMovement(actor));
    logEvent("Self", `${actor.name} ${skill.name}: 이동 +${skill.moveBuff}, ${skill.status.name}.`, "hit");
  }
}

function resolveTerrainSkill(actor, tile, skill) {
  if (!tile) return;
  if (skill.terrainEffect === "freeze") {
    const check = rollCheck(actor, skill.stat, tile.tags.includes("water") ? 11 : 15, "normal");
    logEvent("Dice", `${actor.name} ${skill.name}: ${check.text}.`, check.success ? "hit" : "miss");
    if (check.success) {
      tile.code = "H";
      tile.id = "ice";
      tile.label = "얼어붙은 여울";
      tile.short = "빙";
      tile.moveCost = 1;
      tile.defense = 0;
      tile.avoid = 1;
      tile.cover = "없음";
      tile.height = 0;
      tile.walkable = true;
      tile.hazard = null;
      tile.tags = ["ice", "water"];
      tile.frozen = 3;
      logEvent("Terrain", `${tileKey(tile)} 여울이 얼어붙어 3라운드 동안 빠른 길이 됨.`, "hit");
    }
  }
}

function resolveAoe(actor, tile, skill) {
  if (!tile) return;
  const targets = livingUnits("enemy").filter((unit) => distance(unit.position, tile) <= (skill.radius || 1));
  logEvent("AOE", `${actor.name} ${skill.name}: 중심 ${tileKey(tile)}, 대상 ${targets.length}명.`, targets.length ? "hit" : "miss");
  targets.forEach((target) => resolveCombat(actor, target, skill, { counter: true }));
}

function executeTerrainInteraction(actor, prop) {
  const check = rollCheck(actor, prop.stat, prop.dc, "normal");
  prop.used = true;
  logEvent("Dice", `${actor.name} ${prop.label}: ${check.text}.`, check.success ? "hit" : "miss");
  const tile = getTile(prop.x, prop.y);

  if (prop.kind === "smoke") {
    tilesAround(tile, 1).forEach((t) => { t.smoke = 2; t.cover = "강한 엄폐"; });
    logEvent("Terrain", "향로 연막: 주변 1칸 2라운드 강엄폐.", "hit");
    return;
  }

  if (prop.kind === "fire") {
    tilesAround(tile, 1).forEach((t) => { if (t.walkable) { t.hazard = "fire"; t.burning = 2; } });
    if (check.success) {
      unitsAround(tile, 1).filter((unit) => unit.faction !== actor.faction).forEach((unit) => {
        const damage = rollDice("1d6").total;
        applyDamage(unit, damage, actor);
        upsertStatus(unit, { name: "화상", duration: 2 });
        logEvent("Terrain", `${unit.name} 화염 피해 ${damage}, 화상.`, "hit");
      });
    }
    return;
  }

  if (prop.kind === "bridge") {
    if (check.success) {
      [getTile(7, 4)].forEach((t) => {
        if (!t) return;
        t.code = "W";
        Object.assign(t, structuredClone(terrainDefs.W), { x: t.x, y: t.y, code: "W", smoke: t.smoke, frozen: 0, burning: 0, trap: t.trap });
      });
      livingUnits("enemy").filter((unit) => unit.position.x === 7 && unit.position.y === 4).forEach((unit) => {
        const damage = rollDice("1d6").total;
        applyDamage(unit, damage, actor);
        unit.breakGauge = clamp(unit.breakGauge + 10, 0, 100);
        logEvent("Terrain", `${unit.name} 다리 붕괴 피해 ${damage}, 파훼 +10.`, "hit");
      });
      logEvent("Terrain", "돌다리가 무너져 계류가 됨.", "hit");
    }
    return;
  }

  if (prop.kind === "cart") {
    tile.cover = "강한 엄폐";
    tile.defense += 1;
    if (check.success) {
      unitsAround(tile, 1).filter((unit) => unit.faction !== actor.faction).forEach((unit) => {
        upsertStatus(unit, { name: "노출", duration: 1, defense: -2 });
        unit.morale = clamp(unit.morale - 4, 0, 100);
        logEvent("Terrain", `${unit.name} 노출, 기세 -4.`, "hit");
      });
    }
    return;
  }

  if (prop.kind === "bambooTrap") {
    tile.trap = 3;
    tile.cover = "강한 엄폐";
    logEvent("Terrain", "대나무 덫 설치. 3라운드 동안 이 칸에 들어온 적은 민첩 DC 13 실패 시 노출.", "hit");
    return;
  }

  if (prop.kind === "bell") {
    const amount = check.success ? 8 : 3;
    livingUnits("ally").forEach((unit) => { unit.morale = clamp(unit.morale + amount, 0, 100); });
    logEvent("Terrain", `사당 종 울림. 아군 기세 +${amount}.`, check.success ? "hit" : "system");
  }
}

function consumeSkill(actor, skill, options = {}) {
  actor.inner = clamp(actor.inner - (skill.innerCost || 0), 0, actor.maxInner);
  if (Number.isInteger(skill.uses)) actor.uses[skill.id] = Math.max(0, (actor.uses[skill.id] ?? skill.uses) - 1);
  if (skill.cooldown && !options.counter) actor.cooldowns[skill.id] = skill.cooldown;
}

function canPaySkill(actor, skill) {
  if (!actor || actor.defeated || actor.surrendered) return { ok: false, reason: "행동 불가." };
  if (actor.inner < (skill.innerCost || 0)) return { ok: false, reason: `${skill.name}: 내공 부족.` };
  if (Number.isInteger(skill.uses) && (actor.uses[skill.id] ?? skill.uses) <= 0) return { ok: false, reason: `${skill.name}: 사용 횟수 없음.` };
  if ((actor.cooldowns[skill.id] || 0) > 0) return { ok: false, reason: `${skill.name}: 재사용 ${actor.cooldowns[skill.id]}턴.` };
  return { ok: true, reason: "" };
}

function findCounterSkill(defender, attacker) {
  const dist = distance(defender.position, attacker.position);
  const list = defender.skills.map((id) => skills[id]).filter((skill) => skill.canCounter && skill.type === "attack" && canPaySkill(defender, skill).ok);
  const valid = list.filter((skill) => dist >= skill.rangeMin && dist <= skill.rangeMax);
  if (!valid.length) return null;
  const reaction = valid.find((skill) => skill.reactionOnly);
  return reaction || valid[0];
}

function canFollowUp(actor, target, skill) {
  if (!skill || skill.type !== "attack") return false;
  if (skill.tags.includes("범위") || skill.rangeMax > 3) return false;
  return (actor.stats.agility - target.stats.agility) >= 5;
}

function applyDamage(target, damage, source) {
  target.hp = clamp(target.hp - damage, 0, target.maxHp);
  if (target.hp <= 0) defeatUnit(target, source);
}

function defeatUnit(unit, source) {
  unit.defeated = true;
  unit.acted = true;
  unit.hp = 0;
  unit.morale = 0;
  unit.breakGauge = 100;
  const text = unit.faction === "enemy" ? "비살상 제압" : "전투불능";
  logEvent("Down", `${unit.name} ${text}.`, unit.faction === "enemy" ? "hit" : "miss");
}

function pushUnit(actor, target, steps) {
  let pushed = 0;
  const dx = Math.sign(target.position.x - actor.position.x);
  const dy = Math.sign(target.position.y - actor.position.y);
  const primary = Math.abs(dx) >= Math.abs(dy) ? { dx, dy: 0 } : { dx: 0, dy };
  for (let i = 0; i < steps; i += 1) {
    const next = { x: target.position.x + primary.dx, y: target.position.y + primary.dy };
    const tile = getTile(next.x, next.y);
    if (!tile || !tile.walkable || unitAt(next.x, next.y)) break;
    target.position = next;
    pushed += 1;
    applyTileEntry(target, tile);
  }
  if (pushed) logEvent("Push", `${target.name} ${pushed}칸 밀림.`, "hit");
}

function applyTileEntry(unit, tile) {
  if (tile.hazard === "fire") {
    const damage = rollDice("1d4").total;
    applyDamage(unit, damage, null);
    upsertStatus(unit, { name: "화상", duration: 2 });
    logEvent("Hazard", `${unit.name} 화염 지대 진입: 피해 ${damage}, 화상.`, "miss");
  }
  if (tile.hazard === "slippery" && !hasStatus(unit, "경공")) {
    const check = rollCheck(unit, "agility", 12, "normal");
    if (!check.success) {
      unit.morale = clamp(unit.morale - 4, 0, 100);
      upsertStatus(unit, { name: "노출", duration: 1, defense: -2 });
      logEvent("Hazard", `${unit.name} 여울 경공 실패: ${check.text}. 기세 -4, 노출.`, "miss");
    }
  }
  if (tile.trap > 0 && unit.faction === "enemy") {
    const check = rollCheck(unit, "agility", 13, "normal");
    if (!check.success) {
      upsertStatus(unit, { name: "노출", duration: 1, defense: -2 });
      unit.morale = clamp(unit.morale - 5, 0, 100);
      logEvent("Trap", `${unit.name} 대나무 덫 발동: ${check.text}. 노출, 기세 -5.`, "hit");
    }
  }
}

function computeReachable(unit, movement) {
  const visited = new Map();
  const queue = [{ x: unit.position.x, y: unit.position.y, cost: 0 }];
  visited.set(tileKey(unit.position), { x: unit.position.x, y: unit.position.y, cost: 0, prev: null });
  const blockedByEnemy = new Set(livingUnits(opposingFaction(unit.faction)).map((u) => tileKey(u.position)));
  const occupied = new Set(livingUnits(unit.faction).filter((u) => u.id !== unit.id).map((u) => tileKey(u.position)));

  while (queue.length) {
    queue.sort((a, b) => a.cost - b.cost);
    const current = queue.shift();
    for (const next of neighbors(current)) {
      const tile = getTile(next.x, next.y);
      if (!tile || !tile.walkable) continue;
      const key = tileKey(tile);
      if (blockedByEnemy.has(key)) continue;
      if (occupied.has(key)) continue;
      const heightGap = Math.abs(tile.height - getTile(current.x, current.y).height);
      if (heightGap > 1 && !hasStatus(unit, "경공")) continue;
      const cost = current.cost + movementCost(unit, tile);
      if (cost > movement) continue;
      const old = visited.get(key);
      if (!old || cost < old.cost) {
        visited.set(key, { x: tile.x, y: tile.y, cost, prev: tileKey(current) });
        queue.push({ x: tile.x, y: tile.y, cost });
      }
    }
  }
  return visited;
}

function movementCost(unit, tile) {
  let cost = tile.moveCost;
  if (tile.tags.includes("bamboo") && unit.id === "han") cost = 1;
  if (tile.tags.includes("water") && hasStatus(unit, "경공")) cost = Math.max(1, cost - 1);
  if (tile.tags.includes("ice")) cost = 1;
  return cost;
}

function effectiveMovement(unit) {
  let value = unit.movement + (unit.tempMoveBonus || 0);
  unit.statuses.forEach((status) => { value += status.move || 0; });
  return Math.max(0, value);
}

function computeAttackableFrom(pos, unit) {
  const set = new Set();
  unit.skills.map((id) => skills[id]).filter((skill) => isHostileSkill(skill) && !skill.reactionOnly && canPaySkill(unit, skill).ok).forEach((skill) => {
    computeSkillRange(pos, skill).forEach((_, key) => set.add(key));
  });
  return set;
}

function computeSkillRange(pos, skill) {
  const set = new Set();
  for (let y = 0; y < GRID_H; y += 1) {
    for (let x = 0; x < GRID_W; x += 1) {
      const dist = distance(pos, { x, y });
      if (dist >= skill.rangeMin && dist <= skill.rangeMax) set.add(tileKey({ x, y }));
    }
  }
  return set;
}

function computeInteractable(unit) {
  const set = new Set();
  state.props.forEach((prop) => {
    if (!prop.used && distance(unit.position, prop) <= 1) set.add(prop.id);
  });
  return set;
}

function computeEnemyDanger() {
  const set = new Set();
  livingUnits("enemy").forEach((enemy) => {
    const reach = computeReachable(enemy, enemy.movement);
    reach.forEach((info) => {
      enemy.skills.map((id) => skills[id]).filter((skill) => isHostileSkill(skill) && !skill.reactionOnly).forEach((skill) => {
        computeSkillRange({ x: info.x, y: info.y }, skill).forEach((_, key) => set.add(key));
      });
    });
  });
  return set;
}

function isHostileSkill(skill) {
  return ["attack", "social", "debuff", "aoe"].includes(skill.type);
}

function isValidSkillTarget(actor, skill, target) {
  const dist = distance(actor.position, target.position);
  if (dist < skill.rangeMin || dist > skill.rangeMax) return false;
  if (["attack", "social", "debuff"].includes(skill.type)) return target.faction !== actor.faction;
  if (skill.type === "heal") return target.faction === actor.faction;
  return false;
}

function attackRollMode(actor, target, skill) {
  let adv = 0;
  const actorTile = getTile(actor.position.x, actor.position.y);
  const targetTile = getTile(target.position.x, target.position.y);
  if (actorTile.height > targetTile.height) adv += 1;
  if (targetTile.height > actorTile.height && skill.rangeMax <= 1) adv -= 1;
  if (targetTile.smoke > 0 && skill.rangeMax > 1) adv -= 1;
  if (hasStatus(actor, "은신") && skill.tags.includes("암기")) adv += 1;
  if (skill.tags.includes("빙공") && targetTile.tags.includes("water")) adv += 1;
  if (hasStatus(target, "파훼") || hasStatus(target, "간파") || hasStatus(target, "노출")) adv += 1;
  if (hasStatus(actor, "둔화")) adv -= 1;
  if (adv > 0) return "advantage";
  if (adv < 0) return "disadvantage";
  return "normal";
}

function attackTerrainBonus(actor, target, skill) {
  const actorTile = getTile(actor.position.x, actor.position.y);
  const targetTile = getTile(target.position.x, target.position.y);
  let hit = 0;
  let damage = 0;
  if (actorTile.height > targetTile.height) hit += 2;
  if (skill.tags.includes("원거리") && actorTile.height > targetTile.height) damage += 1;
  if (actorTile.tags.includes("bamboo") && skill.tags.includes("암기")) hit += 2;
  if (targetTile.tags.includes("bamboo") || targetTile.tags.includes("cover")) hit -= targetTile.avoid;
  if (targetTile.smoke > 0 && skill.rangeMax > 1) hit -= 3;
  return { hit, damage };
}

function defenseTerrainBonus(unit) {
  const tile = getTile(unit.position.x, unit.position.y);
  return tile.defense + Math.max(0, tile.avoid);
}

function defenseDamageReduction(unit) {
  const tile = getTile(unit.position.x, unit.position.y);
  return Math.max(0, Math.floor(tile.defense / 2));
}

function statusDefenseBonus(unit) {
  return unit.statuses.reduce((sum, status) => sum + (status.defense || 0), 0);
}

function statusBonus(unit, key) {
  return unit.statuses.reduce((sum, status) => sum + (status[key] || 0), 0);
}

function rollD20Mode(mode) {
  const a = rollD20();
  if (mode === "advantage" || mode === "disadvantage") {
    const b = rollD20();
    const picked = mode === "advantage" ? Math.max(a, b) : Math.min(a, b);
    return { total: picked, natural: picked, text: `${a}/${b}` };
  }
  return { total: a, natural: a, text: String(a) };
}

function rollD20() {
  return 1 + Math.floor(state.rng() * 20);
}

function rollCheck(actor, stat, dc, mode = "normal") {
  const d20 = rollD20Mode(mode);
  const total = d20.total + mod(actor.stats[stat]) + actor.prof;
  return { d20, total, dc, success: d20.natural === 20 || (d20.natural !== 1 && total >= dc), text: `d20 ${d20.text} + ${statLabels[stat]} ${mod(actor.stats[stat])} + 숙련 ${actor.prof} = ${total} vs DC ${dc}` };
}

function rollDice(expr, multiplier = 1) {
  const match = /^([0-9]+)d([0-9]+)$/.exec(expr);
  if (!match) return { total: 0, detail: expr };
  const count = Number(match[1]) * multiplier;
  const sides = Number(match[2]);
  const rolls = Array.from({ length: count }, () => 1 + Math.floor(state.rng() * sides));
  return { total: rolls.reduce((sum, value) => sum + value, 0), detail: rolls.join("+") };
}

function mod(score) { return Math.floor((score - 10) / 2); }
function opposingFaction(faction) { return faction === "ally" ? "enemy" : "ally"; }
function clamp(value, min, max) { return Math.max(min, Math.min(max, value)); }
function tileKey(tile) { return `${tile.x},${tile.y}`; }
function distance(a, b) { return Math.abs(a.x - b.x) + Math.abs(a.y - b.y); }
function neighbors(pos) { return [{ x: pos.x + 1, y: pos.y }, { x: pos.x - 1, y: pos.y }, { x: pos.x, y: pos.y + 1 }, { x: pos.x, y: pos.y - 1 }]; }
function getTile(x, y) { return state.tiles.find((tile) => tile.x === x && tile.y === y); }
function unitAt(x, y) { return state.units.find((unit) => !unit.defeated && !unit.surrendered && unit.position.x === x && unit.position.y === y); }
function propAt(x, y) { return state.props.find((prop) => prop.x === x && prop.y === y); }
function getUnit(id) { return state.units.find((unit) => unit.id === id); }
function selectedUnit() { return getUnit(state.selectedUnitId); }
function selectedSkill() { return skills[state.selectedSkillId]; }
function livingUnits(faction) { return state.units.filter((unit) => unit.faction === faction && !unit.defeated && !unit.surrendered); }
function hasStatus(unit, name) { return unit.statuses.some((status) => status.name === name); }
function upsertStatus(unit, status) { const existing = unit.statuses.find((item) => item.name === status.name); if (existing) Object.assign(existing, status); else unit.statuses.push(status); }
function tilesAround(tile, radius) { return state.tiles.filter((t) => distance(t, tile) <= radius); }
function unitsAround(tile, radius) { return state.units.filter((unit) => !unit.defeated && !unit.surrendered && distance(unit.position, tile) <= radius); }

function checkBattleEnd() {
  const alliesAlive = livingUnits("ally").length > 0;
  const enemiesAlive = livingUnits("enemy").length > 0;
  if (!alliesAlive) {
    state.battleOver = true;
    state.outcome = "defeat";
    logEvent("Defeat", "해동문 전열 붕괴.", "miss");
  } else if (!enemiesAlive) {
    state.battleOver = true;
    state.outcome = "victory";
    logEvent("Victory", "중원정파 사절단 제압 완료. 조선 문파 연합의 명분이 세워졌다.", "hit");
  }
  return state.battleOver;
}

function render() {
  renderBattlefield();
  renderTop();
  renderGuide();
  renderSelectedPanel();
  renderActionBar();
  renderForecast();
  renderTilePanel();
  renderRoster();
  renderLog();
  renderOverlays();
}

function renderTop() {
  els.phaseLabel.textContent = state.battleOver ? "Battle End" : state.phase === "player" ? "Player Phase" : "Enemy Phase";
  els.phaseCard.className = `phase-card ${state.battleOver ? "end" : state.phase}`;
  els.roundCounter.textContent = String(state.round);
  els.seedLabel.textContent = `seed ${state.seed}`;
  els.endPhaseButton.disabled = state.phase !== "player" || state.battleOver;
}

const guideSteps = [
  { label: "아군 선택" },
  { label: "이동" },
  { label: "명령" },
  { label: "대상 선택" }
];

function renderGuide() {
  let kind = "player";
  let stepIndex = -1;
  let hint = "";
  if (state.battleOver) {
    kind = "end";
    hint = state.outcome === "victory" ? "승리! 새 전투 버튼으로 다시 시작할 수 있습니다." : "패배… 새 전투 버튼으로 다시 도전하세요.";
  } else if (state.phase !== "player") {
    kind = "enemy";
    hint = "적 페이즈 진행 중 — 중원 사절단이 행동하고 있습니다.";
  } else {
    stepIndex = { select: 0, move: 1, command: 2, target: 3, interact: 3 }[state.mode] ?? 0;
    hint = {
      select: "행동할 아군을 클릭하세요. 아래 부대 명단을 눌러도 선택됩니다.",
      move: "파란 칸 중 이동할 위치를 클릭하세요. 이동 없이 바로 무공을 골라도 됩니다.",
      command: "오른쪽 명령 패널에서 무공 · 지형 사용 · 대기를 선택하세요.",
      target: "붉은 칸의 대상을 클릭하세요. 적을 가리키면 전투 예측이 표시됩니다.",
      interact: "금색 칸의 지형지물을 클릭해 발동하세요."
    }[state.mode] || "";
  }
  els.guidePanel.className = `guide-panel ${kind}`;
  els.guidePanel.innerHTML = `
    <ol class="guide-steps">
      ${guideSteps.map((step, i) => `<li class="guide-step ${i === stepIndex ? "active" : i < stepIndex ? "done" : ""}"><b>${i + 1}</b>${step.label}</li>`).join("")}
    </ol>
    <p class="guide-hint">${escapeHtml(hint)}</p>
  `;
}

let bannerTimer = null;
function showPhaseBanner(text, kind) {
  if (!els.phaseBanner || state.showIntro) return;
  const textEl = els.phaseBanner.querySelector(".pb-text");
  els.phaseBanner.classList.remove("show", "player", "enemy", "end");
  void els.phaseBanner.offsetWidth;
  textEl.textContent = text;
  els.phaseBanner.classList.add("show", kind);
  if (bannerTimer) window.clearTimeout(bannerTimer);
  bannerTimer = window.setTimeout(() => els.phaseBanner.classList.remove("show"), 1900);
}

function renderOverlays() {
  if (state.showIntro) {
    els.introOverlay.hidden = false;
    els.introOverlay.innerHTML = `
      <div class="briefing-card">
        <p class="briefing-kicker">作戰 BRIEFING</p>
        <h2>백두산 설문 관문전</h2>
        <p class="briefing-lede">중원 감찰단이 백두산 설문(雪門)을 넘어 조선 문파에 중원식 말과 예법을 강요하려 한다. 해동문 다섯 무인이 관문 아래 계곡에서 감찰단을 저지한다.</p>
        <div class="briefing-grid">
          <section>
            <h3>승리 조건</h3>
            <ul>
              <li>사절 주홍문 제압</li>
              <li>또는 사절의 기세를 12 이하로 꺾어 항복시키기</li>
            </ul>
          </section>
          <section>
            <h3>패배 조건</h3>
            <ul>
              <li>아군 전원 전투불능</li>
            </ul>
          </section>
          <section>
            <h3>지형 활용</h3>
            <ul>
              <li>좌측 설죽림 엄폐 · 벼랑 고지 명중 보너스</li>
              <li>돌다리가 유일한 빠른 도하로 — 밧줄을 끊거나 빙공으로 계류를 얼릴 수 있다</li>
            </ul>
          </section>
          <section>
            <h3>조작</h3>
            <ul>
              <li>① 아군 클릭 → ② 파란 칸 이동 → ③ 무공/지형/대기 → ④ 붉은 칸 대상</li>
              <li>모든 아군이 행동하면 적 페이즈가 자동으로 시작됩니다</li>
            </ul>
          </section>
        </div>
        <div class="briefing-actions">
          <button id="introStart" class="primary-button big" type="button">출진</button>
        </div>
      </div>
    `;
    els.introOverlay.querySelector("#introStart").addEventListener("click", () => {
      state.showIntro = false;
      render();
      showPhaseBanner(state.phase === "player" ? `아군 페이즈 · ${state.round}턴` : "적 페이즈", state.phase);
    });
  } else {
    els.introOverlay.hidden = true;
    els.introOverlay.innerHTML = "";
  }

  const showResult = state.battleOver && state.outcome && !state.resultDismissed;
  if (showResult) {
    const victory = state.outcome === "victory";
    const enemies = state.units.filter((unit) => unit.faction === "enemy");
    const downed = enemies.filter((unit) => unit.defeated || unit.surrendered).length;
    els.resultOverlay.hidden = false;
    els.resultOverlay.innerHTML = `
      <div class="briefing-card result ${victory ? "victory" : "defeat"}">
        <p class="briefing-kicker">${victory ? "勝利" : "敗北"}</p>
        <h2>${victory ? "승리" : "패배"}</h2>
        <p class="briefing-lede">${victory ? "중원 사절단을 물리쳤다. 조선 문파 연합의 명분이 세워졌다." : "해동문의 전열이 무너졌다. 전열을 가다듬고 다시 도전하자."}</p>
        <div class="result-stats">
          <div><span>라운드</span><strong>${state.round}</strong></div>
          <div><span>아군 생존</span><strong>${livingUnits("ally").length}/${state.units.filter((unit) => unit.faction === "ally").length}</strong></div>
          <div><span>적 제압</span><strong>${downed}/${enemies.length}</strong></div>
        </div>
        <div class="briefing-actions">
          <button id="resultInspect" class="small-button" type="button">전장 확인</button>
          <button id="resultRestart" class="primary-button big" type="button">새 전투</button>
        </div>
      </div>
    `;
    els.resultOverlay.querySelector("#resultInspect").addEventListener("click", () => {
      state.resultDismissed = true;
      render();
    });
    els.resultOverlay.querySelector("#resultRestart").addEventListener("click", () => resetBattle(Date.now() % 100000000));
  } else {
    els.resultOverlay.hidden = true;
    els.resultOverlay.innerHTML = "";
  }
}

function renderBattlefield() {
  const danger = state.showDanger ? computeEnemyDanger() : new Set();
  els.battlefield.style.setProperty("--cols", GRID_W);
  els.battlefield.style.setProperty("--rows", GRID_H);
  els.battlefield.innerHTML = "";
  state.tiles.forEach((tile) => {
    const button = document.createElement("button");
    button.type = "button";
    const moveTarget = state.mode === "move" && state.reachable.has(tileKey(tile)) && !unitAt(tile.x, tile.y);
    const attackTarget = state.mode === "target" && state.attackable.has(tileKey(tile));
    const prop = propAt(tile.x, tile.y);
    const interactTarget = state.mode === "interact" && prop && state.interactable.has(prop.id);
    button.className = [
      "tile",
      `terrain-${tile.id}`,
      tile.hazard === "fire" ? "hazard-fire" : "",
      tile.smoke > 0 ? "hazard-smoke" : "",
      state.selectedTile && tileKey(state.selectedTile) === tileKey(tile) ? "selected" : "",
      moveTarget ? "move-target" : "",
      attackTarget ? "attack-target" : "",
      interactTarget ? "interact-target" : "",
      danger.has(tileKey(tile)) ? "enemy-danger" : ""
    ].filter(Boolean).join(" ");
    button.title = tileTitle(tile);
    button.addEventListener("click", () => handleTileClick(tile));
    button.addEventListener("mouseenter", () => { state.hoverTile = tile; renderTilePanel(); renderForecast(); });

    if (tile.height > 0) {
      const h = document.createElement("span");
      h.className = "height-tag";
      h.textContent = `H${tile.height}`;
      button.appendChild(h);
    }
    const tag = document.createElement("span");
    tag.className = "terrain-tag";
    tag.textContent = tile.short;
    button.appendChild(tag);

    if (prop && !prop.used) {
      const p = document.createElement("span");
      p.className = "prop-token";
      p.textContent = prop.icon;
      p.title = `${prop.label}: ${prop.desc}`;
      button.appendChild(p);
    }
    if (tile.smoke > 0) {
      const s = document.createElement("span");
      s.className = "prop-token";
      s.textContent = "연";
      s.title = `연막 ${tile.smoke}라운드`;
      button.appendChild(s);
    }
    if (tile.trap > 0) {
      const t = document.createElement("span");
      t.className = "prop-token";
      t.textContent = "덫";
      t.title = `대나무 덫 ${tile.trap}라운드`;
      button.appendChild(t);
    }

    const unit = unitAt(tile.x, tile.y);
    if (unit) button.appendChild(renderUnitToken(unit));
    els.battlefield.appendChild(button);
  });
}

function renderUnitToken(unit) {
  const wrapper = document.createElement("span");
  wrapper.className = ["unit-token", unit.faction, unit.acted ? "acted" : "", unit.defeated ? "defeated" : "", hasStatus(unit, "파훼") ? "broken" : ""].filter(Boolean).join(" ");
  applyAppearanceVars(wrapper, unitAppearance(unit));
  const hp = document.createElement("span");
  hp.className = "hp-pip";
  hp.innerHTML = `<span style="width:${Math.round((unit.hp / unit.maxHp) * 100)}%"></span>`;
  const person = document.createElement("span");
  const appearance = unitAppearance(unit);
  person.className = ["person", `hair-${appearance.hairStyle}`, `weapon-${appearance.weapon}`, `build-${appearance.build}`].join(" ");
  person.innerHTML = `
    <span class="ground-shadow"></span>
    <span class="leg left"><span></span></span>
    <span class="leg right"><span></span></span>
    <span class="body"><span class="collar"></span><span class="sash"></span></span>
    <span class="arm left"></span>
    <span class="arm right"></span>
    <span class="neck"></span>
    <span class="hair-back"></span>
    <span class="head"><span class="bangs"></span><span class="eye left"></span><span class="eye right"></span><span class="mouth"></span></span>
    <span class="hair-tail"></span>
    <span class="weapon"></span>
  `;
  const name = document.createElement("span");
  name.className = "unit-nameplate";
  name.textContent = unit.short;
  wrapper.appendChild(hp);
  wrapper.appendChild(person);
  wrapper.appendChild(name);
  return wrapper;
}

function unitAppearance(unit) {
  return unit.appearance || (unit.faction === "enemy" ? appearances.enemyOfficer : appearances.park);
}

function applyAppearanceVars(el, appearance) {
  [
    ["--skin", appearance.skin],
    ["--hair", appearance.hair],
    ["--hair-shade", appearance.hairShade],
    ["--outfit", appearance.outfit],
    ["--outfit-light", appearance.outfitLight],
    ["--accent", appearance.accent],
    ["--lower", appearance.lower],
    ["--shoe", appearance.shoe]
  ].forEach(([key, value]) => {
    if (value) el.style.setProperty(key, value);
  });
}

function renderSelectedPanel() {
  const unit = selectedUnit() || (state.selectedTile ? unitAt(state.selectedTile.x, state.selectedTile.y) : null);
  if (!unit) {
    els.selectedPanel.innerHTML = `<h2>유닛</h2><p class="empty-hint">아군 페이즈에는 여성 동료 또는 박성준을 클릭하세요. 이동 범위가 표시되고, 이동 후 공격/지형/대기를 선택합니다.</p>`;
    return;
  }
  const statusText = unit.statuses.map((s) => `${s.name} ${s.duration}`).join(", ") || "정상";
  els.selectedPanel.innerHTML = `
    <h2>유닛</h2>
    <div class="unit-summary">
      <div class="name-row"><strong>${escapeHtml(unit.name)}</strong><span class="role">${escapeHtml(unit.role)}</span></div>
      <div class="meters">
        ${meter("HP", unit.hp, unit.maxHp, "")}
        ${meter("내공", unit.inner, unit.maxInner, "inner")}
        ${meter("기세", unit.morale, unit.maxMorale, "morale")}
        ${meter("파훼", unit.breakGauge, 100, "break")}
      </div>
      <div class="chips">
        <span class="chip ${unit.acted ? "used" : "good"}">${unit.acted ? "행동 완료" : "행동 가능"}</span>
        <span class="chip">이동 ${effectiveMovement(unit)}</span>
        <span class="chip">방어 ${unit.guard}</span>
        <span class="chip">상태 ${escapeHtml(statusText)}</span>
      </div>
      <div class="stat-grid">
        ${Object.entries(unit.stats).map(([key, value]) => `<div class="stat"><span>${statLabels[key]}</span><strong>${value} (${signed(mod(value))})</strong></div>`).join("")}
      </div>
    </div>
  `;
}

function renderActionBar() {
  els.actionBar.innerHTML = "";
  if (state.battleOver) {
    addActionButton("전투 종료", "✓", "새 전투로 다시 시작", "", true, () => {});
    return;
  }
  if (state.phase !== "player") {
    addActionButton("적 페이즈", "敵", "중원 사절단 행동 중", "", true, () => {});
    return;
  }

  const unit = selectedUnit();
  if (!unit) {
    addActionButton("유닛 선택", "手", "행동할 아군을 클릭", "", true, () => {});
    addActionButton("페이즈 종료", "終", "적 페이즈 시작", "", false, endPlayerPhase, false, "end");
    return;
  }

  addActionGroup("기본");
  addActionButton(unit.moved ? "이동 완료" : "이동 범위", "步", unit.moved ? "이번 페이즈 이동 소모" : state.mode === "move" ? "표시 중" : "다시 표시", `이동 ${effectiveMovement(unit)}`, unit.moved, () => {
    state.mode = "move";
    state.selectedSkillId = null;
    state.reachable = computeReachable(unit, effectiveMovement(unit));
    render();
  }, state.mode === "move");

  if (state.preMove) {
    addActionButton("이동 취소", "戻", "이동 전 위치로", "", false, cancelCommand);
  }

  addActionGroup("무공");
  unit.skills.map((id) => skills[id]).filter((skill) => !skill.reactionOnly).forEach((skill) => {
    const can = canPaySkill(unit, skill);
    const meta = skillMeta(unit, skill);
    addActionButton(skill.name, skill.icon, skill.desc, meta, !can.ok, () => chooseSkill(skill.id), state.selectedSkillId === skill.id);
  });

  addActionGroup("전장");
  addActionButton("지형 사용", "地", state.interactable.size ? "인접 기물 선택" : "인접 기물 없음", `${state.interactable.size}개`, !state.interactable.size, chooseInteractMode, state.mode === "interact");
  addActionButton("대기", "待", "행동 종료", "", false, waitSelected, false, "end");
}

function addActionGroup(title) {
  const div = document.createElement("div");
  div.className = "action-group-title";
  div.textContent = title;
  els.actionBar.appendChild(div);
}

function addActionButton(name, icon, sub, meta, disabled, onClick, active = false, extra = "") {
  const btn = document.createElement("button");
  btn.type = "button";
  btn.className = ["action-button", active ? "active" : "", extra].filter(Boolean).join(" ");
  btn.disabled = disabled;
  btn.innerHTML = `<span class="action-icon">${escapeHtml(icon)}</span><span><span class="action-title">${escapeHtml(name)}</span><span class="action-sub">${escapeHtml(sub || "")}</span></span><span class="action-meta">${escapeHtml(meta || "")}</span>`;
  btn.addEventListener("click", onClick);
  els.actionBar.appendChild(btn);
}

function skillMeta(unit, skill) {
  const parts = [];
  if (skill.rangeMax > 0) parts.push(`${skill.rangeMin}-${skill.rangeMax}`);
  if (skill.innerCost) parts.push(`내공${skill.innerCost}`);
  if (Number.isInteger(skill.uses)) parts.push(`${unit.uses[skill.id] ?? skill.uses}회`);
  if (skill.cooldown) parts.push(`쿨${skill.cooldown}`);
  return parts.join(" · ");
}

function diceRange(expr, multiplier = 1) {
  const match = /^([0-9]+)d([0-9]+)$/.exec(expr || "");
  if (!match) return { min: 0, max: 0 };
  const count = Number(match[1]) * multiplier;
  return { min: count, max: count * Number(match[2]) };
}

function chanceFromThreshold(threshold, mode) {
  const t = clamp(threshold, 2, 20);
  let p = (21 - t) / 20;
  if (mode === "advantage") p = 1 - (1 - p) * (1 - p);
  if (mode === "disadvantage") p = p * p;
  return clamp(Math.round(p * 100), 0, 100);
}

function critChance(mode) {
  if (mode === "advantage") return 10;
  if (mode === "disadvantage") return 0;
  return 5;
}

function rollModeLabel(mode) {
  return mode === "advantage" ? "기회" : mode === "disadvantage" ? "불리" : "일반";
}

function computeStrike(actor, target, skill) {
  const mode = attackRollMode(actor, target, skill);
  const statMod = mod(actor.stats[skill.stat]);
  const tileBonus = attackTerrainBonus(actor, target, skill);
  const bonus = statMod + actor.prof + (skill.hitBonus || 0) + tileBonus.hit + statusBonus(actor, "hit");
  const defense = target.guard + defenseTerrainBonus(target) + statusDefenseBonus(target);
  const flat = statMod + tileBonus.damage - defenseDamageReduction(target) + (hasStatus(actor, "은신") && skill.tags.includes("암기") ? 2 : 0);
  const range = diceRange(skill.damage);
  const critRange = diceRange(skill.damage, 2);
  return {
    mode,
    hitPct: chanceFromThreshold(defense - bonus, mode),
    critPct: critChance(mode),
    dmgMin: Math.max(1, range.min + flat),
    dmgMax: Math.max(1, range.max + flat),
    critMax: Math.max(1, critRange.max + flat)
  };
}

function socialChance(actor, target, skill) {
  const mode = attackRollMode(actor, target, skill);
  const dc = 12 + mod(target.stats[skill.dcStat || "spirit"]) + Math.floor(target.morale / 35);
  const bonus = mod(actor.stats[skill.stat]) + actor.prof + statusBonus(actor, "hit");
  return { mode, dc, pct: chanceFromThreshold(dc - bonus, mode) };
}

function debuffChance(actor, target, skill) {
  const mode = attackRollMode(actor, target, skill);
  const dc = skill.fixedDc || 12;
  const bonus = mod(actor.stats[skill.stat]) + actor.prof;
  return { mode, dc, pct: chanceFromThreshold(dc - bonus, mode) };
}

function pctClass(pct) {
  return pct >= 75 ? "pct-good" : pct >= 45 ? "pct-mid" : "pct-bad";
}

function vsRow(label, value, extraClass = "") {
  return `<div class="vs-row"><span>${escapeHtml(label)}</span><strong class="${extraClass}">${value}</strong></div>`;
}

function renderForecast() {
  const unit = selectedUnit();
  const skill = selectedSkill();
  const hover = state.hoverTile || state.selectedTile;
  const target = hover ? unitAt(hover.x, hover.y) : null;
  if (!unit || !skill) {
    els.forecastPanel.innerHTML = `<h2>전투 예측</h2><p class="empty-hint">무공을 선택하고 적을 가리키면 명중률, 피해, 반격 여부를 미리 보여줍니다.</p>`;
    return;
  }

  if (target && isValidSkillTarget(unit, skill, target)) {
    const dist = distance(unit.position, target.position);

    if (skill.type === "attack") {
      const atk = computeStrike(unit, target, skill);
      const counterSkill = findCounterSkill(target, unit);
      const counter = counterSkill ? computeStrike(target, unit, counterSkill) : null;
      const followUp = canFollowUp(unit, target, skill);
      els.forecastPanel.innerHTML = `
        <h2>전투 예측</h2>
        <div class="vs-card">
          <div class="vs-side atk">
            <span class="vs-tag">공격</span>
            <strong class="vs-name">${escapeHtml(unit.short)}</strong>
            <span class="vs-skill">${escapeHtml(skill.name)}</span>
            ${vsRow("HP", `${unit.hp}/${unit.maxHp}`)}
            ${vsRow("명중", `${atk.hitPct}% <small>${rollModeLabel(atk.mode)}</small>`, pctClass(atk.hitPct))}
            ${vsRow("피해", `${atk.dmgMin}~${atk.dmgMax}`)}
            ${vsRow("필살", `${atk.critPct}% <small>~${atk.critMax}</small>`)}
          </div>
          <div class="vs-mid"><b>VS</b><span>거리 ${dist}</span></div>
          <div class="vs-side def">
            <span class="vs-tag">수비</span>
            <strong class="vs-name">${escapeHtml(target.short)}</strong>
            <span class="vs-skill">${counterSkill ? escapeHtml(counterSkill.name) : "반격 불가"}</span>
            ${vsRow("HP", `${target.hp}/${target.maxHp}`)}
            ${counter ? vsRow("명중", `${counter.hitPct}% <small>${rollModeLabel(counter.mode)}</small>`, pctClass(counter.hitPct)) : vsRow("명중", "-")}
            ${counter ? vsRow("피해", `${counter.dmgMin}~${counter.dmgMax}`) : vsRow("피해", "-")}
            ${vsRow("파훼", `${target.breakGauge}/100`)}
          </div>
        </div>
        <div class="vs-foot">${followUp ? `⚡ 추격 — ${escapeHtml(unit.short)}이(가) 민첩 우위로 한 번 더 공격합니다.` : "추격 없음 (민첩 차 5 이상이면 2회 공격)"}</div>
      `;
      return;
    }

    if (skill.type === "social") {
      const info = socialChance(unit, target, skill);
      els.forecastPanel.innerHTML = `
        <h2>전투 예측</h2>
        <div class="forecast-card">
          <div class="forecast-row"><span>심리전</span><strong>${escapeHtml(unit.short)} · ${escapeHtml(skill.name)}</strong></div>
          <div class="forecast-row"><span>대상</span><strong>${escapeHtml(target.name)} · 거리 ${dist}</strong></div>
          <div class="forecast-row"><span>성공률</span><strong class="${pctClass(info.pct)}">${info.pct}% (${rollModeLabel(info.mode)} vs DC ${info.dc})</strong></div>
          <div class="forecast-row"><span>성공 시</span><strong>기세 -${skill.moraleDamage}, 파훼 +${skill.breakGain || 0}</strong></div>
          <div class="forecast-row"><span>실패 시</span><strong>${escapeHtml(unit.short)} 기세 -5</strong></div>
        </div>
      `;
      return;
    }

    if (skill.type === "debuff") {
      const info = debuffChance(unit, target, skill);
      els.forecastPanel.innerHTML = `
        <h2>전투 예측</h2>
        <div class="forecast-card">
          <div class="forecast-row"><span>제압기</span><strong>${escapeHtml(unit.short)} · ${escapeHtml(skill.name)}</strong></div>
          <div class="forecast-row"><span>대상</span><strong>${escapeHtml(target.name)} · 거리 ${dist}</strong></div>
          <div class="forecast-row"><span>성공률</span><strong class="${pctClass(info.pct)}">${info.pct}% (${rollModeLabel(info.mode)} vs DC ${info.dc})</strong></div>
          <div class="forecast-row"><span>성공 시</span><strong>파훼 +${skill.breakGain || 0}${skill.statusTarget ? `, ${escapeHtml(skill.statusTarget.name)} ${skill.statusTarget.duration}턴` : ""}</strong></div>
        </div>
      `;
      return;
    }

    if (skill.type === "heal") {
      const range = diceRange(skill.heal);
      const bonus = mod(unit.stats[skill.stat]);
      els.forecastPanel.innerHTML = `
        <h2>전투 예측</h2>
        <div class="forecast-card">
          <div class="forecast-row"><span>회복</span><strong>${escapeHtml(unit.short)} · ${escapeHtml(skill.name)}</strong></div>
          <div class="forecast-row"><span>대상</span><strong>${escapeHtml(target.name)} · HP ${target.hp}/${target.maxHp}</strong></div>
          <div class="forecast-row"><span>회복량</span><strong class="pct-good">${Math.max(1, range.min + bonus)}~${Math.max(1, range.max + bonus)}</strong></div>
          <div class="forecast-row"><span>부가</span><strong>중독 · 화상 정화</strong></div>
        </div>
      `;
      return;
    }
  }

  if (skill.type === "aoe" || skill.type === "terrainSkill") {
    const range = diceRange(skill.damage);
    els.forecastPanel.innerHTML = `
      <h2>전투 예측</h2>
      <div class="forecast-card">
        <div class="forecast-row"><span>무공</span><strong>${escapeHtml(unit.short)} · ${escapeHtml(skill.name)}</strong></div>
        <div class="forecast-row"><span>방식</span><strong>붉은 칸 중 지점을 클릭</strong></div>
        ${skill.type === "aoe" ? `<div class="forecast-row"><span>범위</span><strong>중심 ${skill.radius || 1}칸</strong></div><div class="forecast-row"><span>피해</span><strong>${range.min}~${range.max} + 보정</strong></div>` : `<div class="forecast-row"><span>효과</span><strong>${escapeHtml(skill.desc)}</strong></div>`}
      </div>
    `;
    return;
  }

  const targets = listTargetsForSkill(unit, skill);
  els.forecastPanel.innerHTML = `
    <h2>대상 후보</h2>
    ${targets.length ? `<div class="target-list">${targets.map((candidate) => {
      let pctText = "";
      if (skill.type === "attack") {
        const strike = computeStrike(unit, candidate, skill);
        pctText = `명중 <b class="${pctClass(strike.hitPct)}">${strike.hitPct}%</b> · 피해 ${strike.dmgMin}~${strike.dmgMax}`;
      } else if (skill.type === "social") {
        pctText = `성공 <b class="${pctClass(socialChance(unit, candidate, skill).pct)}">${socialChance(unit, candidate, skill).pct}%</b>`;
      } else if (skill.type === "debuff") {
        pctText = `성공 <b class="${pctClass(debuffChance(unit, candidate, skill).pct)}">${debuffChance(unit, candidate, skill).pct}%</b>`;
      } else if (skill.type === "heal") {
        pctText = `HP ${candidate.hp}/${candidate.maxHp}`;
      }
      const counterText = isHostileSkill(skill) ? ` · 반격 ${escapeHtml(findCounterSkill(candidate, unit)?.name || "불가")}` : "";
      return `<button class="target-button" type="button" data-target="${candidate.id}"><strong>${escapeHtml(candidate.name)}</strong><span>거리 ${distance(unit.position, candidate.position)} · ${pctText}${counterText}</span></button>`;
    }).join("")}</div>` : `<p class="empty-hint">현재 위치에서 사거리 안 대상이 없습니다. 붉은 칸 표시를 참고하세요.</p>`}
  `;
  els.forecastPanel.querySelectorAll(".target-button").forEach((btn) => {
    btn.addEventListener("click", () => {
      const candidate = getUnit(btn.dataset.target);
      if (candidate) handleTargetTile(getTile(candidate.position.x, candidate.position.y), candidate);
    });
  });
}

function listTargetsForSkill(unit, skill) {
  if (skill.type === "terrainSkill" || skill.type === "aoe") return [];
  return state.units.filter((target) => !target.defeated && !target.surrendered && isValidSkillTarget(unit, skill, target));
}

function renderTilePanel() {
  const tile = state.hoverTile || state.selectedTile || getTile(0, 0);
  const unit = unitAt(tile.x, tile.y);
  const prop = propAt(tile.x, tile.y);
  els.tilePanel.innerHTML = `
    <h2>타일 정보</h2>
    <dl>
      <dt>좌표</dt><dd>${tile.x + 1}, ${tile.y + 1}</dd>
      <dt>지형</dt><dd>${escapeHtml(tile.label)}</dd>
      <dt>이동비용</dt><dd>${tile.walkable ? tile.moveCost : "불가"}</dd>
      <dt>방어/회피</dt><dd>${signed(tile.defense)} / ${signed(tile.avoid)}</dd>
      <dt>고도</dt><dd>${tile.height}</dd>
      <dt>엄폐</dt><dd>${escapeHtml(tile.cover)}</dd>
      <dt>기물</dt><dd>${prop && !prop.used ? `${escapeHtml(prop.label)} · DC ${prop.dc}` : tile.smoke ? `연막 ${tile.smoke}` : tile.trap ? `덫 ${tile.trap}` : "없음"}</dd>
      <dt>유닛</dt><dd>${unit ? escapeHtml(unit.name) : "없음"}</dd>
    </dl>
  `;
}

function renderRoster() {
  renderRosterList(els.allyRoster, "ally");
  renderRosterList(els.enemyRoster, "enemy");
}

function renderRosterList(el, faction) {
  el.innerHTML = "";
  state.units.filter((unit) => unit.faction === faction).forEach((unit) => {
    const li = document.createElement("li");
    li.className = [
      "roster-item",
      faction,
      unit.acted ? "acted" : "",
      unit.defeated || unit.surrendered ? "defeated" : "",
      unit.id === state.selectedUnitId ? "selected" : ""
    ].filter(Boolean).join(" ");
    const btn = document.createElement("button");
    btn.type = "button";
    btn.className = "roster-card";
    const hpPct = clamp(Math.round((unit.hp / unit.maxHp) * 100), 0, 100);
    btn.innerHTML = `<span class="faction-line"></span><span class="roster-main"><span class="roster-name">${escapeHtml(unit.name)}</span><span class="roster-role">${escapeHtml(unit.role)}</span><span class="roster-hpbar"><span style="width:${hpPct}%"></span></span></span><span class="roster-hp">${unit.hp}/${unit.maxHp}</span>`;
    btn.title = unit.defeated || unit.surrendered ? `${unit.name} 전투 이탈` : `${unit.name} — 클릭해 ${unit.faction === "ally" ? "선택/확인" : "위치 확인"}`;
    btn.addEventListener("click", () => handleRosterClick(unit));
    li.appendChild(btn);
    el.appendChild(li);
  });
}

function handleRosterClick(unit) {
  if (state.battleOver) {
    focusUnitTile(unit);
    return;
  }
  const actor = selectedUnit();
  const skill = selectedSkill();
  if (state.mode === "target" && actor && skill && !unit.defeated && !unit.surrendered && isValidSkillTarget(actor, skill, unit)) {
    handleTargetTile(getTile(unit.position.x, unit.position.y), unit);
    return;
  }
  if (unit.faction === "ally" && state.phase === "player" && !unit.acted && !unit.defeated && !unit.surrendered) {
    selectUnit(unit.id);
    return;
  }
  focusUnitTile(unit);
}

function focusUnitTile(unit) {
  const tile = getTile(unit.position.x, unit.position.y);
  if (!tile) return;
  state.selectedTile = tile;
  state.hoverTile = tile;
  render();
}

function renderLog() {
  els.combatLog.innerHTML = "";
  state.log.slice(-90).forEach((entry) => {
    const li = document.createElement("li");
    li.innerHTML = `<span class="log-tag">${escapeHtml(entry.tag)}</span><span class="log-${entry.tone}">${escapeHtml(entry.text)}</span>`;
    els.combatLog.appendChild(li);
  });
}

function meter(label, value, max, className) {
  const pct = max ? clamp(Math.round((value / max) * 100), 0, 100) : 0;
  return `<div class="meter"><span>${label}</span><div class="meter-bar ${className}"><span style="width:${pct}%"></span></div><span>${value}/${max}</span></div>`;
}

function tileTitle(tile) {
  return `${tile.x + 1},${tile.y + 1} · ${tile.label} · 이동 ${tile.walkable ? tile.moveCost : "불가"} · 방어 ${signed(tile.defense)} · 회피 ${signed(tile.avoid)} · 고도 ${tile.height}`;
}

function signed(value) { return value >= 0 ? `+${value}` : String(value); }
function escapeHtml(value) { return String(value).replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/\"/g, "&quot;").replace(/'/g, "&#039;"); }
function logEvent(tag, text, tone = "system") { state.log.push({ tag, text, tone }); if (state.log.length > 120) state.log.shift(); }
