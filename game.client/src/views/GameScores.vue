<script>
import ScoreSubmissionForm from '@/components/ScoreSubmissionForm.vue';
import GameTabs from '@/components/GameTabs.vue';
import { GameService } from '@/services/gameService.js';

export default {
    name: 'GameScores',
    components: {
        ScoreSubmissionForm,
        GameTabs
    },
    data() {
        return {
            refreshTrigger: 0,
            totalScores: 0,
            activePlayers: 0,
            gamesCount: 0,
            currentGameId: null,
            gameService: new GameService()
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
        onTabChanged(event) {
            this.currentGameId = event.gameId;
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
        }
    }
};
</script>

<template>
    <div class="game-scores-page">
        <div class="mb-6">
            <h1 class="text-3xl font-bold text-gray-900 mb-2">LinkedIn Game Scores</h1>
            <p class="text-gray-600">Track your daily scores and compete with others!</p>
        </div>

        <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
            <!-- Score Submission Form -->
            <div class="lg:col-span-1">
                <ScoreSubmissionForm @scoreSubmitted="onScoreSubmitted" />
            </div>

            <!-- Leaderboards -->
            <div class="lg:col-span-2">
                <GameTabs :refreshTrigger="refreshTrigger" @tab-changed="onTabChanged" />
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
    </div>
</template>

<style scoped>
.game-scores-page {
    @apply p-6 max-w-7xl mx-auto;
}
</style>
