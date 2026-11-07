// 게임 상태와 규칙
'use strict';

function newGame(world) {
  const g = {
    world,
    time: 0.04,
    player: {
      x: world.spawn.x, y: world.spawn.y, r: 10,
      fx: 0, fy: -1, anim: 0, moving: false,
      hp: 100, hunger: 85, thirst: 80, warmth: 100,
      attackCd: 0, swipe: 0, swipeAngle: 0, hurt: 0,
      sleeping: false, lastCause: null,
    },
    inv: { berry: 2 },
    invVersion: 1,
    enemies: [],
    floaters: [],
    shaking: new Set(),
    fishing: null,
    placing: null,
    message: null,
    stats: { kills: 0, crafted: 0, built: 0, fish: 0 },
    tips: {},
    spawnTimer: 0,
    growTimer: 0,
    lastDay: 0,
    nightWarned: false,
    shake: 0,
    dead: false,
    events: [], // UI/사운드로 전달할 이벤트
  };
  revealAround(world, g.player.x, g.player.y, 9);
  return g;
}

// ── 시간 ────────────────────────────────────────────
function dayNumber(g) { return Math.floor(g.time) + 1; }

function clockText(time) {
  const f = dayFrac(time);
  const mins = Math.floor(((6 + f * 24) % 24) * 60);
  const h = Math.floor(mins / 60), m = mins % 60;
  const ampm = h < 12 ? '오전' : '오후';
  const h12 = h % 12 === 0 ? 12 : h % 12;
  return `${ampm} ${h12}:${String(m).padStart(2, '0')}`;
}

function phaseName(time) {
  const f = dayFrac(time);
  if (f < 0.55) return '낮';
  if (f < 0.68) return '저녁';
  if (f < 0.92) return '밤';
  return '새벽';
}

const isNightTime = (time) => { const f = dayFrac(time); return f >= 0.66 && f < 0.93; };
const isDayTime = (time) => { const f = dayFrac(time); return f < 0.58 || f >= 0.97; };

// ── 인벤토리 ────────────────────────────────────────
function addItem(g, id, n = 1) {
  g.inv[id] = (g.inv[id] || 0) + n;
  g.invVersion++;
}

function removeItem(g, id, n = 1) {
  if ((g.inv[id] || 0) < n) return false;
  g.inv[id] -= n;
  if (g.inv[id] <= 0) delete g.inv[id];
  g.invVersion++;
  return true;
}

const hasItem = (g, id, n = 1) => (g.inv[id] || 0) >= n;

function canCraft(g, recipe) {
  if (ITEMS[recipe.id].kind === 'tool' && hasItem(g, recipe.id)) return false;
  return Object.entries(recipe.need).every(([id, n]) => hasItem(g, id, n));
}

function craft(g, recipeId) {
  const recipe = RECIPES.find((r) => r.id === recipeId);
  if (!recipe) return false;
  if (!canCraft(g, recipe)) {
    say(g, hasItem(g, recipe.id) && ITEMS[recipe.id].kind === 'tool' ? '이미 가지고 있어요.' : '재료가 부족해요.');
    return false;
  }
  for (const [id, n] of Object.entries(recipe.need)) removeItem(g, id, n);
  addItem(g, recipe.id, 1);
  g.stats.crafted++;
  emit(g, 'sound', 'craft');
  float(g, g.player.x, g.player.y - 24, `${ITEMS[recipe.id].name} 완성!`, '#ffe9a8');
  if (ITEMS[recipe.id].kind === 'build') {
    g.placing = { id: recipe.id };
    say(g, `${ITEMS[recipe.id].name}을(를) 놓을 곳을 바라보고 [행동]을 누르세요. [상호작용]은 취소.`);
  } else {
    say(g, `${ITEMS[recipe.id].name}: ${ITEMS[recipe.id].desc}`);
  }
  return true;
}

function useItem(g, id) {
  const p = g.player;
  const item = ITEMS[id];
  if (!item || !hasItem(g, id) || p.sleeping) return;
  if (item.kind === 'food' || item.kind === 'heal') {
    if (item.kind === 'heal' && p.hp >= 100) { say(g, '체력이 이미 가득해요.'); return; }
    if (item.kind === 'food' && p.hunger >= 99 && !(item.thirst && p.thirst < 95)) { say(g, '배가 불러요.'); return; }
    removeItem(g, id);
    const parts = [];
    if (item.hunger) { p.hunger = Math.min(100, p.hunger + item.hunger); parts.push(`배고픔 +${item.hunger}`); }
    if (item.thirst) { p.thirst = Math.min(100, p.thirst + item.thirst); parts.push(`수분 +${item.thirst}`); }
    if (item.hp) { p.hp = Math.min(100, p.hp + item.hp); parts.push(`체력 +${item.hp}`); }
    float(g, p.x, p.y - 26, parts.join(' '), '#c8f2a8');
    emit(g, 'sound', item.kind === 'heal' ? 'heal' : 'eat');
  } else if (item.kind === 'build') {
    g.placing = g.placing && g.placing.id === id ? null : { id };
    if (g.placing) say(g, `${item.name}: 놓을 곳을 바라보고 [행동]으로 설치, [상호작용]으로 취소.`);
  } else {
    say(g, `${item.name}: ${item.desc}`);
  }
}

