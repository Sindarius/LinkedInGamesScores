<script>
import { GameService } from '@/services/gameService.js';
import { useDateStore } from '@/stores/dateStore.js';

export default {
    name: 'PhotoFinishAlerts',
    props: {
        refreshTrigger: {
            type: Number,
            default: 0
        }
    },
    setup() {
        const { selectedDate } = useDateStore();
        return { selectedDate };
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
        selectedDate() {
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
                this.data = await this.gameService.getPhotoFinishes(this.selectedDate);
            } catch (error) {
                console.error('Error loading photo finishes:', error);
                this.error = 'Failed to load photo finish data';
            } finally {
                this.loading = false;
            }
        },
        formatDate(dateString) {
            const date = new Date(dateString);
            return date.toLocaleDateString('en-US', {
                weekday: 'long',
                month: 'short',
                day: 'numeric',
                year: 'numeric'
            });
        }
    }
};
</script>

<template>
    <Card class="analytics-card">
        <template #title>
            <div class="flex items-center gap-2">
                <i class="pi pi-stopwatch text-red-500"></i>
                Photo Finish Alerts
            </div>
        </template>
        <template #content>
            <div v-if="loading" class="text-center py-4">
                <ProgressSpinner style="width: 40px; height: 40px" strokeWidth="4" />
                <p class="text-gray-500 mt-2">Scanning for tight races...</p>
            </div>
            <div v-else-if="error" class="text-red-600 text-center py-4">
                {{ error }}
            </div>
            <div v-else-if="data">
                <div class="text-center mb-4">
                    <div class="text-2xl font-bold text-red-600">{{ data.totalPhotoFinishes }}</div>
                    <div class="text-sm text-gray-600">Photo finishes on {{ formatDate(data.date) }}</div>
                </div>

                <div v-if="data.photoFinishes && data.photoFinishes.length > 0" class="space-y-3">
                    <div v-for="finish in data.photoFinishes" :key="`${finish.gameId}-${finish.date}`" class="border-l-4 border-red-400 bg-red-50 rounded-r-lg p-3">
                        <div class="flex items-center justify-between mb-2">
                            <h4 class="font-semibold text-gray-800">{{ finish.gameName }}</h4>
                            <Badge :value="`${finish.totalParticipants} players`" severity="info" class="text-xs" />
                        </div>

                        <div class="space-y-2">
                            <div class="flex items-center gap-2">
                                <i class="pi pi-trophy text-yellow-500 text-sm"></i>
                                <span class="font-medium text-gray-800">{{ finish.leader }}</span>
                                <span class="text-gray-600">{{ finish.leaderScore }}</span>
                            </div>
                            <div class="flex items-center gap-2">
                                <i class="pi pi-circle text-gray-400 text-sm"></i>
                                <span class="font-medium text-gray-800">{{ finish.runnerUp }}</span>
                                <span class="text-gray-600">{{ finish.runnerUpScore }}</span>
                            </div>
                            <div class="flex items-center gap-2 mt-2 pt-2 border-t border-red-200">
                                <i class="pi pi-bolt text-red-500 text-sm"></i>
                                <span class="text-red-600 font-bold">{{ finish.margin }}</span>
                                <span class="text-gray-500 text-sm">margin</span>
                                <div v-if="finish.margin === 'TIE'" class="ml-auto">
                                    <Badge value="PERFECT TIE" severity="danger" />
                                </div>
                            </div>
                        </div>
                    </div>
                </div>

                <div v-else class="text-center py-6">
                    <i class="pi pi-check-circle text-green-500 text-3xl mb-3"></i>
                    <div class="text-gray-600">No photo finishes today</div>
                    <div class="text-xs text-gray-500 mt-1">All games were decided by comfortable margins</div>
                </div>

                <div class="mt-4 p-2 bg-red-50 rounded text-xs text-gray-600">
                    <i class="pi pi-info-circle mr-1"></i>
                    Photo finish: ≤3 seconds (time games) or tied guesses (guess games)
                </div>
            </div>
        </template>
    </Card>
</template>

<style scoped>
.analytics-card {
    @apply bg-gradient-to-br from-red-50 to-pink-50 border-red-200;
}
</style>
