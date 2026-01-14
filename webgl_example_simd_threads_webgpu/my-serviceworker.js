// my-serviceworker.js
self.addEventListener('fetch', e => {
  e.respondWith(
    fetch(e.request, { cache: 'no-store' }).then(r => {
      if (!r.ok || !r.body) return r; // null body 対応

      const headers = new Headers(r.headers);
      headers.set('Cross-Origin-Opener-Policy', 'same-origin');
      headers.set('Cross-Origin-Embedder-Policy', 'require-corp');

      return new Response(r.body, {
        status: r.status,
        statusText: r.statusText,
        headers
      });
    }).catch(err => {
      console.error('Fetch failed in SW:', err);
      return fetch(e.request);
    })
  );
});
