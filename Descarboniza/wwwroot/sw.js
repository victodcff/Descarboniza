/* Service worker do Descarboniza.
 *
 * O que ele faz:
 *  - guarda só arquivos estáticos (css, js, imagens, bibliotecas) para o app abrir mais rápido;
 *  - se a internet cair durante a navegação, mostra a página /offline.html.
 *
 * O que ele NÃO faz (de propósito):
 *  - não guarda páginas HTML, nem respostas de login, dados, painel admin ou da IA.
 *    Assim, ninguém vê dados de outra pessoa em um aparelho compartilhado;
 *  - não intercepta POST (formulários, envio de foto para a IA, exclusões).
 *
 * Quando mudar este arquivo ou a página offline, aumente a VERSAO.
 */
const VERSAO = 'v1';
const CACHE_ESTATICO = 'descarboniza-estatico-' + VERSAO;
const PAGINA_OFFLINE = '/offline.html';

const PRE_CACHE = [
    PAGINA_OFFLINE,
    '/img/logo.png',
    '/img/pwa/icon-192.png',
    '/favicon.ico'
];

const PREFIXOS_ESTATICOS = ['/css/', '/js/', '/img/', '/lib/'];

self.addEventListener('install', function (event) {
    event.waitUntil(
        caches.open(CACHE_ESTATICO)
            .then(function (cache) { return cache.addAll(PRE_CACHE); })
            .then(function () { return self.skipWaiting(); })
    );
});

self.addEventListener('activate', function (event) {
    event.waitUntil(
        caches.keys()
            .then(function (nomes) {
                return Promise.all(nomes
                    .filter(function (n) { return n.indexOf('descarboniza-') === 0 && n !== CACHE_ESTATICO; })
                    .map(function (n) { return caches.delete(n); }));
            })
            .then(function () { return self.clients.claim(); })
    );
});

function ehEstatico(url) {
    if (url.origin !== self.location.origin) return false;
    if (url.pathname === '/Descarboniza.styles.css') return true;
    return PREFIXOS_ESTATICOS.some(function (p) { return url.pathname.indexOf(p) === 0; });
}

self.addEventListener('fetch', function (event) {
    const req = event.request;
    if (req.method !== 'GET') return;

    const url = new URL(req.url);

    // Navegação entre páginas: sempre vai à rede; só se falhar mostra a página offline.
    if (req.mode === 'navigate') {
        event.respondWith(
            fetch(req).catch(function () {
                return caches.match(PAGINA_OFFLINE);
            })
        );
        return;
    }

    // Arquivos estáticos: responde do cache e atualiza em segundo plano.
    if (ehEstatico(url)) {
        event.respondWith(
            caches.open(CACHE_ESTATICO).then(function (cache) {
                return cache.match(req).then(function (emCache) {
                    const daRede = fetch(req).then(function (resp) {
                        if (resp && resp.ok) cache.put(req, resp.clone());
                        return resp;
                    }).catch(function () { return emCache; });
                    return emCache || daRede;
                });
            })
        );
    }
    // Todo o resto (dados, IA, admin, login) segue direto para a rede.
});