// ── 메시지/이펙트 ───────────────────────────────────
function say(g, text) { g.message = { text, t: 3.2 }; }
function emit(g, type, data) { g.events.push({ type, data }); }
function float(g, x, y, text, color = '#fff') {
  g.floaters.push({ x, y, text, color, t: 1.3 });
  if (g.floaters.length > 24) g.floaters.shift();
}
function tip(g, key, text) {
  if (g.tips[key]) return;
  g.tips[key] = 1;
  say(g, text);
}

// ── 대상 찾기 ───────────────────────────────────────
const GATHER_TYPES = new Set(['tree', 'palm', 'rock', 'bush', 'tuft', 'herb']);
const USE_TYPES = new Set(['campfire', 'nest']);

function findTarget(g, types) {
  const p = g.player, w = g.world;
  const ptx = Math.floor(p.x / TILE), pty = Math.floor(p.y / TILE);
  let best = null, bestScore = Infinity;
  for (let ty = pty - 2; ty <= pty + 2; ty++) {
    for (let tx = ptx - 2; tx <= ptx + 2; tx++) {
      const o = objAt(w, tx, ty);
      if (!o || !types.has(o.type)) continue;
      const ox = tx * TILE + TILE / 2, oy = ty * TILE + TILE / 2;
      const dx = ox - p.x, dy = oy - p.y;
      const d = Math.hypot(dx, dy);
      if (d > 46) continue;
      const dot = d > 0 ? (dx * p.fx + dy * p.fy) / d : 1;
      if (d > 22 && dot < 0.25) continue;
      const score = d - dot * 14;
      if (score < bestScore) { bestScore = score; best = { tx, ty, obj: o, x: ox, y: oy }; }
    }
  }
  return best;
}

function nearbyWater(g) {
  const p = g.player, w = g.world;
  const probe = (ang, d) => {
    const x = p.x + Math.cos(ang) * d, y = p.y + Math.sin(ang) * d;
    return { t: tileAt(w, Math.floor(x / TILE), Math.floor(y / TILE)), x, y, ang };
  };
  const face = Math.atan2(p.fy, p.fx);
  const order = [0, 0.5, -0.5, 1, -1, 1.6, -1.6, 2.4, -2.4, Math.PI];
  let sea = null;
  for (const da of order) {
    for (const d of [p.r + 8, p.r + 20]) {
      const s = probe(face + da, d);
      if (s.t === T.POND) return { kind: 'pond', x: s.x, y: s.y };
      if (!sea && isSea(s.t) && Math.abs(da) <= 1) {
        const far = probe(face + da, d + 18);
        sea = { kind: 'sea', x: far.x, y: far.y };
      }
    }
  }
  return sea;
}

function placementTile(g) {
  const p = g.player;
  const tx = Math.floor((p.x + p.fx * TILE * 0.95) / TILE);
  const ty = Math.floor((p.y + p.fy * TILE * 0.95) / TILE);
  const id = g.placing.id;
  let ok = canBuildAt(g.world, tx, ty);
  if (ok && OBJECTS[id].solid) {
    const bx = tx * TILE, by = ty * TILE;
    if (circleRect(p.x, p.y, p.r, bx, by, TILE, TILE)) ok = false;
    for (const e of g.enemies) if (circleRect(e.x, e.y, e.r, bx, by, TILE, TILE)) ok = false;
  }
  return { tx, ty, ok };
}

// ── 행동 (Space / A) ────────────────────────────────
function doAction(g) {
  const p = g.player;
  if (g.dead) return;
  if (p.sleeping) { wakeUp(g, '잠에서 깼어요.'); return; }
  if (g.placing) { tryPlace(g); return; }
  if (g.fishing) { reel(g); return; }
  if (p.attackCd > 0) return;
  p.attackCd = BALANCE.attackCooldown;
  p.swipe = 1;
  p.swipeAngle = Math.atan2(p.fy, p.fx);

  const spear = hasItem(g, 'spear');
  const reach = spear ? 40 : 28;
  const dmg = spear ? 3 : hasItem(g, 'axe') ? 2 : 1;
  let hit = false;
  for (const e of g.enemies) {
    const dx = e.x - p.x, dy = e.y - p.y;
    const d = Math.hypot(dx, dy);
    if (d - e.r > reach) continue;
    const dot = d > 0 ? (dx * p.fx + dy * p.fy) / d : 1;
    if (dot < 0.15 && d > e.r + p.r + 4) continue;
    damageEnemy(g, e, dmg, dx / (d || 1), dy / (d || 1));
    hit = true;
  }
  if (hit) { emit(g, 'sound', 'hit'); return; }

  const t = findTarget(g, GATHER_TYPES);
  if (!t) { emit(g, 'sound', 'swish'); return; }
  gather(g, t);
}

