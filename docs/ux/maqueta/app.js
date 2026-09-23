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
    resultados: '7 · Resultados',
  };
  var LAYERS = { pausa: '· Pausa', 'sin-mando': '· Sin mando' };

  var GAME_SECONDS = 180;
  var BASKET_STEP = 90;
  var BASKET_LIMIT = 700;

  var stage = document.getElementById('stage');
  var screens = {};
  var layers = {};
  var current = '';
  var openLayer = '';
  var focusIndex = 0;
  var timers = [];

  var basketX = 0;
  var score = 120;
  var streak = 3;
  var remaining = GAME_SECONDS;

  [].forEach.call(document.querySelectorAll('[data-screen]'), function (el) {
    screens[el.dataset.screen] = el;
  });
  [].forEach.call(document.querySelectorAll('[data-layer]'), function (el) {
    layers[el.dataset.layer] = el;
  });

  buildQr(document.getElementById('qr'));
  buildChrome();
  fitStage();
  window.addEventListener('resize', fitStage);
  document.addEventListener('keydown', onKey);
  stage.addEventListener('click', onClick);
  show('sala');

  // --- escalado: la maqueta se piensa en 1920 × 1080 y se encoge a la ventana ---
  function fitStage() {
    var box = document.querySelector('.viewport').getBoundingClientRect();
    var scale = Math.min(box.width / 1920, box.height / 1080);
    stage.style.transform = 'scale(' + scale + ')';
  }

  // --- pantallas ---
  function show(name) {
    clearTimers();
    closeLayer();
    current = name;
    for (var key in screens) screens[key].classList.toggle('is-active', key === name);
    setFocus(0);
    markChrome();
    if (name === 'sala') enterSala();
    if (name === 'cuenta') enterCuenta();
    if (name === 'partida') enterPartida();
    if (name === 'resultados') enterResultados();
  }

  function enterSala() {
    var chip = document.getElementById('pad-chip');
    chip.textContent = 'Esperando mando…';
    chip.className = 'chip chip--wait';
    wait(1800, function () {
      chip.textContent = 'Mando 1 conectado';
      chip.className = 'chip chip--on';
    });
  }

  function enterCuenta() {
    var el = document.getElementById('countdown');
    var n = 3;
    el.textContent = n;
    restart(el, 'countdown');
    var tick = function () {
      n -= 1;
      if (n === 0) return show('partida');
      el.textContent = n;
      restart(el, 'countdown');
      wait(800, tick);
    };
    wait(800, tick);
  }

  function enterPartida() {
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

  function setFocus(index) {
    var items = focusables();
    if (!items.length) return;
    // En los extremos el foco se queda quieto: no da la vuelta.
    focusIndex = Math.min(Math.max(index, 0), items.length - 1);
    items.forEach(function (item, i) {
      item.classList.toggle('is-focus', i === focusIndex);
    });
  }

  function moveFocus(step) {
    setFocus(focusIndex + step);
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
    if (item.dataset.action) return runAction(item.dataset.action);
    if (item.dataset.go) show(item.dataset.go);
  }

  function runAction(action) {
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

  function moveBasket(step) {
    basketX = Math.min(Math.max(basketX + step, -BASKET_LIMIT), BASKET_LIMIT);
    document.getElementById('basket').style.transform =
      'translateX(calc(-50% + ' + basketX + 'px))';
  }

  function hit() {
    score += 10;
    streak += 1;
    paintHud();
    flash('is-hit');
    var popup = document.getElementById('popup');
    popup.textContent = '+10';
    restart(popup, 'is-on');
    var el = document.getElementById('streak');
    el.textContent = streak + ' seguidos';
    el.classList.toggle('is-on', streak >= 2);
  }

  // El error no resta puntos ni escribe nada: solo se apaga la racha (RNF-05).
  function miss() {
    streak = 0;
    flash('is-miss');
    document.getElementById('streak').classList.remove('is-on');
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
    [].forEach.call(document.querySelectorAll('.chrome button'), function (button) {
      var target = button.dataset.target;
      button.classList.toggle('is-on', target === current || target === openLayer);
    });
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
    host.innerHTML = '<svg viewBox="0 0 100 100" fill="#12303b">' + rects + '</svg>';
  }

  function inFinder(i, j, N) {
    var near = function (a, b) {
      return i >= a && i <= a + 7 && j >= b && j <= b + 7;
    };
    return near(0, 0) || near(N - 8, 0) || near(0, N - 8);
  }
})();
