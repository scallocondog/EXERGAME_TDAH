/* Concepto "Neon Saber" de Claude Design, pasado de React a JS sin dependencias
   para abrirlo con doble clic, como la maqueta. La lógica y el dibujo son los del
   diseño tal cual: es referencia visual y NO cumple CLAUDE.md §10.
   Los ajustes del diseño van en la dirección: ?autoplay=0, ?speed=6..30, ?glow=0..2 */
(function () {
  'use strict';

  const params = new URLSearchParams(location.search);
  const AUTOPLAY = params.get('autoplay') !== '0';
  const SPEED = clamp(readNumber('speed', 14), 6, 30);
  const GLOW = clamp(readNumber('glow', 1), 0, 2);

  const RING_LENGTH = 301.6;
  const BEAT_SECONDS = 60 / 128;
  const RED = '#ff2d55';
  const BLUE = '#2de6ff';

  const root = document.getElementById('root');
  const canvas = document.getElementById('canvas');
  const ctx = canvas.getContext('2d');
  const hud = {
    combo: document.getElementById('combo'),
    mult: document.getElementById('mult'),
    score: document.getElementById('score'),
    ring: document.getElementById('ring'),
    hint: document.getElementById('hint'),
  };

  const state = { score: 48320, combo: 59, mult: 8, prog: 0 };
  const swing = { red: 0, blue: 0, redP: null, blueP: null };
  let cubes = [];
  let bits = [];
  let halves = [];
  let travel = 0;
  let beatT = 0;
  let time = 0;
  let mouse = null;
  let W = 0;
  let H = 0;

  hud.hint.textContent = AUTOPLAY
    ? 'AUTOPLAY · MUEVE EL RATÓN PARA GUIAR LOS SABLES'
    : 'CORTA LOS BLOQUES CON EL SABLE DE SU COLOR';
  paintHud();
  resize();
  new ResizeObserver(resize).observe(root);
  window.addEventListener('pointermove', (e) => {
    const r = canvas.getBoundingClientRect();
    mouse = { x: e.clientX - r.left, y: e.clientY - r.top };
  });

  let last = performance.now();
  requestAnimationFrame(function loop(t) {
    const dt = Math.min(0.05, (t - last) / 1000);
    last = t;
    step(dt);
    draw();
    requestAnimationFrame(loop);
  });

  function resize() {
    const r = root.getBoundingClientRect();
    const d = Math.min(2, devicePixelRatio || 1);
    canvas.width = r.width * d;
    canvas.height = r.height * d;
    W = r.width;
    H = r.height;
    ctx.setTransform(d, 0, 0, d, 0, 0);
  }

  function proj(x, y, z) {
    const f = H * 0.9;
    return { x: W / 2 + (x * f) / z, y: H * 0.42 - (y * f) / z, s: f / z };
  }

  function step(dt) {
    time += dt;
    travel += SPEED * dt;
    beatT += dt;
    if (beatT > BEAT_SECONDS) {
      beatT -= BEAT_SECONDS;
      const pair = Math.random() < 0.25;
      if (pair) {
        spawn('red');
        spawn('blue');
      } else {
        spawn(Math.random() < 0.5 ? 'red' : 'blue');
      }
    }
    const tips = saberGeom();
    for (const c of cubes) {
      c.z -= SPEED * dt;
      if (c.cut) continue;
      if (c.z < 4.2 && c.z > 1.4) {
        const p = proj(c.x, c.y, c.z);
        const g = tips[c.color];
        const d = segDist(p, g.a, g.b);
        if (d < p.s * 0.55 || (AUTOPLAY && c.z < 2.6)) slice(c, p);
      } else if (c.z <= 1.4) {
        c.cut = true;
        c.miss = true;
        miss();
      }
    }
    cubes = cubes.filter((c) => !c.cut && c.z > 0.5);
    for (const b of bits) {
      b.x += b.vx * dt;
      b.y += b.vy * dt;
      b.vy += 600 * dt;
      b.life -= dt;
    }
    bits = bits.filter((b) => b.life > 0);
    for (const h of halves) {
      h.x += h.vx * dt;
      h.y += h.vy * dt;
      h.vy += 900 * dt;
      h.rot += h.vr * dt;
      h.life -= dt;
    }
    halves = halves.filter((h) => h.life > 0);
    swing.red = Math.max(0, swing.red - dt * 4);
    swing.blue = Math.max(0, swing.blue - dt * 4);
  }

  function spawn(color) {
    const lane = color === 'red' ? (Math.random() < 0.7 ? 0 : 1) : Math.random() < 0.7 ? 3 : 2;
    const row = Math.random() < 0.65 ? 0 : 1;
    cubes.push({
      x: -1.5 + lane,
      y: row ? 0 : -0.95,
      z: 42,
      color,
      dir: [0, 90, 180, 270][Math.floor(Math.random() * 4)],
      cut: false,
    });
  }

  function segDist(p, a, b) {
    const dx = b.x - a.x;
    const dy = b.y - a.y;
    const l = dx * dx + dy * dy;
    const t = clamp(((p.x - a.x) * dx + (p.y - a.y) * dy) / l, 0, 1);
    return Math.hypot(p.x - (a.x + t * dx), p.y - (a.y + t * dy));
  }

  function slice(c, p) {
    c.cut = true;
    const col = c.color === 'red' ? RED : BLUE;
    const w = p.s * 0.8;
    const ang = (c.dir * Math.PI) / 180;
    const nx = Math.cos(ang);
    const ny = Math.sin(ang);
    for (const sgn of [-1, 1]) {
      halves.push({
        x: p.x,
        y: p.y,
        w,
        col,
        sgn,
        ang,
        rot: 0,
        vr: sgn * 5,
        vx: nx * sgn * 260,
        vy: ny * sgn * 260 - 120,
        life: 0.6,
      });
    }
    for (let i = 0; i < 26; i++) {
      const a = Math.random() * Math.PI * 2;
      const v = 200 + Math.random() * 500;
      bits.push({
        x: p.x,
        y: p.y,
        vx: Math.cos(a) * v,
        vy: Math.sin(a) * v - 200,
        life: 0.3 + Math.random() * 0.4,
        col,
      });
    }
    swing[c.color] = 1;
    swing[c.color + 'P'] = { x: p.x, y: p.y };
    const need = state.mult === 1 ? 2 : state.mult === 2 ? 4 : 8;
    state.prog += 1;
    if (state.mult < 8 && state.prog >= need) {
      state.mult *= 2;
      state.prog = 0;
    }
    if (state.mult === 8) state.prog = Math.min(state.prog, 8);
    state.score += 115 * state.mult;
    state.combo += 1;
    paintHud();
  }

  // Así estaba en el diseño: fallar reinicia el combo y baja el multiplicador.
  // Es justo lo que prohíbe RNF-05 ("el error se corrige, no se castiga").
  function miss() {
    state.combo = 0;
    state.mult = Math.max(1, state.mult / 2);
    state.prog = 0;
    paintHud();
  }

  function paintHud() {
    const need = state.mult === 1 ? 2 : state.mult === 2 ? 4 : 8;
    const frac = state.mult === 8 ? 1 : state.prog / need;
    hud.combo.textContent = state.combo;
    hud.mult.textContent = state.mult;
    hud.score.textContent = state.score.toLocaleString('en-US');
    hud.ring.style.strokeDashoffset = RING_LENGTH * (1 - frac);
  }

  function saberGeom() {
    const m = mouse || { x: W / 2, y: H * 0.55 + Math.sin(time * 1.3) * 30 };
    const out = {};
    for (const [k, bx, off] of [
      ['red', W * 0.36, -W * 0.09],
      ['blue', W * 0.64, W * 0.09],
    ]) {
      let tx = m.x + off;
      let ty = m.y;
      const sw = swing[k];
      const sp = swing[k + 'P'];
      if (sw > 0 && sp) {
        tx += (sp.x - tx) * sw;
        ty += (sp.y - ty) * sw;
      }
      const base = { x: bx, y: H + 30 };
      const ang = clamp(Math.atan2(ty - base.y, tx - base.x), -Math.PI + 0.25, -0.25);
      const L = H * 0.62;
      const hl = 70;
      out[k] = {
        base,
        ang,
        a: { x: base.x + Math.cos(ang) * hl, y: base.y + Math.sin(ang) * hl },
        b: { x: base.x + Math.cos(ang) * L, y: base.y + Math.sin(ang) * L },
      };
    }
    return out;
  }

  function draw() {
    const hy = H * 0.42;
    const t = time;
    ctx.globalCompositeOperation = 'source-over';
    ctx.shadowBlur = 0;
    let g = ctx.createLinearGradient(0, 0, 0, H);
    g.addColorStop(0, '#05030c');
    g.addColorStop(0.42, '#1a0630');
    g.addColorStop(0.43, '#07020f');
    g.addColorStop(1, '#030108');
    ctx.fillStyle = g;
    ctx.fillRect(0, 0, W, H);

    // sol
    const R = H * 0.2;
    ctx.save();
    ctx.beginPath();
    ctx.rect(0, 0, W, hy);
    ctx.clip();
    g = ctx.createLinearGradient(0, hy - R, 0, hy);
    g.addColorStop(0, '#ffb347');
    g.addColorStop(0.5, '#ff3d7f');
    g.addColorStop(1, '#8a1bd1');
    ctx.fillStyle = g;
    ctx.shadowColor = '#ff3d7f';
    ctx.shadowBlur = 60 * GLOW;
    ctx.beginPath();
    ctx.arc(W / 2, hy, R, Math.PI, 0);
    ctx.fill();
    ctx.shadowBlur = 0;
    ctx.fillStyle = '#1a0630';
    for (let i = 0; i < 7; i++) {
      const y = hy - R * 0.08 - i * R * 0.12;
      ctx.fillRect(W / 2 - R, y, R * 2, 2 + (7 - i) * 1.1);
    }
    ctx.restore();

    // láseres
    ctx.globalCompositeOperation = 'lighter';
    const lasers = [
      { x: W * 0.12, c: '255,45,85', s: 0.6, p: 0 },
      { x: W * 0.88, c: '45,230,255', s: -0.6, p: 1 },
      { x: W * 0.3, c: '190,70,255', s: 0.9, p: 2 },
      { x: W * 0.7, c: '190,70,255', s: -0.9, p: 3 },
    ];
    for (const L of lasers) {
      for (let k = 0; k < 2; k++) {
        const a = -Math.PI / 2 + Math.sin(t * L.s + L.p + k * 1.4) * 0.9;
        const pulse = 0.5 + 0.5 * Math.sin(t * 4 + L.p * 2 + k);
        ctx.strokeStyle = `rgba(${L.c},${0.25 + pulse * 0.45})`;
        ctx.lineWidth = 2;
        ctx.shadowColor = `rgb(${L.c})`;
        ctx.shadowBlur = 18 * GLOW;
        ctx.beginPath();
        ctx.moveTo(L.x, hy);
        ctx.lineTo(L.x + Math.cos(a) * H * 2, hy + Math.sin(a) * H * 2);
        ctx.stroke();
      }
    }
    ctx.shadowBlur = 0;
    ctx.fillStyle = 'rgba(255,61,127,0.8)';
    ctx.fillRect(0, hy - 1, W, 2);

    // plano de la pista
    ctx.globalCompositeOperation = 'source-over';
    const a0 = proj(-2.2, -1.6, 60);
    const a1 = proj(2.2, -1.6, 60);
    const a2 = proj(2.2, -1.6, 0.9);
    const a3 = proj(-2.2, -1.6, 0.9);
    g = ctx.createLinearGradient(0, hy, 0, H);
    g.addColorStop(0, 'rgba(20,8,40,0.9)');
    g.addColorStop(1, 'rgba(10,4,24,0.95)');
    ctx.fillStyle = g;
    ctx.beginPath();
    ctx.moveTo(a0.x, a0.y);
    ctx.lineTo(a1.x, a1.y);
    ctx.lineTo(a2.x, a2.y);
    ctx.lineTo(a3.x, a3.y);
    ctx.fill();

    // rejilla
    ctx.globalCompositeOperation = 'lighter';
    ctx.lineWidth = 1;
    for (let x = -6; x <= 6; x++) {
      const p = proj(x, -1.6, 60);
      const q = proj(x, -1.6, 0.9);
      ctx.strokeStyle = Math.abs(x) <= 2 ? 'rgba(140,90,255,0.35)' : 'rgba(255,61,160,0.22)';
      ctx.beginPath();
      ctx.moveTo(p.x, p.y);
      ctx.lineTo(q.x, q.y);
      ctx.stroke();
    }
    const sp = 2.5;
    const off = travel % sp;
    for (let z = sp - off; z < 60; z += sp) {
      if (z < 0.9) continue;
      const p = proj(-6, -1.6, z);
      const q = proj(6, -1.6, z);
      const al = Math.max(0, 1 - z / 60);
      ctx.strokeStyle = `rgba(255,61,160,${0.5 * al})`;
      ctx.beginPath();
      ctx.moveTo(p.x, p.y);
      ctx.lineTo(q.x, q.y);
      ctx.stroke();
    }

    // rieles
    for (const [x, c] of [
      [-2.2, RED],
      [2.2, BLUE],
    ]) {
      const p = proj(x, -1.6, 60);
      const q = proj(x, -1.6, 0.9);
      ctx.strokeStyle = c;
      ctx.lineWidth = 3;
      ctx.shadowColor = c;
      ctx.shadowBlur = 16 * GLOW;
      ctx.beginPath();
      ctx.moveTo(p.x, p.y);
      ctx.lineTo(q.x, q.y);
      ctx.stroke();
    }

    // arcos
    const asp = 8;
    const aoff = travel % asp;
    for (let z = asp - aoff; z < 60; z += asp) {
      if (z < 1.2) continue;
      const al = Math.min(1, (60 - z) / 25) * Math.min(1, (z - 1.2) / 2);
      const p = proj(-4, -1.6, z);
      const q = proj(4, 3.2, z);
      ctx.strokeStyle = `rgba(190,70,255,${0.7 * al})`;
      ctx.lineWidth = Math.max(1, 30 / z);
      ctx.shadowColor = '#be46ff';
      ctx.shadowBlur = 14 * GLOW;
      ctx.beginPath();
      ctx.moveTo(p.x, p.y);
      ctx.lineTo(p.x, q.y);
      ctx.lineTo(q.x, q.y);
      ctx.lineTo(q.x, p.y);
      ctx.stroke();
    }
    ctx.shadowBlur = 0;

    // cubos
    ctx.globalCompositeOperation = 'source-over';
    for (const c of [...cubes].sort((a, b) => b.z - a.z)) drawCube(c);

    // mitades cortadas
    for (const h of halves) {
      ctx.save();
      ctx.globalAlpha = Math.max(0, h.life / 0.6);
      ctx.translate(h.x, h.y);
      ctx.rotate(h.ang + h.rot);
      ctx.fillStyle = h.col === RED ? '#3a0714' : '#062a36';
      ctx.strokeStyle = h.col;
      ctx.lineWidth = 2;
      ctx.shadowColor = h.col;
      ctx.shadowBlur = 14 * GLOW;
      ctx.beginPath();
      ctx.rect(h.sgn < 0 ? -h.w / 2 : 0, -h.w / 2, h.w / 2, h.w);
      ctx.fill();
      ctx.stroke();
      ctx.restore();
    }

    // chispas
    ctx.globalCompositeOperation = 'lighter';
    for (const b of bits) {
      ctx.globalAlpha = Math.min(1, b.life * 2.5);
      ctx.strokeStyle = b.col;
      ctx.lineWidth = 2;
      ctx.beginPath();
      ctx.moveTo(b.x, b.y);
      ctx.lineTo(b.x - b.vx * 0.03, b.y - b.vy * 0.03);
      ctx.stroke();
    }
    ctx.globalAlpha = 1;

    // sables
    const sab = saberGeom();
    for (const [k, c, rgb] of [
      ['red', RED, '255,45,85'],
      ['blue', BLUE, '45,230,255'],
    ]) {
      const s = sab[k];
      ctx.globalCompositeOperation = 'source-over';
      ctx.shadowBlur = 0;
      ctx.strokeStyle = '#2a2838';
      ctx.lineWidth = 16;
      ctx.lineCap = 'round';
      ctx.beginPath();
      ctx.moveTo(s.base.x, s.base.y);
      ctx.lineTo(s.a.x, s.a.y);
      ctx.stroke();
      ctx.strokeStyle = '#8c8aa0';
      ctx.lineWidth = 3;
      ctx.beginPath();
      ctx.moveTo(s.base.x, s.base.y);
      ctx.lineTo(s.a.x, s.a.y);
      ctx.stroke();
      ctx.globalCompositeOperation = 'lighter';
      ctx.shadowColor = c;
      ctx.shadowBlur = 40 * GLOW;
      ctx.strokeStyle = `rgba(${rgb},0.35)`;
      ctx.lineWidth = 26;
      ctx.beginPath();
      ctx.moveTo(s.a.x, s.a.y);
      ctx.lineTo(s.b.x, s.b.y);
      ctx.stroke();
      ctx.shadowBlur = 20 * GLOW;
      ctx.strokeStyle = c;
      ctx.lineWidth = 10;
      ctx.beginPath();
      ctx.moveTo(s.a.x, s.a.y);
      ctx.lineTo(s.b.x, s.b.y);
      ctx.stroke();
      ctx.shadowBlur = 0;
      ctx.strokeStyle = '#fff';
      ctx.lineWidth = 3.5;
      ctx.beginPath();
      ctx.moveTo(s.a.x, s.a.y);
      ctx.lineTo(s.b.x, s.b.y);
      ctx.stroke();
    }
    ctx.globalCompositeOperation = 'source-over';
    ctx.lineCap = 'butt';

    // viñeta
    g = ctx.createRadialGradient(W / 2, H * 0.5, H * 0.3, W / 2, H * 0.5, Math.max(W, H) * 0.8);
    g.addColorStop(0, 'rgba(0,0,0,0)');
    g.addColorStop(1, 'rgba(0,0,0,0.6)');
    ctx.fillStyle = g;
    ctx.fillRect(0, 0, W, H);
  }

  function drawCube(c) {
    const s = 0.4;
    const col = c.color === 'red' ? RED : BLUE;
    const fill = c.color === 'red' ? '#2a0610' : '#04202b';
    const side = c.color === 'red' ? '#1a040a' : '#02141c';
    const al = clamp((42 - c.z) / 10, 0, 1);
    const P = (dx, dy, dz) => proj(c.x + dx, c.y + dy, c.z + dz);
    const f = [P(-s, s, -s), P(s, s, -s), P(s, -s, -s), P(-s, -s, -s)];
    const b = [P(-s, s, s), P(s, s, s), P(s, -s, s), P(-s, -s, s)];
    const poly = (pts, fl) => {
      ctx.beginPath();
      ctx.moveTo(pts[0].x, pts[0].y);
      for (let i = 1; i < pts.length; i++) ctx.lineTo(pts[i].x, pts[i].y);
      ctx.closePath();
      ctx.fillStyle = fl;
      ctx.fill();
      ctx.stroke();
    };
    ctx.save();
    ctx.globalAlpha = al;
    ctx.strokeStyle = col;
    ctx.lineWidth = Math.max(1, f[0].s * 0.035);
    ctx.lineJoin = 'round';
    ctx.shadowColor = col;
    ctx.shadowBlur = 16 * GLOW;
    if (c.y - s > 0) poly([f[3], f[2], b[2], b[3]], side);
    if (c.y + s < 0) poly([f[0], f[1], b[1], b[0]], side);
    if (c.x - s > 0) poly([f[0], f[3], b[3], b[0]], side);
    if (c.x + s < 0) poly([f[1], f[2], b[2], b[1]], side);
    ctx.shadowBlur = 24 * GLOW;
    poly(f, fill);
    const cx = (f[0].x + f[1].x) / 2;
    const cy = (f[0].y + f[3].y) / 2;
    const w = f[1].x - f[0].x;
    ctx.translate(cx, cy);
    ctx.rotate((c.dir * Math.PI) / 180);
    ctx.fillStyle = '#fff';
    ctx.shadowColor = '#fff';
    ctx.shadowBlur = 12 * GLOW;
    ctx.beginPath();
    ctx.moveTo(-w * 0.32, -w * 0.12);
    ctx.lineTo(w * 0.32, -w * 0.12);
    ctx.lineTo(0, -w * 0.36);
    ctx.closePath();
    ctx.fill();
    ctx.shadowBlur = 0;
    ctx.fillStyle = 'rgba(255,255,255,0.9)';
    ctx.beginPath();
    ctx.arc(0, w * 0.12, w * 0.05, 0, Math.PI * 2);
    ctx.fill();
    ctx.restore();
  }

  function readNumber(name, fallback) {
    const raw = params.get(name);
    const value = Number(raw);
    return raw === null || Number.isNaN(value) ? fallback : value;
  }

  function clamp(value, min, max) {
    return Math.min(Math.max(value, min), max);
  }
})();