function giveYields(g, yields, x, y) {
  const parts = [];
  for (const [id, [lo, hi]] of Object.entries(yields)) {
    const n = lo + Math.floor(Math.random() * (hi - lo + 1));
    if (n <= 0) continue;
    addItem(g, id, n);
    parts.push(`+${n} ${ITEMS[id].name}`);
  }
  if (parts.length) float(g, x, y - 18, parts.join('  '), '#fff3c4');
}

function gather(g, t) {
  const o = t.obj, def = OBJECTS[o.type];
  const axe = hasItem(g, 'axe');
  o.hit = 0.25;
  g.shaking.add(o);

  if (o.type === 'bush') {
    if (o.berries) {
      o.berries = 0;
      o.timer = 1;
      giveYields(g, { berry: [2, 3] }, t.x, t.y);
      emit(g, 'sound', 'pick');
    } else {
      say(g, '아직 열매가 열리지 않았어요. 하루쯤 지나면 다시 열려요.');
      emit(g, 'sound', 'thud');
    }
    return;
  }
  if (o.type === 'palm' && o.fruit > 0) {
    o.fruit--;
    giveYields(g, { coconut: [1, 1] }, t.x, t.y - 10);
    emit(g, 'sound', 'pick');
    tip(g, 'coconut', '코코넛은 수분을 많이 채워 줘요. 아이템을 눌러 먹을 수 있어요.');
    return;
  }

  o.hp -= def.axe && axe ? 2 : 1;
  emit(g, 'sound', o.type === 'rock' ? 'stone' : o.type === 'tuft' || o.type === 'herb' ? 'pick' : 'chop');
  if (def.axe && !axe) tip(g, 'axe', '돌도끼를 만들면 나무와 돌을 두 배 빨리 캘 수 있어요. [제작]을 열어 보세요.');
  if (o.hp > 0) return;

  giveYields(g, def.yields, t.x, t.y);
  g.shaking.delete(o);
  let next = null;
  if (def.regrow === 'stump') next = makeObject('stump');
  else if (def.regrow === 'sapling') next = makeObject('sapling');
  else if (def.regrow === 'hidden') next = { type: 'hidden', into: o.type, timer: def.regrowDays, v: 0 };
  setObj(g.world, t.tx, t.ty, next);
}

function tryPlace(g) {
  const pt = placementTile(g);
  const id = g.placing.id;
  if (!pt.ok) { say(g, '여기에는 놓을 수 없어요. 빈 땅을 바라보세요.'); emit(g, 'sound', 'thud'); return; }
  if (!removeItem(g, id)) { g.placing = null; return; }
  setObj(g.world, pt.tx, pt.ty, makeObject(id));
  g.stats.built++;
  emit(g, 'sound', 'build');
  if (id === 'campfire') tip(g, 'fire', '모닥불 앞에서 [상호작용]: 날음식 굽기, 나무 넣기. 불빛 안으로는 들개가 못 들어와요.');
  if (id === 'nest') tip(g, 'nest', '밤에 둥지 앞에서 [상호작용]을 누르면 아침까지 잘 수 있어요.');
  if (!hasItem(g, id)) g.placing = null;
}

// ── 상호작용 (E / B) ────────────────────────────────
function doInteract(g) {
  const p = g.player;
  if (g.dead) return;
  if (p.sleeping) { wakeUp(g, '잠에서 깼어요.'); return; }
  if (g.placing) { g.placing = null; say(g, '설치를 취소했어요.'); return; }
  if (g.fishing) { reel(g); return; }

  const t = findTarget(g, USE_TYPES);
  if (t) {
    if (t.obj.type === 'campfire') useFire(g, t.obj, t);
    else if (t.obj.type === 'nest') trySleep(g, t);
    return;
  }
  const water = nearbyWater(g);
  if (water && water.kind === 'pond') {
    if (p.thirst >= 99) { say(g, '목마르지 않아요.'); return; }
    p.thirst = Math.min(100, p.thirst + BALANCE.drinkAmount);
    float(g, p.x, p.y - 26, `수분 +${BALANCE.drinkAmount}`, '#a8e4ff');
    emit(g, 'sound', 'drink');
    return;
  }
  if (water && water.kind === 'sea') {
    if (hasItem(g, 'rod')) startFishing(g, water);
    else say(g, '바닷물은 짜서 마실 수 없어요. 낚싯대가 있으면 낚시를 할 수 있어요.');
    return;
  }
  say(g, '주변에 상호작용할 것이 없어요. 샘물, 바다, 모닥불, 둥지 앞에서 눌러 보세요.');
}

function firstRaw(g) {
  return ITEM_ORDER.find((id) => ITEMS[id].cook && hasItem(g, id));
}

