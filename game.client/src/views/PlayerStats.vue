<script>
import { GameService } from '@/services/gameService.js';

export default {
    name: 'PlayerStats',
    data() {
        return {
            stats: null,
            isLoading: true,
            error: null,
            selectedGameId: null,
            gameService: new GameService()
        };
    },
    computed: {
        selectedGameStats() {
            if (!this.stats) return null;
            if (this.selectedGameId === null) return null;
            return this.stats.gameStats.find((g) => g.gameId === this.selectedGameId) || null;
        },
        overallWinRate() {
            if (!this.stats?.gameStats?.length) return 0;
            const total = this.stats.gameStats.reduce((a, g) => a + g.totalGames, 0);
            const wins = this.stats.gameStats.reduce((a, g) => a + g.wins, 0);
            return total > 0 ? Math.round((wins / total) * 100) : 0;
        },
        overallAvgRank() {
            if (!this.stats?.gameStats?.length) return 0;
            const withGames = this.stats.gameStats.filter((g) => g.totalGames > 0);
            if (!withGames.length) return 0;
            return (withGames.reduce((a, g) => a + g.avgRank, 0) / withGames.length).toFixed(1);
        },
        rankHistoryForChart() {
            const gs = this.selectedGameStats;
            if (!gs) return [];
            return gs.rankHistory;
        }
    },
    async mounted() {
        const playerName = this.$route.params.name;
        const linkedInUrl = this.$route.query.linkedIn || null;
        try {
            this.stats = await this.gameService.getPlayerStats(playerName, linkedInUrl);
            if (!this.stats) {
                this.error = 'Player not found.';
            } else if (this.stats.gameStats?.length) {
                this.selectedGameId = this.stats.gameStats[0].gameId;
            }
        } catch (e) {
            this.error = 'Failed to load player stats.';
        } finally {
            this.isLoading = false;
        }
    },
    methods: {
        barHeightPx(rank) {
            if (rank === null) return 3;
            if (rank === 1) return 56;
            if (rank === 2) return 42;
            if (rank === 3) return 28;
            return 14; // 4th or worse
        },
        formatScore(result) {
            if (result.isDnf) return 'DNF';
            if (result.scoringType === 2 && result.completionTime) {
                return this.formatTime(result.completionTime);
            }
            if (result.scoringType === 1) {
                const g = result.guessCount;
                return `${g} guess${g === 1 ? '' : 'es'}`;
            }
            return result.score;
        },
        formatTime(timeSpan) {
            if (!timeSpan) return 'N/A';
            const parts = timeSpan.split(':');
            const h = parseInt(parts[0] || 0);
            const m = parseInt(parts[1] || 0);
            const s = parseInt(parts[2] || 0);
            if (h > 0) return `${h}h ${m}m ${s}s`;
            if (m > 0) return `${m}m ${s}s`;
            return `${s}s`;
        },
        formatAvgScore(gs) {
            if (gs.avgScore === null || gs.avgScore === undefined) return '—';
            if (gs.scoringType === 2) return `${gs.avgScore.toFixed(1)}s`;
            return gs.avgScore.toFixed(1);
        },
        formatBestScore(gs) {
            if (gs.bestScore === null || gs.bestScore === undefined) return '—';
            if (gs.scoringType === 2) return `${gs.bestScore.toFixed(0)}s`;
            return gs.bestScore.toFixed(0);
        },
        rankLabel(index) {
            const entry = this.rankHistoryForChart[index];
            if (!entry) return '';
            return entry.date?.slice(5) || ''; // MM-DD
        },
        rankColor(rank) {
            if (rank === 1) return '#daa520';
            if (rank === 2) return '#94a3b8';
            if (rank === 3) return '#cd7f32';
            return '#818cf8';
        }
    }
};
</script>

