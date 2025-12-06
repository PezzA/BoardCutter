import { mount } from 'svelte';
import Twenty48 from '../Twenty48/Twenty48.svelte';

// Mount the Twenty48 game component
const app = mount(Twenty48, {
  target: document.getElementById('app')!
});

export default app;