function useFire(g, fire, t) {
  const raw = firstRaw(g);
  if (fire.fuel <= 0) {
    if (removeItem(g, 'wood')) { fire.fuel = BALANCE.fireAddFuel; float(g, t.x, t.y - 26, '불을 다시 붙였어요', '#ffd08a'); emit(g, 'sound', 'fire'); }
    else say(g, '불이 꺼졌어요. 나무가 있어야 다시 붙일 수 있어요.');
    return;
  }
  if (raw) {
    removeItem(g, raw);
    addItem(g, ITEMS[raw].cook);
    float(g, t.x, t.y - 26, `${ITEMS[ITEMS[raw].cook].name} 완성!`, '#ffd08a');
    emit(g, 'sound', 'cook');
    return;
  }
  if (fire.fuel < 90 && removeItem(g, 'wood')) {
    fire.fuel = Math.min(100, fire.fuel + BALANCE.fireAddFuel);
    float(g, t.x, t.y - 26, '+ 장작', '#ffd08a');
    emit(g, 'sound', 'fire');
    return;
  }
  say(g, fire.fuel >= 90 ? '불이 활활 타고 있어요.' : '넣을 나무가 없어요.');
}

function trySleep(g, t) {
  const f = dayFrac(g.time);
  if (f < 0.6 && f > 0.02) { say(g, '아직 졸리지 않아요. 저녁이 되면 잘 수 있어요.'); return; }
  const near = g.enemies.some((e) => e.type === 'dog' && dist(e.x, e.y, g.player.x, g.player.y) < 260);
  if (near) { say(g, '들개가 가까이 있어서 잘 수 없어요!'); return; }
  const p = g.player;
  p.x = t.x; p.y = t.y + 2;
  p.sleeping = true;
  g.fishing = null;
  say(g, '쿨쿨... 아침까지 잠을 자요. [행동]으로 일어날 수 있어요.');
  emit(g, 'sound', 'sleep');
}

function wakeUp(g, text) {
  g.player.sleeping = false;
  if (text) say(g, text);
}

// ── 낚시 ────────────────────────────────────────────
function startFishing(g, water) {
  g.fishing = { phase: 'wait', t: 1.5 + Math.random() * 3, x: water.x, y: water.y, px: g.player.x, py: g.player.y };
  emit(g, 'sound', 'cast');
  tip(g, 'fish', '찌가 출렁이며 "!"가 뜨면 바로 [상호작용]을 누르세요.');
}

function reel(g) {
  const f = g.fishing;
  if (!f) return;
  if (f.phase === 'bite') {
    const n = Math.random() < 0.2 ? 2 : 1;
    addItem(g, 'fish', n);
    g.stats.fish += n;
    float(g, g.player.x, g.player.y - 26, `+${n} 날생선`, '#a8e4ff');
    emit(g, 'sound', 'splash');
  } else {
    say(g, '너무 일찍 당겼어요. 물고기가 놀라 도망갔어요.');
  }
  g.fishing = null;
}

function updateFishing(g, dt) {
  const f = g.fishing;
  if (!f) return;
  if (dist(f.px, f.py, g.player.x, g.player.y) > 4) { g.fishing = null; return; }
  f.t -= dt;
  if (f.t > 0) return;
  if (f.phase === 'wait') { f.phase = 'bite'; f.t = 0.9; emit(g, 'sound', 'bite'); }
  else { g.fishing = null; say(g, '놓쳤어요! 다시 던져 보세요.'); }
}

// ── 적 ──────────────────────────────────────────────
function spawnEnemy(g, type, x, y) {
  const def = ENEMIES[type];
  g.enemies.push({
    type, x, y, vx: 0, vy: 0, kx: 0, ky: 0,
    hp: def.hp, r: def.r, face: 1, cd: 0, windup: 0, flash: 0,
    seed: Math.random() * 100, wt: 0, wx: 0, wy: 0, angry: 0, stuck: 0, fleeing: false,
  });
}

function litFires(g) {
  const out = [];
  for (const i of g.world.fires) {
    const o = g.world.objects[i];
    if (o && o.fuel > 0) out.push({ x: (i % g.world.W) * TILE + TILE / 2, y: ((i / g.world.W) | 0) * TILE + TILE / 2, o });
  }
  return out;
}

function inFireLight(fires, x, y, pad = 0) {
  return fires.some((f) => dist(f.x, f.y, x, y) < BALANCE.fireRadius * 0.72 + pad);
}

function trySpawn(g) {
  const p = g.player, w = g.world;
  const fires = litFires(g);
  const dogs = g.enemies.filter((e) => e.type === 'dog').length;
  const crabs = g.enemies.filter((e) => e.type === 'crab').length;
  if (isNightTime(g.time)) {
    const target = Math.min(8, 2 + Math.floor((dayNumber(g) - 1) * 0.75));
    if (dogs < target) {
      for (let k = 0; k < 14; k++) {
        const a = Math.random() * Math.PI * 2, d = 400 + Math.random() * 160;
        const x = p.x + Math.cos(a) * d, y = p.y + Math.sin(a) * d;
        const t = tileAt(w, Math.floor(x / TILE), Math.floor(y / TILE));
        if (!isLand(t) || collides(w, x, y, 12) || inFireLight(fires, x, y, 40)) continue;
        spawnEnemy(g, 'dog', x, y);
        if (!g.tips.dog) tip(g, 'dog', '어둠 속에서 들개 울음소리가 들려요. 모닥불 빛 안에 있으면 안전해요.');
        emit(g, 'sound', 'howl');
        break;
      }
    }
  } else if (isDayTime(g.time) && crabs < 5) {
    for (let k = 0; k < 16; k++) {
      const a = Math.random() * Math.PI * 2, d = 220 + Math.random() * 300;
      const x = p.x + Math.cos(a) * d, y = p.y + Math.sin(a) * d;
      if (tileAt(w, Math.floor(x / TILE), Math.floor(y / TILE)) !== T.SAND || collides(w, x, y, 10)) continue;
      spawnEnemy(g, 'crab', x, y);
      break;
    }
  }
}

