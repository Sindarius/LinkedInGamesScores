<script>
import { GameService } from '@/services/gameService.js';

export default {
    name: 'DistributionChart',
    props: {
        days: {
            type: Number,
            default: 30
        },
        refreshTrigger: {
            type: Number,
            default: 0
        }
    },
    data() {
        return {
            selectedScoringType: 1, // Default to Guesses
            data: null,
            loading: false,
            error: null,
            gameService: new GameService()
        };
    },
    watch: {
        selectedScoringType() {
            this.loadData();
        },
        days() {
            this.loadData();
        },
        refreshTrigger() {
            this.loadData();
        }
    },
    async mounted() {
        await this.loadData();
    },
    methods: {
        async loadData() {
            this.loading = true;
            this.error = null;

            try {
                this.data = await this.gameService.getScoreDistribution(this.selectedScoringType, this.days);
            } catch (error) {
                console.error('Error loading distribution data:', error);
                this.error = 'Failed to load distribution data';
            } finally {
                this.loading = false;
            }
        },
        getScoringTypeLabel(scoringType) {
            return scoringType === 1 ? 'Guess' : scoringType === 2 ? 'Time' : 'Unknown';
        },
        formatRangeLabel(range, scoringType) {
            if (scoringType === 2) {
                // Time
                return range;
            } else {
                // Guesses
                return range === '6+' ? '6+' : `${range} guess${range !== '1' ? 'es' : ''}`;
            }
        },
        getPercentage(count, total) {
            return total > 0 ? (count / total) * 100 : 0;
        },
        getBarColor(range, scoringType) {
            if (scoringType === 1) {
                // Guesses - green gradient (fewer guesses = better = greener)
                const colorMap = {
                    1: 'bg-emerald-500',
                    2: 'bg-green-500',
                    3: 'bg-lime-500',
                    4: 'bg-yellow-500',
                    5: 'bg-orange-500',
                    '6+': 'bg-red-500'
                };
                return colorMap[range] || 'bg-gray-500';
            } else {
                // Time - blue gradient (faster = better = bluer)
                const colorMap = {
                    '0-30s': 'bg-blue-600',
                    '31-60s': 'bg-blue-500',
                    '61-120s': 'bg-cyan-500',
                    '121-300s': 'bg-orange-500',
                    '300s+': 'bg-red-500'
                };
                return colorMap[range] || 'bg-gray-500';
            }
        }
    }
};
</script>

<template>
    <Card class="analytics-card">
        <template #title>
            <div class="flex items-center gap-2">
                <i class="pi pi-chart-bar text-purple-500"></i>
                Score Distribution
            </div>
        </template>
        <template #content>
            <div class="mb-4">
                <div class="flex flex-wrap gap-2">
                    <Button label="Guesses" :severity="selectedScoringType === 1 ? 'primary' : 'secondary'" size="small" @click="selectedScoringType = 1" />
                    <Button label="Time" :severity="selectedScoringType === 2 ? 'primary' : 'secondary'" size="small" @click="selectedScoringType = 2" />
                </div>
            </div>

            <div v-if="loading" class="text-center py-4">
                <ProgressSpinner style="width: 40px; height: 40px" strokeWidth="4" />
                <p class="text-gray-500 mt-2">Building distribution charts...</p>
            </div>
            <div v-else-if="error" class="text-red-600 text-center py-4">
                {{ error }}
            </div>
            <div v-else-if="data">
                <div class="text-center mb-4">
                    <div class="text-sm text-gray-600">{{ getScoringTypeLabel(data.scoringType) }} distribution ({{ data.daysAnalyzed }} days)</div>
                </div>

                <div v-if="data.games && data.games.length > 0" class="space-y-6">
                    <div v-for="game in data.games" :key="game.gameId" class="border rounded-lg p-4">
                        <div class="flex justify-between items-center mb-4">
                            <h4 class="font-semibold text-gray-800">{{ game.gameName }}</h4>
                            <Badge :value="`${game.totalScores} scores`" severity="info" />
                        </div>

                        <div v-if="game.totalScores > 0" class="space-y-3">
                            <div v-for="(count, range) in game.distribution" :key="range" class="flex items-center gap-3">
                                <div class="w-20 text-sm font-medium text-gray-700">
                                    {{ formatRangeLabel(range, data.scoringType) }}
                                </div>
                                <div class="flex-1 relative">
                                    <div class="bg-gray-200 rounded-full h-6 overflow-hidden">
                                        <div class="h-full transition-all duration-500 rounded-full" :class="getBarColor(range, data.scoringType)" :style="{ width: `${getPercentage(count, game.totalScores)}%` }"></div>
                                    </div>
                                    <div class="absolute inset-0 flex items-center justify-center text-xs font-medium text-gray-700">{{ count }} ({{ getPercentage(count, game.totalScores).toFixed(1) }}%)</div>
                                </div>
                            </div>
                        </div>

                        <div v-else class="text-gray-500 text-center py-4">No scores available for this game type</div>
                    </div>
                </div>

                <div v-else class="text-gray-500 text-center py-4">No games found for {{ getScoringTypeLabel(selectedScoringType) }} scoring</div>
            </div>
        </template>
    </Card>
</template>

<style scoped>
.analytics-card {
    @apply bg-gradient-to-br from-purple-50 to-pink-50 border-purple-200;
}
</style>
