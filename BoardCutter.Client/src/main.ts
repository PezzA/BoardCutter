import { mount } from 'svelte';
import Twenty48 from './Twenty48.svelte';
import GameList from './GameList.svelte';

// Function to mount the appropriate Svelte component based on the page
function mountSvelteApp() {
  const appElement = document.getElementById('app');
  
  if (!appElement) {
    console.error('Could not find #app element to mount Svelte app');
    return;
  }

  // Determine which component to mount based on the page URL or data attribute
  const currentPath = window.location.pathname.toLowerCase();
  
  if (currentPath.includes('/twenty48')) {
    // Mount the Twenty48 game component using Svelte 5 API
    mount(Twenty48, {
      target: appElement
    });
  } else {
    // Default to GameList for other pages
    mount(GameList, {
      target: appElement
    });
  }
}

// Mount the app when the DOM is ready
if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', mountSvelteApp);
} else {
  mountSvelteApp();
}
