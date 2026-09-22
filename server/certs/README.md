# certs — certificados HTTPS locales

**Nada de esta carpeta se commitea** salvo este README. Cada integrante genera
los suyos en su máquina (RNF-04).

Opción recomendada, `mkcert` (genera un certificado que el sistema ya confía):

```bash
mkcert -install
mkcert -key-file key.pem -cert-file cert.pem 192.168.1.10 localhost
```

Reemplazar `192.168.1.10` por la IP de la PC en la red Wi-Fi donde se juega.

Alternativa sin instalar nada extra, con OpenSSL:

```bash
openssl req -x509 -newkey rsa:2048 -nodes -days 365 \
  -keyout key.pem -out cert.pem -subj "/CN=192.168.1.10"
```

Con el certificado autofirmado el celular mostrará una advertencia la primera
vez; se acepta y queda. Es normal en red local y hay que mencionarlo en las
instrucciones de la demo.
