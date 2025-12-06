import { mount } from 'svelte';
import GameList from '../Shared/GameList.svelte';

// Mount the GameList component for the home page
const app = mount(GameList, {
  target: document.getElementById('app')!
});

export default app;
