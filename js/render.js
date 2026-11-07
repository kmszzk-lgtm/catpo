// 렌더링: 지형, 오브젝트, 캐릭터, 조명, 아이템 아이콘
'use strict';

const COLORS = {
  shadow: 'rgba(20, 30, 20, 0.25)',
  trunk: '#6b4a2b',
  trunkDark: '#4f3420',
  leafDark: '#2f6b3a',
  leafMid: '#3f8a46',
  leafLight: '#57a556',
  leafHi: '#74c164',
  rock: '#8d8a82',
  rockDark: '#6c6961',
  rockHi: '#b4b0a5',
  berry: '#d8344f',
  catBody: '#ec9a4f',
  catStripe: '#c76d2b',
  catBelly: '#f8dcb0',
  catLine: 'rgba(60, 32, 14, 0.75)',
  dog: '#4a4256',
  dogDark: '#332d3d',
  crab: '#e0573a',
  crabDark: '#a93a24',
  foam: 'rgba(255, 255, 255, 0.55)',
};

function prepareWorldRender(world) {
  const colors = new Array(world.W * world.H);
  for (let i = 0; i < colors.length; i++) {
    const x = i % world.W, y = (i / world.W) | 0;
    const base = TILE_INFO[world.tiles[i]].color;
    const k = 1 + (hash2(x, y, 7) - 0.5) * 0.07;
    colors[i] = `rgb(${(base[0] * k) | 0},${(base[1] * k) | 0},${(base[2] * k) | 0})`;
  }
  world.tileColors = colors;
}

function ellipse(ctx, x, y, rx, ry, color) {
  ctx.fillStyle = color;
  ctx.beginPath();
  ctx.ellipse(x, y, rx, ry, 0, 0, Math.PI * 2);
  ctx.fill();
}

function circle(ctx, x, y, r, color) {
  ctx.fillStyle = color;
  ctx.beginPath();
  ctx.arc(x, y, r, 0, Math.PI * 2);
  ctx.fill();
}

// ── 지형 ──────────────────────────────────────────────
function drawTiles(ctx, world, x0, y0, x1, y1, time) {
  const W = world.W;
  for (let ty = y0; ty <= y1; ty++) {
    for (let tx = x0; tx <= x1; tx++) {
      if (tx < 0 || ty < 0 || tx >= W || ty >= world.H) {
        ctx.fillStyle = 'rgb(38,92,140)';
        ctx.fillRect(tx * TILE, ty * TILE, TILE + 0.5, TILE + 0.5);
        continue;
      }
      const i = ty * W + tx;
      const t = world.tiles[i];
      const px = tx * TILE, py = ty * TILE;
      ctx.fillStyle = world.tileColors[i];
      ctx.fillRect(px, py, TILE + 0.5, TILE + 0.5);
      const h = hash2(tx, ty, 3);

      if (t === T.GRASS || t === T.FOREST) {
        if (h < 0.45) {
          ctx.fillStyle = t === T.GRASS ? 'rgba(70,120,50,0.55)' : 'rgba(40,85,40,0.55)';
          const bx = px + 4 + h * 40 % 22, by = py + 6 + (h * 97) % 18;
          ctx.fillRect(bx, by, 1.5, 4);
          ctx.fillRect(bx + 3, by - 1, 1.5, 5);
          ctx.fillRect(bx + 6, by + 1, 1.5, 3);
        }
        if (t === T.GRASS && h > 0.93) {
          circle(ctx, px + 10 + h * 12, py + 20, 1.6, h > 0.965 ? '#f3e28a' : '#f4f1ea');
        }
      } else if (t === T.SAND) {
        if (h < 0.6) {
          ctx.fillStyle = 'rgba(180,150,100,0.35)';
          ctx.fillRect(px + (h * 211) % 28, py + (h * 131) % 28, 2, 2);
          ctx.fillRect(px + (h * 77) % 28, py + (h * 53) % 28, 1.5, 1.5);
        }
      } else if (t === T.ROCKY) {
        if (h < 0.5) {
          circle(ctx, px + 6 + (h * 300) % 20, py + 8 + (h * 190) % 18, 2.2, 'rgba(100,96,86,0.6)');
        }
      } else {
        // 물결
        const wave = (time * 9 + h * 32) % 40 - 4;
        ctx.fillStyle = t === T.DEEP ? 'rgba(255,255,255,0.08)' : 'rgba(255,255,255,0.16)';
        ctx.fillRect(px + wave, py + 8 + (h * 13) % 16, 7, 1.5);
        if (t === T.SHALLOW) {
          const pulse = 0.35 + Math.sin(time * 2 + (tx + ty) * 0.6) * 0.25;
          ctx.fillStyle = `rgba(255,255,255,${pulse})`;
          if (tileAt(world, tx, ty - 1) === T.SAND) ctx.fillRect(px, py, TILE + 0.5, 3);
          if (tileAt(world, tx, ty + 1) === T.SAND) ctx.fillRect(px, py + TILE - 3, TILE + 0.5, 3);
          if (tileAt(world, tx - 1, ty) === T.SAND) ctx.fillRect(px, py, 3, TILE + 0.5);
          if (tileAt(world, tx + 1, ty) === T.SAND) ctx.fillRect(px + TILE - 3, py, 3, TILE + 0.5);
        } else if (t === T.POND) {
          ctx.fillStyle = 'rgba(40,70,40,0.45)';
          if (isLand(tileAt(world, tx, ty - 1))) ctx.fillRect(px, py, TILE + 0.5, 3);
          if (isLand(tileAt(world, tx - 1, ty))) ctx.fillRect(px, py, 2, TILE + 0.5);
          if (isLand(tileAt(world, tx + 1, ty))) ctx.fillRect(px + TILE - 2, py, 2, TILE + 0.5);
          if (h > 0.85) {
            ellipse(ctx, px + 16, py + 16, 5, 3, 'rgba(90,150,70,0.85)');
          }
        }
      }
    }
  }
}

