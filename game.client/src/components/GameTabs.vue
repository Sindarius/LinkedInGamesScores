<script>
import { ref, onMounted, computed } from 'vue';
import { useToast } from 'primevue/usetoast';
import { GameService } from '@/services/gameService.js';
import DailyLeaderboard from './DailyLeaderboard.vue';
import AllGamesChampions from './AllGamesChampions.vue';

export default {
    name: 'GameTabs',
    components: {
        DailyLeaderboard,
        AllGamesChampions
    },
    props: {
        refreshTrigger: {
            type: Number,
            default: 0
        }
    },
    setup() {
        const toast = useToast();
        const gameService = new GameService();
        const games = ref([]);

        // Sort games alphabetically by name
        const sortedGames = computed(() => {
            return [...games.value].sort((a, b) => a.name.localeCompare(b.name));
        });

        const loadGames = async () => {
            try {
                games.value = await gameService.getGames();
            } catch (error) {
                console.error('Error loading games:', error);
                toast.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: 'Failed to load games'
                });
            }
        };

        onMounted(async () => {
            await loadGames();
        });

        return {
            games,
            sortedGames
        };
    }
};
</script>

<template>
    <div class="game-cards">
        <!-- All Games Card -->
        <Card class="mb-6">
            <template #title>
                <div class="flex items-center gap-2">
                    <i class="pi pi-chart-bar text-primary"></i>
                    All Games Champions
                </div>
            </template>
            <template #subtitle>
                <span class="text-sm text-gray-600"> First place players from each game </span>
            </template>
            <template #content>
                <AllGamesChampions :refreshTrigger="refreshTrigger" />
            </template>
        </Card>

        <!-- Individual Game Cards -->
        <div class="grid grid-cols-1 gap-6">
            <Card v-for="game in sortedGames" :key="game.id" class="game-card">
                <template #title>
                    <div class="flex items-center gap-2">
                        <i class="pi pi-trophy text-primary"></i>
                        {{ game.name }}
                    </div>
                </template>
                <template #subtitle>
                    <span class="text-sm text-gray-600">
                        {{ game.scoringType === 1 ? 'Scored by number of guesses (lower is better)' : 'Scored by completion time (faster is better)' }}
                    </span>
                </template>
                <template #content>
                    <DailyLeaderboard :gameId="game.id" :refreshTrigger="refreshTrigger" />
                </template>
            </Card>
        </div>
    </div>
</template>

<style scoped>
.game-cards {
    @apply w-full;
}

.game-card {
    @apply transition-shadow duration-200;
}

.game-card:hover {
    @apply shadow-lg;
}

.text-primary {
    @apply text-blue-600;
}
</style>
