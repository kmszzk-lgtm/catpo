// 게임 데이터: 타일, 아이템, 제작법, 밸런스 수치
'use strict';

const TILE = 32;
const WORLD_W = 112;
const WORLD_H = 112;
const DAY_SECONDS = 240; // 실제 시간 240초 = 게임 속 하루

const T = { DEEP: 0, SHALLOW: 1, SAND: 2, GRASS: 3, FOREST: 4, ROCKY: 5, POND: 6 };

const TILE_INFO = [
  { name: '깊은 바다', walk: false, color: [38, 92, 140] },
  { name: '얕은 바다', walk: true, speed: 0.55, color: [64, 146, 176] },
  { name: '모래사장', walk: true, color: [226, 205, 150] },
  { name: '풀밭', walk: true, color: [120, 170, 82] },
  { name: '숲', walk: true, color: [82, 134, 70] },
  { name: '바위 지대', walk: true, color: [146, 142, 128] },
  { name: '샘물', walk: false, color: [84, 168, 196] },
];

const isSea = (t) => t === T.DEEP || t === T.SHALLOW;
const isLand = (t) => t === T.SAND || t === T.GRASS || t === T.FOREST || t === T.ROCKY;

// kind: res 재료, food 음식, heal 치료, tool 도구, build 설치물
const ITEMS = {
  wood:       { name: '나무',       kind: 'res',   desc: '도구와 모닥불의 기본 재료.' },
  stone:      { name: '돌',         kind: 'res',   desc: '단단한 도구와 화덕에 쓰여요.' },
  fiber:      { name: '풀',         kind: 'res',   desc: '엮어서 끈과 둥지를 만들어요.' },
  hide:       { name: '가죽',       kind: 'res',   desc: '들개에게서 얻는 질긴 가죽.' },
  berry:      { name: '산딸기',     kind: 'food',  hunger: 12, thirst: 4, desc: '배고픔 +12, 수분 +4' },
  coconut:    { name: '코코넛',     kind: 'food',  hunger: 8, thirst: 28, desc: '배고픔 +8, 수분 +28' },
  fish:       { name: '날생선',     kind: 'food',  hunger: 14, desc: '배고픔 +14. 모닥불에 구우면 더 좋아요.', cook: 'cookedFish' },
  cookedFish: { name: '구운 생선',  kind: 'food',  hunger: 38, hp: 6, desc: '배고픔 +38, 체력 +6' },
  crab:       { name: '게살',       kind: 'food',  hunger: 10, desc: '배고픔 +10. 구우면 더 좋아요.', cook: 'cookedCrab' },
  cookedCrab: { name: '게살 구이',  kind: 'food',  hunger: 30, hp: 10, desc: '배고픔 +30, 체력 +10' },
  herb:       { name: '약초',       kind: 'heal',  hp: 18, desc: '체력 +18' },
  bandage:    { name: '약초 붕대',  kind: 'heal',  hp: 45, desc: '체력 +45' },
  axe:        { name: '돌도끼',     kind: 'tool',  desc: '나무와 돌을 두 배 빨리 캐요.' },
  spear:      { name: '돌창',       kind: 'tool',  desc: '공격력과 사거리가 늘어나요.' },
  rod:        { name: '낚싯대',     kind: 'tool',  desc: '바다를 보고 E를 눌러 낚시해요.' },
  cloak:      { name: '가죽 망토',  kind: 'tool',  desc: '밤에 체온이 절반만 떨어져요.' },
  campfire:   { name: '모닥불',     kind: 'build', desc: '빛과 온기. 들개가 다가오지 못해요.' },
  wall:       { name: '나무 울타리', kind: 'build', desc: '들개의 길을 막는 벽.' },
  nest:       { name: '풀 둥지',    kind: 'build', desc: '밤에 E를 눌러 아침까지 잘 수 있어요.' },
};

const ITEM_ORDER = Object.keys(ITEMS);

const RECIPES = [
  { id: 'axe',      need: { wood: 3, stone: 3, fiber: 2 } },
  { id: 'campfire', need: { wood: 5, stone: 3 } },
  { id: 'rod',      need: { wood: 3, fiber: 5 } },
  { id: 'spear',    need: { wood: 3, stone: 4, fiber: 3 } },
  { id: 'wall',     need: { wood: 4 } },
  { id: 'nest',     need: { wood: 4, fiber: 8 } },
  { id: 'bandage',  need: { herb: 2, fiber: 2 } },
  { id: 'cloak',    need: { hide: 3, fiber: 4 } },
];

// 맵 위 오브젝트 정의
// solid: 'circle'(r) 또는 'box', gather: 채집 결과
const OBJECTS = {
  tree:     { name: '나무', hp: 5, solid: 'circle', r: 11, axe: true, regrow: 'stump', yields: { wood: [2, 3] }, verb: '나무 베기' },
  stump:    { name: '그루터기', solid: null, growTo: 'tree', growDays: 1.5 },
  palm:     { name: '야자수', hp: 4, solid: 'circle', r: 9, axe: true, regrow: 'sapling', yields: { wood: [2, 2] }, verb: '야자수 베기' },
  sapling:  { name: '야자 새싹', solid: null, growTo: 'palm', growDays: 1.5 },
  rock:     { name: '바위', hp: 6, solid: 'circle', r: 12, axe: true, regrow: 'hidden', regrowDays: 2, yields: { stone: [2, 3] }, verb: '돌 캐기' },
  bush:     { name: '덤불', solid: 'circle', r: 10, verb: '열매 따기' },
  tuft:     { name: '풀숲', hp: 1, solid: null, regrow: 'hidden', regrowDays: 1, yields: { fiber: [2, 3] }, verb: '풀 뜯기' },
  herb:     { name: '약초', hp: 1, solid: null, regrow: 'hidden', regrowDays: 2, yields: { herb: [1, 1] }, verb: '약초 캐기' },
  hidden:   { name: '', solid: null },
  campfire: { name: '모닥불', solid: 'circle', r: 12, built: true },
  wall:     { name: '나무 울타리', solid: 'box', built: true, hp: 30 },
  nest:     { name: '풀 둥지', solid: null, built: true },
};

const ENEMIES = {
  crab: { name: '게',   hp: 3, speed: 38, dmg: 6,  r: 9,  drops: { crab: [1, 2] } },
  dog:  { name: '들개', hp: 6, speed: 96, dmg: 12, r: 11, drops: { hide: [1, 2] } },
};

const BALANCE = {
  playerSpeed: 122,
  hungerRate: 0.27,   // 초당 감소량
  thirstRate: 0.36,
  warmthLossNight: 0.9,
  warmthGainDay: 2.2,
  warmthGainFire: 5,
  starveDamage: 1.6,
  regen: 0.5,
  drinkAmount: 30,
  fireRadius: 140,    // 빛 반경
  fireWarmRadius: 96,
  fireBurnRate: 0.55, // 초당 연료 소모
  fireAddFuel: 35,
  attackCooldown: 0.38,
};