// ── 오브젝트 ──────────────────────────────────────────
const FLAT_OBJECTS = new Set(['tuft', 'herb', 'stump', 'nest', 'hidden', 'sapling']);

function drawObject(ctx, type, cx, cy, o, time) {
  const v = o ? o.v : 0.5;
  const shake = o && o.hit > 0 ? Math.sin(o.hit * 50) * 2.2 * (o.hit / 0.25) : 0;
  switch (type) {
    case 'tree': {
      const k = 0.92 + v * 0.28;
      ellipse(ctx, cx, cy + 9, 14 * k, 5.5 * k, COLORS.shadow);
      ctx.fillStyle = COLORS.trunk;
      ctx.fillRect(cx - 3, cy - 6, 6, 15);
      ctx.fillStyle = COLORS.trunkDark;
      ctx.fillRect(cx + 1, cy - 6, 2, 15);
      const sx = cx + shake;
      circle(ctx, sx, cy - 15 * k, 15 * k, COLORS.leafDark);
      circle(ctx, sx - 6 * k, cy - 14 * k, 10 * k, COLORS.leafMid);
      circle(ctx, sx + 5 * k, cy - 20 * k, 10 * k, COLORS.leafLight);
      circle(ctx, sx - 3 * k, cy - 23 * k, 5.5 * k, COLORS.leafHi);
      break;
    }
    case 'palm': {
      ellipse(ctx, cx + 4, cy + 9, 12, 4.5, COLORS.shadow);
      const lean = (v - 0.5) * 14;
      ctx.strokeStyle = '#8a6438';
      ctx.lineWidth = 4.5;
      ctx.lineCap = 'round';
      ctx.beginPath();
      ctx.moveTo(cx, cy + 8);
      ctx.quadraticCurveTo(cx + lean * 0.2, cy - 10, cx + lean + shake, cy - 26);
      ctx.stroke();
      ctx.strokeStyle = 'rgba(70,45,20,0.5)';
      ctx.lineWidth = 1;
      for (let s = 0; s < 4; s++) {
        const yy = cy + 4 - s * 7;
        ctx.beginPath();
        ctx.moveTo(cx - 2 + lean * (s / 5), yy);
        ctx.lineTo(cx + 2 + lean * (s / 5), yy);
        ctx.stroke();
      }
      const topX = cx + lean + shake, topY = cy - 27;
      const sway = Math.sin(time * 1.4 + v * 9) * 0.08;
      ctx.fillStyle = '#4f9a49';
      for (let f = 0; f < 6; f++) {
        const a = (f / 6) * Math.PI * 2 + sway + v;
        ctx.save();
        ctx.translate(topX, topY);
        ctx.rotate(a);
        ctx.beginPath();
        ctx.ellipse(10, 0, 12, 3.6, 0.25, 0, Math.PI * 2);
        ctx.fill();
        ctx.restore();
      }
      circle(ctx, topX, topY, 3.5, '#3d7f3a');
      if (o && o.fruit > 0) circle(ctx, topX - 3, topY + 4, 3, '#7a5530');
      if (o && o.fruit > 1) circle(ctx, topX + 3, topY + 4, 3, '#6b4828');
      break;
    }
    case 'rock': {
      ellipse(ctx, cx, cy + 7, 14, 5, COLORS.shadow);
      const sx = cx + shake * 0.5;
      ctx.fillStyle = COLORS.rockDark;
      ctx.beginPath();
      for (let p = 0; p < 8; p++) {
        const a = (p / 8) * Math.PI * 2;
        const rr = 11 + hash2(p, Math.floor(v * 1000), 5) * 3;
        const px = sx + Math.cos(a) * rr * 1.1, py = cy - 2 + Math.sin(a) * rr * 0.8;
        if (p === 0) ctx.moveTo(px, py); else ctx.lineTo(px, py);
      }
      ctx.closePath();
      ctx.fill();
      ellipse(ctx, sx - 1, cy - 4, 10, 7, COLORS.rock);
      ellipse(ctx, sx - 4, cy - 7, 4.5, 2.6, COLORS.rockHi);
      break;
    }
    case 'bush': {
      ellipse(ctx, cx, cy + 8, 13, 4.5, COLORS.shadow);
      const sx = cx + shake;
      circle(ctx, sx - 6, cy - 1, 8, '#3b7d3c');
      circle(ctx, sx + 6, cy - 1, 8, '#3b7d3c');
      circle(ctx, sx, cy - 7, 9, '#4a9447');
      circle(ctx, sx - 3, cy - 10, 4, '#62ad57');
      if (o && o.berries) {
        for (let b = 0; b < 6; b++) {
          const bx = sx + Math.cos(b * 2.1 + v * 5) * 8;
          const by = cy - 4 + Math.sin(b * 2.1 + v * 5) * 6;
          circle(ctx, bx, by, 2.1, COLORS.berry);
          circle(ctx, bx - 0.6, by - 0.7, 0.7, '#ff9aa8');
        }
      }
      break;
    }
    case 'tuft': {
      ctx.fillStyle = '#5f9e47';
      for (let b = 0; b < 5; b++) {
        const bx = cx - 8 + b * 4 + shake;
        const hgt = 8 + ((b * 7 + v * 10) % 5);
        const sway = Math.sin(time * 2 + b + v * 6) * 1.5;
        ctx.beginPath();
        ctx.moveTo(bx - 2, cy + 6);
        ctx.lineTo(bx + sway, cy + 6 - hgt);
        ctx.lineTo(bx + 2, cy + 6);
        ctx.fill();
      }
      ctx.fillStyle = '#7dbb5c';
      ctx.fillRect(cx - 2 + shake, cy - 1, 1.5, 7);
      break;
    }
    case 'herb': {
      ctx.strokeStyle = '#3f7d36';
      ctx.lineWidth = 1.5;
      ctx.beginPath();
      ctx.moveTo(cx, cy + 6);
      ctx.lineTo(cx, cy - 4);
      ctx.stroke();
      ellipse(ctx, cx - 3, cy + 2, 3, 1.5, '#4d8f3f');
      ellipse(ctx, cx + 3, cy, 3, 1.5, '#4d8f3f');
      for (let p = 0; p < 5; p++) {
        const a = (p / 5) * Math.PI * 2 + time * 0.3;
        circle(ctx, cx + Math.cos(a) * 3, cy - 6 + Math.sin(a) * 3, 2.2, '#a777e0');
      }
      circle(ctx, cx, cy - 6, 1.6, '#f7d65a');
      break;
    }
    case 'stump': {
      ellipse(ctx, cx, cy + 5, 9, 4, COLORS.shadow);
      ctx.fillStyle = COLORS.trunk;
      ctx.fillRect(cx - 6, cy - 3, 12, 7);
      ellipse(ctx, cx, cy - 3, 6, 3, '#c49a63');
      ellipse(ctx, cx, cy - 3, 3, 1.5, '#a37a48');
      break;
    }
    case 'sapling': {
      ctx.strokeStyle = '#8a6438';
      ctx.lineWidth = 2;
      ctx.beginPath();
      ctx.moveTo(cx, cy + 6);
      ctx.lineTo(cx, cy - 4);
      ctx.stroke();
      ellipse(ctx, cx - 4, cy - 5, 5, 2, '#4f9a49');
      ellipse(ctx, cx + 4, cy - 5, 5, 2, '#4f9a49');
      break;
    }
    case 'campfire': {
      ellipse(ctx, cx, cy + 6, 14, 5, COLORS.shadow);
      for (let s = 0; s < 7; s++) {
        const a = (s / 7) * Math.PI * 2;
        ellipse(ctx, cx + Math.cos(a) * 10, cy + 1 + Math.sin(a) * 5, 3.6, 2.8, s % 2 ? '#8b877f' : '#a19d94');
      }
      ctx.save();
      ctx.translate(cx, cy);
      ctx.fillStyle = '#6b4a2b';
      ctx.rotate(0.5);
      ctx.fillRect(-9, -2, 18, 4);
      ctx.rotate(-1);
      ctx.fillRect(-9, -2, 18, 4);
      ctx.restore();
      const fuel = o ? o.fuel : 60;
      if (fuel > 0) {
        const k = 0.55 + Math.min(1, fuel / 60) * 0.55;
        const f1 = Math.sin(time * 13 + v * 10) * 1.6;
        const f2 = Math.cos(time * 17 + v * 3) * 1.3;
        const flame = (w, h, col, dx) => {
          ctx.fillStyle = col;
          ctx.beginPath();
          ctx.moveTo(cx - w + dx, cy);
          ctx.quadraticCurveTo(cx - w * 0.8 + dx, cy - h * 0.6, cx + dx + f1 * 0.6, cy - h);
          ctx.quadraticCurveTo(cx + w * 0.8 + dx, cy - h * 0.6, cx + w + dx, cy);
          ctx.closePath();
          ctx.fill();
        };
        flame(8 * k, (20 + f2) * k, '#e8562a', 0);
        flame(5.5 * k, (15 + f1) * k, '#f59a2e', f2 * 0.4);
        flame(3 * k, (9 + f2 * 0.5) * k, '#ffe08a', 0);
      } else {
        circle(ctx, cx - 2, cy - 1, 2, '#c4502a');
        circle(ctx, cx + 3, cy, 1.5, '#a8432a');
      }
      break;
    }
    case 'wall': {
      ellipse(ctx, cx, cy + 12, 16, 4, COLORS.shadow);
      for (let l = 0; l < 3; l++) {
        const lx = cx - 14 + l * 10 + 0.5;
        const top = cy - 20 + ((l + Math.floor(v * 3)) % 2) * 2;
        ctx.fillStyle = l % 2 ? '#8a6438' : '#7b5631';
        ctx.beginPath();
        ctx.moveTo(lx, cy + 12);
        ctx.lineTo(lx, top + 5);
        ctx.lineTo(lx + 4.5, top);
        ctx.lineTo(lx + 9, top + 5);
        ctx.lineTo(lx + 9, cy + 12);
        ctx.closePath();
        ctx.fill();
        ctx.fillStyle = 'rgba(40,25,10,0.3)';
        ctx.fillRect(lx + 6.5, top + 5, 2.5, cy + 7 - top);
      }
      ctx.fillStyle = '#5a3c20';
      ctx.fillRect(cx - 15, cy - 6, 30, 3);
      ctx.fillRect(cx - 15, cy + 4, 30, 3);
      if (o && o.hp < OBJECTS.wall.hp) {
        ctx.fillStyle = 'rgba(0,0,0,0.4)';
        ctx.fillRect(cx - 12, cy - 26, 24, 3);
        ctx.fillStyle = '#e7c35a';
        ctx.fillRect(cx - 12, cy - 26, 24 * (o.hp / OBJECTS.wall.hp), 3);
      }
      break;
    }
    case 'nest': {
      ellipse(ctx, cx, cy + 2, 15, 9, '#b48a45');
      ellipse(ctx, cx, cy + 1, 11, 6, '#7d5c2c');
      ctx.strokeStyle = '#d6b066';
      ctx.lineWidth = 1.2;
      for (let s = 0; s < 9; s++) {
        const a = (s / 9) * Math.PI * 2;
        ctx.beginPath();
        ctx.moveTo(cx + Math.cos(a) * 11, cy + 2 + Math.sin(a) * 6.5);
        ctx.lineTo(cx + Math.cos(a + 0.5) * 15, cy + 2 + Math.sin(a + 0.5) * 8.5);
        ctx.stroke();
      }
      break;
    }
  }
}