<template>
    <div class="p-4 md:p-6 max-w-3xl mx-auto">
        <!-- Loading -->
        <div v-if="isLoading" class="flex justify-center py-16">
            <ProgressSpinner />
        </div>

        <!-- Error -->
        <div v-else-if="error || !stats" class="text-center py-16 text-muted-color">
            <i class="pi pi-user-minus text-5xl mb-4 block"></i>
            <p>{{ error || 'Player not found.' }}</p>
            <RouterLink to="/" class="text-primary underline text-sm mt-2 inline-block">Back to leaderboard</RouterLink>
        </div>

        <template v-else>
            <!-- Hero card -->
            <Card class="mb-4 bg-surface-0 dark:bg-surface-800 border border-surface-200 dark:border-surface-700">
                <template #content>
                    <div class="flex items-center gap-4">
                        <Avatar :label="stats.playerName.charAt(0).toUpperCase()" size="xlarge" class="flex-shrink-0" />
                        <div class="flex-1 min-w-0">
                            <div class="flex items-center gap-2 flex-wrap">
                                <h1 class="text-2xl font-bold">{{ stats.playerName }}</h1>
                                <span v-if="stats.currentStreak >= 2" class="streak-badge-lg">🔥 {{ stats.currentStreak }}-day streak</span>
                            </div>
                            <a v-if="stats.linkedInProfileUrl" :href="stats.linkedInProfileUrl" target="_blank" class="text-xs text-blue-500 hover:underline">LinkedIn Profile</a>
                            <div class="flex flex-wrap gap-2 mt-2">
                                <span v-for="(ach, i) in stats.achievements" :key="i" class="achievement-chip">{{ ach }}</span>
                            </div>
                        </div>
                        <div class="text-right flex-shrink-0">
                            <div class="text-3xl font-bold">{{ stats.totalGames }}</div>
                            <div class="text-xs text-muted-color">games played</div>
                        </div>
                    </div>
                </template>
            </Card>

            <!-- Stat tiles -->
            <div class="grid grid-cols-3 gap-3 mb-4">
                <Card class="bg-surface-0 dark:bg-surface-800 border border-surface-200 dark:border-surface-700 text-center">
                    <template #content>
                        <div class="stat-value text-indigo-400">{{ overallWinRate }}%</div>
                        <div class="stat-label">Win rate</div>
                    </template>
                </Card>
                <Card class="bg-surface-0 dark:bg-surface-800 border border-surface-200 dark:border-surface-700 text-center">
                    <template #content>
                        <div class="stat-value text-emerald-400">#{{ overallAvgRank }}</div>
                        <div class="stat-label">Avg rank</div>
                    </template>
                </Card>
                <Card class="bg-surface-0 dark:bg-surface-800 border border-surface-200 dark:border-surface-700 text-center">
                    <template #content>
                        <div class="stat-value text-yellow-400">{{ stats.bestStreak }}</div>
                        <div class="stat-label">Best streak</div>
                    </template>
                </Card>
            </div>

            <!-- Per-game breakdown -->
            <Card class="mb-4 bg-surface-0 dark:bg-surface-800 border border-surface-200 dark:border-surface-700">
                <template #title>Performance by game</template>
                <template #content>
                    <div class="space-y-4">
                        <div v-for="gs in stats.gameStats" :key="gs.gameId">
                            <div class="flex items-center justify-between mb-1">
                                <span class="font-semibold text-sm">{{ gs.gameName }}</span>
                                <div class="flex gap-3 text-xs text-muted-color">
                                    <span>{{ gs.totalGames }} games</span>
                                    <span>Avg rank #{{ gs.avgRank }}</span>
                                    <span class="font-semibold" :class="gs.winRate >= 50 ? 'text-emerald-400' : 'text-muted-color'">{{ gs.winRate }}% wins</span>
                                </div>
                            </div>
                            <div class="win-rate-track">
                                <div class="win-rate-fill" :style="{ width: gs.winRate + '%' }"></div>
                            </div>
                            <div class="flex gap-3 text-xs text-muted-color mt-1">
                                <span>Best: {{ formatBestScore(gs) }}</span>
                                <span>Avg: {{ formatAvgScore(gs) }}</span>
                                <span v-if="gs.neverDnf && gs.scoringType === 1" class="text-emerald-400">💯 Never DNF'd</span>
                            </div>
                        </div>
                    </div>
                </template>
            </Card>

            <!-- Rank history chart -->
            <Card class="mb-4 bg-surface-0 dark:bg-surface-800 border border-surface-200 dark:border-surface-700">
                <template #title>
                    <div class="flex items-center justify-between flex-wrap gap-2">
                        <span>Rank history — last 14 days</span>
                        <div class="flex gap-1 flex-wrap">
                            <button
                                v-for="gs in stats.gameStats"
                                :key="gs.gameId"
                                class="game-tab-btn"
                                :class="{ active: selectedGameId === gs.gameId }"
                                @click="selectedGameId = gs.gameId"
                            >
                                {{ gs.gameName }}
                            </button>
                        </div>
                    </div>
                </template>
                <template #content>
                    <div v-if="selectedGameStats">
                        <div class="chart-scroll">
                        <div class="chart-bars">
                            <div v-for="(entry, i) in rankHistoryForChart" :key="i" class="chart-col">
                                <span v-if="entry.rank" class="rank-dot" :style="{ background: rankColor(entry.rank) }">#{{ entry.rank }}</span>
                                <span v-else class="rank-dot-empty"></span>
                                <div style="flex: 1"></div>
                                <div
                                    class="bar"
                                    :class="{ 'bar-empty': entry.rank === null, 'bar-first': entry.rank === 1 }"
                                    :style="{ height: barHeightPx(entry.rank) + 'px' }"
                                ></div>
                                <div class="bar-date">{{ rankLabel(i) }}</div>
                            </div>
                        </div>
                        </div>
                        <div class="flex justify-between text-xs text-muted-color mt-1 px-1">
                            <span>Better ↑</span>
                            <span class="text-muted-color">Grey bars = didn't play</span>
                        </div>
                    </div>
                    <p v-else class="text-muted-color text-sm">Select a game above.</p>
                </template>
            </Card>

            <!-- Recent results -->
            <Card class="bg-surface-0 dark:bg-surface-800 border border-surface-200 dark:border-surface-700">
                <template #title>Recent results</template>
                <template #content>
                    <div class="space-y-2">
                        <div v-for="result in stats.recentResults" :key="result.scoreId" class="result-row">
                            <span class="result-date">{{ result.date }}</span>
                            <span
                                class="result-rank"
                                :style="{ color: rankColor(result.rank) }"
                            >#{{ result.rank }}</span>
                            <span class="result-game text-muted-color text-sm">{{ result.gameName }}</span>
                            <span class="result-score font-semibold ml-auto" :class="{ 'text-red-400': result.isDnf }">
                                {{ formatScore(result) }}
                            </span>
                        </div>
                        <p v-if="!stats.recentResults?.length" class="text-muted-color text-sm text-center py-4">No results yet.</p>
                    </div>
                </template>
            </Card>
        </template>
    </div>
