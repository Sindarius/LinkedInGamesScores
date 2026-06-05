<script setup>
import { useLayout } from '@/layout/composables/layout';
import AppConfigurator from './AppConfigurator.vue';

const { toggleMenu, toggleDarkMode, isDarkTheme } = useLayout();
</script>

<template>
    <div class="layout-topbar">
        <div class="layout-topbar-logo-container">
            <button class="layout-menu-button layout-topbar-action" @click="toggleMenu">
                <i class="pi pi-bars"></i>
            </button>
            <router-link to="/" class="layout-topbar-logo">
                <svg viewBox="0 0 44 38" fill="none" xmlns="http://www.w3.org/2000/svg" style="width:36px;height:36px">
                    <!-- 2nd place bar (silver) -->
                    <rect x="4"  y="20" width="9" height="16" rx="1.5" fill="#94a3b8"/>
                    <!-- 1st place bar (gold) -->
                    <rect x="17" y="14" width="10" height="22" rx="1.5" fill="#f59e0b"/>
                    <!-- 3rd place bar (bronze) -->
                    <rect x="31" y="23" width="9" height="13" rx="1.5" fill="#a16207"/>
                    <!-- Base bar -->
                    <rect x="2" y="36" width="40" height="2.5" rx="1.25" fill="#6366f1"/>
                    <!-- Crown outline -->
                    <path d="M2 35 L2 11 L10 18 L22 3 L34 18 L42 11 L42 35"
                          stroke="#6366f1" stroke-width="2.6"
                          stroke-linejoin="round" stroke-linecap="round" fill="none"/>
                    <!-- Crown tip jewels -->
                    <circle cx="2"  cy="11" r="2.6" fill="#818cf8"/>
                    <circle cx="22" cy="3"  r="2.6" fill="#818cf8"/>
                    <circle cx="42" cy="11" r="2.6" fill="#818cf8"/>
                </svg>

                <span>LinkedIn Game Scores</span>
            </router-link>
        </div>

        <div class="layout-topbar-actions">
            <div class="layout-config-menu">
                <button type="button" class="layout-topbar-action" @click="toggleDarkMode">
                    <i :class="['pi', { 'pi-moon': isDarkTheme, 'pi-sun': !isDarkTheme }]"></i>
                </button>
                <div class="relative">
                    <button
                        v-styleclass="{ selector: '@next', enterFromClass: 'hidden', enterActiveClass: 'animate-scalein', leaveToClass: 'hidden', leaveActiveClass: 'animate-fadeout', hideOnOutsideClick: true }"
                        type="button"
                        class="layout-topbar-action layout-topbar-action-highlight"
                    >
                        <i class="pi pi-palette"></i>
                    </button>
                    <AppConfigurator />
                </div>
            </div>

            <button
                class="layout-topbar-menu-button layout-topbar-action"
                v-styleclass="{ selector: '@next', enterFromClass: 'hidden', enterActiveClass: 'animate-scalein', leaveToClass: 'hidden', leaveActiveClass: 'animate-fadeout', hideOnOutsideClick: true }"
            >
                <i class="pi pi-ellipsis-v"></i>
            </button>

            <div class="layout-topbar-menu hidden lg:block">
                <div class="layout-topbar-menu-content">
                    <button type="button" class="layout-topbar-action">
                        <i class="pi pi-trophy"></i>
                        <span>Leaderboard</span>
                    </button>
                    <button type="button" class="layout-topbar-action">
                        <i class="pi pi-chart-line"></i>
                        <span>Statistics</span>
                    </button>
                </div>
            </div>
        </div>
    </div>
</template>
