// Deployed build identity. The prod image bakes the commit SHA in as
// NUXT_PUBLIC_BUILD_SHA (see Dockerfile); CD's post-deploy step asserts this
// endpoint carries the commit it just shipped, so a stale container can never
// pass a deploy silently.
export default defineEventHandler(() => {
  const sha = useRuntimeConfig().public.buildSha || 'unknown'
  return { sha }
})
