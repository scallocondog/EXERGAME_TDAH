// RNF-05: la pantalla del mando no debe apagarse a mitad de una partida
let lock = null;

export async function keepScreenAwake() {
  if (!navigator.wakeLock) return;
  try {
    lock = await navigator.wakeLock.request('screen');
    lock.addEventListener('release', () => {
      lock = null;
    });
  } catch {
    // Batería baja o pestaña en segundo plano: se vuelve a pedir al volver.
    lock = null;
  }
}

// El navegador suelta el bloqueo al cambiar de app; hay que pedirlo de nuevo.
export function watchScreenAwake() {
  document.addEventListener('visibilitychange', () => {
    if (document.visibilityState === 'visible' && !lock) keepScreenAwake();
  });
}
