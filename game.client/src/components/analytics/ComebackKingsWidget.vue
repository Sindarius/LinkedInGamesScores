<script>
import { GameService } from '@/services/gameService.js';

export default {
    name: 'ComebackKingsWidget',
    props: {
        days: {
            type: Number,
            default: 14
        },
        refreshTrigger: {
            type: Number,
            default: 0
        }
    },
    data() {
        return {
            data: null,
            loading: false,
            error: null,
            gameService: new GameService()
        };
    },
    watch: {
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
                this.data = await this.gameService.getComebackKings(this.days);
            } catch (error) {
                console.error('Error loading comeback kings:', error);
                this.error = 'Failed to load comeback kings data';
            } finally {
                this.loading = false;
            }
        },
        formatImprovement(improvement, scoringType) {
            if (scoringType === 2) {
                // Time
                return improvement > 0 ? `-${improvement.toFixed(1)}s` : `+${Math.abs(improvement).toFixed(1)}s`;
            } else {
                // Guesses
                return improvement > 0 ? `-${improvement.toFixed(1)}` : `+${Math.abs(improvement).toFixed(1)}`;
            }
        }
    }
};
</script>

<template>
    <Card class="analytics-card">
        <template #title>
            <div class="flex items-center gap-2">
                <i class="pi pi-chart-line text-green-500"></i>
                Comeback Kings
            </div>
        </template>
        <template #content>
            <div v-if="loading" class="text-center py-4">
                <ProgressSpinner style="width: 40px; height: 40px" strokeWidth="4" />
                <p class="text-gray-500 mt-2">Finding improvement streaks...</p>
            </div>
            <div v-else-if="error" class="text-red-600 text-center py-4">
                {{ error }}
            </div>
            <div v-else-if="data">
                <div class="text-center mb-4">
                    <div class="text-sm text-gray-600">Most improved players ({{ data.daysAnalyzed }} days)</div>
                </div>

                <div v-if="data.games && data.games.length > 0" class="space-y-4">
                    <div v-for="game in data.games" :key="game.gameId" class="border rounded-lg p-3">
                        <h4 class="font-semibold text-gray-800 mb-3">{{ game.gameName }}</h4>

                        <div v-if="game.topPlayers && game.topPlayers.length > 0" class="space-y-2">
                            <div v-for="(player, index) in game.topPlayers" :key="player.playerName" class="flex items-center justify-between bg-green-50 rounded p-2">
                                <div class="flex items-center gap-3">
                                    <div class="flex-shrink-0">
                                        <Badge :value="index + 1" :severity="index === 0 ? 'success' : 'info'" />
                                    </div>
                                    <div>
                                        <div class="font-medium">{{ player.playerName }}</div>
                                        <div class="text-xs text-gray-600">{{ player.totalImprovements }} improvement{{ player.totalImprovements !== 1 ? 's' : '' }} ({{ player.recentScoresCount }} scores)</div>
                                    </div>
                                </div>
                                <div class="text-right">
                                    <div class="text-green-600 font-bold">
                                        {{ formatImprovement(player.averageImprovement, game.scoringType) }}
                                    </div>
                                    <div class="text-xs text-gray-500">avg improvement</div>
                                </div>
                            </div>
                        </div>

                        <div v-else class="text-gray-500 text-center py-4">
                            <i class="pi pi-info-circle mb-2"></i>
                            <div>No improvement patterns found</div>
                            <div class="text-xs">Players need at least 3 scores to qualify</div>
                        </div>
                    </div>
                </div>

                <div v-else class="text-gray-500 text-center py-4">No comeback data available</div>
            </div>
        </template>
    </Card>
</template>

<style scoped>
.analytics-card {
    @apply bg-gradient-to-br from-green-50 to-emerald-50 border-green-200;
}
</style>
