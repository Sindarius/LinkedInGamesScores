import AppLayout from '@/layout/AppLayout.vue';
import { createRouter, createWebHistory } from 'vue-router';

const router = createRouter({
    history: createWebHistory(),
    routes: [
        {
            path: '/',
            component: AppLayout,
            children: [
                {
                    path: '/',
                    name: 'gamescores',
                    component: () => import('@/views/GameScores.vue')
                },
                {
                    path: '/admin',
                    name: 'admin',
                    component: () => import('@/views/Admin.vue')
                },
                {
                    path: '/quick-submit',
                    name: 'quicksubmit',
                    component: () => import('@/views/QuickSubmit.vue')
                },
                {
                    path: '/ocr-training',
                    name: 'ocrtraining',
                    component: () => import('@/views/OCRTraining.vue')
                },
                {
                    path: '/player/:name',
                    name: 'playerstats',
                    component: () => import('@/views/PlayerStats.vue')
                }
            ]
        },
        {
            path: '/landing',
            name: 'landing',
            component: () => import('@/views/pages/Landing.vue')
        },
        {
            path: '/pages/notfound',
            name: 'notfound',
            component: () => import('@/views/pages/NotFound.vue')
        },

        {
            path: '/auth/login',
            name: 'login',
            component: () => import('@/views/pages/auth/Login.vue')
        },
        {
            path: '/auth/access',
            name: 'accessDenied',
            component: () => import('@/views/pages/auth/Access.vue')
        },
        {
            path: '/auth/error',
            name: 'error',
            component: () => import('@/views/pages/auth/Error.vue')
        }
    ]
});

export default router;