// ── 캐릭터 ────────────────────────────────────────────
function drawCat(ctx, x, y, fx, fy, anim, moving, opts = {}) {
  ctx.save();
  if (opts.flash) ctx.globalAlpha = 0.55;
  ellipse(ctx, x, y + 8, 11, 4, COLORS.shadow);
  if (opts.sleeping) {
    ellipse(ctx, x, y + 1, 12, 9, COLORS.catBody);
    ctx.strokeStyle = COLORS.catBody;
    ctx.lineWidth = 4;
    ctx.lineCap = 'round';
    ctx.beginPath();
    ctx.arc(x, y + 1, 12, 0.2, 1.9);
    ctx.stroke();
    ctx.strokeStyle = COLORS.catStripe;
    ctx.lineWidth = 2;
    ctx.beginPath(); ctx.arc(x - 1, y, 7, -2.6, -1.9); ctx.stroke();
    ctx.beginPath(); ctx.arc(x - 1, y, 7, -1.4, -0.7); ctx.stroke();
    circle(ctx, x + 7, y + 3, 6, COLORS.catBody);
    ctx.fillStyle = COLORS.catBody;
    ctx.beginPath();
    ctx.moveTo(x + 4, y - 1); ctx.lineTo(x + 6, y - 6); ctx.lineTo(x + 9, y - 1);
    ctx.fill();
    ctx.strokeStyle = COLORS.catLine;
    ctx.lineWidth = 1;
    ctx.beginPath(); ctx.moveTo(x + 6, y + 3); ctx.lineTo(x + 9, y + 3); ctx.stroke();
    ctx.restore();
    return;
  }
  const bob = moving ? -Math.abs(Math.sin(anim * 12)) * 1.6 : Math.sin(anim * 2) * 0.4;
  const step = moving ? Math.sin(anim * 12) * 2.2 : 0;
  const sway = Math.sin(anim * (moving ? 8 : 2.5)) * 3;
  y += bob;
  ctx.lineWidth = 1.2;
  ctx.strokeStyle = COLORS.catLine;
  ctx.lineCap = 'round';
  ctx.lineJoin = 'round';

  const side = Math.abs(fx) > Math.abs(fy);
  if (side) {
    const d = fx >= 0 ? 1 : -1;
    // 꼬리
    ctx.strokeStyle = COLORS.catBody;
    ctx.lineWidth = 3.6;
    ctx.beginPath();
    ctx.moveTo(x - 9 * d, y);
    ctx.quadraticCurveTo(x - 17 * d, y - 4, x - 15 * d + sway * 0.4 * d, y - 13);
    ctx.stroke();
    ctx.lineWidth = 1.2;
    ctx.strokeStyle = COLORS.catLine;
    // 다리
    ctx.fillStyle = COLORS.catStripe;
    ctx.fillRect(x - 7 * d - 1.5 + step * d * 0.5, y + 3, 3, 5);
    ctx.fillRect(x + 5 * d - 1.5 - step * d * 0.5, y + 3, 3, 5);
    ctx.fillStyle = COLORS.catBody;
    ctx.fillRect(x - 4 * d - 1.5 - step * d * 0.5, y + 3, 3, 5.5);
    ctx.fillRect(x + 8 * d - 1.5 + step * d * 0.5, y + 3, 3, 5.5);
    // 몸통
    ctx.fillStyle = COLORS.catBody;
    ctx.beginPath();
    ctx.ellipse(x, y - 1, 11, 6.5, 0, 0, Math.PI * 2);
    ctx.fill();
    ctx.stroke();
    ellipse(ctx, x + 1 * d, y + 2.5, 6, 2.5, COLORS.catBelly);
    ctx.strokeStyle = COLORS.catStripe;
    ctx.lineWidth = 2;
    for (let s = -1; s <= 1; s++) {
      ctx.beginPath();
      ctx.moveTo(x + s * 4 - 1, y - 7);
      ctx.lineTo(x + s * 4 + 1, y - 3);
      ctx.stroke();
    }
    ctx.strokeStyle = COLORS.catLine;
    ctx.lineWidth = 1.2;
    // 머리
    const hx = x + 9 * d, hy = y - 8;
    ctx.fillStyle = COLORS.catBody;
    ctx.beginPath();
    ctx.moveTo(hx - 4 * d, hy - 4);
    ctx.lineTo(hx - 2.5 * d, hy - 11);
    ctx.lineTo(hx + 1 * d, hy - 5);
    ctx.lineTo(hx + 4 * d, hy - 10.5);
    ctx.lineTo(hx + 6 * d, hy - 3);
    ctx.closePath();
    ctx.fill();
    ctx.stroke();
    ctx.beginPath();
    ctx.arc(hx, hy, 7, 0, Math.PI * 2);
    ctx.fill();
    ctx.stroke();
    ellipse(ctx, hx + 3 * d, hy + 2.5, 3.5, 2.5, COLORS.catBelly);
    circle(ctx, hx + 3 * d, hy - 1.5, 1.4, '#2a1a10');
    circle(ctx, hx + 3.4 * d, hy - 2, 0.5, '#fff');
    circle(ctx, hx + 6.5 * d, hy + 1.3, 1.1, '#e07a8a');
  } else if (fy > 0) {
    // 정면
    ctx.strokeStyle = COLORS.catBody;
    ctx.lineWidth = 3.6;
    ctx.beginPath();
    ctx.moveTo(x + 4, y - 2);
    ctx.quadraticCurveTo(x + 14, y - 4, x + 12 + sway * 0.5, y - 15);
    ctx.stroke();
    ctx.strokeStyle = COLORS.catLine;
    ctx.lineWidth = 1.2;
    ctx.fillStyle = COLORS.catBody;
    ctx.beginPath();
    ctx.ellipse(x, y, 8.5, 8, 0, 0, Math.PI * 2);
    ctx.fill();
    ctx.stroke();
    ellipse(ctx, x, y + 2, 4.5, 5, COLORS.catBelly);
    ellipse(ctx, x - 4, y + 7 + step * 0.4, 3, 2.2, COLORS.catBody);
    ellipse(ctx, x + 4, y + 7 - step * 0.4, 3, 2.2, COLORS.catBody);
    const hx = x, hy = y - 9;
    ctx.fillStyle = COLORS.catBody;
    ctx.beginPath();
    ctx.moveTo(hx - 7, hy - 2); ctx.lineTo(hx - 6, hy - 11); ctx.lineTo(hx - 1.5, hy - 6);
    ctx.lineTo(hx + 1.5, hy - 6); ctx.lineTo(hx + 6, hy - 11); ctx.lineTo(hx + 7, hy - 2);
    ctx.closePath();
    ctx.fill();
    ctx.stroke();
    ctx.beginPath();
    ctx.arc(hx, hy, 7.5, 0, Math.PI * 2);
    ctx.fill();
    ctx.stroke();
    ctx.fillStyle = '#e8a0a8';
    ctx.beginPath(); ctx.moveTo(hx - 5.5, hy - 5); ctx.lineTo(hx - 5, hy - 9); ctx.lineTo(hx - 3, hy - 6); ctx.fill();
    ctx.beginPath(); ctx.moveTo(hx + 5.5, hy - 5); ctx.lineTo(hx + 5, hy - 9); ctx.lineTo(hx + 3, hy - 6); ctx.fill();
    ctx.strokeStyle = COLORS.catStripe;
    ctx.lineWidth = 1.6;
    ctx.beginPath(); ctx.moveTo(hx, hy - 7); ctx.lineTo(hx, hy - 4); ctx.stroke();
    ctx.beginPath(); ctx.moveTo(hx - 2.5, hy - 6.5); ctx.lineTo(hx - 2, hy - 4.5); ctx.stroke();
    ctx.beginPath(); ctx.moveTo(hx + 2.5, hy - 6.5); ctx.lineTo(hx + 2, hy - 4.5); ctx.stroke();
    ellipse(ctx, hx, hy + 3, 4, 2.6, COLORS.catBelly);
    circle(ctx, hx - 3, hy - 0.5, 1.4, '#2a1a10');
    circle(ctx, hx + 3, hy - 0.5, 1.4, '#2a1a10');
    circle(ctx, hx - 2.6, hy - 1, 0.5, '#fff');
    circle(ctx, hx + 3.4, hy - 1, 0.5, '#fff');
    circle(ctx, hx, hy + 2, 1, '#e07a8a');
  } else {
    // 뒷모습
    ctx.fillStyle = COLORS.catBody;
    ctx.beginPath();
    ctx.ellipse(x, y, 8.5, 8, 0, 0, Math.PI * 2);
    ctx.fill();
    ctx.stroke();
    ellipse(ctx, x - 4, y + 7 - step * 0.4, 3, 2.2, COLORS.catStripe);
    ellipse(ctx, x + 4, y + 7 + step * 0.4, 3, 2.2, COLORS.catStripe);
    ctx.strokeStyle = COLORS.catStripe;
    ctx.lineWidth = 2;
    for (let s = 0; s < 3; s++) {
      ctx.beginPath();
      ctx.moveTo(x - 4, y - 3 + s * 3.5);
      ctx.quadraticCurveTo(x, y - 1 + s * 3.5, x + 4, y - 3 + s * 3.5);
      ctx.stroke();
    }
    const hx = x, hy = y - 9;
    ctx.strokeStyle = COLORS.catLine;
    ctx.lineWidth = 1.2;
    ctx.fillStyle = COLORS.catBody;
    ctx.beginPath();
    ctx.moveTo(hx - 7, hy - 2); ctx.lineTo(hx - 6, hy - 11); ctx.lineTo(hx - 1.5, hy - 6);
    ctx.lineTo(hx + 1.5, hy - 6); ctx.lineTo(hx + 6, hy - 11); ctx.lineTo(hx + 7, hy - 2);
    ctx.closePath();
    ctx.fill();
    ctx.stroke();
    ctx.beginPath();
    ctx.arc(hx, hy, 7.5, 0, Math.PI * 2);
    ctx.fill();
    ctx.stroke();
    ctx.strokeStyle = COLORS.catStripe;
    ctx.lineWidth = 1.6;
    ctx.beginPath(); ctx.moveTo(hx - 3, hy - 4); ctx.lineTo(hx - 2, hy + 2); ctx.stroke();
    ctx.beginPath(); ctx.moveTo(hx + 3, hy - 4); ctx.lineTo(hx + 2, hy + 2); ctx.stroke();
    // 꼬리는 앞쪽(화면 쪽)
    ctx.strokeStyle = COLORS.catBody;
    ctx.lineWidth = 3.6;
    ctx.beginPath();
    ctx.moveTo(x, y + 6);
    ctx.quadraticCurveTo(x - 3, y + 14, x - 9 + sway * 0.5, y + 13);
    ctx.stroke();
  }
  ctx.restore();
}

