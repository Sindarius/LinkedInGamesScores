<script>
import { GameService } from '@/services/gameService.js';
import { useDateStore } from '@/stores/dateStore.js';

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
    setup() {
        const { selectedDate } = useDateStore();
        return { selectedDate };
    },
    data() {
        return {
            scores: [],
            selectedGame: null,
            isLoading: false,
            gameService: new GameService(),
            showImageDialog: false,
            selectedImageUrl: null,
            imageLoading: false,
            hoverThumbnail: {
                visible: false,
                url: null,
                x: 0,
                y: 0
            },
            isMobile: false
        };
    },
    mounted() {
        // Detect if device is mobile
        this.isMobile = /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent) || window.innerWidth <= 768;

        // Add event listener for mobile orientation changes
        window.addEventListener('resize', () => {
            this.isMobile = window.innerWidth <= 768;
        });
    },
    watch: {
        gameId: {
            handler() {
                this.loadGameInfo();
                this.loadLeaderboard();
            },
            immediate: true
        },
        refreshTrigger() {
            this.loadLeaderboard();
        },
        selectedDate() {
            this.loadLeaderboard();
        }
    },
    methods: {
        async loadGameInfo() {
            if (this.gameId) {
                try {
                    const games = await this.gameService.getGames();
                    this.selectedGame = games.find((g) => g.id === this.gameId);
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
                    this.scores = await this.gameService.getLeaderboard(this.gameId, this.selectedDate, 10);
                } else {
                    // Load all games leaderboard for selected date
                    const games = await this.gameService.getGames();
                    let allScores = [];

                    for (const game of games) {
                        const gameScores = await this.gameService.getLeaderboard(game.id, this.selectedDate, 10);
                        allScores = [...allScores, ...gameScores];
                    }

                    this.scores = allScores.sort((a, b) => b.score - a.score).slice(0, 10);
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
                case 0:
                    return 'warning'; // Gold
                case 1:
                    return 'secondary'; // Silver
                case 2:
                    return 'success'; // Bronze
                default:
                    return 'info';
            }
        },
        formatCompletionTime(timeSpan) {
            if (!timeSpan) return 'N/A';

            // timeSpan comes as "HH:MM:SS" format from .NET TimeSpan
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
        },
        async viewScoreImage(scoreId) {
            this.imageLoading = true;
            this.showImageDialog = true;

            try {
                const imageBlob = await this.gameService.getScoreImage(scoreId);
                if (imageBlob) {
                    this.selectedImageUrl = URL.createObjectURL(imageBlob);
                } else {
                    this.$toast.add({
                        severity: 'warn',
                        summary: 'No Image',
                        detail: 'No screenshot available for this score'
                    });
                    this.showImageDialog = false;
                }
            } catch (error) {
                console.error('Error loading image:', error);
                this.$toast.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: 'Failed to load score image'
                });
                this.showImageDialog = false;
            } finally {
                this.imageLoading = false;
            }
        },
        closeImageDialog() {
            this.showImageDialog = false;
            if (this.selectedImageUrl) {
                URL.revokeObjectURL(this.selectedImageUrl);
                this.selectedImageUrl = null;
            }
        },
        async handleImageHover(scoreId, event) {
            if (this.isMobile) return; // Don't show hover on mobile

            try {
                const thumbnailBlob = await this.gameService.getScoreImageThumbnail(scoreId, 400, 300);
                if (thumbnailBlob) {
                    this.hoverThumbnail.url = URL.createObjectURL(thumbnailBlob);
                    this.hoverThumbnail.x = event.clientX + 10;
                    this.hoverThumbnail.y = event.clientY + 10;
                    this.hoverThumbnail.visible = true;
                }
            } catch (error) {
                console.error('Error loading thumbnail:', error);
            }
        },
        handleImageHoverOut() {
            if (this.hoverThumbnail.url) {
                URL.revokeObjectURL(this.hoverThumbnail.url);
            }
            this.hoverThumbnail.visible = false;
            this.hoverThumbnail.url = null;
        },
        handleImageClick(scoreId) {
            if (this.isMobile) {
                // On mobile, open popup dialog
                this.viewScoreImage(scoreId);
            }
            // On desktop, hover handles the preview, click also opens dialog
            else {
                this.viewScoreImage(scoreId);
            }
        },
        getRankDisplay(index, currentScore, scores) {
            if (index === 0) return 1;

            // Since scores are already sorted, find the actual rank by counting unique better scores
            let actualRank = 1;
            for (let i = 0; i < index; i++) {
                const prevScore = scores[i];
                let isBetter = false;

                if (this.selectedGame?.scoringType === 1) {
                    // Guess-based: lower is better, but DNF (99) is worst
                    if (currentScore.guessCount === 99 && prevScore.guessCount === 99) {
                        isBetter = false; // Both DNF, tied
                    } else if (prevScore.guessCount === 99) {
                        isBetter = false; // Previous is DNF, current is better
                    } else if (currentScore.guessCount === 99) {
                        isBetter = true; // Current is DNF, previous is better
                    } else {
                        isBetter = prevScore.guessCount < currentScore.guessCount;
                    }
                } else if (this.selectedGame?.scoringType === 2) {
                    // Time-based: lower is better
                    isBetter = prevScore.score < currentScore.score;
                } else {
                    // Score-based: higher is better
                    isBetter = prevScore.score > currentScore.score;
                }

                if (isBetter) {
                    actualRank++;
                }
            }

            // Check if tied with previous score
            const prevScore = scores[index - 1];
            let isTied = false;

            if (this.selectedGame?.scoringType === 1) {
                isTied = prevScore.guessCount === currentScore.guessCount;
            } else if (this.selectedGame?.scoringType === 2) {
                isTied = prevScore.score === currentScore.score;
            } else {
                isTied = prevScore.score === currentScore.score;
            }

            return isTied ? 'T' + actualRank : actualRank;
        },
        getRankInfo(index, currentScore, scores) {
            // Calculate actual rank position (1, 2, 3, etc.)
            let actualRank = 1;
            let isTied = false;

            if (index === 0) {
                actualRank = 1;
            } else {
                // Find actual rank by counting unique better scores
                for (let i = 0; i < index; i++) {
                    const prevScore = scores[i];
                    let isBetter = false;

                    if (this.selectedGame?.scoringType === 1) {
                        // Guess-based: lower is better, but DNF (99) is worst
                        if (currentScore.guessCount === 99 && prevScore.guessCount === 99) {
                            isBetter = false; // Both DNF, tied
                        } else if (prevScore.guessCount === 99) {
                            isBetter = false; // Previous is DNF, current is better
                        } else if (currentScore.guessCount === 99) {
                            isBetter = true; // Current is DNF, previous is better
                        } else {
                            isBetter = prevScore.guessCount < currentScore.guessCount;
                        }
                    } else if (this.selectedGame?.scoringType === 2) {
                        // Time-based: lower is better
                        isBetter = prevScore.score < currentScore.score;
                    } else {
                        // Score-based: higher is better
                        isBetter = prevScore.score > currentScore.score;
                    }

                    if (isBetter) {
                        actualRank++;
                    }
                }
            }

            // Check if tied with any other score (previous or next)
            const isScoreEqual = (score1, score2) => {
                if (this.selectedGame?.scoringType === 1) {
                    return score1.guessCount === score2.guessCount;
                } else if (this.selectedGame?.scoringType === 2) {
                    return score1.score === score2.score;
                } else {
                    return score1.score === score2.score;
                }
            };

            // Check if tied with previous score
            if (index > 0 && isScoreEqual(currentScore, scores[index - 1])) {
                isTied = true;
            }
            // Check if tied with next score
            else if (index < scores.length - 1 && isScoreEqual(currentScore, scores[index + 1])) {
                isTied = true;
            }

            return { actualRank, isTied };
        }
    }
};
</script>

