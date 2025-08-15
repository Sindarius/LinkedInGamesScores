<script setup>
import { ref } from 'vue';

import AppMenuItem from './AppMenuItem.vue';

const model = ref([
    {
        label: 'LinkedIn Games',
        items: [
            { label: 'Game Scores', icon: 'pi pi-fw pi-trophy', to: '/' },
            { label: 'Admin Panel', icon: 'pi pi-fw pi-cog', to: '/admin' },
            { label: 'OCR Training', icon: 'pi pi-fw pi-eye', to: '/ocr-training' }
        ]
    }
]);
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
