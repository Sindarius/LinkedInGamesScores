<script>
import { GameService } from '@/services/gameService.js';

export default {
    name: 'CloseCallsWidget',
    props: {
        days: {
            type: Number,
            default: 7
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
                this.data = await this.gameService.getCloseCalls(this.days);
            } catch (error) {
                console.error('Error loading close calls:', error);
                this.error = 'Failed to load close calls data';
            } finally {
                this.loading = false;
            }
        },
        formatDate(dateString) {
            const date = new Date(dateString);
            return date.toLocaleDateString('en-US', {
                month: 'short',
                day: 'numeric'
            });
        }
    }
};
</script>

<template>
    <Card class="analytics-card">
        <template #title>
            <div class="flex items-center gap-2">
                <i class="pi pi-bolt text-orange-500"></i>
                Close Calls
            </div>
        </template>
        <template #content>
            <div v-if="loading" class="text-center py-4">
                <ProgressSpinner style="width: 40px; height: 40px" strokeWidth="4" />
                <p class="text-gray-500 mt-2">Analyzing nail-biting finishes...</p>
            </div>
            <div v-else-if="error" class="text-red-600 text-center py-4">
                {{ error }}
            </div>
            <div v-else-if="data">
                <div class="text-center mb-4">
                    <div class="text-3xl font-bold text-orange-600">{{ data.totalCloseCalls }}</div>
                    <div class="text-sm text-gray-600">Close calls in the last {{ data.daysAnalyzed }} days</div>
                </div>

                <div v-if="data.games && data.games.length > 0" class="space-y-4">
                    <div v-for="game in data.games" :key="game.gameId" class="border rounded-lg p-3">
                        <div class="flex justify-between items-center mb-2">
                            <h4 class="font-semibold text-gray-800">{{ game.gameName }}</h4>
                            <Badge :value="game.closeCallCount" severity="warning" />
                        </div>

                        <div v-if="game.examples && game.examples.length > 0" class="space-y-2">
                            <div v-for="example in game.examples" :key="`${example.date}-${example.winner}`" class="bg-orange-50 rounded p-2 text-sm">
                                <div class="flex justify-between items-center">
                                    <div>
                                        <span class="font-medium">{{ example.winner }}</span>
                                        <span class="text-gray-600"> beat </span>
                                        <span class="font-medium">{{ example.runnerUp }}</span>
                                    </div>
                                    <div class="text-orange-600 font-bold">{{ example.margin }}</div>
                                </div>
                                <div class="text-xs text-gray-500 mt-1">{{ example.winnerScore }} vs {{ example.runnerUpScore }} on {{ formatDate(example.date) }}</div>
                            </div>
                        </div>

                        <div v-else-if="game.closeCallCount === 0" class="text-gray-500 text-sm">No close calls yet</div>
                    </div>
                </div>

                <div v-else class="text-gray-500 text-center py-4">No close calls found</div>
            </div>
        </template>
    </Card>
</template>

<style scoped>
.analytics-card {
    @apply bg-gradient-to-br from-orange-50 to-yellow-50 border-orange-200;
}
</style>