<template>
    <Card class="bg-surface-0 dark:bg-surface-800 border border-surface-200 dark:border-surface-700">
        <template #title>
            <div class="flex justify-between items-center">
                <span>{{ selectedGame?.name || 'All Games' }} Leaderboard</span>
                <div class="flex items-center gap-3">
                    <Button icon="pi pi-refresh" @click="loadLeaderboard" :loading="isLoading" class="px-3 py-2" aria-label="Refresh leaderboard" />
                </div>
            </div>
        </template>
        <template #content>
            <div v-if="isLoading" class="text-center py-4">
                <ProgressSpinner size="40" />
            </div>

            <div v-else-if="scores.length === 0" class="text-center py-8 text-muted-color">
                <i class="pi pi-info-circle text-4xl mb-4"></i>
                <p>No scores found for {{ formatDate(selectedDate || new Date()) }}</p>
            </div>

            <div v-else>
                <div class="mb-4 text-sm text-muted-color">Showing scores for {{ formatDate(selectedDate || new Date()) }}</div>

                <DataTable :value="scores" :rows="10" :paginator="scores.length > 10" responsiveLayout="scroll">
                    <Column field="rank" header="Rank" class="w-16">
                        <template #body="{ data, index }">
                            <div class="flex justify-center items-center gap-1">
                                <template v-if="getRankInfo(index, data, scores).actualRank <= 3">
                                    <!-- Medal icons with matching colored numbers for top 3 actual ranks -->
                                    <div v-if="getRankInfo(index, data, scores).actualRank === 1" class="flex items-center gap-1">
                                        <i class="pi pi-trophy text-xl" title="1st Place - Gold Medal" style="color: #daa520"></i>
                                        <span class="text-sm font-bold" style="color: #daa520">1</span>
                                        <span v-if="getRankInfo(index, data, scores).isTied" class="text-xs font-bold" style="color: #daa520">T</span>
                                    </div>
                                    <div v-else-if="getRankInfo(index, data, scores).actualRank === 2" class="flex items-center gap-1">
                                        <i class="pi pi-trophy text-xl" title="2nd Place - Silver Medal" style="color: #c0c0c0"></i>
                                        <span class="text-sm font-bold" style="color: #c0c0c0">2</span>
                                        <span v-if="getRankInfo(index, data, scores).isTied" class="text-xs font-bold" style="color: #c0c0c0">T</span>
                                    </div>
                                    <div v-else-if="getRankInfo(index, data, scores).actualRank === 3" class="flex items-center gap-1">
                                        <i class="pi pi-trophy text-xl" title="3rd Place - Bronze Medal" style="color: #cd7f32"></i>
                                        <span class="text-sm font-bold" style="color: #cd7f32">3</span>
                                        <span v-if="getRankInfo(index, data, scores).isTied" class="text-xs font-bold" style="color: #cd7f32">T</span>
                                    </div>
                                </template>
                                <!-- Regular rank badges for 4+ -->
                                <Badge v-else :value="getRankDisplay(index, data, scores)" />
                            </div>
                        </template>
                    </Column>

                    <Column field="playerName" header="Player">
                        <template #body="{ data }">
                            <div class="flex items-center">
                                <Avatar :label="data.playerName.charAt(0).toUpperCase()" class="mr-2" size="small" />
                                <div>
                                    <div class="font-medium">{{ data.playerName }}</div>
                                    <div v-if="data.linkedInProfileUrl" class="text-xs text-blue-600">
                                        <a :href="data.linkedInProfileUrl" target="_blank" class="hover:underline"> LinkedIn Profile </a>
                                    </div>
                                </div>
                            </div>
                        </template>
                    </Column>

                    <Column field="score" class="text-right">
                        <template #header>
                            <span v-if="selectedGame?.scoringType === 1">Guesses</span>
                            <span v-else-if="selectedGame?.scoringType === 2">Time</span>
                            <span v-else>Score</span>
                        </template>
                        <template #body="{ data }">
                            <span v-if="data.scoringType === 1" class="font-bold text-lg">
                                <span v-if="data.guessCount === 99" class="text-red-600">DNF</span>
                                <span v-else>{{ data.guessCount }}</span>
                            </span>
                            <span v-else-if="data.scoringType === 2" class="font-bold text-lg">{{ formatCompletionTime(data.completionTime) }}</span>
                            <span v-else class="font-bold text-lg">{{ data.score?.toLocaleString() || 'N/A' }}</span>
                        </template>
                    </Column>

                    <Column header="Screenshot" class="w-24 text-center">
                        <template #body="{ data }">
                            <i
                                v-if="data.hasScoreImage"
                                class="pi pi-image text-green-600 cursor-pointer hover:text-green-700 transition-colors"
                                @click="handleImageClick(data.id)"
                                @mouseenter="handleImageHover(data.id, $event)"
                                @mouseleave="handleImageHoverOut"
                                :title="isMobile ? 'Tap to view screenshot' : 'Hover to preview, click to view full size'"
                            ></i>
                            <i v-else class="pi pi-minus text-gray-400" title="No screenshot"></i>
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

    <!-- Score Image Dialog -->
    <Dialog v-model:visible="showImageDialog" header="Score Screenshot" :modal="true" :closable="true" :dismissableMask="true" @hide="closeImageDialog" :style="{ width: 'auto', maxWidth: '95vw', maxHeight: '95vh' }" :contentStyle="{ padding: '0' }">
        <div v-if="imageLoading" class="flex justify-center py-8">
            <ProgressSpinner />
        </div>
        <div v-else-if="selectedImageUrl" class="flex justify-center items-center">
            <img :src="selectedImageUrl" alt="Score screenshot" class="max-w-full max-h-[90vh] object-contain" style="display: block" />
        </div>
    </Dialog>

    <!-- Hover Thumbnail (Desktop only) -->
    <div
        v-if="hoverThumbnail.visible && hoverThumbnail.url && !isMobile"
        class="fixed z-50 pointer-events-none border-2 border-gray-300 rounded shadow-lg bg-white p-1"
        :style="{
            left: hoverThumbnail.x + 'px',
            top: hoverThumbnail.y + 'px',
            maxWidth: '400px'
        }"
    >
        <img :src="hoverThumbnail.url" alt="Score thumbnail" class="max-w-full max-h-32 object-contain" />
    </div>
</template>

<style scoped>
.pi-trophy {
    color: #fbbf24;
}
</style>
