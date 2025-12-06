import { mount } from 'svelte';
import GameList from '../Shared/GameList.svelte';

// Mount the GameList component for the stratagems page
// TODO: Create a dedicated Stratagems component when ready
const app = mount(GameList, {
  target: document.getElementById('app')!
});

export default app;
