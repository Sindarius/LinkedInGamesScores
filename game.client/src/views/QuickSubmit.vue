<script>
import { ref, computed, onMounted } from 'vue';
import { useToast } from 'primevue/usetoast';
import { usePlayerStore } from '@/stores/playerStore.js';
import { GameService } from '@/services/gameService.js';
import { ocrService } from '@/services/ocrService.js';

export default {
    name: 'QuickSubmit',
    emits: ['scoreSubmitted'],
    setup(props, { emit }) {
        const toast = useToast();
        const playerStore = usePlayerStore();
        const gameService = new GameService();

        const isSubmittingAll = ref(false);
        const gameEntries = ref([]);

        const playerName = computed({
            get: () => playerStore.playerName,
            set: (value) => playerStore.setPlayerName(value)
        });

        const linkedinUrl = computed({
            get: () => playerStore.linkedInProfileUrl,
            set: (value) => playerStore.setLinkedInUrl(value)
        });

        onMounted(async () => {
            playerStore.loadFromStorage();
            await loadGames();
        });

        const loadGames = async () => {
            try {
                const games = await gameService.getGames();
                gameEntries.value = games.map((game) => ({
                    game,
                    scoreImage: null,
                    imagePreview: null,
                    guessCount: null,
                    minutes: null,
                    seconds: null,
                    ranOutOfGuesses: false,
                    isParsingOCR: false,
                    isDragging: false,
                    status: 'pending' // pending | ready | submitted | error
                }));
            } catch (error) {
                console.error('Error loading games:', error);
                toast.add({ severity: 'error', summary: 'Error', detail: 'Failed to load games' });
            }
        };

        const handleFileDrop = (entry, event) => {
            entry.isDragging = false;
            const file = event.dataTransfer?.files?.[0];
            if (file) processFile(entry, file);
        };

        const handleFileSelect = (entry, event) => {
            const file = event.target?.files?.[0];
            if (file) processFile(entry, file);
            event.target.value = '';
        };

        const processFile = (entry, file) => {
            const allowedTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/gif'];
            if (!allowedTypes.includes(file.type)) {
                toast.add({ severity: 'error', summary: 'Invalid File', detail: 'Only JPEG, PNG, GIF allowed' });
                return;
            }
            if (file.size > 5 * 1024 * 1024) {
                toast.add({ severity: 'error', summary: 'File Too Large', detail: 'Max 5MB per image' });
                return;
            }

            entry.scoreImage = file;
            const reader = new FileReader();
            reader.onload = (e) => {
                entry.imagePreview = e.target.result;
                autoDetectScore(entry);
            };
            reader.readAsDataURL(file);
        };

        const removeImage = (entry) => {
            entry.scoreImage = null;
            entry.imagePreview = null;
            if (entry.status !== 'submitted') {
                entry.status = 'pending';
            }
        };

        const autoDetectScore = async (entry) => {
            entry.isParsingOCR = true;
            try {
                const text = await ocrService.extractTextFromImage(entry.scoreImage);
                const gameType = entry.game.scoringType === 1 ? 'guess' : 'time';

                // Try ocrService parser first
                const parsed = ocrService.parseLinkedInGameScore(text, gameType);
                if (parsed) {
                    applyScore(entry, parsed);
                    if (isEntryValid(entry)) entry.status = 'ready';
                    return;
                }

                // Fallback: extract from lines
                const lines = text
                    .split(/\r?\n/)
                    .map((l) => l.trim())
                    .filter((l) => l.length);
                const cleanLines = lines.filter((l) => !l.toLowerCase().includes('avg') && !l.toLowerCase().includes('average'));

                const result = gameType === 'time' ? extractTimeScore(cleanLines) : extractGuessScore(cleanLines);
                if (result) {
                    applyScore(entry, result);
                    if (isEntryValid(entry)) entry.status = 'ready';
                }
            } catch (e) {
                console.error('OCR failed for', entry.game.name, e);
            } finally {
                entry.isParsingOCR = false;
            }
        };

        const applyScore = (entry, result) => {
            if (result.type === 'time' || result.minutes !== undefined) {
                entry.minutes = result.minutes ?? null;
                entry.seconds = result.seconds ?? null;
                entry.ranOutOfGuesses = false;
                entry.guessCount = null;
            } else if (result.type === 'guess' || result.guessCount !== undefined || result.isDNF) {
                if (result.isDNF) {
                    entry.ranOutOfGuesses = true;
                    entry.guessCount = null;
                } else {
                    entry.guessCount = result.guessCount ?? null;
                    entry.ranOutOfGuesses = false;
                }
                entry.minutes = null;
                entry.seconds = null;
            }
        };

        const extractTimeScore = (lines) => {
            for (const line of lines) {
                const cleanLine = line.replace(/solved\s+in\s*/gi, '').trim();
                for (const pattern of [/^(\d{1,2}):(\d{2})$/, /^\s*(\d{1,2}):(\d{2})\s*$/]) {
                    const match = cleanLine.match(pattern);
                    if (match) {
                        const m = parseInt(match[1]);
                        const s = parseInt(match[2]);
                        if (m >= 0 && m < 60 && s >= 0 && s < 60) return { type: 'time', minutes: m, seconds: s };
                    }
                }
            }
            return null;
        };

        const extractGuessScore = (lines) => {
            for (const line of lines) {
                const cleanLine = line.replace(/solved\s+in\s*/gi, '').trim();
                const lowerLine = cleanLine.toLowerCase();
                for (const pattern of [/better\s*luck\s*next\s*time/gi, /dnf/gi, /did\s*not\s*finish/gi, /x\s*\/\s*\d+/gi]) {
                    if (pattern.test(lowerLine)) return { type: 'guess', isDNF: true };
                }
                if (lowerLine.includes('guess')) {
                    const match = cleanLine.match(/(\d+)\s*guesses?/i);
                    if (match) {
                        const g = parseInt(match[1]);
                        if (g >= 1 && g <= 10) return { type: 'guess', guessCount: g };
                    }
                }
            }
            return null;
        };

        const isEntryValid = (entry) => {
            if (entry.game.scoringType === 1) {
                return entry.ranOutOfGuesses || (entry.guessCount && entry.guessCount > 0);
            } else if (entry.game.scoringType === 2) {
                const m = entry.minutes || 0;
                return entry.seconds !== null && entry.seconds >= 0 && entry.seconds < 60 && (m > 0 || entry.seconds > 0);
            }
            return false;
        };

        const onScoreInput = (entry) => {
            if (entry.status !== 'submitted') {
                entry.status = isEntryValid(entry) ? 'ready' : 'pending';
            }
        };

        const readyEntries = computed(() => gameEntries.value.filter((e) => isEntryValid(e) && e.status !== 'submitted'));

        const submittedCount = computed(() => gameEntries.value.filter((e) => e.status === 'submitted').length);

        const submitAll = async () => {
            if (!playerName.value?.trim()) {
                toast.add({ severity: 'warn', summary: 'Player Name Required', detail: 'Enter your name before submitting' });
                return;
            }
            if (!readyEntries.value.length) {
                toast.add({ severity: 'warn', summary: 'No Scores Ready', detail: 'Enter at least one score before submitting' });
                return;
            }

            isSubmittingAll.value = true;

            const results = await Promise.allSettled(readyEntries.value.map((entry) => submitEntry(entry)));

            const succeeded = results.filter((r) => r.status === 'fulfilled').length;
            const failed = results.filter((r) => r.status === 'rejected').length;

            if (succeeded > 0) {
                gameService.clearAllCaches();
                emit('scoreSubmitted');
                toast.add({
                    severity: failed ? 'warn' : 'success',
                    summary: failed ? 'Partial Success' : 'All Submitted!',
                    detail: `${succeeded} score${succeeded !== 1 ? 's' : ''} submitted${failed ? `, ${failed} failed` : ''}`,
                    life: 5000
                });
            } else {
                toast.add({ severity: 'error', summary: 'Submission Failed', detail: 'All submissions failed. Please try again.', life: 5000 });
            }

            isSubmittingAll.value = false;
        };

        const submitEntry = async (entry) => {
            const gameScore = {
                gameId: entry.game.id,
                playerName: playerName.value,
                linkedInProfileUrl: linkedinUrl.value || null
            };

            if (entry.game.scoringType === 1) {
                gameScore.guessCount = entry.ranOutOfGuesses ? 99 : entry.guessCount;
            } else if (entry.game.scoringType === 2) {
                const totalSeconds = (entry.minutes || 0) * 60 + (entry.seconds || 0);
                gameScore.completionTime = `${Math.floor(totalSeconds / 3600).toString().padStart(2, '0')}:${Math.floor((totalSeconds % 3600) / 60).toString().padStart(2, '0')}:${(totalSeconds % 60).toString().padStart(2, '0')}`;
            }

            try {
                if (entry.scoreImage) {
                    await gameService.submitScoreWithImage(gameScore, entry.scoreImage);
                } else {
                    await gameService.submitScore(gameScore);
                }
                entry.status = 'submitted';
            } catch (error) {
                entry.status = 'error';
                throw error;
            }
        };

        const resetEntry = (entry) => {
            entry.scoreImage = null;
            entry.imagePreview = null;
            entry.guessCount = null;
            entry.minutes = null;
            entry.seconds = null;
            entry.ranOutOfGuesses = false;
            entry.status = 'pending';
        };

        const triggerFileInput = (gameId) => {
            document.getElementById(`file-input-${gameId}`)?.click();
        };

        const statusLabel = (status) => {
            const map = { pending: 'Pending', ready: 'Ready', submitted: 'Submitted', error: 'Error' };
            return map[status] || status;
        };

        const statusSeverity = (status) => {
            const map = { pending: 'secondary', ready: 'warn', submitted: 'success', error: 'danger' };
            return map[status] || 'secondary';
        };

        return {
            gameEntries,
            playerName,
            linkedinUrl,
            isSubmittingAll,
            readyEntries,
            submittedCount,
            isEntryValid,
            onScoreInput,
            handleFileDrop,
            handleFileSelect,
            removeImage,
            autoDetectScore,
            submitAll,
            resetEntry,
            triggerFileInput,
            statusLabel,
            statusSeverity
        };
    }
};
</script>

