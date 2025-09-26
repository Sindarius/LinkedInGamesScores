<script>
import { GameService } from '@/services/gameService.js';

export default {
    name: 'TemperatureIndicator',
    props: {
        playerName: {
            type: String,
            required: true
        },
        days: {
            type: Number,
            default: 7
        },
        showDetails: {
            type: Boolean,
            default: false
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
    computed: {
        temperatureIcon() {
            if (!this.data?.overallTemperature) return 'pi pi-circle text-gray-400';

            const temp = this.data.overallTemperature;
            switch (temp) {
                case 'Hot':
                    return 'pi pi-sun text-red-500';
                case 'Warm':
                    return 'pi pi-circle-fill text-orange-500';
                case 'Cool':
                    return 'pi pi-circle text-blue-500';
                case 'Cold':
                    return 'pi pi-snowflake text-blue-600';
                default:
                    return 'pi pi-circle text-gray-400';
            }
        },
        temperatureTextColor() {
            if (!this.data?.overallTemperature) return 'text-gray-500';

            const temp = this.data.overallTemperature;
            switch (temp) {
                case 'Hot':
                    return 'text-red-600';
                case 'Warm':
                    return 'text-orange-600';
                case 'Cool':
                    return 'text-blue-600';
                case 'Cold':
                    return 'text-blue-700';
                default:
                    return 'text-gray-500';
            }
        }
    },
    watch: {
        playerName() {
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
        if (this.playerName?.trim()) {
            await this.loadData();
        }
    },
    methods: {
        async loadData() {
            if (!this.playerName?.trim()) {
                this.data = null;
                return;
            }

            this.loading = true;
            this.error = null;

            try {
                this.data = await this.gameService.getPlayerTemperature(this.playerName, this.days);
            } catch (error) {
                console.error('Error loading player temperature:', error);
                this.error = 'Failed to load temperature data';
            } finally {
                this.loading = false;
            }
        },
        getGameTemperatureIcon(temperature) {
            switch (temperature) {
                case 'Hot':
                    return 'pi pi-sun text-red-500';
                case 'Warm':
                    return 'pi pi-circle-fill text-orange-500';
                case 'Cool':
                    return 'pi pi-circle text-blue-500';
                case 'Cold':
                    return 'pi pi-snowflake text-blue-600';
                default:
                    return 'pi pi-circle text-gray-400';
            }
        },
        getGameTemperatureColor(temperature) {
            switch (temperature) {
                case 'Hot':
                    return 'text-red-600';
                case 'Warm':
                    return 'text-orange-600';
                case 'Cool':
                    return 'text-blue-600';
                case 'Cold':
                    return 'text-blue-700';
                default:
                    return 'text-gray-500';
            }
        }
    }
};
</script>

<template>
    <div v-if="data && !loading" class="temperature-indicator">
        <div class="flex items-center gap-2 text-sm">
            <i :class="temperatureIcon" class="text-lg"></i>
            <span :class="temperatureTextColor" class="font-medium">
                {{ data.overallTemperature }}
            </span>
            <span class="text-gray-500">{{ data.games?.length || 0 }} game{{ (data.games?.length || 0) !== 1 ? 's' : '' }}</span>
        </div>

        <div v-if="showDetails && data.games && data.games.length > 0" class="mt-2 p-2 bg-gray-50 rounded text-xs space-y-1">
            <div v-for="game in data.games" :key="game.gameId" class="flex justify-between items-center">
                <span class="text-gray-700">{{ game.gameName }}</span>
                <div class="flex items-center gap-1">
                    <i :class="getGameTemperatureIcon(game.temperature)"></i>
                    <span :class="getGameTemperatureColor(game.temperature)">{{ game.temperature }}</span>
                    <span class="text-gray-500">({{ game.trend }})</span>
                </div>
            </div>
        </div>
    </div>
    <div v-else-if="loading" class="temperature-indicator">
        <div class="flex items-center gap-2 text-sm text-gray-500">
            <i class="pi pi-spin pi-spinner"></i>
            <span>Loading...</span>
        </div>
    </div>
    <div v-else-if="error" class="temperature-indicator">
        <div class="text-xs text-red-500">
            <i class="pi pi-exclamation-triangle"></i>
            Error loading temperature
        </div>
    </div>
</template>

<style scoped>
.temperature-indicator {
    @apply min-h-[24px];
}
</style>