function damageEnemy(g, e, dmg, ax, ay) {
  e.hp -= dmg;
  e.flash = 0.15;
  e.kx = ax * 230; e.ky = ay * 230;
  e.windup = 0;
  if (e.type === 'crab') e.angry = 5;
  float(g, e.x, e.y - 18, `-${dmg}`, '#ffb3a0');
  if (e.hp <= 0) {
    g.enemies.splice(g.enemies.indexOf(e), 1);
    g.stats.kills++;
    giveYields(g, ENEMIES[e.type].drops, e.x, e.y);
    emit(g, 'sound', 'kill');
  }
}

function hurtPlayer(g, dmg, cause, fromX, fromY) {
  const p = g.player;
  if (p.hurt > 0 || g.dead) return;
  p.hp -= dmg;
  p.hurt = 0.7;
  p.lastCause = cause;
  g.shake = 0.25;
  const d = dist(p.x, p.y, fromX, fromY) || 1;
  moveEntity(g.world, p, ((p.x - fromX) / d) * 10, ((p.y - fromY) / d) * 10);
  float(g, p.x, p.y - 28, `-${dmg}`, '#ff7b6b');
  emit(g, 'sound', 'hurt');
  if (p.sleeping) wakeUp(g, '공격을 받아 잠에서 깼어요!');
  g.fishing = null;
}

function moveEntity(world, e, dx, dy) {
  let moved = false;
  if (dx && !collides(world, e.x + dx, e.y, e.r)) { e.x += dx; moved = true; }
  if (dy && !collides(world, e.x, e.y + dy, e.r)) { e.y += dy; moved = true; }
  return moved;
}

function steer(world, e, dirX, dirY, step) {
  for (const a of [0, 0.55, -0.55, 1.1, -1.1, 1.6, -1.6]) {
    const c = Math.cos(a), s = Math.sin(a);
    const mx = (dirX * c - dirY * s) * step, my = (dirX * s + dirY * c) * step;
    if (!collides(world, e.x + mx, e.y + my, e.r)) {
      e.x += mx; e.y += my;
      return true;
    }
  }
  return false;
}

