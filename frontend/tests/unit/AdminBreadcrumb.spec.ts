import { describe, expect, it } from 'vitest'
import { mountSuspended } from '@nuxt/test-utils/runtime'
import AdminBreadcrumb from '~/components/AdminBreadcrumb.vue'

describe('AdminBreadcrumb', () => {
  it('gives one way back up, not two', async () => {
    // The bug this replaces: detail pages carried the switcher's "← Backoffice"
    // and their own "← Back to list", two arrows pointing at different places.
    const wrapper = await mountSuspended(AdminBreadcrumb, {
      props: {
        sectionKey: 'admin.sections.lexicon.label',
        sectionTo: '/admin/lexicon',
        current: 'ܐܠܗܐ',
        currentIsSyriac: true,
      },
    })

    const links = wrapper.findAll('a')
    expect(links).toHaveLength(2)
    expect(links[0]!.attributes('href')).toBe('/admin')
    expect(links[1]!.attributes('href')).toBe('/admin/lexicon')
  })

  it('leaves the current item as text, since you are already on it', async () => {
    const wrapper = await mountSuspended(AdminBreadcrumb, {
      props: {
        sectionKey: 'admin.sections.lexicon.label',
        sectionTo: '/admin/lexicon',
        current: 'ܐܠܗܐ',
        currentIsSyriac: true,
      },
    })

    expect(wrapper.text()).toContain('ܐܠܗܐ')
    expect(wrapper.find('[aria-current="page"]').exists()).toBe(true)
    expect(wrapper.find('a[aria-current="page"]').exists()).toBe(false)
  })

  it('ends the trail at the section on a section index', async () => {
    const wrapper = await mountSuspended(AdminBreadcrumb, {
      props: { sectionKey: 'admin.sections.people.label', sectionTo: '/admin/people' },
    })

    expect(wrapper.findAll('a')).toHaveLength(2)
    expect(wrapper.find('a[aria-current="page"]').attributes('href')).toBe('/admin/people')
  })

  it('renders a Syriac leaf right-to-left', async () => {
    // A Syriac word set left-to-right beside Latin breadcrumbs renders in the
    // wrong order; it needs its own isolated element.
    const wrapper = await mountSuspended(AdminBreadcrumb, {
      props: {
        sectionKey: 'admin.sections.lexicon.label',
        sectionTo: '/admin/lexicon',
        current: 'ܐܠܗܐ',
        currentIsSyriac: true,
      },
    })

    expect(wrapper.find('[dir="rtl"]').exists()).toBe(true)
  })

  it('leaves a Latin leaf alone', async () => {
    const wrapper = await mountSuspended(AdminBreadcrumb, {
      props: {
        sectionKey: 'admin.sections.figures.label',
        sectionTo: '/admin/historical-figures',
        current: 'Jacob of Serugh',
      },
    })

    expect(wrapper.text()).toContain('Jacob of Serugh')
    expect(wrapper.find('[dir="rtl"]').exists()).toBe(false)
  })
})