function drawSwipe(ctx, x, y, angle, t, reach) {
  // t: 1 → 0
  const a0 = angle - 1.0, a1 = angle + 1.0;
  const prog = 1 - t;
  ctx.save();
  ctx.globalAlpha = Math.min(1, t * 2.2);
  ctx.strokeStyle = '#fffbe8';
  ctx.lineCap = 'round';
  for (let c = 0; c < 3; c++) {
    const r = reach - 6 + c * 4;
    ctx.lineWidth = 2.2 - c * 0.4;
    ctx.beginPath();
    ctx.arc(x, y - 4, r, a0 + prog * 0.3, a0 + (a1 - a0) * Math.min(1, prog * 1.8 + 0.2));
    ctx.stroke();
  }
  ctx.restore();
}

function drawDog(ctx, e, time, night) {
  const x = e.x, y = e.y;
  const d = e.face >= 0 ? 1 : -1;
  const moving = Math.hypot(e.vx, e.vy) > 5;
  const step = moving ? Math.sin(time * 16 + e.seed) * 2.5 : 0;
  ctx.save();
  if (e.flash > 0) ctx.globalAlpha = 0.6;
  ellipse(ctx, x, y + 9, 13, 4, COLORS.shadow);
  ctx.strokeStyle = COLORS.dog;
  ctx.lineWidth = 3;
  ctx.lineCap = 'round';
  ctx.beginPath();
  ctx.moveTo(x - 10 * d, y - 3);
  ctx.quadraticCurveTo(x - 16 * d, y - 6, x - 17 * d, y - 1 + Math.sin(time * 9 + e.seed) * 2);
  ctx.stroke();
  ctx.fillStyle = COLORS.dogDark;
  ctx.fillRect(x - 8 * d - 1.5 + step * d * 0.4, y + 2, 3, 7);
  ctx.fillRect(x + 6 * d - 1.5 - step * d * 0.4, y + 2, 3, 7);
  ctx.fillStyle = COLORS.dog;
  ctx.fillRect(x - 5 * d - 1.5 - step * d * 0.4, y + 2, 3, 7.5);
  ctx.fillRect(x + 9 * d - 1.5 + step * d * 0.4, y + 2, 3, 7.5);
  ellipse(ctx, x, y - 2, 12, 6.5, COLORS.dog);
  ellipse(ctx, x - 2 * d, y - 6, 8, 2.5, COLORS.dogDark);
  const hx = x + 11 * d, hy = y - 8 + (e.windup > 0 ? 2 : 0);
  ctx.fillStyle = COLORS.dog;
  ctx.beginPath();
  ctx.moveTo(hx - 4 * d, hy - 3); ctx.lineTo(hx - 3 * d, hy - 12); ctx.lineTo(hx + 1 * d, hy - 4);
  ctx.closePath();
  ctx.fill();
  circle(ctx, hx, hy, 6, COLORS.dog);
  ctx.beginPath();
  ctx.ellipse(hx + 6 * d, hy + 2, 5, 3, 0, 0, Math.PI * 2);
  ctx.fill();
  circle(ctx, hx + 10.5 * d, hy + 1.5, 1.4, '#1a1520');
  if (!night) circle(ctx, hx + 2 * d, hy - 1.5, 1.4, '#ff6b5a');
  if (e.windup > 0) {
    ctx.fillStyle = '#fff';
    ctx.fillRect(hx + 6 * d, hy + 3.5, 1.2, 2);
    ctx.fillRect(hx + 8.5 * d, hy + 3.5, 1.2, 2);
  }
  ctx.restore();
}

