/* Maqueta de pantallas. Script clásico a propósito: así el archivo se abre con
   doble clic, sin servidor (los módulos ES están bloqueados en file://). */
(function () {
  'use strict';

  var LABELS = {
    sala: '1 · Sala',
    menu: '2 · Elegir juego',
    dificultad: '3 · Dificultad',
    instrucciones: '4 · Instrucciones',
    cuenta: '5 · Cuenta',
    partida: '6 · Partida',
    resultados: '8 · Resultados',
  };
  var LAYERS = { pausa: '7 · Pausa', 'sin-mando': '9 · Sin mando' };

  var GAMES = {
    atrapa: { name: 'Atrapa lo correcto', icon: 'juego-atrapa.svg' },
    secuencia: { name: 'Sigue la secuencia', icon: 'juego-secuencia.svg' },
    corta: { name: 'Corta sin fallar', icon: 'juego-corta.svg' },
    equilibrio: { name: 'Equilibrio', icon: 'juego-equilibrio.svg' },
  };
  var ICON_DIR = '../iconos/';

  var GAME_SECONDS = 180;
  var BASKET_STEP = 90;
  var BASKET_LIMIT = 700;
  var STREAK_SHOWN_FROM = 3;
  var SCORE_COUNT_MS = 200;
  var MISS_FADE_MS = 600;

  var stage = document.getElementById('stage');
  var screens = {};
  var layers = {};
  var current = '';
  var openLayer = '';
  var focusIndex = 0;
  var levelIndex = 0;
  var timers = [];

  var basketX = 0;
  var score = 120;
  var streak = 3;
  var remaining = GAME_SECONDS;

  // Voces y efectos son los archivos reales del juego (unity/Assets/Audio).
  var AUDIO_DIR = '../../../unity/Assets/Audio/';
  var VOICES = {
    sala: 'Voces/EscaneaElCodigo.wav',
    menu: 'Voces/EligeUnJuego.wav',
    dificultad: 'Voces/QueTanRapido.wav',
    instrucciones: 'Voces/InclinaParaMoverLaCanasta.wav',
    cuenta: 'Voces/CuentaRegresiva.wav',
    resultados: 'Voces/MuyBien.wav',
  };
  // La música acompaña solo los menús: en la partida se apaga para no distraer (RNF-05).
  var MUSIC_SCREENS = { sala: true, menu: true, dificultad: true, resultados: true };
  var MUSIC_VOLUME = 0.55;
  // Mientras habla una voz, la música baja: nunca compiten dos cosas por la atención.
  var MUSIC_UNDER_VOICE = 0.15;

  var music = new Audio(AUDIO_DIR + 'Musica/Menu.wav');
  music.loop = true;
  music.volume = MUSIC_VOLUME;
  var voice = new Audio();
  voice.addEventListener('ended', function () {
    music.volume = MUSIC_VOLUME;
  });
  var musicOn = true;
  var clicks = null;

  [].forEach.call(document.querySelectorAll('[data-screen]'), function (el) {
    screens[el.dataset.screen] = el;
  });
  [].forEach.call(document.querySelectorAll('[data-layer]'), function (el) {
    layers[el.dataset.layer] = el;
  });

  loadTheme();
  buildQr(document.getElementById('qr'));
  buildChrome();
  fitStage();
  window.addEventListener('resize', fitStage);
  document.addEventListener('keydown', onKey);
  // El navegador no deja sonar nada hasta el primer toque del usuario.
  document.addEventListener('pointerdown', unlockSound);
  stage.addEventListener('click', onClick);
  // index.html#menu abre directo en esa pantalla (#pausa y #sin-mando, en la capa);
  // ?tema=claro elige el tema.
  var startAt = location.hash.slice(1);
  if (layers[startAt]) toggleLayer(startAt);
  else show(screens[startAt] ? startAt : 'sala');

  // --- escalado: la maqueta se piensa en 1920 × 1080 y se encoge a la ventana ---
  function fitStage() {
    var box = document.querySelector('.viewport').getBoundingClientRect();
    var scale = Math.min(box.width / 1920, box.height / 1080);
    stage.style.transform = 'translate(-50%, -50%) scale(' + scale + ')';
  }

  // --- pantallas ---
  function show(name) {
    clearTimers();
    closeLayer();
    current = name;
    stage.dataset.screen = name;
    for (var key in screens) screens[key].classList.toggle('is-active', key === name);
    setFocus(initialFocus(name));
    markChrome();
    screenSound(name);
    if (name === 'sala') enterSala();
    if (name === 'cuenta') enterCuenta();
    if (name === 'partida') enterPartida();
    if (name === 'resultados') enterResultados();
  }

  function enterSala() {
    screens.sala.dataset.pad = 'wait';
    wait(1800, function () {
      screens.sala.dataset.pad = 'on';
    });
  }

  // Un número por segundo, igual que la voz "tres, dos, uno" y GameSession.CountdownSeconds.
  function enterCuenta() {
    var el = document.getElementById('countdown');
    var n = 3;
    el.textContent = n;
    restart(el, 'is-in');
    var tick = function () {
      n -= 1;
      if (n === 0) return show('partida');
      el.textContent = n;
      restart(el, 'is-in');
      wait(1000, tick);
    };
    wait(1000, tick);
  }

  function enterPartida() {
    say('Voces/Ya.wav');
    basketX = 0;
    remaining = GAME_SECONDS;
    moveBasket(0);
    paintHud();
    var tick = function () {
      remaining -= 1;
      paintHud();
      if (remaining > 0) wait(1000, tick);
    };
    wait(1000, tick);
  }

  function enterResultados() {
    var stars = document.querySelectorAll('.star');
    [].forEach.call(stars, function (star, i) {
      star.classList.remove('is-in');
      wait(250 + i * 300, function () {
        star.classList.add('is-in');
      });
    });
  }

  // --- foco (RF-08) ---
  function focusables() {
    var root = openLayer ? layers[openLayer] : screens[current];
    return root ? [].slice.call(root.querySelectorAll('[data-focus]')) : [];
  }

  // Al entrar siempre hay un foco puesto: en Dificultad, la última elegida; en
  // Instrucciones, "empezar" y no el audio.
  function initialFocus(name) {
    if (name === 'dificultad') return levelIndex;
    var items = focusables();
    for (var i = 0; i < items.length; i++) {
      if (items[i].hasAttribute('data-focus-start')) return i;
    }
    return 0;
  }

  function setFocus(index) {
    var items = focusables();
    if (!items.length) return;
    // En los extremos el foco se queda quieto: no da la vuelta.
    focusIndex = Math.min(Math.max(index, 0), items.length - 1);
    items.forEach(function (item, i) {
      item.classList.toggle('is-focus', i === focusIndex);
    });
  }

  // Mover el foco suena y hace vibrar el mando con "tap" (docs/ux/sistema-visual.md §4).
  function moveFocus(step) {
    var before = focusIndex;
    setFocus(focusIndex + step);
    if (focusIndex === before) return;
    blip(660, 0.09, 0.05);
    [].forEach.call(document.querySelectorAll('.pill--pad'), function (pill) {
      restart(pill, 'is-tap');
    });
  }

  function confirm() {
    var items = focusables();
    if (!items.length) {
      if (current === 'sala') show('menu');
      return;
    }
    activate(items[focusIndex]);
  }

  function activate(item) {
    blip(880, 0.1, 0.06);
    blip(1175, 0.16, 0.05, 0.07);
    if (item.dataset.game) chooseGame(item.dataset.game);
    if (item.dataset.level) levelIndex = Number(item.dataset.level);
    if (item.dataset.action) return runAction(item.dataset.action);
    if (item.dataset.go) show(item.dataset.go);
  }

  // Las pantallas siguientes muestran arriba el juego elegido.
  function chooseGame(key) {
    var game = GAMES[key];
    [].forEach.call(document.querySelectorAll('.game-icon'), function (img) {
      img.src = ICON_DIR + game.icon;
    });
    [].forEach.call(document.querySelectorAll('.game-name'), function (el) {
      el.textContent = game.name;
    });
  }

  function runAction(action) {
    if (action === 'voice') return say(VOICES[current]);
    if (action === 'resume') return closeLayer();
    if (action === 'restart') {
      closeLayer();
      return show('cuenta');
    }
    if (action === 'exit') {
      closeLayer();
      show('menu');
    }
  }

  // --- capas: pausa y mando desconectado ---
  function toggleLayer(name) {
    if (openLayer === name) return closeLayer();
    if (current !== 'partida') show('partida');
    closeLayer();
    openLayer = name;
    layers[name].classList.add('is-open');
    setFocus(0);
    markChrome();
    if (name === 'sin-mando') say('Voces/EsperandoTuMando.wav');
  }

  function closeLayer() {
    if (!openLayer) return;
    layers[openLayer].classList.remove('is-open');
    openLayer = '';
    setFocus(0);
    markChrome();
  }

  // --- partida: HUD y retroalimentación ---
  function paintHud() {
    var minutes = Math.floor(remaining / 60);
    var seconds = remaining % 60;
    document.getElementById('hud-time').textContent = minutes + ':' + pad(seconds);
    document.getElementById('hud-score').textContent = score;
    var bar = document.getElementById('hud-bar');
    var left = remaining / GAME_SECONDS;
    bar.style.width = left * 100 + '%';
    bar.classList.toggle('is-low', left < 0.25);
  }

  // El "+10" y la racha van con la canasta: aparecen donde se atrapó.
  function moveBasket(step) {
    basketX = Math.min(Math.max(basketX + step, -BASKET_LIMIT), BASKET_LIMIT);
    document.getElementById('catcher').style.transform = 'translateX(' + basketX + 'px)';
  }

  function hit() {
    effect('Efectos/Acierto.wav');
    countScore(score, score + 10);
    score += 10;
    streak += 1;
    flash('is-hit');
    restart(document.getElementById('popup'), 'is-on');
    if (streak < STREAK_SHOWN_FROM) return;
    var el = document.getElementById('streak');
    el.textContent = streak + ' seguidos';
    restart(el, 'is-on');
  }

  // El error no resta puntos ni escribe nada: el objeto se apaga y la racha vuelve
  // a contar en silencio (RNF-05).
  function miss() {
    effect('Efectos/Error.wav');
    streak = 0;
    flash('is-miss');
    var wrong = document.getElementById('obj-miss');
    wrong.classList.add('is-gone');
    wait(MISS_FADE_MS, function () {
      wrong.classList.remove('is-gone');
    });
  }

  // El puntaje sube contando, no salta.
  function countScore(from, to) {
    var el = document.getElementById('hud-score');
    var start = performance.now();
    var frame = function (now) {
      var t = Math.min((now - start) / SCORE_COUNT_MS, 1);
      el.textContent = Math.round(from + (to - from) * t);
      if (t < 1) requestAnimationFrame(frame);
    };
    requestAnimationFrame(frame);
  }

  function flash(kind) {
    var el = document.getElementById('flash');
    el.className = 'flash';
    void el.offsetWidth;
    el.classList.add(kind);
  }

  // --- entrada ---
  function onKey(event) {
    var key = event.key.toLowerCase();
    unlockSound();

    if (key === 't') return toggleTheme();
    if (key === 'm') return toggleMusic();
    if (key === '2') return togglePad2();
    if (key === 'arrowleft' || key === 'arrowup') return step(event, -1);
    if (key === 'arrowright' || key === 'arrowdown') return step(event, 1);
    if (key === 'enter' || key === ' ') {
      event.preventDefault();
      return confirm();
    }
    if (key === 'escape') return closeLayer();
    if (key === 'p') return toggleLayer('pausa');
    if (key === 'd') return toggleLayer('sin-mando');
    if (current !== 'partida' || openLayer) return;
    if (key === 'a') return hit();
    if (key === 'e') return miss();
  }

  function step(event, direction) {
    event.preventDefault();
    // Durante la partida, inclinar mueve la canasta en vez del foco.
    if (current === 'partida' && !openLayer) return moveBasket(direction * BASKET_STEP);
    moveFocus(direction);
  }

  function onClick(event) {
    var item = event.target.closest('[data-focus]');
    if (!item) return;
    var items = focusables();
    var index = items.indexOf(item);
    if (index < 0) return;
    setFocus(index);
    activate(item);
  }

  // --- controles de la maqueta ---
  function buildChrome() {
    var host = document.getElementById('chrome-buttons');
    Object.keys(LABELS).forEach(function (name) {
      host.appendChild(chromeButton(name, LABELS[name], false));
    });
    Object.keys(LAYERS).forEach(function (name) {
      host.appendChild(chromeButton(name, LAYERS[name], true));
    });
    var theme = document.createElement('button');
    theme.type = 'button';
    theme.id = 'chrome-theme';
    theme.addEventListener('click', toggleTheme);
    host.appendChild(theme);
  }

  function chromeButton(name, label, isLayer) {
    var button = document.createElement('button');
    button.type = 'button';
    button.textContent = label;
    button.dataset.target = name;
    button.addEventListener('click', function () {
      if (isLayer) toggleLayer(name);
      else show(name);
    });
    return button;
  }

  function markChrome() {
    [].forEach.call(document.querySelectorAll('.chrome button[data-target]'), function (button) {
      var target = button.dataset.target;
      button.classList.toggle('is-on', target === current || target === openLayer);
    });
    document.getElementById('chrome-theme').textContent = 'Tema: ' + document.body.dataset.theme;
  }

  // El Mando 2 se une en cualquier momento con el mismo código (RF-20).
  function togglePad2() {
    stage.dataset.pad2 = stage.dataset.pad2 === 'on' ? 'off' : 'on';
  }

  // --- tema: "oscuro" (B, el recomendado) y "claro" (A, tipo Wii U) ---
  function toggleTheme() {
    document.body.dataset.theme = document.body.dataset.theme === 'claro' ? 'oscuro' : 'claro';
    try {
      localStorage.setItem('movimente-tema', document.body.dataset.theme);
    } catch (e) {
      // Sin almacenamiento, el tema solo dura mientras la página esté abierta.
    }
    markChrome();
  }

  function loadTheme() {
    var saved = new URLSearchParams(location.search).get('tema');
    try {
      saved = saved || localStorage.getItem('movimente-tema');
    } catch (e) {
      // Sin almacenamiento se queda el tema de la dirección o el de por defecto.
    }
    if (saved === 'claro' || saved === 'oscuro') document.body.dataset.theme = saved;
  }

  // --- sonido ---
  function unlockSound() {
    if (clicks) return;
    var Context = window.AudioContext || window.webkitAudioContext;
    clicks = Context ? new Context() : {};
    document.getElementById('chrome-sound').textContent = '🔊 Sonido activo';
    screenSound(current);
  }

  function screenSound(name) {
    if (!clicks) return;
    var wanted = musicOn && MUSIC_SCREENS[name];
    if (wanted && music.paused) music.play().catch(function () {});
    if (!wanted && !music.paused) music.pause();
    paintMusic();
    if (VOICES[name]) say(VOICES[name]);
  }

  // Una sola voz a la vez: la nueva corta a la anterior (RNF-05).
  function say(file) {
    if (!clicks) return;
    voice.pause();
    voice.src = AUDIO_DIR + file;
    music.volume = MUSIC_UNDER_VOICE;
    voice.play().catch(function () {
      music.volume = MUSIC_VOLUME;
    });
  }

  function effect(file) {
    if (!clicks) return;
    new Audio(AUDIO_DIR + file).play().catch(function () {});
  }

  function toggleMusic() {
    musicOn = !musicOn;
    screenSound(current);
  }

  function paintMusic() {
    [].forEach.call(document.querySelectorAll('.pill--music'), function (pill) {
      pill.classList.toggle('is-off', !musicOn);
    });
  }

  // Clic corto y suave, sintetizado: marca el foco sin sobresaltar.
  function blip(frequency, seconds, volume, delay) {
    if (!clicks || !clicks.createOscillator) return;
    var start = clicks.currentTime + (delay || 0);
    var tone = clicks.createOscillator();
    var gain = clicks.createGain();
    tone.type = 'sine';
    tone.frequency.value = frequency;
    gain.gain.setValueAtTime(0.0001, start);
    gain.gain.exponentialRampToValueAtTime(volume, start + 0.01);
    gain.gain.exponentialRampToValueAtTime(0.0001, start + seconds);
    tone.connect(gain);
    gain.connect(clicks.destination);
    tone.start(start);
    tone.stop(start + seconds + 0.02);
  }

  // --- utilidades ---
  function wait(ms, fn) {
    timers.push(setTimeout(fn, ms));
  }

  function clearTimers() {
    timers.forEach(clearTimeout);
    timers = [];
  }

  // Reinicia una animación CSS que ya corrió.
  function restart(el, className) {
    el.classList.remove(className);
    void el.offsetWidth;
    el.classList.add(className);
  }

  function pad(n) {
    return n < 10 ? '0' + n : String(n);
  }

  // QR de relleno: el de verdad lo genera el servidor en /qr/<código> (RF-01).
  function buildQr(host) {
    var N = 25;
    var seed = 20260922;
    var grid = [];
    var i;
    var j;

    for (i = 0; i < N; i++) {
      grid[i] = [];
      for (j = 0; j < N; j++) grid[i][j] = false;
    }

    [[0, 0], [N - 7, 0], [0, N - 7]].forEach(function (corner) {
      for (i = 0; i < 7; i++) {
        for (j = 0; j < 7; j++) {
          var edge = i === 0 || i === 6 || j === 0 || j === 6;
          var core = i >= 2 && i <= 4 && j >= 2 && j <= 4;
          grid[corner[0] + i][corner[1] + j] = edge || core;
        }
      }
    });

    for (i = 0; i < N; i++) {
      for (j = 0; j < N; j++) {
        if (inFinder(i, j, N)) continue;
        seed = (seed * 1664525 + 1013904223) % 4294967296;
        grid[i][j] = seed / 4294967296 > 0.52;
      }
    }

    var cell = 100 / N;
    var rects = '';
    for (i = 0; i < N; i++) {
      for (j = 0; j < N; j++) {
        if (!grid[i][j]) continue;
        rects +=
          '<rect x="' + i * cell + '" y="' + j * cell + '" width="' + cell + '" height="' + cell + '"/>';
      }
    }
    host.innerHTML = '<svg viewBox="0 0 100 100" fill="currentColor">' + rects + '</svg>';
  }

  function inFinder(i, j, N) {
    var near = function (a, b) {
      return i >= a && i <= a + 7 && j >= b && j <= b + 7;
    };
    return near(0, 0) || near(N - 8, 0) || near(0, N - 8);
  }
})();
