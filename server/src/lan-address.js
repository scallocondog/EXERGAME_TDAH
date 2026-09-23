// RF-01: IP de la PC en la red Wi-Fi, para la URL del QR
import { networkInterfaces } from 'node:os';

// Adaptadores virtuales (WSL, Hyper-V, VirtualBox, VPN) no son alcanzables desde el celular.
const VIRTUAL_ADAPTER = /vethernet|virtualbox|vmware|wsl|hyper-v|loopback|tailscale|zerotier/i;

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

function privateRank(ip) {
  if (ip.startsWith('192.168.')) return 0;
  if (ip.startsWith('10.')) return 1;
  if (/^172\.(1[6-9]|2\d|3[01])\./.test(ip)) return 2;
  return 3;
}
