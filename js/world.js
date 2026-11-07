// 섬 생성과 지형/오브젝트 관리
'use strict';

function makeObject(type, rnd = Math.random) {
  const def = OBJECTS[type];
  const o = { type, hp: def.hp || 1, hit: 0, v: rnd() };
  if (type === 'bush') { o.berries = rnd() < 0.8 ? 1 : 0; o.timer = o.berries ? 0 : 0.5; }
  if (type === 'palm') { o.fruit = rnd() < 0.7 ? 2 : 1; o.timer = 0.8; }
  if (type === 'campfire') o.fuel = 70;
  if (type === 'stump' || type === 'sapling') o.timer = OBJECTS[type].growDays;
  return o;
}

function generateWorld(seed) {
  const W = WORLD_W, H = WORLD_H;
  const rnd = mulberry32(seed);
  const n1 = makeNoise((seed ^ 0x9e3779b9) >>> 0);
  const n2 = makeNoise((seed ^ 0x85ebca6b) >>> 0);
  const tiles = new Uint8Array(W * H);
  const heights = new Float32Array(W * H);
  const cx = W / 2, cy = H / 2;

  for (let y = 0; y < H; y++) {
    for (let x = 0; x < W; x++) {
      const dx = (x - cx) / (W / 2), dy = (y - cy) / (H / 2);
      const d = Math.sqrt(dx * dx + dy * dy);
      const n = n1(x * 0.045, y * 0.045, 5);
      const h = n * 0.9 + 0.5 - d * d * 1.15;
      heights[y * W + x] = h;
      let t;
      if (h < 0.22) t = T.DEEP;
      else if (h < 0.3) t = T.SHALLOW;
      else if (h < 0.38) t = T.SAND;
      else {
        const m = n2(x * 0.07, y * 0.07, 4);
        if (h > 0.66 && m < 0.45) t = T.ROCKY;
        else if (m > 0.55) t = T.FOREST;
        else t = T.GRASS;
      }
      // 가장자리는 항상 깊은 바다
      if (x < 2 || y < 2 || x >= W - 2 || y >= H - 2) t = T.DEEP;
      tiles[y * W + x] = t;
    }
  }

  // 중앙에서 이어진 본섬만 표시
  const main = new Uint8Array(W * H);
  {
    let start = -1, best = Infinity;
    for (let i = 0; i < W * H; i++) {
      if (!isLand(tiles[i])) continue;
      const x = i % W, y = (i / W) | 0;
      const d = (x - cx) ** 2 + (y - cy) ** 2;
      if (d < best) { best = d; start = i; }
    }
    const stack = [start];
    main[start] = 1;
    while (stack.length) {
      const i = stack.pop();
      const x = i % W, y = (i / W) | 0;
      for (const [nx, ny] of [[x + 1, y], [x - 1, y], [x, y + 1], [x, y - 1]]) {
        if (nx < 0 || ny < 0 || nx >= W || ny >= H) continue;
        const j = ny * W + nx;
        if (main[j] || !TILE_INFO[tiles[j]].walk) continue;
        main[j] = 1;
        stack.push(j);
      }
    }
  }

  // 남쪽 해변에서 시작
  let spawnIdx = -1;
  {
    let best = Infinity;
    const tx = cx, ty = cy + H * 0.34;
    for (let i = 0; i < W * H; i++) {
      if (tiles[i] !== T.SAND || !main[i]) continue;
      const x = i % W, y = (i / W) | 0;
      const d = (x - tx) ** 2 + (y - ty) ** 2;
      if (d < best) { best = d; spawnIdx = i; }
    }
  }
  const sx = spawnIdx % W, sy = (spawnIdx / W) | 0;

  const carvePond = (px, py, radius) => {
    const R = Math.ceil(radius + 1);
    for (let y = py - R; y <= py + R; y++) {
      for (let x = px - R; x <= px + R; x++) {
        if (x < 3 || y < 3 || x >= W - 3 || y >= H - 3) continue;
        const i = y * W + x;
        const t = tiles[i];
        if (t !== T.GRASS && t !== T.FOREST && t !== T.ROCKY) continue;
        if (Math.abs(x - sx) + Math.abs(y - sy) < 4) continue;
        const jitter = (hash2(x, y, seed) - 0.5) * 1.2;
        if (Math.hypot(x - px, y - py) <= radius + jitter) tiles[i] = T.POND;
      }
    }
  };

  // 시작 지점 근처의 샘
  {
    const ang = Math.atan2(cy - sy, cx - sx);
    let placed = false;
    for (let step = 7; step <= 16 && !placed; step++) {
      const px = Math.round(sx + Math.cos(ang) * step);
      const py = Math.round(sy + Math.sin(ang) * step);
      const t = tiles[py * W + px];
      if (t === T.GRASS || t === T.FOREST) {
        carvePond(px, py, 2.2);
        placed = true;
      }
    }
  }
  // 섬 곳곳의 샘
  for (let k = 0, tries = 0; k < 5 && tries < 400; tries++) {
    const x = randInt(rnd, 6, W - 7), y = randInt(rnd, 6, H - 7);
    const i = y * W + x;
    if (!main[i] || heights[i] < 0.5) continue;
    if (Math.hypot(x - sx, y - sy) < 14) continue;
    carvePond(x, y, 1.5 + rnd() * 1.4);
    k++;
  }

  // 오브젝트 배치
  const objects = new Array(W * H).fill(null);
  for (let y = 0; y < H; y++) {
    for (let x = 0; x < W; x++) {
      const i = y * W + x;
      const t = tiles[i];
      if (!isLand(t)) continue;
      if (Math.hypot(x - sx, y - sy) < 2.5) continue;
      const r = rnd();
      let type = null;
      if (t === T.GRASS) {
        if (r < 0.05) type = 'tree';
        else if (r < 0.08) type = 'bush';
        else if (r < 0.15) type = 'tuft';
        else if (r < 0.165) type = 'rock';
        else if (r < 0.18) type = 'herb';
      } else if (t === T.FOREST) {
        if (r < 0.25) type = 'tree';
        else if (r < 0.29) type = 'bush';
        else if (r < 0.33) type = 'tuft';
        else if (r < 0.345) type = 'herb';
        else if (r < 0.355) type = 'rock';
      } else if (t === T.SAND) {
        if (r < 0.06) type = 'palm';
        else if (r < 0.05) type = 'rock';
      } else if (t === T.ROCKY) {
        if (r < 0.17) type = 'rock';
        else if (r < 0.2) type = 'tuft';
        else if (r < 0.21) type = 'herb';
      }
      if (type) objects[i] = makeObject(type, rnd);
    }
  }

  return {
    seed,
    W, H,
    tiles,
    objects,
    fires: new Set(),
    explored: new Uint8Array(W * H),
    spawn: { x: sx * TILE + TILE / 2, y: sy * TILE + TILE / 2 },
  };
}

