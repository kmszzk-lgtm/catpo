// 공용 유틸리티: 난수, 노이즈, 수학 함수
'use strict';

function mulberry32(seed) {
  let a = seed >>> 0;
  return function () {
    a = (a + 0x6d2b79f5) >>> 0;
    let t = a;
    t = Math.imul(t ^ (t >>> 15), t | 1);
    t ^= t + Math.imul(t ^ (t >>> 7), t | 61);
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

// 좌표 기반 해시 (0~1). 타일별 고정 변주에 사용
function hash2(x, y, s = 0) {
  let h = (x * 374761393 + y * 668265263 + s * 2147483647) | 0;
  h = Math.imul(h ^ (h >>> 13), 1274126177);
  h ^= h >>> 16;
  return (h >>> 0) / 4294967296;
}

function makeNoise(seed) {
  const SIZE = 256;
  const rnd = mulberry32(seed);
  const vals = new Float32Array(SIZE * SIZE);
  for (let i = 0; i < vals.length; i++) vals[i] = rnd();
  const smooth = (t) => t * t * (3 - 2 * t);
  function value(x, y) {
    const xi = Math.floor(x), yi = Math.floor(y);
    const xf = x - xi, yf = y - yi;
    const x0 = xi & (SIZE - 1), y0 = yi & (SIZE - 1);
    const x1 = (x0 + 1) & (SIZE - 1), y1 = (y0 + 1) & (SIZE - 1);
    const a = vals[y0 * SIZE + x0], b = vals[y0 * SIZE + x1];
    const c = vals[y1 * SIZE + x0], d = vals[y1 * SIZE + x1];
    const u = smooth(xf), v = smooth(yf);
    return a + (b - a) * u + (c - a) * v + (a - b - c + d) * u * v;
  }
  return function fbm(x, y, oct = 4) {
    let sum = 0, amp = 1, freq = 1, norm = 0;
    for (let i = 0; i < oct; i++) {
      sum += value(x * freq, y * freq) * amp;
      norm += amp;
      amp *= 0.5;
      freq *= 2.03;
    }
    return sum / norm;
  };
}

const clamp = (v, lo, hi) => (v < lo ? lo : v > hi ? hi : v);
const lerp = (a, b, t) => a + (b - a) * t;
const dist = (ax, ay, bx, by) => Math.hypot(ax - bx, ay - by);
const randInt = (rnd, lo, hi) => lo + Math.floor(rnd() * (hi - lo + 1));

function circleRect(cx, cy, r, rx, ry, rw, rh) {
  const nx = clamp(cx, rx, rx + rw);
  const ny = clamp(cy, ry, ry + rh);
  const dx = cx - nx, dy = cy - ny;
  return dx * dx + dy * dy < r * r;
}

// 탐험 기록 같은 0/1 배열을 짧은 문자열로 압축
function rleEncode(arr) {
  const out = [];
  let cur = arr[0], run = 0;
  for (let i = 0; i < arr.length; i++) {
    if (arr[i] === cur) run++;
    else { out.push(run); cur = arr[i]; run = 1; }
  }
  out.push(run);
  return arr[0] + ':' + out.join(',');
}

function rleDecode(str, len) {
  const arr = new Uint8Array(len);
  if (!str) return arr;
  const [first, runs] = str.split(':');
  let v = Number(first), i = 0;
  for (const r of runs.split(',')) {
    const n = Number(r);
    if (v) arr.fill(1, i, Math.min(len, i + n));
    i += n;
    v = v ? 0 : 1;
  }
  return arr;
}

function storageGet(key) {
  try { return window.localStorage.getItem(key); } catch (e) { return null; }
}
function storageSet(key, val) {
  try { window.localStorage.setItem(key, val); return true; } catch (e) { return false; }
}
function storageRemove(key) {
  try { window.localStorage.removeItem(key); } catch (e) { /* 무시 */ }
}