function updateEnemies(g, dt) {
  const p = g.player, w = g.world;
  const fires = litFires(g);
  const night = isNightTime(g.time);
  const lightR = BALANCE.fireRadius * 0.72;

  for (let i = g.enemies.length - 1; i >= 0; i--) {
    const e = g.enemies[i];
    const def = ENEMIES[e.type];
    e.flash = Math.max(0, e.flash - dt);
    e.cd = Math.max(0, e.cd - dt);
    const dx = p.x - e.x, dy = p.y - e.y;
    const d = Math.hypot(dx, dy) || 1;

    // 넉백
    if (Math.abs(e.kx) + Math.abs(e.ky) > 1) {
      moveEntity(w, e, e.kx * dt, e.ky * dt);
      const decay = Math.exp(-12 * dt);
      e.kx *= decay; e.ky *= decay;
    }

    // 사라지기
    if (d > 950 || (e.type === 'crab' && !isDayTime(g.time) && d > 420) || (e.type === 'dog' && e.fleeing && d > 520)) {
      g.enemies.splice(i, 1);
      continue;
    }

    let mx = 0, my = 0, speed = def.speed;
    if (e.type === 'dog') {
      if (!night) e.fleeing = true;
      if (e.fleeing) {
        mx = -dx / d; my = -dy / d; speed *= 1.1;
      } else if (!p.dead) {
        mx = dx / d; my = dy / d;
      }
      // 불빛 회피
      for (const f of fires) {
        const fd = dist(e.x, e.y, f.x, f.y) || 1;
        if (fd < lightR + 36) {
          const push = (lightR + 36 - fd) / 36;
          mx += ((e.x - f.x) / fd) * push * 2.4;
          my += ((e.y - f.y) / fd) * push * 2.4;
        }
      }
      const playerLit = inFireLight(fires, p.x, p.y, 6);
      if (playerLit && d < lightR + 80) speed *= 0.35;
      // 공격
      if (!e.fleeing && e.windup <= 0 && e.cd <= 0 && d < e.r + p.r + 10 && !playerLit && !p.dead) {
        e.windup = 0.35;
      }
    } else {
      // 게
      e.angry = Math.max(0, e.angry - dt);
      if (e.angry > 0) { mx = dx / d; my = dy / d; speed = 58; }
      else {
        e.wt -= dt;
        if (e.wt <= 0) {
          e.wt = 1 + Math.random() * 2.5;
          const r = Math.random();
          e.wx = r < 0.35 ? 0 : (Math.random() < 0.5 ? -1 : 1);
          e.wy = (Math.random() - 0.5) * 0.4;
        }
        mx = e.wx; my = e.wy;
        const nt = tileAt(w, Math.floor((e.x + mx * 14) / TILE), Math.floor((e.y + my * 14) / TILE));
        if (nt !== T.SAND && nt !== T.SHALLOW) { e.wx = -e.wx; mx = e.wx; }
      }
      if (e.windup <= 0 && e.cd <= 0 && d < e.r + p.r + 8 && !p.sleeping) e.windup = 0.45;
    }

    if (e.windup > 0) {
      e.windup -= dt;
      mx *= 0.15; my *= 0.15;
      if (e.windup <= 0) {
        if (d < e.r + p.r + 16) hurtPlayer(g, def.dmg, e.type, e.x, e.y);
        e.cd = e.type === 'dog' ? 1.1 : 1.3;
        if (e.type === 'dog') { e.kx = -dx / d * 120; e.ky = -dy / d * 120; }
      }
    }

    const ml = Math.hypot(mx, my);
    const ox = e.x, oy = e.y;
    if (ml > 0.05) {
      const ux = mx / ml, uy = my / ml;
      if (Math.abs(ux) > 0.2) e.face = ux > 0 ? 1 : -1;
      const step = speed * Math.min(1, ml) * dt;
      // 울타리에 막힌 들개는 울타리를 공격
      const direct = collides(w, e.x + ux * (step + 4), e.y + uy * (step + 4), e.r);
      if (e.type === 'dog' && !e.fleeing && direct && direct.obj && direct.obj.type === 'wall') {
        e.stuck += dt;
        if (e.stuck > 1.2 && e.cd <= 0) {
          direct.obj.hp -= 5;
          direct.obj.hit = 0.25;
          e.cd = 0.8;
          emit(g, 'sound', 'chop');
          if (direct.obj.hp <= 0) {
            setObj(w, direct.tx, direct.ty, null);
            emit(g, 'sound', 'break');
            say(g, '들개가 울타리를 부쉈어요!');
          }
        }
        if (e.stuck <= 1.2) steer(w, e, ux, uy, step);
      } else {
        if (!direct) e.stuck = 0;
        steer(w, e, ux, uy, step);
      }
    }
    e.vx = (e.x - ox) / Math.max(dt, 1e-4);
    e.vy = (e.y - oy) / Math.max(dt, 1e-4);
  }

  // 서로 겹치지 않게
  for (let a = 0; a < g.enemies.length; a++) {
    for (let b = a + 1; b < g.enemies.length; b++) {
      const A = g.enemies[a], B = g.enemies[b];
      const dx = B.x - A.x, dy = B.y - A.y, d = Math.hypot(dx, dy);
      const min = A.r + B.r;
      if (d > 0 && d < min) {
        const push = (min - d) / 2;
        moveEntity(w, A, (-dx / d) * push, (-dy / d) * push);
        moveEntity(w, B, (dx / d) * push, (dy / d) * push);
      }
    }
  }
}