function drawDogEyes(ctx, e) {
  const d = e.face >= 0 ? 1 : -1;
  const hx = e.x + 11 * d, hy = e.y - 8 + (e.windup > 0 ? 2 : 0);
  circle(ctx, hx + 2 * d, hy - 1.5, 3.2, 'rgba(255,60,50,0.35)');
  circle(ctx, hx + 2 * d, hy - 1.5, 1.5, '#ff5a4a');
}

function drawCrab(ctx, e, time) {
  const x = e.x, y = e.y;
  const moving = Math.hypot(e.vx, e.vy) > 3;
  const wig = moving ? Math.sin(time * 20 + e.seed) * 1.5 : 0;
  ctx.save();
  if (e.flash > 0) ctx.globalAlpha = 0.6;
  ellipse(ctx, x, y + 6, 11, 3.5, COLORS.shadow);
  ctx.strokeStyle = COLORS.crabDark;
  ctx.lineWidth = 1.5;
  for (let s = -1; s <= 1; s += 2) {
    for (let l = 0; l < 3; l++) {
      ctx.beginPath();
      ctx.moveTo(x + s * 5, y + 1 + l * 1.5);
      ctx.lineTo(x + s * (10 + l), y + 4 + l * 1.5 + (l % 2 ? wig : -wig));
      ctx.stroke();
    }
  }
  ctx.fillStyle = COLORS.crab;
  ctx.beginPath();
  ctx.ellipse(x, y, 8, 5.5, 0, Math.PI, 0);
  ctx.lineTo(x + 8, y + 2);
  ctx.quadraticCurveTo(x, y + 5, x - 8, y + 2);
  ctx.fill();
  const pinch = e.windup > 0 ? 1.5 : Math.sin(time * 6 + e.seed) * 0.6;
  circle(ctx, x - 10, y - 4 - pinch, 3.6, COLORS.crab);
  circle(ctx, x + 10, y - 4 - pinch, 3.6, COLORS.crab);
  ctx.strokeStyle = COLORS.crabDark;
  ctx.beginPath(); ctx.moveTo(x - 3, y - 4); ctx.lineTo(x - 3, y - 8); ctx.stroke();
  ctx.beginPath(); ctx.moveTo(x + 3, y - 4); ctx.lineTo(x + 3, y - 8); ctx.stroke();
  circle(ctx, x - 3, y - 8.5, 1.6, '#fff');
  circle(ctx, x + 3, y - 8.5, 1.6, '#fff');
  circle(ctx, x - 3, y - 8.5, 0.8, '#111');
  circle(ctx, x + 3, y - 8.5, 0.8, '#111');
  ctx.restore();
}

