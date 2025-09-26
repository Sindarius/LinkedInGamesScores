<script>
import ScoreSubmissionForm from '@/components/ScoreSubmissionForm.vue';
import GameTabs from '@/components/GameTabs.vue';
import WinsTrendChart from '@/components/WinsTrendChart.vue';
import CloseCallsWidget from '@/components/analytics/CloseCallsWidget.vue';
import ComebackKingsWidget from '@/components/analytics/ComebackKingsWidget.vue';
import ConsistencyChampionsWidget from '@/components/analytics/ConsistencyChampionsWidget.vue';
import DistributionChart from '@/components/analytics/DistributionChart.vue';
import PhotoFinishAlerts from '@/components/analytics/PhotoFinishAlerts.vue';
import { GameService } from '@/services/gameService.js';
import { useDateStore } from '@/stores/dateStore.js';
import { usePlayerStore } from '@/stores/playerStore.js';

export default {
    name: 'GameScores',
    components: {
        ScoreSubmissionForm,
        GameTabs,
        WinsTrendChart,
        CloseCallsWidget,
        ComebackKingsWidget,
        ConsistencyChampionsWidget,
        DistributionChart,
        PhotoFinishAlerts
    },
    setup() {
        const { selectedDate, setSelectedDate, setToday, formatSelectedDate } = useDateStore();
        const playerStore = usePlayerStore();

        return {
            selectedDate,
            setSelectedDate,
            setToday,
            formatSelectedDate,
            playerStore
        };
    },
    data() {
        return {
            refreshTrigger: 0,
            totalScores: 0,
            activePlayers: 0,
            gamesCount: 0,
            gameService: new GameService(),
            isRefreshing: false,
            availableGames: [],
            playerHighlights: null,
            highlightLoading: false,
            highlightError: null,
            highlightAllScoresCache: {},
            highlightDailyScoresCache: {}
        };
    },
    computed: {
        hasPlayerName() {
            return Boolean(this.playerStore?.playerName?.trim());
        },
        highlightTopSpotSummary() {
            if (this.highlightError) {
                return '';
            }
            if (this.highlightLoading) {
                return 'Reviewing leaderboards...';
            }
            if (!this.playerHighlights) {
                return this.hasPlayerName ? 'Play today to claim the top spot' : 'Add your player name to unlock highlights';
            }

            const { topSpotsToday = 0, topSpotsYesterday = 0, topSpotGames = [], topSpots7Day = 0 } = this.playerHighlights;
            if (!topSpotsToday) {
                if (topSpotsYesterday) {
                    return `No #1 finishes today (you had ${topSpotsYesterday} yesterday).`;
                }
                if (topSpots7Day) {
                    return `No #1 finishes today, but ${topSpots7Day} in the last 7 days.`;
                }
                return 'No #1 finishes logged yet today.';
            }

            const delta = topSpotsToday - topSpotsYesterday;
            const changeText = delta === 0 ? 'same as yesterday' : delta > 0 ? `+${delta} vs yesterday` : `${delta} vs yesterday`;
            const gameList = topSpotGames.length ? ` - ${topSpotGames.map((entry) => `${entry.gameName}${entry.isTie ? ' (tie)' : ''}`).join(', ')}` : '';
            return `First place in ${topSpotsToday} game${topSpotsToday === 1 ? '' : 's'} today (${changeText})${gameList}`;
        },
        highlightPodiumSummary() {
            if (this.highlightError) {
                return '';
            }
            if (this.highlightLoading) {
                return 'Crunching podium stats...';
            }
            if (!this.playerHighlights) {
                return this.hasPlayerName ? 'Log scores to start a podium streak' : 'Add your player name to track podium finishes';
            }

            const { podiumCount7Day = 0, avgRank7Day = null, rankSamples7Day = 0 } = this.playerHighlights;
            if (!rankSamples7Day) {
                return 'No recent leaderboard history yet.';
            }

            const avgRankText = avgRank7Day ? avgRank7Day.toFixed(1) : 'N/A';
            return `On the podium ${podiumCount7Day} time${podiumCount7Day === 1 ? '' : 's'} in the last 7 days (avg rank ${avgRankText}).`;
        },
        highlightRecordsSummary() {
            if (this.highlightError) {
                return '';
            }
            if (this.highlightLoading) {
                return 'Checking for personal records...';
            }
            if (!this.playerHighlights) {
                return this.hasPlayerName ? 'Log a score to chase a personal record' : 'Add your player name to unlock highlights';
            }

            const { recordGames = [], newRecordCount = 0 } = this.playerHighlights;
            if (!newRecordCount) {
                return recordGames.length ? 'Personal records stand from earlier.' : 'No new personal records yet today.';
            }

            const listings = recordGames.map((entry) => `${entry.gameName} (${entry.displayScore})`).join(', ');
            return `New personal record${newRecordCount === 1 ? '' : 's'} in ${newRecordCount} game${newRecordCount === 1 ? '' : 's'}: ${listings}`;
        },
        shouldShowHighlights() {
            return true;
        }
    },
    async mounted() {
        if (this.playerStore?.loadFromStorage) {
            this.playerStore.loadFromStorage();
        }
        await this.loadStats();
        await this.loadPlayerHighlights();
    },
    methods: {
        onScoreSubmitted() {
            this.refreshTrigger++;
            this.loadStats();
            this.loadPlayerHighlights(true);
        },
        onDateChange() {
            // Trigger refresh when date changes
            this.refreshTrigger++;
            this.loadPlayerHighlights();
        },
        async loadStats() {
            try {
                // Load games count
                const games = await this.gameService.getGames();
                this.gamesCount = games.length;
                this.availableGames = games;

                // Load total scores and active players across all games
                let allScores = [];
                for (const game of games) {
                    const gameScores = await this.gameService.getGameScores(game.id);
                    allScores = [...allScores, ...gameScores];
                }

                this.totalScores = allScores.length;
                this.activePlayers = new Set(allScores.map((score) => score.playerName)).size;
            } catch (error) {
                console.error('Error loading stats:', error);
            }
        },
        async onFullRefresh() {
            this.isRefreshing = true;
            try {
                // Trigger refresh for all components
                this.refreshTrigger++;

                // Reload stats
                await this.loadStats();
                await this.loadPlayerHighlights(true);

                // Show success message
                this.$toast.add({
                    severity: 'success',
                    summary: 'Refreshed',
                    detail: 'All game data and times have been updated',
                    life: 3000
                });
            } catch (error) {
                console.error('Error during full refresh:', error);
                this.$toast.add({
                    severity: 'error',
                    summary: 'Refresh Failed',
                    detail: 'Failed to refresh game data',
                    life: 5000
                });
            } finally {
                this.isRefreshing = false;
            }
        },

        async loadPlayerHighlights(force = false) {
            if (!this.playerStore?.playerName?.trim()) {
                this.playerHighlights = null;
                this.highlightError = null;
                this.highlightLoading = false;
                return;
            }

            if (force) {
                this.highlightAllScoresCache = {};
                this.highlightDailyScoresCache = {};
            }

            this.highlightLoading = true;
            this.highlightError = null;

            try {
                const games = this.availableGames.length ? this.availableGames : await this.gameService.getGames();
                if (!this.availableGames.length) {
                    this.availableGames = games;
                }

                const selectedDateObj = this.selectedDate ? new Date(this.selectedDate) : new Date();
                selectedDateObj.setHours(0, 0, 0, 0);
                const playerNameLower = this.playerStore.playerName.trim().toLowerCase();
                const lookbackDays = 7;

                const summary = {
                    topSpotsToday: 0,
                    topSpotsYesterday: 0,
                    topSpots7Day: 0,
                    topSpotGames: [],
                    podiumCount7Day: 0,
                    recordGames: [],
                    newRecordCount: 0,
                    totalGamesToday: 0
                };

                let rankSum7Day = 0;
                let rankSamples7Day = 0;

                for (const game of games) {
                    const todaysScores = await this.getScoresForDateHighlight(game.id, selectedDateObj, { force });
                    if (!todaysScores?.length) {
                        continue;
                    }

                    const bestMapToday = this.buildBestScoreMap(todaysScores, game.scoringType);
                    const placementToday = this.getPlacementFromBestMap(bestMapToday, playerNameLower, game.scoringType);

                    if (placementToday) {
                        summary.totalGamesToday++;
                    }

                    for (let dayOffset = 0; dayOffset < lookbackDays; dayOffset++) {
                        const targetDate = new Date(selectedDateObj);
                        targetDate.setDate(targetDate.getDate() - dayOffset);

                        let bestMap;
                        if (dayOffset === 0) {
                            bestMap = bestMapToday;
                        } else {
                            const dailyScores = await this.getScoresForDateHighlight(game.id, targetDate, { force });
                            if (!dailyScores?.length) {
                                continue;
                            }
                            bestMap = this.buildBestScoreMap(dailyScores, game.scoringType);
                        }

                        if (!bestMap?.size) {
                            continue;
                        }

                        const placement = this.getPlacementFromBestMap(bestMap, playerNameLower, game.scoringType);
                        if (!placement) {
                            continue;
                        }

                        if (placement.rank <= 3) {
                            summary.podiumCount7Day++;
                        }

                        if (placement.rank === 1) {
                            summary.topSpots7Day++;
                            if (dayOffset === 0) {
                                summary.topSpotsToday++;
                                summary.topSpotGames.push({
                                    gameName: game.name,
                                    isTie: placement.isTieForTop
                                });
                            } else if (dayOffset === 1) {
                                summary.topSpotsYesterday++;
                            }
                        }

                        rankSum7Day += placement.rank;
                        rankSamples7Day++;
                    }

                    const allScores = await this.getAllScoresForHighlight(game.id, force);
                    if (placementToday) {
                        const personalInfo = this.evaluatePersonalBest(allScores, playerNameLower, selectedDateObj, game.scoringType);
                        if (personalInfo?.isNewRecord && placementToday.score && personalInfo.scoreIds.includes(placementToday.score.id)) {
                            summary.recordGames.push({
                                gameName: game.name,
                                displayScore: this.formatScoreForHighlight(placementToday.score, game.scoringType)
                            });
                        }
                    }
                }

                summary.rankSamples7Day = rankSamples7Day;
                summary.avgRank7Day = rankSamples7Day ? rankSum7Day / rankSamples7Day : null;
                summary.newRecordCount = summary.recordGames.length;
                summary.topSpotDelta = summary.topSpotsToday - summary.topSpotsYesterday;

                this.playerHighlights = summary;
            } catch (error) {
                console.error('Error building player highlights:', error);
                this.highlightError = 'Highlights are temporarily unavailable.';
                this.playerHighlights = null;
            } finally {
                this.highlightLoading = false;
            }
        },
        buildBestScoreMap(scores, scoringType) {
            const map = new Map();
            scores.forEach((score) => {
                if (!score.playerName) {
                    return;
                }
                const key = score.playerName.trim().toLowerCase();
                if (!key) {
                    return;
                }
                const normalized = this.normalizeScoreValueForHighlights(score, scoringType);
                const existing = map.get(key);
                if (!existing || this.isStrictlyBetterHighlight(normalized, existing.value, scoringType)) {
                    map.set(key, {
                        score,
                        value: normalized,
                        originalName: score.playerName
                    });
                }
            });
            return map;
        },
        getPlacementFromBestMap(bestMap, playerNameLower, scoringType) {
            if (!bestMap || bestMap.size === 0) {
                return null;
            }

            const entries = [...bestMap.entries()].map(([key, entry]) => ({
                key,
                value: entry.value,
                score: entry.score
            }));

            if (!entries.length) {
                return null;
            }

            entries.sort((a, b) => {
                if (scoringType === 1 || scoringType === 2) {
                    return a.value - b.value;
                }
                return b.value - a.value;
            });

            entries[0].rank = 1;
            for (let i = 1; i < entries.length; i++) {
                const prev = entries[i - 1];
                const curr = entries[i];
                if (this.isStrictlyBetterHighlight(prev.value, curr.value, scoringType)) {
                    curr.rank = i + 1;
                } else {
                    curr.rank = prev.rank;
                }
            }

            const playerEntry = entries.find((entry) => entry.key === playerNameLower);
            if (!playerEntry) {
                return null;
            }

            const topEntrants = entries.filter((entry) => entry.rank === 1);
            return {
                rank: playerEntry.rank,
                isTieForTop: topEntrants.length > 1,
                score: playerEntry.score,
                value: playerEntry.value
            };
        },
        async getScoresForDateHighlight(gameId, date, options = {}) {
            if (!gameId || !date) {
                return [];
            }

            const { force = false } = options;
            const dateKey = `${gameId}|${this.getDateKey(date)}`;
            if (!force && this.highlightDailyScoresCache[dateKey]) {
                return this.highlightDailyScoresCache[dateKey];
            }

            try {
                const scores = await this.gameService.getGameScores(gameId, date);
                this.highlightDailyScoresCache[dateKey] = Array.isArray(scores) ? scores : [];
            } catch (error) {
                console.error('Error fetching daily scores for highlights:', error);
                this.highlightDailyScoresCache[dateKey] = [];
            }

            return this.highlightDailyScoresCache[dateKey];
        },
        async getAllScoresForHighlight(gameId, force = false) {
            if (!gameId) {
                return [];
            }
            if (!force && this.highlightAllScoresCache[gameId]) {
                return this.highlightAllScoresCache[gameId];
            }

            try {
                const scores = await this.gameService.getGameScores(gameId);
                this.highlightAllScoresCache[gameId] = Array.isArray(scores) ? scores : [];
            } catch (error) {
                console.error('Error fetching historical scores for highlights:', error);
                this.highlightAllScoresCache[gameId] = [];
            }

            return this.highlightAllScoresCache[gameId];
        },
        evaluatePersonalBest(allScores, playerNameLower, selectedDate, scoringType) {
            if (!Array.isArray(allScores) || !allScores.length) {
                return null;
            }

            const { start, end } = this.getDayBounds(selectedDate);
            let hasHistoricalBest = false;
            let bestBeforeValue = Number.POSITIVE_INFINITY;
            let bestTodayValue = Number.POSITIVE_INFINITY;
            const todayBestIds = [];

            allScores.forEach((score) => {
                if (!score.playerName || score.playerName.trim().toLowerCase() !== playerNameLower) {
                    return;
                }

                const value = this.normalizeScoreValueForHighlights(score, scoringType);
                if (!Number.isFinite(value)) {
                    return;
                }

                const achieved = new Date(score.dateAchieved);
                if (achieved < start) {
                    if (!hasHistoricalBest || this.isStrictlyBetterHighlight(value, bestBeforeValue, scoringType)) {
                        bestBeforeValue = value;
                        hasHistoricalBest = true;
                    }
                } else if (achieved >= start && achieved < end) {
                    if (!todayBestIds.length || this.isStrictlyBetterHighlight(value, bestTodayValue, scoringType)) {
                        bestTodayValue = value;
                        todayBestIds.length = 0;
                        todayBestIds.push(score.id);
                    } else if (!this.isStrictlyBetterHighlight(value, bestTodayValue, scoringType) && !this.isStrictlyBetterHighlight(bestTodayValue, value, scoringType)) {
                        todayBestIds.push(score.id);
                    }
                }
            });

            if (!todayBestIds.length) {
                return { isNewRecord: false, scoreIds: [] };
            }

            const isNewRecord = !hasHistoricalBest || this.isStrictlyBetterHighlight(bestTodayValue, bestBeforeValue, scoringType);

            return {
                isNewRecord,
                scoreIds: todayBestIds,
                bestTodayValue
            };
        },
        getDateKey(date) {
            const d = new Date(date);
            const year = d.getFullYear();
            const month = String(d.getMonth() + 1).padStart(2, '0');
            const day = String(d.getDate()).padStart(2, '0');
            return `${year}-${month}-${day}`;
        },

        getDayBounds(date) {
            const start = new Date(date);
            start.setHours(0, 0, 0, 0);
            const end = new Date(start);
            end.setDate(end.getDate() + 1);
            return { start, end };
        },
        normalizeScoreValueForHighlights(score, scoringType) {
            if (scoringType === 1) {
                if (!Number.isFinite(score?.guessCount) || score.guessCount === 99) {
                    return Number.POSITIVE_INFINITY;
                }
                return score.guessCount;
            }

            if (scoringType === 2) {
                if (Number.isFinite(score?.score)) {
                    return score.score;
                }
                return this.timeStringToSeconds(score?.completionTime);
            }

            return Number.isFinite(score?.score) ? score.score : 0;
        },
        isStrictlyBetterHighlight(candidate, existing, scoringType) {
            if (!Number.isFinite(existing)) {
                return true;
            }

            if (scoringType === 1 || scoringType === 2) {
                return candidate < existing;
            }

            return candidate > existing;
        },
        timeStringToSeconds(timeSpan) {
            if (!timeSpan || typeof timeSpan !== 'string') {
                return Number.POSITIVE_INFINITY;
            }

            const parts = timeSpan.split(':').map((value) => Number.parseInt(value, 10));
            if (parts.some((part) => Number.isNaN(part))) {
                return Number.POSITIVE_INFINITY;
            }

            while (parts.length < 3) {
                parts.unshift(0);
            }

            const [hours, minutes, seconds] = parts;
            return hours * 3600 + minutes * 60 + seconds;
        },
        formatScoreForHighlight(score, scoringType) {
            if (scoringType === 1) {
                if (score.guessCount === 99) {
                    return 'DNF';
                }
                if (!Number.isFinite(score.guessCount)) {
                    return 'N/A';
                }
                return `${score.guessCount} guess${score.guessCount === 1 ? '' : 'es'}`;
            }

            if (scoringType === 2) {
                return this.formatCompletionTimeSpan(score.completionTime);
            }

            return Number.isFinite(score.score) ? score.score.toLocaleString() : 'N/A';
        },
        formatCompletionTimeSpan(timeSpan) {
            if (!timeSpan) {
                return 'N/A';
            }

            const parts = timeSpan.split(':');
            const hours = Number.parseInt(parts[0] || '0', 10);
            const minutes = Number.parseInt(parts[1] || '0', 10);
            const seconds = Math.round(Number.parseFloat(parts[2] || '0'));

            if (hours > 0) {
                return `${hours}h ${minutes}m ${seconds}s`;
            }
            if (minutes > 0) {
                return `${minutes}m ${seconds}s`;
            }
            return `${seconds}s`;
        }
    }
};
</script>

