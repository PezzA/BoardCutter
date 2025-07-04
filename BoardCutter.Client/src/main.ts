import { mount } from 'svelte'
import Twenty48 from './Twenty48.svelte'

const app = mount(Twenty48, {
  target: document.getElementById('app')!,
})

export default app
