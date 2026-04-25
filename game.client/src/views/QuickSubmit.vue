<script>
import { ref, computed, onMounted } from 'vue';
import { useToast } from 'primevue/usetoast';
import { useRouter } from 'vue-router';
import { usePlayerStore } from '@/stores/playerStore.js';
import { GameService } from '@/services/gameService.js';
import { ocrService } from '@/services/ocrService.js';

let nextId = 1;

export default {
    name: 'QuickSubmit',
    emits: ['scoreSubmitted'],
    setup(props, { emit }) {
        const toast = useToast();
        const router = useRouter();
        const playerStore = usePlayerStore();
        const gameService = new GameService();

        const games = ref([]);
        const uploads = ref([]); // array of upload entries
        const isSubmittingAll = ref(false);
        const isDragging = ref(false);
        const isProcessingOCR = ref(false); // true while any OCR is running

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
                games.value = await gameService.getGames();
            } catch (error) {
                console.error('Error loading games:', error);
                toast.add({ severity: 'error', summary: 'Error', detail: 'Failed to load games' });
            }
        };

        // ── File handling ──────────────────────────────────────────────────────

        const handleDrop = (event) => {
            isDragging.value = false;
            const files = [...(event.dataTransfer?.files || [])];
            addFiles(files);
        };

        const handleFileSelect = (event) => {
            const files = [...(event.target?.files || [])];
            addFiles(files);
            event.target.value = '';
        };

        const addFiles = (files) => {
            const imageFiles = files.filter((f) => ['image/jpeg', 'image/jpg', 'image/png', 'image/gif'].includes(f.type) && f.size <= 5 * 1024 * 1024);

            const oversized = files.filter((f) => f.size > 5 * 1024 * 1024);
            const invalid = files.filter((f) => !['image/jpeg', 'image/jpg', 'image/png', 'image/gif'].includes(f.type) && f.size <= 5 * 1024 * 1024);

            if (oversized.length) toast.add({ severity: 'warn', summary: 'Files Skipped', detail: `${oversized.length} file(s) exceed 5MB`, life: 4000 });
            if (invalid.length) toast.add({ severity: 'warn', summary: 'Files Skipped', detail: `${invalid.length} file(s) are not valid images`, life: 4000 });

            for (const file of imageFiles) {
                const entry = {
                    id: nextId++,
                    file,
                    imagePreview: null,
                    ocrText: null,
                    selectedGame: null,
                    guessCount: null,
                    minutes: null,
                    seconds: null,
                    ranOutOfGuesses: false,
                    scoreConfidence: null, // 'high' | 'medium' | null
                    detectedScoreLabel: null, // e.g. "2:45" or "3 guesses" — shown for user verification
                    status: 'queued' // queued | processing | needs-review | ready | submitted | error
                };

                // Build preview immediately
                const reader = new FileReader();
                reader.onload = (e) => (entry.imagePreview = e.target.result);
                reader.readAsDataURL(file);

                uploads.value.push(entry);
            }

            // Kick off OCR queue
            runOcrQueue();
        };

        // ── OCR helpers ────────────────────────────────────────────────────────

        // Strip lines that contain average stats — same logic as ScoreSubmissionForm.
        // LinkedIn screenshots often show "avg 1:30" or "Your average: 2:15" which
        // parseLinkedInGameScore would match before the player's actual score.
        const filterOcrText = (text) => {
            return text
                .split(/\r?\n/)
                .map((l) => l.trim())
                .filter((l) => l.length > 0)
                .filter((l) => !l.toLowerCase().includes('avg') && !l.toLowerCase().includes('average'))
                .join('\n');
        };

        const formatDetectedScore = (parsed, scoringType) => {
            if (!parsed) return null;
            if (parsed.isDNF) return 'DNF';
            if (scoringType === 2 && parsed.minutes !== undefined) {
                return `${parsed.minutes}:${String(parsed.seconds ?? 0).padStart(2, '0')}`;
            }
            if (scoringType === 1 && parsed.guessCount !== undefined) {
                return `${parsed.guessCount} guess${parsed.guessCount === 1 ? '' : 'es'}`;
            }
            return null;
        };

        // ── OCR queue — process one at a time ─────────────────────────────────

        const runOcrQueue = async () => {
            if (isProcessingOCR.value) return; // already running
            const next = uploads.value.find((e) => e.status === 'queued');
            if (!next) return;

            isProcessingOCR.value = true;
            next.status = 'processing';

            try {
                const rawText = await ocrService.extractTextFromImage(next.file);
                next.ocrText = rawText;
                const filteredText = filterOcrText(rawText);

                const detectedGame = detectGameFromText(rawText); // game names won't be in avg lines
                if (detectedGame) {
                    next.selectedGame = detectedGame;
                    const gameType = detectedGame.scoringType === 1 ? 'guess' : 'time';
                    const parsed = ocrService.parseLinkedInGameScore(filteredText, gameType);
                    if (parsed) {
                        applyParsedScore(next, parsed);
                        next.scoreConfidence = parsed.confidence || 'high';
                        next.detectedScoreLabel = formatDetectedScore(parsed, detectedGame.scoringType);
                        next.status = 'ready';
                    } else {
                        next.status = 'needs-review';
                    }
                } else {
                    next.status = 'needs-review';
                }
            } catch (e) {
                console.error('OCR error', e);
                next.status = 'needs-review';
            } finally {
                isProcessingOCR.value = false;
                // Process next in queue
                const remaining = uploads.value.find((e) => e.status === 'queued');
                if (remaining) runOcrQueue();
            }
        };

        // ── Score helpers ──────────────────────────────────────────────────────

        const detectGameFromText = (text) => {
            const lines = text
                .split(/\r?\n/)
                .map((l) => l.trim())
                .filter((l) => l.length > 0);

            for (const game of games.value) {
                const gameName = game.name.toLowerCase();
                for (const line of lines) {
                    if (line.toLowerCase().includes(gameName)) return game;
                }
            }
            return null;
        };

        const applyParsedScore = (entry, parsed) => {
            if (parsed.isDNF) {
                entry.ranOutOfGuesses = true;
                entry.guessCount = null;
                entry.minutes = null;
                entry.seconds = null;
            } else if (parsed.minutes !== undefined) {
                entry.minutes = parsed.minutes;
                entry.seconds = parsed.seconds;
                entry.guessCount = null;
                entry.ranOutOfGuesses = false;
            } else if (parsed.guessCount !== undefined) {
                entry.guessCount = parsed.guessCount;
                entry.ranOutOfGuesses = false;
                entry.minutes = null;
                entry.seconds = null;
            }
        };

        const isEntryValid = (entry) => {
            if (!entry.selectedGame) return false;
            if (entry.selectedGame.scoringType === 1) {
                return entry.ranOutOfGuesses || (entry.guessCount && entry.guessCount > 0);
            }
            if (entry.selectedGame.scoringType === 2) {
                const m = entry.minutes || 0;
                return entry.seconds !== null && entry.seconds >= 0 && entry.seconds < 60 && (m > 0 || entry.seconds > 0);
            }
            return false;
        };

        const onFieldChange = (entry) => {
            if (entry.status !== 'submitted') {
                entry.status = isEntryValid(entry) ? 'ready' : 'needs-review';
            }
        };

        const onGameChange = (entry) => {
            // Reset score fields when game changes
            entry.guessCount = null;
            entry.minutes = null;
            entry.seconds = null;
            entry.ranOutOfGuesses = false;
            entry.scoreConfidence = null;
            entry.detectedScoreLabel = null;

            // Re-parse OCR text with new game type if we have it
            if (entry.ocrText && entry.selectedGame) {
                const filteredText = filterOcrText(entry.ocrText);
                const gameType = entry.selectedGame.scoringType === 1 ? 'guess' : 'time';
                const parsed = ocrService.parseLinkedInGameScore(filteredText, gameType);
                if (parsed) {
                    applyParsedScore(entry, parsed);
                    entry.scoreConfidence = parsed.confidence || 'high';
                    entry.detectedScoreLabel = formatDetectedScore(parsed, entry.selectedGame.scoringType);
                }
            }
            onFieldChange(entry);
        };

        const rerunOcr = async (entry) => {
            entry.status = 'processing';
            entry.scoreConfidence = null;
            entry.detectedScoreLabel = null;
            try {
                const rawText = await ocrService.extractTextFromImage(entry.file);
                entry.ocrText = rawText;
                const filteredText = filterOcrText(rawText);

                const detectedGame = detectGameFromText(rawText);
                if (detectedGame) entry.selectedGame = detectedGame;

                const gameToUse = entry.selectedGame;
                if (gameToUse) {
                    const gameType = gameToUse.scoringType === 1 ? 'guess' : 'time';
                    const parsed = ocrService.parseLinkedInGameScore(filteredText, gameType);
                    if (parsed) {
                        applyParsedScore(entry, parsed);
                        entry.scoreConfidence = parsed.confidence || 'high';
                        entry.detectedScoreLabel = formatDetectedScore(parsed, gameToUse.scoringType);
                        entry.status = 'ready';
                        return;
                    }
                }
                entry.status = 'needs-review';
            } catch (e) {
                entry.status = 'needs-review';
            }
        };

        const removeUpload = (entry) => {
            uploads.value = uploads.value.filter((u) => u.id !== entry.id);
        };

        // ── Submission ─────────────────────────────────────────────────────────

        const readyEntries = computed(() => uploads.value.filter((e) => e.status === 'ready'));
        const needsReviewCount = computed(() => uploads.value.filter((e) => e.status === 'needs-review').length);
        const submittedCount = computed(() => uploads.value.filter((e) => e.status === 'submitted').length);
        const processingCount = computed(() => uploads.value.filter((e) => e.status === 'processing' || e.status === 'queued').length);

        const submitAll = async () => {
            if (!playerName.value?.trim()) {
                toast.add({ severity: 'warn', summary: 'Player Name Required', detail: 'Enter your name before submitting' });
                return;
            }
            if (!readyEntries.value.length) {
                toast.add({ severity: 'warn', summary: 'No Scores Ready', detail: 'Review and correct any flagged items first' });
                return;
            }

            isSubmittingAll.value = true;
            const results = await Promise.allSettled(readyEntries.value.map((e) => submitEntry(e)));
            isSubmittingAll.value = false;

            const succeeded = results.filter((r) => r.status === 'fulfilled').length;
            const failed = results.filter((r) => r.status === 'rejected').length;

            if (succeeded > 0) {
                gameService.clearAllCaches();
                emit('scoreSubmitted');
                toast.add({
                    severity: failed ? 'warn' : 'success',
                    summary: failed ? 'Partial Success' : 'All Submitted!',
                    detail: `${succeeded} score${succeeded !== 1 ? 's' : ''} submitted${failed ? `, ${failed} failed — fix and resubmit` : ''}`,
                    life: 4000
                });
                // Navigate back to leaderboards after a short delay so the toast is visible
                if (!failed) {
                    setTimeout(() => router.push('/'), 1500);
                }
            } else {
                toast.add({ severity: 'error', summary: 'Submission Failed', detail: 'All submissions failed. Please try again.', life: 5000 });
            }
        };

        const submitEntry = async (entry) => {
            const gameScore = {
                gameId: entry.selectedGame.id,
                playerName: playerName.value,
                linkedInProfileUrl: linkedinUrl.value || null
            };

            if (entry.selectedGame.scoringType === 1) {
                gameScore.guessCount = entry.ranOutOfGuesses ? 99 : entry.guessCount;
            } else if (entry.selectedGame.scoringType === 2) {
                const total = (entry.minutes || 0) * 60 + (entry.seconds || 0);
                gameScore.completionTime = `${Math.floor(total / 3600).toString().padStart(2, '0')}:${Math.floor((total % 3600) / 60).toString().padStart(2, '0')}:${(total % 60).toString().padStart(2, '0')}`;
            }

            try {
                if (entry.file) {
                    await gameService.submitScoreWithImage(gameScore, entry.file);
                } else {
                    await gameService.submitScore(gameScore);
                }
                entry.status = 'submitted';
            } catch (err) {
                entry.status = 'error';
                throw err;
            }
        };

        const clearSubmitted = () => {
            uploads.value = uploads.value.filter((e) => e.status !== 'submitted');
        };

        const triggerFileInput = () => document.getElementById('bulk-file-input')?.click();

        // ── Status helpers ─────────────────────────────────────────────────────

        const statusConfig = {
            queued: { label: 'Queued', severity: 'secondary', icon: 'pi-clock' },
            processing: { label: 'Scanning...', severity: 'info', icon: 'pi-spin pi-spinner' },
            'needs-review': { label: 'Needs Review', severity: 'warn', icon: 'pi-exclamation-triangle' },
            ready: { label: 'Ready', severity: 'success', icon: 'pi-check' },
            submitted: { label: 'Submitted', severity: 'success', icon: 'pi-check-circle' },
            error: { label: 'Error', severity: 'danger', icon: 'pi-times-circle' }
        };

        const getStatusConfig = (status) => statusConfig[status] || statusConfig['queued'];

        const confidenceBadge = (conf) => {
            if (conf === 'high') return { label: 'High confidence', class: 'text-green-600' };
            if (conf === 'medium') return { label: 'Medium confidence — please verify', class: 'text-yellow-600' };
            return null;
        };

        return {
            games,
            uploads,
            playerName,
            linkedinUrl,
            isSubmittingAll,
            isDragging,
            readyEntries,
            needsReviewCount,
            submittedCount,
            processingCount,
            handleDrop,
            handleFileSelect,
            triggerFileInput,
            removeUpload,
            rerunOcr,
            onFieldChange,
            onGameChange,
            isEntryValid,
            submitAll,
            clearSubmitted,
            getStatusConfig,
            confidenceBadge
        };
    }
};
</script>

