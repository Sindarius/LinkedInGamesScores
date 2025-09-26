<script setup>
import { onMounted, ref, watch } from 'vue';
import { StatsService } from '@/services/statsService.js';

const props = defineProps({
    days: { type: Number, default: 7 },
    top: { type: Number, default: 5 },
    gameId: { type: Number, default: null },
    refreshKey: { type: Number, default: 0 }
});

const statsService = new StatsService();
const loading = ref(false);
const chartData = ref({ labels: [], datasets: [] });
const chartOptions = ref({
    responsive: true,
    maintainAspectRatio: false,
    plugins: { legend: { position: 'bottom' } },
    scales: { y: { beginAtZero: true, precision: 0, ticks: { stepSize: 1 } } }
});

const colorPalette = ['#3b82f6', '#22c55e', '#ef4444', '#a855f7', '#f59e0b', '#06b6d4', '#10b981', '#e11d48'];

async function load() {
    loading.value = true;
    try {
        const data = await statsService.getTopWinnersTrend({ days: props.days, top: props.top, gameId: props.gameId });
        chartData.value.labels = data.labels;
        chartData.value.datasets = data.series.map((s, idx) => ({
            label: s.playerName || s.playerId,
            data: s.data,
            fill: false,
            borderColor: colorPalette[idx % colorPalette.length],
            backgroundColor: colorPalette[idx % colorPalette.length],
            tension: 0.25
        }));
    } catch (e) {
        console.error('Failed to load wins trend', e);
    } finally {
        loading.value = false;
    }
}

onMounted(load);
watch(() => [props.days, props.top, props.gameId, props.refreshKey], load);
</script>

<template>
    <Card>
        <template #title>Winners Trend (Last {{ days }} Days)</template>
        <template #content>
            <div class="h-72">
                <Chart type="line" :data="chartData" :options="chartOptions" />
            </div>
            <div v-if="loading" class="text-sm text-gray-500 mt-2">Loading…</div>
            <div class="text-xs text-gray-500 mt-2">Counts represent daily wins across games for each player.</div>
        </template>
    </Card>
</template>
