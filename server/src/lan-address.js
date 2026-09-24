// RF-01: IP de la PC en la red Wi-Fi, para la URL del QR
import { createSocket } from 'node:dgram';
import { networkInterfaces } from 'node:os';

// Adaptadores virtuales (WSL, Hyper-V, VirtualBox, VPN) no son alcanzables desde el celular.
const VIRTUAL_ADAPTER = /vethernet|virtualbox|vmware|wsl|hyper-v|loopback|tailscale|zerotier|openvpn|tap-windows/i;

export function findLanAddresses() {
  const candidates = [];
  for (const [name, addresses] of Object.entries(networkInterfaces())) {
    if (VIRTUAL_ADAPTER.test(name)) continue;
    for (const address of addresses ?? []) {
      if (address.family === 'IPv4' && !address.internal) {
        candidates.push({ name, address: address.address });
      }
    }
  }
  return candidates.sort((a, b) => privateRank(a.address) - privateRank(b.address));
}

// IP por la que la PC sale a la red de verdad (la de la ruta por defecto).
// Windows deja la IP vieja en un adaptador desconectado, así que la lista de
// interfaces sola no alcanza. Un "connect" UDP solo elige la interfaz: no envía nada.
export function findRoutedAddress() {
  return new Promise((resolve) => {
    const socket = createSocket('udp4');
    socket.once('error', () => {
      socket.close();
      resolve(null);
    });
    socket.connect(53, '8.8.8.8', () => {
      const { address } = socket.address();
      socket.close();
      resolve(address);
    });
  });
}

// La ruta manda si es una IP real de la red; si no hay ruta (red sin
// internet) o sale por una VPN, se usa la primera candidata.
export function pickHostAddress(candidates, routedAddress) {
  const routed = candidates.find((candidate) => candidate.address === routedAddress);
  return (routed ?? candidates[0])?.address ?? null;
}

function privateRank(ip) {
  if (ip.startsWith('192.168.')) return 0;
  if (ip.startsWith('10.')) return 1;
  if (/^172\.(1[6-9]|2\d|3[01])\./.test(ip)) return 2;
  return 3;
}
