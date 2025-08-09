<script>
import { ref, onMounted, computed, watchEffect } from 'vue';
import { useToast } from 'primevue/usetoast';
import { GameService } from '@/services/gameService.js';
import { useDateStore } from '@/stores/dateStore.js';

export default {
    name: 'AllGamesChampions',
    props: {
        refreshTrigger: {
            type: Number,
            default: 0
        }
    },
    setup(props) {
        const toast = useToast();
        const gameService = new GameService();
        const champions = ref([]);
        const isLoading = ref(false);
        const { selectedDate } = useDateStore();

        const sortedChampions = computed(() => {
            return [...champions.value].sort((a, b) => a.gameName.localeCompare(b.gameName));
        });

        const loadChampions = async () => {
            isLoading.value = true;

            try {
                const games = await gameService.getGames();
                const championData = [];

                for (const game of games) {
                    const leaderboard = await gameService.getLeaderboard(game.id, selectedDate.value, 10);
                    if (leaderboard.length > 0) {
                        // Find all players tied for first place
                        const firstPlaceScore = leaderboard[0].score;
                        const firstPlaceGuesses = leaderboard[0].guessCount;
                        const firstPlaceTime = leaderboard[0].completionTime;

                        const tiedChampions = leaderboard.filter((player) => {
                            if (game.scoringType === 1) {
                                // Guess-based: tied if same guess count
                                return player.guessCount === firstPlaceGuesses;
                            } else if (game.scoringType === 2) {
                                // Time-based: tied if same completion time
                                return player.completionTime === firstPlaceTime;
                            } else {
                                // Score-based: tied if same score
                                return player.score === firstPlaceScore;
                            }
                        });

                        championData.push({
                            gameId: game.id,
                            gameName: game.name,
                            scoringType: game.scoringType,
                            champions: tiedChampions,
                            score: firstPlaceScore,
                            guessCount: firstPlaceGuesses,
                            completionTime: firstPlaceTime,
                            dateAchieved: tiedChampions[0].dateAchieved
                        });
                    } else {
                        // No champion for this date
                        championData.push({
                            gameId: game.id,
                            gameName: game.name,
                            scoringType: game.scoringType,
                            champions: [],
                            score: null,
                            guessCount: null,
                            completionTime: null,
                            dateAchieved: null
                        });
                    }
                }

                champions.value = championData;
            } catch (error) {
                console.error('Error loading champions:', error);
                toast.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: 'Failed to load game champions'
                });
            } finally {
                isLoading.value = false;
            }
        };

        const formatDate = (date) => {
            return new Date(date).toLocaleDateString('en-US', {
                weekday: 'long',
                year: 'numeric',
                month: 'long',
                day: 'numeric'
            });
        };

        const formatTime = (dateString) => {
            return new Date(dateString).toLocaleTimeString('en-US', {
                hour: '2-digit',
                minute: '2-digit'
            });
        };

        const formatCompletionTime = (timeSpan) => {
            if (!timeSpan) return 'N/A';

            const parts = timeSpan.split(':');
            const hours = parseInt(parts[0] || 0);
            const minutes = parseInt(parts[1] || 0);
            const seconds = parseInt(parts[2] || 0);

            if (hours > 0) {
                return `${hours}h ${minutes}m ${seconds}s`;
            } else if (minutes > 0) {
                return `${minutes}m ${seconds}s`;
            } else {
                return `${seconds}s`;
            }
        };

        const scrollToGameSection = (gameId) => {
            const element = document.getElementById(`game-${gameId}`);
            if (element) {
                element.scrollIntoView({
                    behavior: 'smooth',
                    block: 'start'
                });
                // Add a subtle highlight effect
                element.classList.add('highlight-game');
                setTimeout(() => {
                    element.classList.remove('highlight-game');
                }, 2000);
            }
        };

        onMounted(async () => {
            await loadChampions();
        });

        // Watch for refresh trigger changes and selected date changes
        watchEffect(() => {
            if (props.refreshTrigger > 0) {
                loadChampions();
            }
        });

        watchEffect(() => {
            if (selectedDate.value) {
                loadChampions();
            }
        });

        return {
            champions,
            sortedChampions,
            selectedDate,
            isLoading,
            loadChampions,
            formatDate,
            formatTime,
            formatCompletionTime,
            scrollToGameSection
        };
    }
};
</script>