// ── 메인 업데이트 ───────────────────────────────────
function updateGame(g, dt, input) {
  const p = g.player;
  if (g.dead) return;
  const scale = p.sleeping ? 10 : 1;
  const gdt = dt * scale;
  const prevFrac = dayFrac(g.time);
  g.time += gdt / DAY_SECONDS;
  const frac = dayFrac(g.time);

  // 날짜/시간 알림
  const day = dayNumber(g);
  if (day !== g.lastDay) {
    if (g.lastDay) { say(g, `${day}일째 아침이 밝았어요.`); emit(g, 'newday', day); }
    g.lastDay = day;
    g.nightWarned = false;
  }
  if (!g.nightWarned && frac >= 0.56 && prevFrac < 0.56) {
    g.nightWarned = true;
    const hasFire = g.world.fires.size > 0 || hasItem(g, 'campfire');
    say(g, hasFire ? '해가 지고 있어요. 모닥불 곁으로 돌아가세요.' : '해가 지고 있어요! 밤이 오기 전에 모닥불을 만드세요. (나무 5, 돌 3)');
  }
  if (p.sleeping && frac > 0.96) wakeUp(g, '상쾌한 아침이에요!');

  // 생존 수치
  const fires = litFires(g);
  const nearFire = fires.some((f) => dist(f.x, f.y, p.x, p.y) < BALANCE.fireWarmRadius);
  const dark = darknessAt(g.time);
  const inShallow = tileAt(g.world, Math.floor(p.x / TILE), Math.floor(p.y / TILE)) === T.SHALLOW;
  p.hunger = Math.max(0, p.hunger - BALANCE.hungerRate * gdt);
  p.thirst = Math.max(0, p.thirst - BALANCE.thirstRate * gdt);
  if (nearFire) p.warmth = Math.min(100, p.warmth + BALANCE.warmthGainFire * gdt);
  else if (dark > 0.3 || inShallow) {
    let loss = BALANCE.warmthLossNight * (dark > 0.3 ? 1 : 0.5);
    if (hasItem(g, 'cloak')) loss *= 0.45;
    if (p.sleeping) loss *= 0.5;
    if (inShallow) loss *= 1.6;
    p.warmth = Math.max(0, p.warmth - loss * gdt);
  } else p.warmth = Math.min(100, p.warmth + BALANCE.warmthGainDay * gdt);

  let starving = 0, cause = null;
  if (p.hunger <= 0) { starving++; cause = 'hunger'; }
  if (p.thirst <= 0) { starving++; cause = 'thirst'; }
  if (p.warmth <= 0) { starving++; cause = 'cold'; }
  if (starving) {
    p.hp -= BALANCE.starveDamage * starving * gdt;
    p.lastCause = cause;
  } else if (p.hunger > 40 && p.thirst > 40 && p.warmth > 25) {
    p.hp = Math.min(100, p.hp + BALANCE.regen * gdt * (p.sleeping ? 2 : 1));
  }
  if (p.hunger < 20) tip(g, 'hungry', '배가 고파요! 산딸기, 코코넛, 생선을 먹어 보세요.');
  if (p.thirst < 25) tip(g, 'thirsty', '목이 말라요! 샘물 앞에서 [상호작용]을 누르거나 코코넛을 먹으세요.');
  if (p.warmth < 30) tip(g, 'cold', '몸이 차가워요! 모닥불 곁에 있으면 체온이 올라가요.');

  // 이동
  p.attackCd = Math.max(0, p.attackCd - dt);
  p.swipe = Math.max(0, p.swipe - dt * 5);
  p.hurt = Math.max(0, p.hurt - dt);
  p.anim += dt;
  p.moving = false;
  if (!p.sleeping) {
    let mx = input.mx, my = input.my;
    const ml = Math.hypot(mx, my);
    if (ml > 0.15) {
      mx /= Math.max(1, ml); my /= Math.max(1, ml);
      const len = Math.hypot(mx, my);
      p.fx = mx / len; p.fy = my / len;
      const speed = BALANCE.playerSpeed * tileSpeed(g.world, p.x, p.y) * Math.min(1, ml);
      const sx = mx * speed * dt, sy = my * speed * dt;
      if (!moveEntity(g.world, p, sx, sy)) {
        // 모서리에서 미끄러지기
        for (const a of [0.7, -0.7]) {
          const c = Math.cos(a), s = Math.sin(a);
          if (moveEntity(g.world, p, (sx * c - sy * s) * 0.8, (sx * s + sy * c) * 0.8)) break;
        }
      }
      p.moving = true;
    }
    if (input.actionHeld && p.attackCd <= 0 && !g.placing && !g.fishing) doAction(g);
  }

  updateFishing(g, dt);

  // 흔들림 애니메이션
  for (const o of g.shaking) {
    o.hit -= dt;
    if (o.hit <= 0) { o.hit = 0; g.shaking.delete(o); }
  }

  // 모닥불 연료
  for (const f of fires) f.o.fuel = Math.max(0, f.o.fuel - BALANCE.fireBurnRate * gdt);

  // 자원 재생
  g.growTimer += gdt;
  if (g.growTimer > 2) {
    const days = g.growTimer / DAY_SECONDS;
    g.growTimer = 0;
    growWorld(g.world, days, (tx, ty) => {
      const bx = tx * TILE, by = ty * TILE;
      if (circleRect(p.x, p.y, p.r, bx, by, TILE, TILE)) return true;
      return g.enemies.some((e) => circleRect(e.x, e.y, e.r, bx, by, TILE, TILE));
    });
  }

  // 적
  g.spawnTimer -= gdt;
  if (g.spawnTimer <= 0) { g.spawnTimer = 2.2; trySpawn(g); }
  updateEnemies(g, dt);
  if (p.sleeping) {
    const danger = g.enemies.some((e) => e.type === 'dog' && !e.fleeing && dist(e.x, e.y, p.x, p.y) < 150);
    if (danger) wakeUp(g, '들개가 다가와서 잠에서 깼어요!');
  }

  // 이펙트
  for (let i = g.floaters.length - 1; i >= 0; i--) {
    const f = g.floaters[i];
    f.t -= dt;
    f.y -= 22 * dt;
    if (f.t <= 0) g.floaters.splice(i, 1);
  }
  if (g.message) { g.message.t -= dt; if (g.message.t <= 0) g.message = null; }
  g.shake = Math.max(0, g.shake - dt);

  revealAround(g.world, p.x, p.y, 8, (tx, ty) => emit(g, 'reveal', ty * g.world.W + tx));

  if (p.hp <= 0) {
    p.hp = 0;
    g.dead = true;
    p.sleeping = false;
    emit(g, 'dead');
  }
}

