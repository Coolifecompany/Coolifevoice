/* CoolifeVO PWA Service Worker — 离线缓存 + 自动更新 */
const CACHE = 'coolifevo-v1';
const ASSETS = [
  './',
  './CoolifeVOI.html',
  './CoolifeVOI-Translator.html',
  './CoolifeVOI-Thai2Zh.html',
  './pwa/manifest-main.json',
  './pwa/manifest-trans.json',
  './pwa/manifest-thai2zh.json',
  './pwa/icon-192.png',
  './pwa/icon-512.png',
  './pwa/apple-touch-icon.png'
];

self.addEventListener('install', e => {
  e.waitUntil(
    caches.open(CACHE).then(c => c.addAll(ASSETS)).catch(()=>{})
  );
  self.skipWaiting();
});

self.addEventListener('activate', e => {
  e.waitUntil(
    caches.keys().then(keys => Promise.all(
      keys.filter(k => k !== CACHE).map(k => caches.delete(k))
    ))
  );
  self.clients.claim();
});

// 网络优先，失败回退缓存（保证在线始终最新）
self.addEventListener('fetch', e => {
  const url = new URL(e.request.url);
  if (e.request.method !== 'GET') return;
  if (url.origin !== location.origin) return;
  e.respondWith(
    fetch(e.request)
      .then(res => {
        const copy = res.clone();
        caches.open(CACHE).then(c => c.put(e.request, copy)).catch(()=>{});
        return res;
      })
      .catch(() => caches.match(e.request).then(m => m || caches.match('./')))
  );
});
