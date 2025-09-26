<script>
import { GameService } from '@/services/gameService.js';

export default {
    name: 'ConsistencyChampionsWidget',
    props: {
        days: {
            type: Number,
            default: 30
        },
        minScores: {
            type: Number,
            default: 5
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
        minScores() {
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
                this.data = await this.gameService.getConsistencyChampions(this.days, this.minScores);
            } catch (error) {
                console.error('Error loading consistency champions:', error);
                this.error = 'Failed to load consistency champions data';
            } finally {
                this.loading = false;
            }
        },
        formatScore(score, scoringType) {
            if (scoringType === 2) {
                // Time
                return `${score.toFixed(1)}s`;
            } else {
                // Guesses
                const guesses = Math.round(score);
                return `${guesses} guess${guesses !== 1 ? 'es' : ''}`;
            }
        }
    }
};
</script>

<template>
    <Card class="analytics-card">
        <template #title>
            <div class="flex items-center gap-2">
                <i class="pi pi-shield text-blue-500"></i>
                Consistency Champions
            </div>
        </template>
        <template #content>
            <div v-if="loading" class="text-center py-4">
                <ProgressSpinner style="width: 40px; height: 40px" strokeWidth="4" />
                <p class="text-gray-500 mt-2">Calculating consistency scores...</p>
            </div>
            <div v-else-if="error" class="text-red-600 text-center py-4">
                {{ error }}
            </div>
            <div v-else-if="data">
                <div class="text-center mb-4">
                    <div class="text-sm text-gray-600">Most consistent players ({{ data.daysAnalyzed }} days, min {{ data.minimumScores }} scores)</div>
                </div>

                <div v-if="data.games && data.games.length > 0" class="space-y-4">
                    <div v-for="game in data.games" :key="game.gameId" class="border rounded-lg p-3">
                        <h4 class="font-semibold text-gray-800 mb-3">{{ game.gameName }}</h4>

                        <div v-if="game.topPlayers && game.topPlayers.length > 0" class="space-y-2">
                            <div v-for="(player, index) in game.topPlayers.slice(0, 5)" :key="player.playerName" class="flex items-center justify-between bg-blue-50 rounded p-2">
                                <div class="flex items-center gap-3">
                                    <div class="flex-shrink-0">
                                        <Badge :value="index + 1" :severity="index === 0 ? 'info' : 'secondary'" />
                                    </div>
                                    <div>
                                        <div class="font-medium">{{ player.playerName }}</div>
                                        <div class="text-xs text-gray-600">{{ player.scoreCount }} scores | Best: {{ formatScore(player.bestScore, game.scoringType) }}</div>
                                    </div>
                                </div>
                                <div class="text-right">
                                    <div class="text-blue-600 font-bold">{{ player.coefficientOfVariation.toFixed(1) }}%</div>
                                    <div class="text-xs text-gray-500">variation</div>
                                </div>
                            </div>
                        </div>

                        <div v-else class="text-gray-500 text-center py-4">
                            <i class="pi pi-info-circle mb-2"></i>
                            <div>Not enough data for consistency analysis</div>
                            <div class="text-xs">Players need at least {{ data.minimumScores }} scores</div>
                        </div>
                    </div>
                </div>

                <div v-else class="text-gray-500 text-center py-4">No consistency data available</div>

                <div class="mt-4 p-2 bg-blue-50 rounded text-xs text-gray-600">
                    <i class="pi pi-info-circle mr-1"></i>
                    Lower variation % = more consistent performance
                </div>
            </div>
        </template>
    </Card>
</template>

<style scoped>
.analytics-card {
    @apply bg-gradient-to-br from-blue-50 to-indigo-50 border-blue-200;
}
</style>
