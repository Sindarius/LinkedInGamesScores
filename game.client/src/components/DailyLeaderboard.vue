<template>
    <Card>
        <template #title>
            <div class="flex justify-between items-center">
                <span>{{ selectedGame?.name || 'All Games' }} Leaderboard</span>
                <div class="flex items-center space-x-2">
                    <DatePicker 
                        v-model="selectedDate" 
                        dateFormat="mm/dd/yy"
                        :showIcon="true"
                        placeholder="Today"
                        @date-select="loadLeaderboard"
                        class="w-40"
                    />
                    <Button 
                        icon="pi pi-refresh" 
                        @click="loadLeaderboard"
                        :loading="isLoading"
                        size="small"
                    />
                </div>
            </div>
        </template>
        <template #content>
            <div v-if="isLoading" class="text-center py-4">
                <ProgressSpinner size="40" />
            </div>
            
            <div v-else-if="scores.length === 0" class="text-center py-8 text-gray-500">
                <i class="pi pi-info-circle text-4xl mb-4"></i>
                <p>No scores found for {{ formatDate(selectedDate || new Date()) }}</p>
            </div>
            
            <div v-else>
                <div class="mb-4 text-sm text-gray-600">
                    Showing scores for {{ formatDate(selectedDate || new Date()) }}
                </div>
                
                <DataTable 
                    :value="scores" 
                    :rows="10"
                    :paginator="scores.length > 10"
                    responsiveLayout="scroll"
                >
                    <Column field="rank" header="Rank" class="w-16">
                        <template #body="{ index }">
                            <Badge 
                                :value="index + 1" 
                                :severity="getBadgeSeverity(index)"
                            />
                        </template>
                    </Column>
                    
                    <Column field="playerName" header="Player">
                        <template #body="{ data }">
                            <div class="flex items-center">
                                <Avatar 
                                    :label="data.playerName.charAt(0).toUpperCase()" 
                                    class="mr-2" 
                                    size="small"
                                />
                                <div>
                                    <div class="font-medium">{{ data.playerName }}</div>
                                    <div v-if="data.linkedInProfileUrl" class="text-xs text-blue-600">
                                        <a 
                                            :href="data.linkedInProfileUrl" 
                                            target="_blank" 
                                            class="hover:underline"
                                        >
                                            LinkedIn Profile
                                        </a>
                                    </div>
                                </div>
                            </div>
                        </template>
                    </Column>
                    
                    <Column field="score" header="Score" class="text-right">
                        <template #body="{ data }">
                            <span class="font-bold text-lg">{{ data.score.toLocaleString() }}</span>
                        </template>
                    </Column>
                    
                    <Column field="dateAchieved" header="Time" class="text-right">
                        <template #body="{ data }">
                            {{ formatTime(data.dateAchieved) }}
                        </template>
                    </Column>
                </DataTable>
            </div>
        </template>
    </Card>
</template>

<script>
import { GameService } from '@/services/gameService.js';

export default {
    name: 'DailyLeaderboard',
    props: {
        gameId: {
            type: Number,
            default: null
        },
        refreshTrigger: {
            type: Number,
            default: 0
        }
    },
    data() {
        return {
            scores: [],
            selectedDate: null,
            selectedGame: null,
            isLoading: false,
            gameService: new GameService()
        };
    },
    watch: {
        gameId: {
            handler(newGameId) {
                this.loadGameInfo();
                this.loadLeaderboard();
            },
            immediate: true
        },
        refreshTrigger() {
            this.loadLeaderboard();
        }
    },
    methods: {
        async loadGameInfo() {
            if (this.gameId) {
                try {
                    const games = await this.gameService.getGames();
                    this.selectedGame = games.find(g => g.id === this.gameId);
                } catch (error) {
                    console.error('Error loading game info:', error);
                }
            } else {
                this.selectedGame = null;
            }
        },
        async loadLeaderboard() {
            this.isLoading = true;
            
            try {
                if (this.gameId) {
                    this.scores = await this.gameService.getLeaderboard(
                        this.gameId, 
                        this.selectedDate, 
                        10
                    );
                } else {
                    // Load all games leaderboard for selected date
                    const games = await this.gameService.getGames();
                    let allScores = [];
                    
                    for (const game of games) {
                        const gameScores = await this.gameService.getLeaderboard(
                            game.id, 
                            this.selectedDate, 
                            10
                        );
                        allScores = [...allScores, ...gameScores];
                    }
                    
                    this.scores = allScores
                        .sort((a, b) => b.score - a.score)
                        .slice(0, 10);
                }
            } catch (error) {
                console.error('Error loading leaderboard:', error);
                this.$toast.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: 'Failed to load leaderboard'
                });
            } finally {
                this.isLoading = false;
            }
        },
        formatDate(date) {
            return new Date(date).toLocaleDateString('en-US', {
                weekday: 'long',
                year: 'numeric',
                month: 'long',
                day: 'numeric'
            });
        },
        formatTime(dateString) {
            return new Date(dateString).toLocaleTimeString('en-US', {
                hour: '2-digit',
                minute: '2-digit'
            });
        },
        getBadgeSeverity(index) {
            switch (index) {
                case 0: return 'warning'; // Gold
                case 1: return 'secondary'; // Silver
                case 2: return 'success'; // Bronze
                default: return 'info';
            }
        }
    }
};
</script>

<style scoped>
.pi-trophy {
    color: #fbbf24;
}
</style>