import {defineConfig} from 'vite'
import {svelte} from '@sveltejs/vite-plugin-svelte'

// https://vitejs.dev/config/
export default defineConfig({
    plugins: [svelte()],
    server: {
        port: 5173,
        cors: true,
        host: 'localhost'
    },
    build: {
        outDir: '../BoardCutter.Web/wwwroot/dist',
        emptyOutDir: true,
        assetsDir: 'assets',
        rollupOptions: {
            input: {
                home: './src/entries/home.ts',
                twenty48: './src/entries/twenty48.ts',
                stratagems: './src/entries/stratagems.ts'
            },
            output: {
                entryFileNames: 'assets/[name].js',
                chunkFileNames: 'assets/[name]-[hash].js',  // Add hash for shared chunks
                assetFileNames: 'assets/[name].[ext]'
            }
        }
    }
})