</template>

<style scoped>
.streak-badge-lg {
    display: inline-flex;
    align-items: center;
    padding: 2px 10px;
    border-radius: 9999px;
    font-size: 13px;
    font-weight: 700;
    background: linear-gradient(135deg, #ef4444, #f97316);
    color: #fff;
}

.achievement-chip {
    display: inline-flex;
    align-items: center;
    padding: 2px 10px;
    border-radius: 9999px;
    font-size: 11px;
    font-weight: 600;
    background: rgba(99, 102, 241, 0.15);
    color: #818cf8;
    border: 1px solid rgba(99, 102, 241, 0.3);
}

.stat-value {
    font-size: 1.75rem;
    font-weight: 700;
    line-height: 1.2;
}

.stat-label {
    font-size: 11px;
    color: var(--p-text-muted-color, #94a3b8);
    margin-top: 2px;
}

.win-rate-track {
    height: 8px;
    border-radius: 9999px;
    background: rgba(99, 102, 241, 0.1);
    overflow: hidden;
}

.win-rate-fill {
    height: 100%;
    border-radius: 9999px;
    background: linear-gradient(90deg, #818cf8, #a78bfa);
    transition: width 0.4s ease;
}

.game-tab-btn {
    padding: 3px 10px;
    border-radius: 6px;
    font-size: 12px;
    font-weight: 500;
    border: 1px solid transparent;
    cursor: pointer;
    background: transparent;
    color: var(--p-text-muted-color, #94a3b8);
    transition: all 0.15s;
}

.game-tab-btn.active {
    background: rgba(99, 102, 241, 0.15);
    border-color: rgba(99, 102, 241, 0.4);
    color: #818cf8;
}

.chart-scroll {
    overflow-x: auto;
    -webkit-overflow-scrolling: touch;
}

.chart-bars {
    display: flex;
    gap: 4px;
    height: 90px;
    align-items: stretch;
    min-width: 420px;
}

.chart-col {
    flex: 1;
    display: flex;
    flex-direction: column;
    align-items: center;
    min-width: 0;
}

.rank-dot {
    font-size: 9px;
    font-weight: 700;
    color: #fff;
    padding: 1px 3px;
    border-radius: 3px;
    white-space: nowrap;
    flex-shrink: 0;
}

.rank-dot-empty {
    display: inline-block;
    height: 14px;
    flex-shrink: 0;
}

.bar {
    width: 100%;
    border-radius: 3px 3px 0 0;
    background: linear-gradient(180deg, #818cf8, #6366f1);
    flex-shrink: 0;
    transition: height 0.3s ease;
}

.bar.bar-empty {
    background: rgba(99, 102, 241, 0.15);
    border-radius: 2px;
}

.bar.bar-first {
    background: linear-gradient(180deg, #daa520, #b8860b);
}

.bar-date {
    font-size: 9px;
    color: var(--p-text-muted-color, #94a3b8);
    margin-top: 3px;
    white-space: nowrap;
    flex-shrink: 0;
}

.result-row {
    display: flex;
    align-items: center;
    gap: 10px;
    padding: 6px 0;
    border-bottom: 1px solid rgba(99, 102, 241, 0.08);
    font-size: 13px;
}

.result-row:last-child {
    border-bottom: none;
}

.result-date {
    color: var(--p-text-muted-color, #94a3b8);
    font-size: 11px;
    width: 70px;
    flex-shrink: 0;
}

.result-rank {
    font-weight: 700;
    width: 28px;
    flex-shrink: 0;
}

.result-game {
    flex: 1;
}

.result-score {
    flex-shrink: 0;
}
</style>
