<template>
    <div class="game-tabs">
        <TabView @tab-change="onTabChange">
            <TabPanel header="All Games">
                <DailyLeaderboard 
                    :gameId="null" 
                    :refreshTrigger="refreshTrigger"
                />
            </TabPanel>
            
            <TabPanel 
                v-for="game in games" 
                :key="game.id" 
                :header="game.name"
            >
                <DailyLeaderboard 
                    :gameId="game.id" 
                    :refreshTrigger="refreshTrigger"
                />
            </TabPanel>
        </TabView>
    </div>
</template>

<script>
import { GameService } from '@/services/gameService.js';
import DailyLeaderboard from './DailyLeaderboard.vue';

export default {
    name: 'GameTabs',
    components: {
        DailyLeaderboard
    },
    props: {
        refreshTrigger: {
            type: Number,
            default: 0
        }
    },
    data() {
        return {
            games: [],
            activeIndex: 0,
            gameService: new GameService()
        };
    },
    async mounted() {
        await this.loadGames();
    },
    methods: {
        async loadGames() {
            try {
                this.games = await this.gameService.getGames();
            } catch (error) {
                console.error('Error loading games:', error);
                this.$toast.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: 'Failed to load games'
                });
            }
        },
        onTabChange(event) {
            this.activeIndex = event.index;
            this.$emit('tab-changed', {
                index: event.index,
                gameId: event.index === 0 ? null : this.games[event.index - 1]?.id
            });
        }
    }
};
</script>

<style scoped>
.game-tabs {
    @apply w-full;
}
</style>