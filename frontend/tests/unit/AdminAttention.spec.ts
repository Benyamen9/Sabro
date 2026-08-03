import { describe, expect, it } from 'vitest'
import { mountSuspended } from '@nuxt/test-utils/runtime'
import AdminAttention from '~/components/AdminAttention.vue'

describe('AdminAttention', () => {
  it('shows what is waiting and offers a way to it', async () => {
    const wrapper = await mountSuspended(AdminAttention, {
      props: { pendingProposals: 3, playableWords: 42, poolTarget: 150 },
    })

    expect(wrapper.text()).toContain('3')
    expect(wrapper.text()).toContain('waiting on your decision')
    expect(wrapper.find('a[href="/admin/proposals"]').exists()).toBe(true)
  })

  it('stays calm when the queue is empty', async () => {
    // An empty queue is good news. Offering "review them" for nothing to review
    // is how a panel teaches people to ignore it.
    const wrapper = await mountSuspended(AdminAttention, {
      props: { pendingProposals: 0, playableWords: 42, poolTarget: 150 },
    })

    expect(wrapper.find('a[href="/admin/proposals"]').exists()).toBe(false)
  })

  it('gives the pool number something to be measured against', async () => {
    const wrapper = await mountSuspended(AdminAttention, {
      props: { pendingProposals: 0, playableWords: 42, poolTarget: 150 },
    })

    expect(wrapper.text()).toContain('42')
    expect(wrapper.text()).toContain('150')
  })

  it('hides a count it could not read rather than showing a zero', async () => {
    // The proposals count is Owner-only and the word count comes through
    // Meilisearch: null means "not known", and rendering that as 0 would be a
    // lie about an empty queue.
    const wrapper = await mountSuspended(AdminAttention, {
      props: { pendingProposals: null, playableWords: 42, poolTarget: 150 },
    })

    expect(wrapper.text()).not.toContain('waiting on your decision')
    expect(wrapper.text()).toContain('42')
  })

  it('renders nothing at all when neither count is available', async () => {
    const wrapper = await mountSuspended(AdminAttention, {
      props: { pendingProposals: null, playableWords: null, poolTarget: 150 },
    })

    expect(wrapper.find('section').exists()).toBe(false)
  })

  it('does not overflow the bar once the target is passed', async () => {
    const wrapper = await mountSuspended(AdminAttention, {
      props: { pendingProposals: 0, playableWords: 300, poolTarget: 150 },
    })

    const bar = wrapper.find('[role="img"] > div')
    expect(bar.attributes('style')).toContain('width: 100%')
  })
})