<template>
    <div class="game-scores-page">
        <div class="mb-6">
            <!-- Header -->
            <div class="flex flex-col md:flex-row md:items-center md:justify-between gap-4 mb-6">
                <div>
                    <h1 class="text-3xl font-bold text-gray-900 mb-2">LinkedIn Game Scores</h1>
                    <p class="text-gray-600">Track your daily scores and compete with others!</p>
                </div>
            </div>

            <!-- Centralized Date Selection -->
            <Card class="mb-6">
                <template #content>
                    <div class="flex flex-col md:flex-row md:items-center md:justify-center gap-4">
                        <div class="flex items-center gap-3">
                            <i class="pi pi-calendar text-blue-600 text-xl"></i>
                            <span class="text-lg font-medium text-gray-700">Select Date:</span>
                            <Calendar v-model="selectedDate" dateFormat="mm/dd/yy" :showIcon="true" placeholder="Select date" @date-select="onDateChange" class="w-48" />
                            <Button
                                label="Today"
                                @click="
                                    setToday();
                                    onDateChange();
                                "
                                severity="secondary"
                                class="px-4 py-2"
                            />
                            <Button label="Refresh" icon="pi pi-refresh" @click="onFullRefresh" severity="info" class="px-4 py-2" :loading="isRefreshing" title="Refresh all game data and times" />
                        </div>
                    </div>
                    <div class="text-center text-lg font-medium text-blue-600 mt-4">📅 Viewing scores for {{ formatSelectedDate() }}</div>
                </template>
            </Card>

            <div v-if="shouldShowHighlights" class="mt-6">
                <Card class="highlights-card">
                    <template #title>
                        <div class="flex items-center gap-2">
                            <i class="pi pi-bolt text-yellow-500"></i>
                            Today's Highlights
                        </div>
                    </template>
                    <template #content>
                        <div v-if="!hasPlayerName" class="text-sm text-gray-600">Add your player name in the submission form to unlock personalized insights.</div>
                        <div v-else-if="highlightError" class="text-sm text-red-600">
                            {{ highlightError }}
                        </div>
                        <div v-else>
                            <div class="grid grid-cols-1 md:grid-cols-4 gap-4">
                                <div class="highlight-tile">
                                    <div class="highlight-label">Top Spot Wins</div>
                                    <div class="highlight-value">{{ highlightTopSpotSummary }}</div>
                                </div>
                                <div class="highlight-tile">
                                    <div class="highlight-label">Seven-Day Podiums</div>
                                    <div class="highlight-value">{{ highlightPodiumSummary }}</div>
                                </div>
                                <div class="highlight-tile">
                                    <div class="highlight-label">Personal Records</div>
                                    <div class="highlight-value">{{ highlightRecordsSummary }}</div>
                                </div>
                                <div class="highlight-tile">
                                    <div class="highlight-label">Photo Finishes</div>
                                    <div class="highlight-value">
                                        <PhotoFinishAlerts :refreshTrigger="refreshTrigger" />
                                    </div>
                                </div>
                            </div>
                        </div>
                    </template>
                </Card>
            </div>
        </div>

        <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
            <!-- Score Submission Form -->
            <div class="lg:col-span-1">
                <ScoreSubmissionForm @scoreSubmitted="onScoreSubmitted" />
            </div>

            <!-- Leaderboards -->
            <div class="lg:col-span-2">
                <GameTabs :refreshTrigger="refreshTrigger" />
            </div>
        </div>

        <div class="mt-8">
            <Card>
                <template #title>Quick Stats</template>
                <template #content>
                    <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
                        <div class="text-center p-4 bg-blue-50 rounded-lg">
                            <div class="text-2xl font-bold text-blue-600">{{ totalScores }}</div>
                            <div class="text-sm text-gray-600">Total Scores</div>
                        </div>
                        <div class="text-center p-4 bg-green-50 rounded-lg">
                            <div class="text-2xl font-bold text-green-600">{{ activePlayers }}</div>
                            <div class="text-sm text-gray-600">Active Players</div>
                        </div>
                        <div class="text-center p-4 bg-purple-50 rounded-lg">
                            <div class="text-2xl font-bold text-purple-600">{{ gamesCount }}</div>
                            <div class="text-sm text-gray-600">Games Available</div>
                        </div>
                    </div>
                </template>
            </Card>
        </div>

        <div class="mt-8">
            <WinsTrendChart :days="7" :top="5" :refreshKey="refreshTrigger" />
        </div>

        <!-- Analytics Dashboard -->
        <div class="mt-8">
            <div class="mb-6 text-center">
                <h2 class="text-2xl font-bold text-gray-900 mb-2">Game Analytics Dashboard</h2>
                <p class="text-gray-600">Deep dive into game statistics and player performance</p>
            </div>

            <div class="grid grid-cols-1 lg:grid-cols-2 xl:grid-cols-3 gap-6">
                <CloseCallsWidget :days="7" :refreshTrigger="refreshTrigger" />
                <ComebackKingsWidget :days="14" :refreshTrigger="refreshTrigger" />
                <ConsistencyChampionsWidget :days="30" :minScores="5" :refreshTrigger="refreshTrigger" />
            </div>

            <div class="mt-6">
                <DistributionChart :days="30" :refreshTrigger="refreshTrigger" />
            </div>
        </div>
    </div>
</template>

<style scoped>
.game-scores-page {
    @apply p-6 max-w-7xl mx-auto;
}

.highlights-card {
    @apply border border-blue-100 bg-blue-50/40;
}

.highlight-tile {
    @apply rounded-xl bg-white shadow-sm p-4 border border-blue-100 flex flex-col gap-2 min-h-[96px];
}

.highlight-label {
    @apply text-xs font-semibold uppercase tracking-wide text-blue-600;
}

.highlight-value {
    @apply text-base font-semibold text-gray-900;
}

.highlight-loading {
    @apply text-sm text-gray-500;
}
</style>
