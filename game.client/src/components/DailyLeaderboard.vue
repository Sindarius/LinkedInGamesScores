<script>
import { GameService } from '@/services/gameService.js';
import { useDateStore } from '@/stores/dateStore.js';
import TemperatureIndicator from '@/components/analytics/TemperatureIndicator.vue';

export default {
    name: 'DailyLeaderboard',
    components: {
        TemperatureIndicator
    },
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
                y: 0,
                scoreId: null
            },
            isMobile: false,
            scoreInsights: {},
            cachedScoresByDate: {},
            cachedAllScores: {}
        };
    },
    mounted() {
        // Detect if device is mobile
        this.isMobile = /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent) || window.innerWidth <= 768;

        // Add event listener for mobile orientation changes
        window.addEventListener('resize', () => {
            this.isMobile = window.innerWidth <= 768;
        });

        // Add global mouse leave detection to fix stuck hover states
        document.addEventListener('mouseleave', this.clearHoverThumbnail);
        window.addEventListener('scroll', this.clearHoverThumbnail);
        window.addEventListener('resize', this.clearHoverThumbnail);
    },
    beforeUnmount() {
        // Clean up hover thumbnail and event listeners
        this.clearHoverThumbnail();
        document.removeEventListener('mouseleave', this.clearHoverThumbnail);
        window.removeEventListener('scroll', this.clearHoverThumbnail);
        window.removeEventListener('resize', this.clearHoverThumbnail);
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
                    if (this.scores.length) {
                        await this.buildScoreInsights(this.scores);
                    }
                } catch (error) {
                    console.error('Error loading game info:', error);
                }
            } else {
                this.selectedGame = null;
                if (this.scores.length) {
                    await this.buildScoreInsights(this.scores);
                }
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

                await this.buildScoreInsights(this.scores);
            } catch (error) {
                console.error('Error loading leaderboard:', error);
                this.scoreInsights = {};
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

            // Clear any existing hover thumbnail first
            this.clearHoverThumbnail();

            try {
                const thumbnailBlob = await this.gameService.getScoreImageThumbnail(scoreId, 400, 300);
                if (thumbnailBlob) {
                    this.hoverThumbnail.url = URL.createObjectURL(thumbnailBlob);
                    this.hoverThumbnail.x = event.clientX + 10;
                    this.hoverThumbnail.y = event.clientY + 10;
                    this.hoverThumbnail.visible = true;
                    this.hoverThumbnail.scoreId = scoreId; // Track which image we're showing
                }
            } catch (error) {
                console.error('Error loading thumbnail:', error);
                this.clearHoverThumbnail();
            }
        },
        handleImageHoverOut() {
            // Use a small delay to prevent flicker when moving between hover area and thumbnail
            setTimeout(() => {
                this.clearHoverThumbnail();
            }, 100);
        },
        clearHoverThumbnail() {
            if (this.hoverThumbnail.url) {
                URL.revokeObjectURL(this.hoverThumbnail.url);
            }
            this.hoverThumbnail.visible = false;
            this.hoverThumbnail.url = null;
            this.hoverThumbnail.x = 0;
            this.hoverThumbnail.y = 0;
            this.hoverThumbnail.scoreId = null;
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

        async buildScoreInsights(scores) {
            if (!Array.isArray(scores) || scores.length === 0) {
                this.scoreInsights = {};
                return;
            }

            const selectedDateObj = this.selectedDate ? new Date(this.selectedDate) : new Date();
            const scoringType = this.selectedGame?.scoringType ?? scores[0]?.scoringType ?? 1;

            let previousDayRanks = new Map();
            let personalRecordMap = new Map();

            if (this.gameId && this.selectedGame) {
                try {
                    const previousDate = this.getPreviousDate(selectedDateObj);
                    const [yesterdayScores, allScores] = await Promise.all([this.getScoresForDate(this.gameId, previousDate), this.getAllScoresForGame(this.gameId)]);

                    previousDayRanks = this.calculateRankings(yesterdayScores, scoringType);
                    personalRecordMap = this.calculatePersonalRecordMap(allScores, scoringType, selectedDateObj);
                } catch (error) {
                    console.error('Error building score insights:', error);
                }
            }

            const insights = {};

            scores.forEach((score, index) => {
                const { actualRank } = this.getRankInfo(index, score, scores);
                const chips = [];
                const previousRank = previousDayRanks.get(score.playerName);

                if (typeof previousRank === 'number' && Number.isFinite(previousRank)) {
                    const delta = previousRank - actualRank;
                    if (delta > 0) {
                        chips.push({ label: `+${delta} vs yesterday`, severity: 'success', key: `delta-${score.id}` });
                    } else if (delta < 0) {
                        chips.push({ label: `${delta} vs yesterday`, severity: 'danger', key: `delta-${score.id}` });
                    } else {
                        chips.push({ label: 'Even with yesterday', severity: 'info', key: `delta-${score.id}` });
                    }
                }

                const personalRecord = personalRecordMap.get(score.playerName);
                if (personalRecord?.isNewRecord && personalRecord.todayBestIds.includes(score.id)) {
                    chips.push({ label: 'New personal record', severity: 'success', key: `record-${score.id}` });
                }

                insights[score.id] = {
                    chips,
                    actualRank,
                    canShare: actualRank <= 3,
                    shareSummary: this.buildShareSummary(score, selectedDateObj)
                };
            });

            this.scoreInsights = insights;
        },
        async getScoresForDate(gameId, date) {
            if (!gameId || !date) {
                return [];
            }

            const dateKey = `${gameId}|${this.getDateKey(date)}`;
            if (this.cachedScoresByDate[dateKey]) {
                return this.cachedScoresByDate[dateKey];
            }

            try {
                const scores = await this.gameService.getGameScores(gameId, date);
                this.cachedScoresByDate[dateKey] = Array.isArray(scores) ? scores : [];
            } catch (error) {
                console.error('Error fetching scores for date:', error);
                this.cachedScoresByDate[dateKey] = [];
            }

            return this.cachedScoresByDate[dateKey];
        },
        async getAllScoresForGame(gameId) {
            if (!gameId) {
                return [];
            }

            if (this.cachedAllScores[gameId]) {
                return this.cachedAllScores[gameId];
            }

            try {
                const scores = await this.gameService.getGameScores(gameId);
                this.cachedAllScores[gameId] = Array.isArray(scores) ? scores : [];
            } catch (error) {
                console.error('Error fetching all scores for game:', error);
                this.cachedAllScores[gameId] = [];
            }

            return this.cachedAllScores[gameId];
        },
        getDateKey(date) {
            const d = new Date(date);
            const year = d.getFullYear();
            const month = String(d.getMonth() + 1).padStart(2, '0');
            const day = String(d.getDate()).padStart(2, '0');
            return `${year}-${month}-${day}`;
        },
        getPreviousDate(date) {
            const prev = new Date(date);
            prev.setDate(prev.getDate() - 1);
            return prev;
        },
        getLocalDayBounds(date) {
            const start = new Date(date);
            start.setHours(0, 0, 0, 0);
            const end = new Date(start);
            end.setDate(end.getDate() + 1);
            return { start, end };
        },
        calculateRankings(scores, scoringType) {
            const ranks = new Map();
            if (!Array.isArray(scores) || scores.length === 0) {
                return ranks;
            }

            const sorted = [...scores].sort((a, b) => {
                const aValue = this.normalizeScoreValue(a, scoringType);
                const bValue = this.normalizeScoreValue(b, scoringType);
                if (scoringType === 1 || scoringType === 2) {
                    return aValue - bValue;
                }
                return bValue - aValue;
            });

            sorted.forEach((score, index) => {
                const rank = this.getActualRankForScores(sorted, index, scoringType);
                if (!ranks.has(score.playerName)) {
                    ranks.set(score.playerName, rank);
                }
            });

            return ranks;
        },
        getActualRankForScores(sortedScores, index, scoringType) {
            if (index === 0) {
                return 1;
            }

            let actualRank = 1;
            const currentValue = this.normalizeScoreValue(sortedScores[index], scoringType);

            for (let i = 0; i < index; i++) {
                const compareValue = this.normalizeScoreValue(sortedScores[i], scoringType);
                if (this.isStrictlyBetter(compareValue, currentValue, scoringType)) {
                    actualRank++;
                }
            }

            return actualRank;
        },
        normalizeScoreValue(score, scoringType) {
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
        isStrictlyBetter(candidate, existing, scoringType) {
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
        calculatePersonalRecordMap(allScores, scoringType, selectedDate) {
            const records = new Map();
            if (!Array.isArray(allScores) || allScores.length === 0) {
                return records;
            }

            const { start, end } = this.getLocalDayBounds(selectedDate);
            const grouped = allScores.reduce((acc, score) => {
                const key = score.playerName || 'Unknown';
                acc[key] = acc[key] || [];
                acc[key].push(score);
                return acc;
            }, {});

            Object.entries(grouped).forEach(([playerName, scores]) => {
                let bestBeforeValue = Number.POSITIVE_INFINITY;
                let hasHistoricalBest = false;
                let bestTodayValue = Number.POSITIVE_INFINITY;
                const todayBestIds = [];

                scores.forEach((score) => {
                    const value = this.normalizeScoreValue(score, scoringType);
                    if (!Number.isFinite(value)) {
                        return;
                    }

                    const achieved = new Date(score.dateAchieved);
                    if (achieved < start) {
                        if (!hasHistoricalBest || this.isStrictlyBetter(value, bestBeforeValue, scoringType)) {
                            bestBeforeValue = value;
                            hasHistoricalBest = true;
                        }
                    } else if (achieved >= start && achieved < end) {
                        if (todayBestIds.length === 0 || this.isStrictlyBetter(value, bestTodayValue, scoringType)) {
                            bestTodayValue = value;
                            todayBestIds.length = 0;
                            todayBestIds.push(score.id);
                        } else if (!this.isStrictlyBetter(value, bestTodayValue, scoringType) && !this.isStrictlyBetter(bestTodayValue, value, scoringType)) {
                            todayBestIds.push(score.id);
                        }
                    }
                });

                const hasToday = todayBestIds.length > 0;
                const isNewRecord = hasToday && (!hasHistoricalBest || this.isStrictlyBetter(bestTodayValue, bestBeforeValue, scoringType));

                records.set(playerName, {
                    isNewRecord,
                    todayBestIds,
                    bestTodayValue: hasToday ? bestTodayValue : null
                });
            });

            return records;
        },
        formatScoreSummary(score, overrideType = null) {
            const scoringType = overrideType ?? score.scoringType ?? this.selectedGame?.scoringType ?? 1;

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
                return this.formatCompletionTime(score.completionTime);
            }

            return Number.isFinite(score.score) ? score.score.toLocaleString() : 'N/A';
        },
        buildShareSummary(score, selectedDateObj = new Date()) {
            const scoringType = score.scoringType ?? this.selectedGame?.scoringType ?? 1;
            const gameName = this.selectedGame?.name || score.gameName || 'LinkedIn Game Scores';
            const formattedScore = this.formatScoreSummary(score, scoringType);
            const dateKey = this.getDateKey(selectedDateObj);
            const origin = typeof window !== 'undefined' && window.location ? window.location.origin : 'https://www.linkedin.com';
            const url = `${origin}/?game=${score.gameId}&date=${dateKey}`;
            const text = `I just logged ${formattedScore} in ${gameName} on LinkedIn Game Scores!`;

            return { text, url, gameName, formattedScore };
        },
        async shareScore(score) {
            const insight = this.scoreInsights?.[score.id];
            if (!insight || !insight.canShare) {
                this.$toast.add({
                    severity: 'info',
                    summary: 'Share',
                    detail: 'Only the top three players can share directly from the leaderboard.',
                    life: 2500
                });
                return;
            }

            const payload = insight.shareSummary;
            const shareData = {
                title: 'LinkedIn Game Scores',
                text: payload.text,
                url: payload.url
            };

            try {
                if (typeof navigator !== 'undefined' && navigator.share) {
                    await navigator.share(shareData);
                    this.$toast.add({
                        severity: 'success',
                        summary: 'Ready to share',
                        detail: 'Your score is in the share sheet.',
                        life: 2200
                    });
                } else if (typeof window !== 'undefined') {
                    const linkedInUrl = `https://www.linkedin.com/shareArticle?mini=true&url=${encodeURIComponent(payload.url)}&title=${encodeURIComponent('LinkedIn Game Scores')}&summary=${encodeURIComponent(payload.text)}`;
                    window.open(linkedInUrl, '_blank', 'noopener');
                }
            } catch (error) {
                if (error?.name !== 'AbortError') {
                    console.error('Error sharing score:', error);
                    this.$toast.add({
                        severity: 'warn',
                        summary: 'Share canceled',
                        detail: 'We could not complete the share.',
                        life: 2500
                    });
                }
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
                            <div class="flex flex-col gap-2">
                                <div class="flex items-start justify-between gap-3">
                                    <div class="flex items-center">
                                        <Avatar :label="data.playerName.charAt(0).toUpperCase()" class="mr-2" size="small" />
                                        <div>
                                            <div class="font-medium">{{ data.playerName }}</div>
                                            <div v-if="data.linkedInProfileUrl" class="text-xs text-blue-600">
                                                <a :href="data.linkedInProfileUrl" target="_blank" class="hover:underline"> LinkedIn Profile </a>
                                            </div>
                                            <div class="mt-1">
                                                <TemperatureIndicator :playerName="data.playerName" :days="7" :refreshTrigger="refreshTrigger" />
                                            </div>
                                        </div>
                                    </div>
                                    <Button v-if="scoreInsights[data.id]?.canShare" icon="pi pi-share-alt" label="Share" size="small" text class="share-button" @click="shareScore(data)" :aria-label="`Share ${data.playerName}'s score to LinkedIn`" />
                                </div>
                                <div v-if="scoreInsights[data.id]?.chips?.length" class="flex flex-wrap gap-2 pl-10 md:pl-12">
                                    <span v-for="chip in scoreInsights[data.id].chips" :key="chip.key" class="info-chip" :class="`info-chip--${chip.severity || 'info'}`">
                                        {{ chip.label }}
                                    </span>
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
                                @mouseout="clearHoverThumbnail"
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
        @mouseenter="clearHoverThumbnail"
    >
        <img :src="hoverThumbnail.url" alt="Score thumbnail" class="max-w-full max-h-32 object-contain" />
    </div>
</template>

<style scoped>
.pi-trophy {
    color: #fbbf24;
}

.info-chip {
    @apply text-xs font-semibold px-3 py-1 rounded-full bg-blue-50 text-blue-700 whitespace-nowrap;
}

.info-chip--success {
    @apply bg-green-50 text-green-700;
}

.info-chip--danger {
    @apply bg-red-50 text-red-700;
}

.info-chip--info {
    @apply bg-blue-50 text-blue-700;
}

.share-button :deep(.p-button) {
    @apply px-3 py-1;
}
</style>