// ── 화면 안내 ───────────────────────────────────────
function contextHints(g) {
  const p = g.player;
  if (g.dead) return { a: '', b: '' };
  if (p.sleeping) return { a: '일어나기', b: '일어나기' };
  if (g.placing) return { a: `${ITEMS[g.placing.id].name} 놓기`, b: '취소' };
  if (g.fishing) return { a: '당기기', b: '당기기' };
  let a = '';
  const spear = hasItem(g, 'spear');
  const reach = spear ? 40 : 28;
  if (g.enemies.some((e) => dist(e.x, e.y, p.x, p.y) - e.r < reach + 6)) a = '공격';
  else {
    const t = findTarget(g, GATHER_TYPES);
    if (t) {
      const o = t.obj;
      if (o.type === 'bush') a = o.berries ? '열매 따기' : '';
      else if (o.type === 'palm' && o.fruit > 0) a = '코코넛 따기';
      else a = OBJECTS[o.type].verb;
    }
  }
  let b = '';
  const u = findTarget(g, USE_TYPES);
  if (u) {
    if (u.obj.type === 'campfire') b = u.obj.fuel <= 0 ? '불 붙이기' : firstRaw(g) ? '굽기' : '장작 넣기';
    else b = '잠자기';
  } else {
    const w = nearbyWater(g);
    if (w && w.kind === 'pond') b = '물 마시기';
    else if (w && w.kind === 'sea' && hasItem(g, 'rod')) b = '낚시';
  }
  return { a, b };
}

function currentTargetForDraw(g) {
  if (g.placing || g.fishing || g.player.sleeping) return null;
  return findTarget(g, GATHER_TYPES) || findTarget(g, USE_TYPES);
}

// ── 저장/불러오기 ───────────────────────────────────
const SAVE_KEY = 'catpo-save-v1';
const BEST_KEY = 'catpo-best-v1';

function serializeGame(g) {
  const objs = [];
  const W = g.world.W;
  g.world.objects.forEach((o, i) => {
    if (!o) return;
    const e = { i, t: o.type };
    if (o.hp !== undefined && o.hp !== OBJECTS[o.type].hp) e.h = o.hp;
    if (o.berries !== undefined) e.b = o.berries;
    if (o.fruit !== undefined) e.f = o.fruit;
    if (o.timer !== undefined) e.m = Math.round(o.timer * 1000) / 1000;
    if (o.fuel !== undefined) e.u = Math.round(o.fuel);
    if (o.into) e.n = o.into;
    if (o.v) e.v = Math.round(o.v * 100) / 100;
    objs.push(e);
  });
  const p = g.player;
  return JSON.stringify({
    v: 1,
    seed: g.world.seed,
    W,
    time: g.time,
    player: { x: p.x, y: p.y, fx: p.fx, fy: p.fy, hp: p.hp, hunger: p.hunger, thirst: p.thirst, warmth: p.warmth },
    inv: g.inv,
    objs,
    explored: rleEncode(g.world.explored),
    stats: g.stats,
    tips: g.tips,
    lastDay: g.lastDay,
  });
}

function deserializeGame(str) {
  const s = JSON.parse(str);
  if (!s || s.v !== 1) throw new Error('지원하지 않는 저장 형식');
  const world = generateWorld(s.seed);
  world.objects.fill(null);
  world.fires.clear();
  for (const e of s.objs) {
    if (!OBJECTS[e.t]) continue;
    const o = { type: e.t, hp: e.h !== undefined ? e.h : OBJECTS[e.t].hp || 1, hit: 0, v: e.v || 0 };
    if (e.b !== undefined) o.berries = e.b;
    if (e.f !== undefined) o.fruit = e.f;
    if (e.m !== undefined) o.timer = e.m;
    if (e.u !== undefined) o.fuel = e.u;
    if (e.n) o.into = e.n;
    world.objects[e.i] = o;
    if (o.type === 'campfire') world.fires.add(e.i);
  }
  world.explored = rleDecode(s.explored, world.W * world.H);
  const g = newGame(world);
  Object.assign(g.player, s.player);
  g.time = s.time;
  g.inv = s.inv || {};
  g.stats = Object.assign(g.stats, s.stats || {});
  g.tips = s.tips || {};
  g.lastDay = s.lastDay || dayNumber(g);
  g.nightWarned = dayFrac(g.time) >= 0.56;
  return g;
}

function saveGame(g) {
  if (!g || g.dead) return false;
  return storageSet(SAVE_KEY, serializeGame(g));
}

function loadSavedGame() {
  const str = storageGet(SAVE_KEY);
  if (!str) return null;
  try { return deserializeGame(str); } catch (e) { return null; }
}

function savedSummary() {
  const str = storageGet(SAVE_KEY);
  if (!str) return null;
  try {
    const s = JSON.parse(str);
    return { day: Math.floor(s.time) + 1 };
  } catch (e) { return null; }
}

function survivalText(time) {
  const days = Math.floor(time - 0.04);
  const hours = Math.floor(((time - 0.04) % 1) * 24);
  return days > 0 ? `${days}일 ${hours}시간` : `${hours}시간`;
}

function recordBest(g) {
  const prev = Number(storageGet(BEST_KEY) || 0);
  const survived = g.time - 0.04;
  if (survived > prev) { storageSet(BEST_KEY, String(survived)); return { best: survived, isNew: true }; }
  return { best: prev, isNew: false };
}
