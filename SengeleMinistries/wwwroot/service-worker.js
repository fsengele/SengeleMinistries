
/*
 * Sengele Ministries PWA
 * Service Worker - Version 1
 *
 * No caching of member information,
 * passwords, payments, or private pages.
 */

// Install the service worker
self.addEventListener("install", (event) => {
    console.log("Sengele Ministries Service Worker installed");

    self.skipWaiting();
});

// Activate the service worker
self.addEventListener("activate", (event) => {
    console.log("Sengele Ministries Service Worker activated");

    event.waitUntil(self.clients.claim());
});
