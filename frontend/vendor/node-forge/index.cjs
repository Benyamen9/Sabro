'use strict'
// See package.json. Any property read fails loudly, so a code path that
// really needs forge (only `nuxt dev --https`) says why instead of
// misbehaving.
const message = 'node-forge is stubbed out in this repo (no fixed release for '
  + 'GHSA-86w9-cpqp-85rv). `nuxt dev --https` is unavailable until it is '
  + 'restored; see vendor/node-forge/package.json.'

module.exports = new Proxy({}, {
  get(_target, key) {
    // Let module interop probes (`__esModule`, `default`, `then`, symbols)
    // answer "not here" instead of throwing at import time.
    if (typeof key === 'symbol' || key === '__esModule' || key === 'default' || key === 'then') return undefined
    throw new Error(message)
  },
})
