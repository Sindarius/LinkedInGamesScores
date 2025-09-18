<script>
import ScoreSubmissionForm from '@/components/ScoreSubmissionForm.vue';
import GameTabs from '@/components/GameTabs.vue';
import WinsTrendChart from '@/components/WinsTrendChart.vue';
import { GameService } from '@/services/gameService.js';
import { useDateStore } from '@/stores/dateStore.js';

export default {
    name: 'GameScores',
    components: {
        ScoreSubmissionForm,
        GameTabs
    },
    setup() {
        const { selectedDate, setSelectedDate, setToday, formatSelectedDate } = useDateStore();
        return {
            selectedDate,
            setSelectedDate,
            setToday,
            formatSelectedDate
        };
    },
    data() {
        return {
            refreshTrigger: 0,
            totalScores: 0,
            activePlayers: 0,
            gamesCount: 0,
            gameService: new GameService(),
            isRefreshing: false
        };
    },
    async mounted() {
        await this.loadStats();
    },
    methods: {
        onScoreSubmitted() {
            this.refreshTrigger++;
            this.loadStats();
        },
        onDateChange() {
            // Trigger refresh when date changes
            this.refreshTrigger++;
        },
        async loadStats() {
            try {
                // Load games count
                const games = await this.gameService.getGames();
                this.gamesCount = games.length;

                // Load total scores and active players across all games
                let allScores = [];
                for (const game of games) {
                    const gameScores = await this.gameService.getGameScores(game.id);
                    allScores = [...allScores, ...gameScores];
                }

                this.totalScores = allScores.length;
                this.activePlayers = new Set(allScores.map((score) => score.playerName)).size;
            } catch (error) {
                console.error('Error loading stats:', error);
            }
        },
        async onFullRefresh() {
            this.isRefreshing = true;
            try {
                // Trigger refresh for all components
                this.refreshTrigger++;

                // Reload stats
                await this.loadStats();

                // Show success message
                this.$toast.add({
                    severity: 'success',
                    summary: 'Refreshed',
                    detail: 'All game data and times have been updated',
                    life: 3000
                });
            } catch (error) {
                console.error('Error during full refresh:', error);
                this.$toast.add({
                    severity: 'error',
                    summary: 'Refresh Failed',
                    detail: 'Failed to refresh game data',
                    life: 5000
                });
            } finally {
                this.isRefreshing = false;
            }
        }
    }
};
</script>

<template>
    <div class="game-scores-page">
        <div class="mb-6">
            <!-- Header -->
            <div class="flex flex-col md:flex-row md:items-center md:justify-between gap-4 mb-6">
                <div>
                    <h1 class="text-3xl font-bold text-gray-900 mb-2">LinkedIn Game Scores</h1>
                    <p class="text-gray-600">Track your daily scores and compete with others!</p>
                </div>
            </div>

            <!-- Centralized Date Selection -->
            <Card class="mb-6">
                <template #content>
                    <div class="flex flex-col md:flex-row md:items-center md:justify-center gap-4">
                        <div class="flex items-center gap-3">
                            <i class="pi pi-calendar text-blue-600 text-xl"></i>
                            <span class="text-lg font-medium text-gray-700">Select Date:</span>
                            <Calendar v-model="selectedDate" dateFormat="mm/dd/yy" :showIcon="true" placeholder="Select date" @date-select="onDateChange" class="w-48" />
                            <Button
                                label="Today"
                                @click="
                                    setToday();
                                    onDateChange();
                                "
                                severity="secondary"
                                class="px-4 py-2"
                            />
                            <Button label="Refresh" icon="pi pi-refresh" @click="onFullRefresh" severity="info" class="px-4 py-2" :loading="isRefreshing" title="Refresh all game data and times" />
                        </div>
                    </div>
                    <div class="text-center text-lg font-medium text-blue-600 mt-4">📅 Viewing scores for {{ formatSelectedDate() }}</div>
                </template>
            </Card>
        </div>

        <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
            <!-- Score Submission Form -->
            <div class="lg:col-span-1">
                <ScoreSubmissionForm @scoreSubmitted="onScoreSubmitted" />
            </div>

            <!-- Leaderboards -->
            <div class="lg:col-span-2">
                <GameTabs :refreshTrigger="refreshTrigger" />
            </div>
        </div>

        <div class="mt-8">
            <Card>
                <template #title>Quick Stats</template>
                <template #content>
                    <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
                        <div class="text-center p-4 bg-blue-50 rounded-lg">
                            <div class="text-2xl font-bold text-blue-600">{{ totalScores }}</div>
                            <div class="text-sm text-gray-600">Total Scores</div>
                        </div>
                        <div class="text-center p-4 bg-green-50 rounded-lg">
                            <div class="text-2xl font-bold text-green-600">{{ activePlayers }}</div>
                            <div class="text-sm text-gray-600">Active Players</div>
                        </div>
                        <div class="text-center p-4 bg-purple-50 rounded-lg">
                            <div class="text-2xl font-bold text-purple-600">{{ gamesCount }}</div>
                            <div class="text-sm text-gray-600">Games Available</div>
                        </div>
                    </div>
                </template>
            </Card>
        </div>

        <div class="mt-8">
            <WinsTrendChart :days="7" :top="5" :refreshKey="refreshTrigger" />
        </div>
    </div>
</template>

<style scoped>
.game-scores-page {
    @apply p-6 max-w-7xl mx-auto;
}
</style>