<template>
    <div class="champions-display">
        <div class="flex justify-between items-center mb-4">
            <h3 class="text-lg font-semibold">Daily Champions</h3>
            <div class="flex items-center space-x-2">
                <Button icon="pi pi-refresh" @click="loadChampions" :loading="isLoading" size="small" />
            </div>
        </div>

        <div class="mb-4 text-sm text-gray-600">Showing first place players for {{ formatDate(selectedDate || new Date()) }}</div>

        <div v-if="isLoading" class="text-center py-4">
            <ProgressSpinner size="40" />
        </div>

        <div v-else class="space-y-4">
            <div
                v-for="champion in sortedChampions"
                :key="champion.gameId"
                class="champion-card p-4 border border-gray-200 rounded-lg bg-gradient-to-r from-yellow-50 to-orange-50 hover:shadow-md transition-shadow cursor-pointer"
                @click="scrollToGameSection(champion.gameId)"
            >
                <div class="flex items-center justify-between">
                    <div class="flex items-center space-x-3">
                        <div class="flex-shrink-0">
                            <i class="pi pi-crown text-yellow-600 text-2xl"></i>
                        </div>
                        <div>
                            <h4 class="font-bold text-lg text-gray-900">{{ champion.gameName }}</h4>
                            <div v-if="champion.champions && champion.champions.length > 0" class="space-y-2">
                                <!-- Single Champion -->
                                <div v-if="champion.champions.length === 1" class="flex items-center space-x-2">
                                    <Avatar :label="champion.champions[0].playerName.charAt(0).toUpperCase()" size="small" />
                                    <div>
                                        <span class="font-medium text-gray-900">{{ champion.champions[0].playerName }}</span>
                                        <div v-if="champion.champions[0].linkedInProfileUrl" class="text-xs text-blue-600">
                                            <a :href="champion.champions[0].linkedInProfileUrl" target="_blank" class="hover:underline"> LinkedIn Profile </a>
                                        </div>
                                    </div>
                                </div>

                                <!-- Multiple Tied Champions -->
                                <div v-else class="space-y-2">
                                    <div class="text-sm font-medium text-gray-700">{{ champion.champions.length }} players tied for 1st:</div>
                                    <div class="flex flex-wrap gap-2">
                                        <div v-for="(player, index) in champion.champions" :key="player.id" class="flex items-center space-x-1 bg-white bg-opacity-70 rounded-full px-2 py-1">
                                            <Avatar :label="player.playerName.charAt(0).toUpperCase()" size="small" />
                                            <span class="text-sm font-medium text-gray-900">{{ player.playerName }}</span>
                                            <span v-if="index < champion.champions.length - 1" class="text-gray-400">•</span>
                                        </div>
                                    </div>
                                </div>
                            </div>
                            <div v-else class="text-gray-500 italic">No scores recorded for this date</div>
                        </div>
                    </div>

                    <div v-if="champion.champions && champion.champions.length > 0" class="text-right">
                        <div class="text-2xl font-bold text-yellow-600">
                            <span v-if="champion.scoringType === 1">
                                <span v-if="champion.guessCount === 99" class="text-red-600">DNF</span>
                                <span v-else>{{ champion.guessCount }}</span>
                            </span>
                            <span v-else-if="champion.scoringType === 2">{{ formatCompletionTime(champion.completionTime) }}</span>
                            <span v-else>{{ champion.score?.toLocaleString() || 'N/A' }}</span>
                        </div>
                        <div class="text-xs text-gray-600">
                            <span v-if="champion.scoringType === 1">
                                <span v-if="champion.guessCount === 99">did not finish</span>
                                <span v-else>guesses</span>
                            </span>
                            <span v-else-if="champion.scoringType === 2">completion time</span>
                            <span v-else>score</span>
                        </div>
                        <div class="text-xs text-gray-500 mt-1">
                            {{ formatTime(champion.dateAchieved) }}
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
</template>

<style scoped>
.champion-card {
    @apply transition-transform duration-200;
}

.champion-card:hover {
    @apply transform scale-[1.01];
}

.pi-crown {
    filter: drop-shadow(0 2px 4px rgba(0, 0, 0, 0.1));
}
</style>
