// 효과음: 외부 파일 없이 Web Audio로 합성
'use strict';

const Sound = {
  ctx: null,
  enabled: storageGet('catpo-sound') !== 'off',
  noise: null,

  unlock() {
    if (this.ctx) { if (this.ctx.state === 'suspended') this.ctx.resume(); return; }
    const AC = window.AudioContext || window.webkitAudioContext;
    if (!AC) return;
    try {
      this.ctx = new AC();
      const len = this.ctx.sampleRate * 0.5;
      this.noise = this.ctx.createBuffer(1, len, this.ctx.sampleRate);
      const d = this.noise.getChannelData(0);
      for (let i = 0; i < len; i++) d[i] = Math.random() * 2 - 1;
    } catch (e) { this.ctx = null; }
  },

  setEnabled(on) {
    this.enabled = on;
    storageSet('catpo-sound', on ? 'on' : 'off');
  },

  tone(freq, dur, { type = 'sine', vol = 0.15, slide = 0, delay = 0 } = {}) {
    const c = this.ctx, t0 = c.currentTime + delay;
    const o = c.createOscillator(), g = c.createGain();
    o.type = type;
    o.frequency.setValueAtTime(freq, t0);
    if (slide) o.frequency.exponentialRampToValueAtTime(Math.max(30, freq + slide), t0 + dur);
    g.gain.setValueAtTime(0.0001, t0);
    g.gain.exponentialRampToValueAtTime(vol, t0 + 0.01);
    g.gain.exponentialRampToValueAtTime(0.0001, t0 + dur);
    o.connect(g).connect(c.destination);
    o.start(t0);
    o.stop(t0 + dur + 0.02);
  },

  hiss(dur, { vol = 0.12, freq = 1200, q = 1, delay = 0 } = {}) {
    const c = this.ctx, t0 = c.currentTime + delay;
    const s = c.createBufferSource(), f = c.createBiquadFilter(), g = c.createGain();
    s.buffer = this.noise;
    f.type = 'bandpass';
    f.frequency.value = freq;
    f.Q.value = q;
    g.gain.setValueAtTime(vol, t0);
    g.gain.exponentialRampToValueAtTime(0.0001, t0 + dur);
    s.connect(f).connect(g).connect(c.destination);
    s.start(t0);
    s.stop(t0 + dur + 0.02);
  },

  play(name) {
    if (!this.enabled || !this.ctx || this.ctx.state !== 'running') return;
    switch (name) {
      case 'swish': this.hiss(0.12, { vol: 0.06, freq: 2400, q: 0.7 }); break;
      case 'hit': this.hiss(0.1, { vol: 0.16, freq: 900 }); this.tone(180, 0.12, { type: 'square', vol: 0.06, slide: -80 }); break;
      case 'chop': this.tone(220, 0.08, { type: 'triangle', vol: 0.14, slide: -90 }); this.hiss(0.06, { vol: 0.08, freq: 700 }); break;
      case 'stone': this.tone(620, 0.06, { type: 'square', vol: 0.05, slide: -200 }); this.hiss(0.05, { vol: 0.1, freq: 3000 }); break;
      case 'pick': this.tone(660, 0.07, { vol: 0.1 }); this.tone(990, 0.09, { vol: 0.08, delay: 0.05 }); break;
      case 'thud': this.tone(120, 0.1, { type: 'triangle', vol: 0.1, slide: -40 }); break;
      case 'eat': this.hiss(0.06, { vol: 0.1, freq: 1500 }); this.hiss(0.06, { vol: 0.1, freq: 1300, delay: 0.1 }); break;
      case 'heal': this.tone(523, 0.12, { vol: 0.08 }); this.tone(784, 0.18, { vol: 0.08, delay: 0.08 }); break;
      case 'drink': this.tone(400, 0.12, { vol: 0.08, slide: 300 }); this.tone(500, 0.1, { vol: 0.06, slide: 300, delay: 0.12 }); break;
      case 'craft': [523, 659, 784].forEach((f, i) => this.tone(f, 0.14, { type: 'triangle', vol: 0.09, delay: i * 0.07 })); break;
      case 'build': this.tone(160, 0.12, { type: 'triangle', vol: 0.16, slide: -50 }); this.hiss(0.1, { vol: 0.08, freq: 600 }); break;
      case 'fire': this.hiss(0.35, { vol: 0.1, freq: 500, q: 0.5 }); break;
      case 'cook': this.hiss(0.4, { vol: 0.1, freq: 3500, q: 0.4 }); this.tone(700, 0.1, { vol: 0.06, delay: 0.3 }); break;
      case 'cast': this.hiss(0.18, { vol: 0.07, freq: 2000, q: 0.6 }); this.tone(300, 0.1, { vol: 0.05, slide: -100, delay: 0.25 }); break;
      case 'bite': this.tone(880, 0.08, { type: 'square', vol: 0.06 }); this.tone(880, 0.08, { type: 'square', vol: 0.06, delay: 0.12 }); break;
      case 'splash': this.hiss(0.3, { vol: 0.14, freq: 1100, q: 0.5 }); this.tone(700, 0.15, { vol: 0.08, delay: 0.1 }); break;
      case 'hurt': this.tone(300, 0.18, { type: 'sawtooth', vol: 0.08, slide: -180 }); break;
      case 'kill': this.tone(400, 0.1, { type: 'square', vol: 0.06, slide: -200 }); this.tone(600, 0.15, { vol: 0.08, delay: 0.1 }); break;
      case 'break': this.hiss(0.25, { vol: 0.16, freq: 400 }); break;
      case 'howl': this.tone(420, 0.9, { type: 'sine', vol: 0.05, slide: 180 }); break;
      case 'sleep': this.tone(392, 0.3, { vol: 0.06 }); this.tone(330, 0.4, { vol: 0.06, delay: 0.25 }); break;
      case 'newday': [392, 523, 659, 784].forEach((f, i) => this.tone(f, 0.25, { type: 'triangle', vol: 0.07, delay: i * 0.1 })); break;
      case 'dead': [392, 330, 262, 196].forEach((f, i) => this.tone(f, 0.35, { type: 'triangle', vol: 0.09, delay: i * 0.18 })); break;
      case 'click': this.tone(800, 0.04, { vol: 0.05 }); break;
    }
  },
};