<template>
    <div class="quick-submit-page">
        <!-- Header -->
        <div class="mb-6">
            <h1 class="text-3xl font-bold text-gray-900 mb-2">Quick Submit</h1>
            <p class="text-gray-600">Drop all your screenshots at once — we'll scan each one and fill in the scores automatically.</p>
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
                    <div>
                        <label class="block text-sm font-medium mb-2" for="qs-name">Player Name <span class="text-red-500">*</span></label>
                        <InputText id="qs-name" v-model="playerName" placeholder="Enter your name" class="w-full" />
                    </div>
                    <div>
                        <label class="block text-sm font-medium mb-2" for="qs-url">LinkedIn Profile URL (Optional)</label>
                        <InputText id="qs-url" v-model="linkedinUrl" placeholder="https://linkedin.com/in/yourprofile" class="w-full" />
                    </div>
                </div>
            </template>
        </Card>

        <!-- Drop Zone -->
        <div
            class="drop-zone mb-6"
            :class="{ 'drop-zone--active': isDragging }"
            @dragover.prevent="isDragging = true"
            @dragleave="isDragging = false"
            @drop.prevent="handleDrop"
            @click="triggerFileInput"
        >
            <i class="pi pi-images text-5xl text-gray-300 mb-3"></i>
            <p class="text-lg font-medium text-gray-600">Drop all your game screenshots here</p>
            <p class="text-sm text-gray-400 mt-1">or click to browse · JPEG, PNG, GIF · Max 5MB each · Multiple files supported</p>
            <input id="bulk-file-input" type="file" accept="image/jpeg,image/jpg,image/png,image/gif" multiple class="hidden" @change="handleFileSelect" />
        </div>

        <!-- Review List -->
        <div v-if="uploads.length" class="space-y-3 mb-6">
            <div class="flex items-center justify-between mb-4">
                <h2 class="text-lg font-semibold text-gray-800">
                    Review &amp; Correct
                    <span v-if="processingCount" class="text-sm font-normal text-blue-500 ml-2">
                        <i class="pi pi-spin pi-spinner text-xs"></i> Scanning {{ processingCount }} image{{ processingCount !== 1 ? 's' : '' }}...
                    </span>
                </h2>
                <div class="flex gap-3 text-sm text-gray-500">
                    <span v-if="needsReviewCount" class="text-yellow-600 font-medium">⚠ {{ needsReviewCount }} need{{ needsReviewCount === 1 ? 's' : '' }} review</span>
                    <span v-if="submittedCount" class="text-blue-600 font-medium">✓ {{ submittedCount }} submitted</span>
                    <Button v-if="submittedCount" label="Clear submitted" size="small" text severity="secondary" @click="clearSubmitted" />
                </div>
            </div>

            <!-- Upload Entry Card -->
            <Card v-for="entry in uploads" :key="entry.id" class="entry-card" :class="`entry-${entry.status}`">
                <template #content>
                    <div class="flex flex-col sm:flex-row gap-4 items-start">

                        <!-- Thumbnail: full-width on mobile, fixed sidebar on sm+ -->
                        <div class="thumbnail-col w-full sm:w-24 shrink-0">
                            <div v-if="!entry.imagePreview" class="thumbnail-placeholder">
                                <i class="pi pi-image text-gray-300 text-2xl"></i>
                            </div>
                            <img v-else :src="entry.imagePreview" alt="Screenshot" class="thumbnail-img" />
                        </div>

                        <!-- Main content -->
                        <div class="flex-1 min-w-0">
                            <!-- Top row: filename + status + remove -->
                            <div class="flex items-center justify-between gap-2 mb-3 flex-wrap">
                                <div class="flex items-center gap-2 min-w-0">
                                    <Tag :severity="getStatusConfig(entry.status).severity">
                                        <span class="flex items-center gap-1">
                                            <i :class="`pi ${getStatusConfig(entry.status).icon} text-xs`"></i>
                                            {{ getStatusConfig(entry.status).label }}
                                        </span>
                                    </Tag>
                                    <span class="text-sm text-gray-500 truncate">{{ entry.file.name }}</span>
                                    <span v-if="entry.scoreConfidence" :class="confidenceBadge(entry.scoreConfidence)?.class" class="text-xs">
                                        · {{ confidenceBadge(entry.scoreConfidence)?.label }}
                                    </span>
                                </div>
                                <div class="flex gap-2 shrink-0">
                                    <Button
                                        v-if="entry.status === 'needs-review' || entry.status === 'ready'"
                                        icon="pi pi-refresh"
                                        label="Re-scan"
                                        size="small"
                                        severity="secondary"
                                        text
                                        @click="rerunOcr(entry)"
                                    />
                                    <Button
                                        v-if="entry.status !== 'submitted'"
                                        icon="pi pi-times"
                                        size="small"
                                        severity="danger"
                                        text
                                        rounded
                                        @click="removeUpload(entry)"
                                        aria-label="Remove"
                                    />
                                </div>
                            </div>

                            <!-- Processing state -->
                            <div v-if="entry.status === 'processing'" class="flex items-center gap-2 text-sm text-blue-600 py-2">
                                <i class="pi pi-spin pi-spinner"></i>
                                Scanning image for game and score...
                            </div>

                            <!-- Queued state -->
                            <div v-else-if="entry.status === 'queued'" class="text-sm text-gray-400 py-2">
                                Waiting to scan...
                            </div>

                            <!-- Submitted state -->
                            <div v-else-if="entry.status === 'submitted'" class="flex items-center gap-2 text-sm text-green-600 py-2">
                                <i class="pi pi-check-circle"></i>
                                Score submitted successfully for <strong>{{ entry.selectedGame?.name }}</strong>
                            </div>

                            <!-- Review / Ready / Error: show editable fields -->
                            <div v-else class="grid grid-cols-1 sm:grid-cols-2 gap-3">

                                <!-- Game selector -->
                                <div>
                                    <label class="block text-xs font-semibold uppercase tracking-wide text-gray-500 mb-1">Game</label>
                                    <Select
                                        v-model="entry.selectedGame"
                                        :options="games"
                                        optionLabel="name"
                                        placeholder="Select game…"
                                        class="w-full"
                                        :class="{ 'p-invalid': !entry.selectedGame && entry.status === 'needs-review' }"
                                        @change="onGameChange(entry)"
                                    />
                                    <small v-if="!entry.selectedGame" class="text-yellow-600 text-xs mt-1 block">
                                        <i class="pi pi-exclamation-triangle text-xs"></i> Could not detect game — please select
                                    </small>
                                </div>

                                <!-- Score inputs -->
                                <div v-if="entry.selectedGame">
                                    <!-- Guess-based -->
                                    <div v-if="entry.selectedGame.scoringType === 1">
                                        <label class="block text-xs font-semibold uppercase tracking-wide text-gray-500 mb-1">Guesses</label>
                                        <InputNumber
                                            :model-value="entry.guessCount"
                                            placeholder="Number of guesses"
                                            class="w-full"
                                            :input-style="{ width: '100%' }"
                                            :min="1"
                                            :disabled="entry.ranOutOfGuesses"
                                            @update:model-value="(val) => { entry.guessCount = val; onFieldChange(entry); }"
                                        />
                                        <div class="flex items-center gap-2 mt-2">
                                            <Checkbox :inputId="`dnf-${entry.id}`" v-model="entry.ranOutOfGuesses" :binary="true" @change="onFieldChange(entry)" />
                                            <label :for="`dnf-${entry.id}`" class="text-sm cursor-pointer">Did Not Finish (DNF)</label>
                                        </div>
                                    </div>

                                    <!-- Time-based -->
                                    <div v-else-if="entry.selectedGame.scoringType === 2">
                                        <label class="block text-xs font-semibold uppercase tracking-wide text-gray-500 mb-1">Completion Time</label>
                                        <div class="grid grid-cols-2 gap-2">
                                            <div>
                                                <InputNumber
                                                    :model-value="entry.minutes"
                                                    placeholder="0"
                                                    class="w-full"
                                                    :input-style="{ width: '100%' }"
                                                    :min="0"
                                                    :max="99"
                                                    @update:model-value="(val) => { entry.minutes = val; onFieldChange(entry); }"
                                                />
                                                <span class="text-xs text-gray-400 block text-center mt-1">Minutes</span>
                                            </div>
                                            <div>
                                                <InputNumber
                                                    :model-value="entry.seconds"
                                                    placeholder="0"
                                                    class="w-full"
                                                    :input-style="{ width: '100%' }"
                                                    :min="0"
                                                    :max="59"
                                                    @update:model-value="(val) => { entry.seconds = val; onFieldChange(entry); }"
                                                />
                                                <span class="text-xs text-gray-400 block text-center mt-1">Seconds</span>
                                            </div>
                                        </div>
                                    </div>
                                </div>

                                <div v-else class="flex items-end pb-1">
                                    <p class="text-sm text-gray-400 italic">Select a game to enter the score</p>
                                </div>

                                <!-- OCR reading confirmation -->
                                <div v-if="entry.detectedScoreLabel" class="sm:col-span-2">
                                    <p class="text-xs text-gray-400">
                                        <i class="pi pi-eye text-xs mr-1"></i>OCR read: <span class="font-mono font-medium text-gray-600">{{ entry.detectedScoreLabel }}</span>
                                        <span v-if="entry.scoreConfidence === 'medium'" class="text-yellow-500 ml-1">— medium confidence, please verify</span>
                                    </p>
                                </div>
                            </div>
                        </div>
                    </div>
                </template>
            </Card>
        </div>

        <!-- Empty state -->
        <div v-else class="text-center py-12 text-gray-400">
            <i class="pi pi-arrow-up text-4xl mb-3 block"></i>
            <p class="text-lg">Drop your screenshots above to get started</p>
        </div>

        <!-- Submit Bar -->
        <Card v-if="uploads.length" class="submit-bar">
            <template #content>
                <div class="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
                    <div class="flex flex-wrap gap-4 text-sm">
                        <span>
                            <strong :class="readyEntries.length ? 'text-green-600' : 'text-gray-400'">{{ readyEntries.length }}</strong>
                            <span class="text-gray-500"> ready to submit</span>
                        </span>
                        <span v-if="needsReviewCount" class="text-yellow-600">
                            <strong>{{ needsReviewCount }}</strong> need{{ needsReviewCount === 1 ? 's' : '' }} review
                        </span>
                        <span v-if="processingCount" class="text-blue-500">
                            <i class="pi pi-spin pi-spinner text-xs"></i> <strong>{{ processingCount }}</strong> scanning...
                        </span>
                    </div>
                    <Button
                        :label="`Submit ${readyEntries.length} Score${readyEntries.length !== 1 ? 's' : ''}`"
                        icon="pi pi-send"
                        size="large"
                        class="px-8"
                        :loading="isSubmittingAll"
                        :disabled="!readyEntries.length || !playerName || isSubmittingAll"
                        @click="submitAll"
                    />
                </div>
            </template>
        </Card>
    </div>
