import { createApp } from 'vue';
import { createPinia } from 'pinia';
import App from './App.vue';
import router from './router';

import PrimeVue from 'primevue/config';
import ConfirmationService from 'primevue/confirmationservice';
import ToastService from 'primevue/toastservice';

import { loadThemeFromStorage, presets, getPresetExt, applyDarkModeClass, surfaces } from '@/stores/themeStore.js';

import '@/assets/styles.scss';

// Load theme preferences before PrimeVue initialization
const savedTheme = loadThemeFromStorage();

// Apply dark mode class immediately if needed
applyDarkModeClass(savedTheme.darkTheme);

// Get preset and primary color configuration
const presetValue = presets[savedTheme.preset];
const primaryExtension = getPresetExt(savedTheme.primary);
const surfacePalette = savedTheme.surface ? surfaces.find((s) => s.name === savedTheme.surface)?.palette : null;

const app = createApp(App);
const pinia = createPinia();

app.use(pinia);
app.use(router);

// Initialize PrimeVue with complete theme configuration
app.use(PrimeVue, {
    theme: {
        preset: presetValue,
        options: {
            darkModeSelector: '.app-dark'
        }
    }
});

app.use(ToastService);
app.use(ConfirmationService);

app.mount('#app');

// Apply primary colors and surface palette after mounting
import { $t } from '@primeuix/themes';
const primeuiTheme = $t().preset(presetValue).preset(primaryExtension);

if (surfacePalette) {
    primeuiTheme.surfacePalette(surfacePalette);
}

// Use useDefaultOptions: true to preserve default surface styling for light/dark themes
primeuiTheme.use({ useDefaultOptions: true });

// Initialize reactive theme store for dynamic changes
import { useThemeStore } from '@/stores/themeStore.js';
useThemeStore();