// ── 아이템 아이콘 (32×32 좌표계) ─────────────────────
function drawItemIcon(ctx, id, size) {
  ctx.save();
  ctx.scale(size / 32, size / 32);
  ctx.lineCap = 'round';
  ctx.lineJoin = 'round';
  switch (id) {
    case 'wood':
      ctx.save(); ctx.translate(16, 17); ctx.rotate(-0.35);
      ctx.fillStyle = '#8a6438'; ctx.fillRect(-11, -5, 22, 10);
      ctx.fillStyle = '#6b4a2b'; ctx.fillRect(-11, 1, 22, 4);
      ellipse(ctx, 11, 0, 3, 5, '#d4ab72'); ellipse(ctx, 11, 0, 1.4, 2.4, '#a37a48');
      ctx.restore();
      break;
    case 'stone':
      ellipse(ctx, 16, 19, 11, 8, COLORS.rockDark);
      ellipse(ctx, 15, 17, 9.5, 6.5, COLORS.rock);
      ellipse(ctx, 12, 14, 4, 2.2, COLORS.rockHi);
      break;
    case 'fiber':
      ctx.strokeStyle = '#6aa84f'; ctx.lineWidth = 2.4;
      for (let i = -2; i <= 2; i++) {
        ctx.beginPath(); ctx.moveTo(16 + i * 2, 27); ctx.quadraticCurveTo(16 + i * 3, 15, 16 + i * 5, 5); ctx.stroke();
      }
      ctx.strokeStyle = '#c9a35a'; ctx.lineWidth = 3;
      ctx.beginPath(); ctx.moveTo(10, 21); ctx.lineTo(22, 21); ctx.stroke();
      break;
    case 'hide':
      ctx.fillStyle = '#9c7650';
      ctx.beginPath();
      ctx.moveTo(8, 8); ctx.quadraticCurveTo(16, 4, 24, 8); ctx.lineTo(27, 14); ctx.lineTo(24, 26);
      ctx.quadraticCurveTo(16, 29, 8, 26); ctx.lineTo(5, 14); ctx.closePath(); ctx.fill();
      ellipse(ctx, 16, 16, 6, 7, '#b58d63');
      break;
    case 'berry':
      ctx.strokeStyle = '#3f7d36'; ctx.lineWidth = 2;
      ctx.beginPath(); ctx.moveTo(16, 6); ctx.lineTo(16, 12); ctx.stroke();
      ellipse(ctx, 20, 7, 4, 2, '#4d8f3f');
      circle(ctx, 11, 18, 5, COLORS.berry); circle(ctx, 20, 17, 5, COLORS.berry); circle(ctx, 16, 24, 5, '#c22a45');
      circle(ctx, 9.5, 16.5, 1.4, '#ff9aa8'); circle(ctx, 18.5, 15.5, 1.4, '#ff9aa8');
      break;
    case 'coconut':
      circle(ctx, 16, 17, 10, '#6b4828');
      circle(ctx, 13, 13, 3.5, '#8a6438');
      circle(ctx, 13, 19, 1.3, '#3a2414'); circle(ctx, 17, 21, 1.3, '#3a2414'); circle(ctx, 19, 17, 1.3, '#3a2414');
      break;
    case 'fish':
    case 'cookedFish': {
      const body = id === 'fish' ? '#6fa8c8' : '#c98a45';
      const dark = id === 'fish' ? '#4a7f9c' : '#8f5a26';
      ctx.fillStyle = dark;
      ctx.beginPath(); ctx.moveTo(6, 16); ctx.lineTo(1, 10); ctx.lineTo(1, 22); ctx.closePath(); ctx.fill();
      ellipse(ctx, 17, 16, 12, 7, body);
      ctx.fillStyle = dark;
      ctx.beginPath(); ctx.moveTo(14, 10); ctx.lineTo(19, 5); ctx.lineTo(21, 10); ctx.fill();
      circle(ctx, 24, 14, 1.8, '#1a1a1a');
      if (id === 'cookedFish') {
        ctx.strokeStyle = '#6d3f15'; ctx.lineWidth = 1.6;
        for (let i = 0; i < 3; i++) { ctx.beginPath(); ctx.moveTo(11 + i * 4, 12); ctx.lineTo(9 + i * 4, 20); ctx.stroke(); }
      }
      break;
    }
    case 'crab':
    case 'cookedCrab': {
      const c1 = id === 'crab' ? '#f08a6a' : '#c45a2e';
      ctx.fillStyle = c1;
      ctx.beginPath();
      ctx.moveTo(6, 22); ctx.quadraticCurveTo(8, 8, 18, 9); ctx.quadraticCurveTo(28, 10, 26, 22);
      ctx.quadraticCurveTo(16, 27, 6, 22); ctx.fill();
      ellipse(ctx, 16, 20, 6, 3, id === 'crab' ? '#fde3d6' : '#e8a070');
      if (id === 'cookedCrab') { ctx.strokeStyle = '#7a3015'; ctx.lineWidth = 1.5; ctx.beginPath(); ctx.moveTo(10, 14); ctx.lineTo(22, 14); ctx.stroke(); }
      break;
    }
    case 'herb':
      ctx.strokeStyle = '#3f7d36'; ctx.lineWidth = 2;
      ctx.beginPath(); ctx.moveTo(16, 29); ctx.lineTo(16, 12); ctx.stroke();
      ellipse(ctx, 11, 22, 5, 2.4, '#4d8f3f'); ellipse(ctx, 21, 19, 5, 2.4, '#4d8f3f');
      for (let p = 0; p < 5; p++) {
        const a = (p / 5) * Math.PI * 2;
        circle(ctx, 16 + Math.cos(a) * 4.5, 9 + Math.sin(a) * 4.5, 3.2, '#a777e0');
      }
      circle(ctx, 16, 9, 2.4, '#f7d65a');
      break;
    case 'bandage':
      ctx.save(); ctx.translate(16, 16); ctx.rotate(-0.6);
      ctx.fillStyle = '#efe6d2'; ctx.fillRect(-12, -5, 24, 10);
      ctx.fillStyle = '#7fb069'; ctx.fillRect(-4, -5, 8, 10);
      ctx.fillStyle = 'rgba(0,0,0,0.12)'; ctx.fillRect(-12, 2, 24, 3);
      ctx.restore();
      break;
    case 'axe':
      ctx.strokeStyle = '#8a6438'; ctx.lineWidth = 3.5;
      ctx.beginPath(); ctx.moveTo(9, 28); ctx.lineTo(21, 6); ctx.stroke();
      ctx.fillStyle = COLORS.rock;
      ctx.beginPath(); ctx.moveTo(18, 6); ctx.lineTo(28, 7); ctx.lineTo(27, 17); ctx.lineTo(19, 13); ctx.closePath(); ctx.fill();
      ctx.strokeStyle = '#c9a35a'; ctx.lineWidth = 2;
      ctx.beginPath(); ctx.moveTo(17, 9); ctx.lineTo(22, 12); ctx.stroke();
      break;
    case 'spear':
      ctx.strokeStyle = '#8a6438'; ctx.lineWidth = 3;
      ctx.beginPath(); ctx.moveTo(5, 28); ctx.lineTo(23, 10); ctx.stroke();
      ctx.fillStyle = COLORS.rockHi;
      ctx.beginPath(); ctx.moveTo(29, 3); ctx.lineTo(20, 9); ctx.lineTo(24, 13); ctx.closePath(); ctx.fill();
      ctx.strokeStyle = '#c9a35a'; ctx.lineWidth = 2;
      ctx.beginPath(); ctx.moveTo(19, 12); ctx.lineTo(22, 15); ctx.stroke();
      break;
    case 'rod':
      ctx.strokeStyle = '#8a6438'; ctx.lineWidth = 2.6;
      ctx.beginPath(); ctx.moveTo(5, 28); ctx.lineTo(26, 4); ctx.stroke();
      ctx.strokeStyle = 'rgba(240,240,240,0.9)'; ctx.lineWidth = 1;
      ctx.beginPath(); ctx.moveTo(26, 4); ctx.quadraticCurveTo(29, 16, 25, 24); ctx.stroke();
      circle(ctx, 25, 25, 2.5, '#e24b4b');
      break;
    case 'cloak':
      ctx.fillStyle = '#8a6440';
      ctx.beginPath(); ctx.moveTo(10, 5); ctx.lineTo(22, 5); ctx.lineTo(28, 28); ctx.lineTo(4, 28); ctx.closePath(); ctx.fill();
      ctx.fillStyle = '#e8dcc0'; ctx.fillRect(9, 4, 14, 4);
      ctx.fillStyle = 'rgba(0,0,0,0.18)'; ctx.fillRect(15, 8, 2, 20);
      break;
    case 'campfire':
      ctx.save(); ctx.translate(0, 4); drawObject(ctx, 'campfire', 16, 18, null, 0.3); ctx.restore();
      break;
    case 'wall':
      ctx.save(); ctx.translate(16, 18); ctx.scale(0.8, 0.8); drawObject(ctx, 'wall', 0, 0, { v: 0, hp: 30 }, 0); ctx.restore();
      break;
    case 'nest':
      drawObject(ctx, 'nest', 16, 16, null, 0);
      break;
  }
  ctx.restore();
}

// ── 조명 ─────────────────────────────────────────────
function dayFrac(time) { return ((time % 1) + 1) % 1; }

function darknessAt(time) {
  const f = dayFrac(time);
  const MAX = 0.84;
  if (f < 0.6) return 0;
  if (f < 0.7) return ((f - 0.6) / 0.1) * MAX;
  if (f < 0.92) return MAX;
  return (1 - (f - 0.92) / 0.08) * MAX;
}

function duskTint(time) {
  const f = dayFrac(time);
  if (f > 0.52 && f < 0.72) return Math.sin(((f - 0.52) / 0.2) * Math.PI) * 0.14;
  if (f > 0.9) return Math.sin(((f - 0.9) / 0.1) * Math.PI) * 0.1;
  return 0;
}
