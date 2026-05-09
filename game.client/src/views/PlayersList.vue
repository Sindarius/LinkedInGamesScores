<script>
import { GameService } from '@/services/gameService.js';

export default {
    name: 'PlayersList',
    data() {
        return {
            players: [],
            isLoading: true,
            error: null,
            gameService: new GameService()
        };
    },
    async mounted() {
        try {
            this.players = await this.gameService.getPlayers(30);
        } catch (e) {
            this.error = 'Failed to load players.';
        } finally {
            this.isLoading = false;
        }
    },
    methods: {
        playerRoute(player) {
            return {
                name: 'playerstats',
                params: { name: player.playerName },
                query: player.linkedInProfileUrl ? { linkedIn: player.linkedInProfileUrl } : {}
            };
        },
        initial(name) {
            return name?.charAt(0).toUpperCase() || '?';
        }
    }
};
</script>

<template>
    <div class="p-4 md:p-6 max-w-2xl mx-auto">
        <div class="flex items-center justify-between mb-5">
            <div>
                <h1 class="text-2xl font-bold">Players</h1>
                <p class="text-sm text-muted-color mt-0.5">Last 30 days · click any player to see their stats</p>
            </div>
            <Badge :value="players.length" severity="secondary" v-if="!isLoading" />
        </div>

        <div v-if="isLoading" class="flex justify-center py-16">
            <ProgressSpinner />
        </div>

        <div v-else-if="error" class="text-center py-12 text-muted-color">
            <i class="pi pi-exclamation-circle text-4xl mb-3 block"></i>
            <p>{{ error }}</p>
        </div>

        <div v-else-if="players.length === 0" class="text-center py-12 text-muted-color">
            <i class="pi pi-users text-4xl mb-3 block"></i>
            <p>No players found in the last 30 days.</p>
        </div>

        <Card v-else class="bg-surface-0 dark:bg-surface-800 border border-surface-200 dark:border-surface-700">
            <template #content>
                <div class="divide-y divide-surface-200 dark:divide-surface-700">
                    <RouterLink
                        v-for="player in players"
                        :key="player.playerName"
                        :to="playerRoute(player)"
                        class="player-row"
                    >
                        <!-- Avatar -->
                        <Avatar :label="initial(player.playerName)" size="normal" class="flex-shrink-0" />

                        <!-- Name + games -->
                        <div class="flex-1 min-w-0">
                            <div class="flex items-center gap-2 flex-wrap">
                                <span class="font-semibold truncate">{{ player.playerName }}</span>
                                <span v-if="player.currentStreak >= 2" class="streak-pill">🔥 {{ player.currentStreak }}</span>
                            </div>
                            <div class="text-xs text-muted-color mt-0.5 truncate">{{ player.gameNames.join(' · ') }}</div>
                        </div>

                        <!-- Stats -->
                        <div class="flex items-center gap-4 flex-shrink-0 text-right">
                            <div>
                                <div class="font-bold text-lg leading-none">{{ player.wins }}</div>
                                <div class="text-xs text-muted-color">wins</div>
                            </div>
                            <div>
                                <div class="font-semibold leading-none text-muted-color">{{ player.totalGames }}</div>
                                <div class="text-xs text-muted-color">played</div>
                            </div>
                            <i class="pi pi-chevron-right text-muted-color text-xs"></i>
                        </div>
                    </RouterLink>
                </div>
            </template>
        </Card>
    </div>
</template>

<style scoped>
.player-row {
    display: flex;
    align-items: center;
    gap: 12px;
    padding: 12px 4px;
    cursor: pointer;
    transition: background 0.15s;
    text-decoration: none;
    color: inherit;
    border-radius: 6px;
}

.player-row:hover {
    background: rgba(99, 102, 241, 0.06);
}

.streak-pill {
    display: inline-flex;
    align-items: center;
    padding: 1px 7px;
    border-radius: 9999px;
    font-size: 11px;
    font-weight: 700;
    background: linear-gradient(135deg, #ef4444, #f97316);
    color: #fff;
    white-space: nowrap;
}
</style>