function tileAt(world, tx, ty) {
  if (tx < 0 || ty < 0 || tx >= world.W || ty >= world.H) return T.DEEP;
  return world.tiles[ty * world.W + tx];
}

function objAt(world, tx, ty) {
  if (tx < 0 || ty < 0 || tx >= world.W || ty >= world.H) return null;
  return world.objects[ty * world.W + tx];
}

function setObj(world, tx, ty, o) {
  const i = ty * world.W + tx;
  world.objects[i] = o;
  if (o && o.type === 'campfire') world.fires.add(i);
  else world.fires.delete(i);
}

// 충돌 검사. 지형이면 true, 오브젝트면 {tx, ty, obj}, 비어 있으면 null
function collides(world, x, y, r) {
  const x0 = Math.floor((x - r) / TILE), x1 = Math.floor((x + r) / TILE);
  const y0 = Math.floor((y - r) / TILE), y1 = Math.floor((y + r) / TILE);
  for (let ty = y0; ty <= y1; ty++) {
    for (let tx = x0; tx <= x1; tx++) {
      if (!TILE_INFO[tileAt(world, tx, ty)].walk) {
        if (circleRect(x, y, r, tx * TILE, ty * TILE, TILE, TILE)) return true;
        continue;
      }
      const o = objAt(world, tx, ty);
      if (!o) continue;
      const def = OBJECTS[o.type];
      if (def.solid === 'box') {
        if (circleRect(x, y, r, tx * TILE + 1, ty * TILE + 1, TILE - 2, TILE - 2)) return { tx, ty, obj: o };
      } else if (def.solid === 'circle') {
        const ox = tx * TILE + TILE / 2, oy = ty * TILE + TILE / 2;
        if (dist(x, y, ox, oy) < r + def.r) return { tx, ty, obj: o };
      }
    }
  }
  return null;
}

function tileSpeed(world, x, y) {
  const t = tileAt(world, Math.floor(x / TILE), Math.floor(y / TILE));
  return TILE_INFO[t].speed || 1;
}

function canBuildAt(world, tx, ty) {
  if (!isLand(tileAt(world, tx, ty))) return false;
  const o = objAt(world, tx, ty);
  return !o || o.type === 'hidden';
}

function revealAround(world, x, y, radius, onNew) {
  const ctx = Math.floor(x / TILE), cty = Math.floor(y / TILE);
  const r2 = radius * radius;
  for (let ty = cty - radius; ty <= cty + radius; ty++) {
    for (let tx = ctx - radius; tx <= ctx + radius; tx++) {
      if (tx < 0 || ty < 0 || tx >= world.W || ty >= world.H) continue;
      if ((tx - ctx) ** 2 + (ty - cty) ** 2 > r2) continue;
      const i = ty * world.W + tx;
      if (!world.explored[i]) {
        world.explored[i] = 1;
        if (onNew) onNew(tx, ty);
      }
    }
  }
}

// 게임 속 시간 경과(일 단위)에 따라 자원이 다시 자람
function growWorld(world, dDays, blockedAt) {
  const objs = world.objects;
  for (let i = 0; i < objs.length; i++) {
    const o = objs[i];
    if (!o) continue;
    const tx = i % world.W, ty = (i / world.W) | 0;
    if (o.type === 'bush') {
      if (!o.berries) {
        o.timer -= dDays;
        if (o.timer <= 0) o.berries = 1;
      }
    } else if (o.type === 'palm') {
      if (o.fruit < 2) {
        o.timer -= dDays;
        if (o.timer <= 0) { o.fruit++; o.timer = 0.8; }
      }
    } else if (o.type === 'stump' || o.type === 'sapling' || o.type === 'hidden') {
      if (o.timer === undefined) continue;
      o.timer -= dDays;
      if (o.timer <= 0) {
        const into = o.into || OBJECTS[o.type].growTo;
        if (!into) { objs[i] = null; continue; }
        if (OBJECTS[into].solid && blockedAt(tx, ty)) { o.timer = 0.05; continue; }
        objs[i] = makeObject(into);
      }
    }
  }
}