</template>

<style scoped>
.quick-submit-page {
    @apply p-6 max-w-5xl mx-auto;
}

.drop-zone {
    @apply flex flex-col items-center justify-center border-2 border-dashed border-gray-300 rounded-xl p-12 cursor-pointer transition-all duration-200 bg-white;
}

.drop-zone:hover,
.drop-zone--active {
    @apply border-blue-400 bg-blue-50;
}

/* Entry cards */
.entry-card {
    @apply border border-gray-200 transition-all duration-200;
}

.entry-needs-review {
    @apply border-yellow-300 bg-yellow-50/30;
}

.entry-ready {
    @apply border-green-300;
}

.entry-submitted {
    @apply border-blue-200 opacity-70;
}

.entry-error {
    @apply border-red-300 bg-red-50/30;
}

/* Thumbnail */
.thumbnail-img {
    @apply w-full max-h-64 sm:w-24 sm:h-24 sm:max-h-none object-contain sm:object-cover rounded-lg border border-gray-200 bg-gray-50;
}

.thumbnail-placeholder {
    @apply w-full h-24 sm:w-24 rounded-lg border-2 border-dashed border-gray-200 flex items-center justify-center bg-gray-50;
}

.submit-bar {
    @apply border border-gray-200 sticky bottom-4 shadow-lg;
}
</style>
