<script setup>
import { ref, computed } from 'vue';
import { AdminService } from '@/services/adminService.js';

import AppMenuItem from './AppMenuItem.vue';

const adminService = new AdminService();

const model = computed(() => {
    const menuItems = [
        { label: 'Game Scores', icon: 'pi pi-fw pi-trophy', to: '/' },
        { label: 'Admin Panel', icon: 'pi pi-fw pi-cog', to: '/admin' }
    ];

    // Only show OCR Training if authenticated
    if (adminService.isAuthenticated()) {
        menuItems.push({ label: 'OCR Training', icon: 'pi pi-fw pi-eye', to: '/ocr-training' });
    }

    return [
        {
            label: 'LinkedIn Games',
            items: menuItems
        }
    ];
});
</script>

<template>
    <ul class="layout-menu">
        <template v-for="(item, i) in model" :key="item">
            <app-menu-item v-if="!item.separator" :item="item" :index="i"></app-menu-item>
            <li v-if="item.separator" class="menu-separator"></li>
        </template>
    </ul>
</template>

<style lang="scss" scoped></style>