<template>
    <div class="quick-submit-page">
        <div class="mb-6">
            <h1 class="text-3xl font-bold text-gray-900 mb-2">Quick Submit</h1>
            <p class="text-gray-600">Upload all your game screenshots at once and submit in one click.</p>
        </div>

        <!-- Player Info -->
        <Card class="mb-6">
            <template #title>
                <div class="flex items-center gap-2">
                    <i class="pi pi-user text-blue-600"></i>
                    Your Info
                </div>
            </template>
            <template #content>
                <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <div class="field">
                        <label class="block text-sm font-medium mb-2" for="qs-playerName">Player Name <span class="text-red-500">*</span></label>
                        <InputText id="qs-playerName" v-model="playerName" placeholder="Enter your name" class="w-full" />
                    </div>
                    <div class="field">
                        <label class="block text-sm font-medium mb-2" for="qs-linkedinUrl">LinkedIn Profile URL (Optional)</label>
                        <InputText id="qs-linkedinUrl" v-model="linkedinUrl" placeholder="https://linkedin.com/in/yourprofile" class="w-full" />
                    </div>
                </div>
            </template>
        </Card>

        <!-- Game Cards -->
        <div class="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-6 mb-6">
            <Card v-for="entry in gameEntries" :key="entry.game.id" class="game-entry-card" :class="`status-${entry.status}`">
                <template #title>
                    <div class="flex items-center justify-between">
                        <div class="flex items-center gap-2">
                            <i class="pi pi-trophy text-yellow-500"></i>
                            {{ entry.game.name }}
                        </div>
                        <Tag :value="statusLabel(entry.status)" :severity="statusSeverity(entry.status)" />
                    </div>
                </template>
                <template #content>
                    <!-- Drop Zone / Image Preview -->
                    <div v-if="!entry.imagePreview">
                        <div
                            class="drop-zone"
                            :class="{ 'drop-zone--active': entry.isDragging }"
                            @dragover.prevent="entry.isDragging = true"
                            @dragleave="entry.isDragging = false"
                            @drop.prevent="handleFileDrop(entry, $event)"
                            @click="triggerFileInput(entry.game.id)"
                        >
                            <i class="pi pi-cloud-upload text-3xl text-gray-400 mb-2"></i>
                            <p class="text-sm text-gray-500">Drop screenshot here or click to browse</p>
                            <p class="text-xs text-gray-400 mt-1">JPEG, PNG, GIF · Max 5MB</p>
                        </div>
                        <input :id="`file-input-${entry.game.id}`" type="file" accept="image/jpeg,image/jpg,image/png,image/gif" class="hidden" @change="handleFileSelect(entry, $event)" />
                    </div>

                    <div v-else class="mb-4">
                        <div class="relative">
                            <img :src="entry.imagePreview" alt="Score screenshot" class="w-full max-h-40 object-contain rounded border border-gray-200" />
                            <Button
                                icon="pi pi-times"
                                size="small"
                                severity="danger"
                                rounded
                                class="absolute top-1 right-1 !w-7 !h-7"
                                @click="removeImage(entry)"
                                aria-label="Remove image"
                            />
                        </div>
                        <div v-if="entry.isParsingOCR" class="flex items-center gap-2 mt-2 text-sm text-blue-600">
                            <i class="pi pi-spin pi-spinner"></i>
                            Auto-detecting score...
                        </div>
                        <Button
                            v-if="!entry.isParsingOCR && entry.status !== 'submitted'"
                            icon="pi pi-search"
                            label="Re-detect Score"
                            size="small"
                            severity="secondary"
                            class="w-full mt-2"
                            @click="autoDetectScore(entry)"
                        />
                    </div>

                    <!-- Score Input -->
                    <div v-if="entry.status !== 'submitted'" class="score-input-area">
                        <!-- Guess-based game -->
                        <div v-if="entry.game.scoringType === 1" class="space-y-2">
                            <label class="block text-sm font-medium">Number of Guesses</label>
                            <InputNumber
                                v-model="entry.guessCount"
                                placeholder="Guesses"
                                class="w-full"
                                :input-style="{ width: '100%' }"
                                :min="1"
                                :disabled="entry.ranOutOfGuesses"
                                @input="onScoreInput(entry)"
                            />
                            <div class="flex items-center gap-2">
                                <Checkbox :inputId="`dnf-${entry.game.id}`" v-model="entry.ranOutOfGuesses" :binary="true" @change="onScoreInput(entry)" />
                                <label :for="`dnf-${entry.game.id}`" class="text-sm cursor-pointer">Did Not Finish (DNF)</label>
                            </div>
                        </div>

                        <!-- Time-based game -->
                        <div v-else-if="entry.game.scoringType === 2">
                            <label class="block text-sm font-medium mb-2">Completion Time</label>
                            <div class="grid grid-cols-2 gap-2">
                                <div>
                                    <InputNumber v-model="entry.minutes" placeholder="0" class="w-full" :input-style="{ width: '100%' }" :min="0" :max="99" @input="onScoreInput(entry)" />
                                    <span class="text-xs text-gray-500 block text-center mt-1">Minutes</span>
                                </div>
                                <div>
                                    <InputNumber v-model="entry.seconds" placeholder="0" class="w-full" :input-style="{ width: '100%' }" :min="0" :max="59" @input="onScoreInput(entry)" />
                                    <span class="text-xs text-gray-500 block text-center mt-1">Seconds</span>
                                </div>
                            </div>
                        </div>
                    </div>

                    <!-- Submitted state -->
                    <div v-else class="flex items-center gap-2 py-4 justify-center text-green-600">
                        <i class="pi pi-check-circle text-2xl"></i>
                        <span class="font-medium">Score submitted!</span>
                        <Button label="Reset" size="small" severity="secondary" text @click="resetEntry(entry)" />
                    </div>
                </template>
            </Card>
        </div>

        <!-- Submit Bar -->
        <Card class="submit-bar sticky-footer">
            <template #content>
                <div class="flex flex-col md:flex-row md:items-center md:justify-between gap-4">
                    <div class="flex items-center gap-4 text-sm text-gray-600">
                        <span>
                            <strong class="text-green-600">{{ readyEntries.length }}</strong> ready to submit
                        </span>
                        <span v-if="submittedCount">
                            <strong class="text-blue-600">{{ submittedCount }}</strong> already submitted
                        </span>
                    </div>
                    <Button
                        :label="`Submit All (${readyEntries.length})`"
                        icon="pi pi-send"
                        size="large"
                        class="px-8"
                        :loading="isSubmittingAll"
                        :disabled="!readyEntries.length || !playerName"
                        @click="submitAll"
                    />
                </div>
            </template>
        </Card>
    </div>
</template>

<style scoped>
.quick-submit-page {
    @apply p-6 max-w-7xl mx-auto;
}

.drop-zone {
    @apply flex flex-col items-center justify-center border-2 border-dashed border-gray-300 rounded-lg p-8 cursor-pointer transition-colors;
}

.drop-zone:hover,
.drop-zone--active {
    @apply border-blue-400 bg-blue-50;
}

.score-input-area {
    @apply mt-3;
}

.game-entry-card {
    @apply transition-all duration-200 overflow-hidden;
}

.game-entry-card.status-ready {
    @apply ring-2 ring-green-300;
}

.game-entry-card.status-submitted {
    @apply ring-2 ring-blue-300 opacity-80;
}

.game-entry-card.status-error {
    @apply ring-2 ring-red-300;
}

.submit-bar {
    @apply border border-gray-200;
}
</style>
