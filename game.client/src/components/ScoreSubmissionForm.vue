<script>
import { ref, onMounted, computed } from 'vue';
import { useToast } from 'primevue/usetoast';
import { usePlayerStore } from '@/stores/playerStore.js';
import { GameService } from '@/services/gameService.js';
import { ocrService } from '@/services/ocrService.js';

export default {
    name: 'ScoreSubmissionForm',
    emits: ['scoreSubmitted'],
    setup(props, { emit }) {
        const toast = useToast();
        const playerStore = usePlayerStore();
        const gameService = new GameService();

        const games = ref([]);
        const selectedGame = ref(null);
        const guessCount = ref(null);
        const minutes = ref(null);
        const seconds = ref(null);
        const ranOutOfGuesses = ref(false);
        const scoreImage = ref(null);
        const imagePreview = ref(null);
        const fileUploadRef = ref(null);
        const isSubmitting = ref(false);
        const showErrors = ref(false);
        const isParsingOCR = ref(false);

        // Use computed properties for player data that sync with store
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

        const handleImageUpload = (event) => {
            const file = event.files?.[0] || event.target.files?.[0];
            if (file) {
                // Validate file type
                const allowedTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/gif'];
                if (!allowedTypes.includes(file.type)) {
                    toast.add({
                        severity: 'error',
                        summary: 'Invalid File Type',
                        detail: 'Only JPEG, PNG, and GIF images are allowed'
                    });
                    return;
                }

                // Validate file size (5MB limit)
                if (file.size > 5 * 1024 * 1024) {
                    toast.add({
                        severity: 'error',
                        summary: 'File Too Large',
                        detail: 'Image size cannot exceed 5MB'
                    });
                    return;
                }

                scoreImage.value = file;

                // Create preview
                const reader = new FileReader();
                reader.onload = (e) => {
                    imagePreview.value = e.target.result;

                    // Automatically attempt to detect game and score
                    autoDetectGameAndScore();
                };
                reader.readAsDataURL(file);
            }
        };

        const removeImage = () => {
            scoreImage.value = null;
            imagePreview.value = null;
            if (fileUploadRef.value) {
                fileUploadRef.value.clear();
            }
        };

        const parseScoreFromImage = async () => {
            if (!scoreImage.value || !selectedGame.value) {
                return;
            }

            isParsingOCR.value = true;

            try {
                toast.add({
                    severity: 'info',
                    summary: 'Processing Image',
                    detail: 'Extracting text from image...',
                    life: 3000
                });

                const extractedText = await ocrService.extractTextFromImage(scoreImage.value);

                const gameType = selectedGame.value.scoringType === 1 ? 'guess' : 'time';
                const parsedScore = ocrService.parseLinkedInGameScore(extractedText, gameType);

                if (parsedScore) {
                    if (gameType === 'time') {
                        minutes.value = parsedScore.minutes;
                        seconds.value = parsedScore.seconds;
                        toast.add({
                            severity: 'success',
                            summary: 'Score Parsed',
                            detail: `Found time: ${parsedScore.minutes}:${parsedScore.seconds.toString().padStart(2, '0')} (confidence: ${parsedScore.confidence})`,
                            life: 5000
                        });
                    } else if (gameType === 'guess') {
                        if (parsedScore.isDNF) {
                            ranOutOfGuesses.value = true;
                            guessCount.value = null;
                            toast.add({
                                severity: 'success',
                                summary: 'Score Parsed',
                                detail: 'Detected DNF (Did Not Finish)',
                                life: 5000
                            });
                        } else {
                            guessCount.value = parsedScore.guessCount;
                            ranOutOfGuesses.value = false;
                            toast.add({
                                severity: 'success',
                                summary: 'Score Parsed',
                                detail: `Found ${parsedScore.guessCount} guesses (confidence: ${parsedScore.confidence})`,
                                life: 5000
                            });
                        }
                    }
                } else {
                    toast.add({
                        severity: 'warn',
                        summary: 'No Score Found',
                        detail: 'Could not detect a valid score in the image. Please enter manually.',
                        life: 5000
                    });
                }
            } catch (error) {
                console.error('OCR parsing failed:', error);
                toast.add({
                    severity: 'error',
                    summary: 'Parsing Failed',
                    detail: 'Failed to process the image. Please try again or enter manually.',
                    life: 5000
                });
            } finally {
                isParsingOCR.value = false;
            }
        };

        // Auto-detect game and score from OCR text
        const autoDetectGameAndScore = async () => {
            if (!scoreImage.value) {
                return;
            }

            isParsingOCR.value = true;

            try {
                toast.add({
                    severity: 'info',
                    summary: 'Auto-detecting',
                    detail: 'Analyzing image for game and score...',
                    life: 3000
                });

                const extractedText = await ocrService.extractTextFromImage(scoreImage.value);

                // Detect game first
                const detectedGame = detectGameFromText(extractedText);
                if (detectedGame) {
                    selectedGame.value = detectedGame;

                    // Extract score based on detected game type
                    const scoreResult = extractScoreFromText(extractedText, detectedGame);
                    if (scoreResult) {
                        applyDetectedScore(scoreResult, detectedGame);

                        toast.add({
                            severity: 'success',
                            summary: 'Auto-detection Success',
                            detail: `Detected ${detectedGame.name} with score`,
                            life: 5000
                        });
                    } else {
                        toast.add({
                            severity: 'warn',
                            summary: 'Game Detected',
                            detail: `Found ${detectedGame.name} but could not detect score. Please enter manually.`,
                            life: 5000
                        });
                    }
                } else {
                    toast.add({
                        severity: 'warn',
                        summary: 'Auto-detection Failed',
                        detail: 'Could not detect game or score. Please select manually.',
                        life: 5000
                    });
                }
            } catch (error) {
                console.error('Auto-detection failed:', error);
                toast.add({
                    severity: 'error',
                    summary: 'Auto-detection Error',
                    detail: 'Failed to analyze the image. Please try again.',
                    life: 5000
                });
            } finally {
                isParsingOCR.value = false;
            }
        };

        // Detect game from OCR text by searching for game names (case insensitive)
        // Ignore lines with "avg" or "average" as those contain game averages, not player scores
        const detectGameFromText = (text) => {
            const lines = text.split(/\r?\n/).map(line => line.trim()).filter(line => line.length > 0);

            // Search for each game name in the text
            for (const game of games.value) {
                const gameName = game.name.toLowerCase();

                // Look for the game name in lines that DON'T contain "avg" or "average"
                for (const line of lines) {
                    const lowerLine = line.toLowerCase();
                    if (lowerLine.includes(gameName)) {
                        return game;
                    }
                }
            }

            return null;
        };

        // Extract score from text based on game type - search independently across all lines
        // Ignore lines with "avg" or "average" as those contain game averages
        const extractScoreFromText = (text, game) => {
            const lines = text.split(/\r?\n/).map(line => line.trim()).filter(line => line.length > 0);

            // Filter out lines that contain average information
            const cleanLines = lines.filter(line => {
                const lowerLine = line.toLowerCase();
                return !lowerLine.includes('avg') && !lowerLine.includes('average');
            });


            if (game.scoringType === 2) { // Time-based game
                return extractTimeScore(cleanLines);
            } else if (game.scoringType === 1) { // Guess-based game
                return extractGuessScore(cleanLines);
            }

            return null;
        };

        // Extract time score (MM:SS format) - should be just the time on its own line
        const extractTimeScore = (lines) => {
            for (const line of lines) {
                // Clean up "solved in" text and trim
                const cleanLine = line.replace(/solved\s+in\s*/gi, '').trim();
                
                // Look for lines that contain primarily just time patterns
                const timePatterns = [
                    /^(\d{1,2}):(\d{2})$/,  // Exact match: just MM:SS
                    /^(\d{1,2})\/(\d{2})$/, // Exact match: just MM/SS
                    /^\s*(\d{1,2}):(\d{2})\s*$/, // With possible whitespace
                    /^\s*(\d{1,2})\/(\d{2})\s*$/ // With possible whitespace
                ];

                for (const pattern of timePatterns) {
                    const match = cleanLine.match(pattern);
                    if (match) {
                        const minutes = parseInt(match[1]);
                        const seconds = parseInt(match[2]);

                        if (minutes >= 0 && minutes < 60 && seconds >= 0 && seconds < 60) {
                            return { type: 'time', minutes, seconds };
                        }
                    }
                }
            }
            return null;
        };

        // Extract guess score (# guesses or DNF) - should contain "guess/guesses" word
        const extractGuessScore = (lines) => {
            for (const line of lines) {
                // Clean up "solved in" text and trim
                const cleanLine = line.replace(/solved\s+in\s*/gi, '').trim();
                const lowerLine = cleanLine.toLowerCase();

                // Check for DNF patterns first, including "better luck next time"
                const dnfPatterns = [
                    /better\s*luck\s*next\s*time/gi,
                    /dnf/gi,
                    /did\s*not\s*finish/gi,
                    /x\s*\/\s*\d+/gi,
                    /failed/gi,
                    /ran\s*out/gi,
                    /no\s*luck/gi,
                    /try\s*again/gi
                ];

                for (const pattern of dnfPatterns) {
                    if (pattern.test(lowerLine)) {
                        return { type: 'guess', isDNF: true };
                    }
                }

                // Look for guess patterns - prioritize lines that contain "guess" or "guesses"
                if (lowerLine.includes('guess')) {
                    const guessPatterns = [
                        /(\d+)\s*guesses?/gi,
                        /(\d+)\s*guess/gi,
                        /guess\s*(\d+)/gi,
                        /in\s*(\d+)\s*guesses?/gi
                    ];

                    for (const pattern of guessPatterns) {
                        const matches = [...cleanLine.matchAll(pattern)];
                        for (const match of matches) {
                            const guesses = parseInt(match[1]);
                            if (guesses >= 1 && guesses <= 10) {
                                return { type: 'guess', guessCount: guesses };
                            }
                        }
                    }
                }

                // Fallback: look for other attempt patterns if no "guess" word found
                const fallbackPatterns = [
                    /(\d+)\s*\/\s*\d+/gi,
                    /attempt\s*(\d+)/gi,
                    /try\s*(\d+)/gi,
                    /(\d+)\s*tries/gi
                ];

                for (const pattern of fallbackPatterns) {
                    const matches = [...cleanLine.matchAll(pattern)];
                    for (const match of matches) {
                        const guesses = parseInt(match[1]);
                        if (guesses >= 1 && guesses <= 10) {
                            return { type: 'guess', guessCount: guesses };
                        }
                    }
                }
            }
            return null;
        };

        // Apply detected score to form fields
        const applyDetectedScore = (scoreResult, game) => {
            if (scoreResult.type === 'time') {
                minutes.value = scoreResult.minutes;
                seconds.value = scoreResult.seconds;
                ranOutOfGuesses.value = false;
                guessCount.value = null;
            } else if (scoreResult.type === 'guess') {
                if (scoreResult.isDNF) {
                    ranOutOfGuesses.value = true;
                    guessCount.value = null;
                } else {
                    guessCount.value = scoreResult.guessCount;
                    ranOutOfGuesses.value = false;
                }
                minutes.value = null;
                seconds.value = null;
            }
        };

        const loadGames = async () => {
            try {
                games.value = await gameService.getGames();
            } catch (error) {
                console.error('Error loading games:', error);
                toast.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: 'Failed to load games'
                });
            }
        };

        const submitScore = async () => {
            showErrors.value = true;

            if (!isFormValid()) {
                return;
            }

            isSubmitting.value = true;

            try {
                const gameScore = {
                    gameId: selectedGame.value.id,
                    playerName: playerName.value,
                    linkedInProfileUrl: linkedinUrl.value || null
                };

                if (selectedGame.value.scoringType === 1) {
                    gameScore.guessCount = ranOutOfGuesses.value ? 99 : guessCount.value;
                } else if (selectedGame.value.scoringType === 2) {
                    const totalSeconds = (minutes.value || 0) * 60 + (seconds.value || 0);
                    gameScore.completionTime = `${Math.floor(totalSeconds / 3600)
                        .toString()
                        .padStart(2, '0')}:${Math.floor((totalSeconds % 3600) / 60)
                        .toString()
                        .padStart(2, '0')}:${(totalSeconds % 60).toString().padStart(2, '0')}`;
                }

                if (scoreImage.value) {
                    await gameService.submitScoreWithImage(gameScore, scoreImage.value);
                } else {
                    await gameService.submitScore(gameScore);
                }

                toast.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: 'Score submitted successfully!',
                    life: 5000
                });

                clearForm();
                emit('scoreSubmitted');
            } catch (error) {
                console.error('Error submitting score:', error);
                toast.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: 'Failed to submit score'
                });
            } finally {
                isSubmitting.value = false;
            }
        };

        const isFormValid = () => {
            if (!selectedGame.value || !playerName.value.trim()) {
                return false;
            }

            if (selectedGame.value.scoringType === 1) {
                return ranOutOfGuesses.value || (guessCount.value && guessCount.value > 0);
            } else if (selectedGame.value.scoringType === 2) {
                const mins = minutes.value || 0;
                return seconds.value !== null && seconds.value >= 0 && seconds.value < 60 && (mins > 0 || seconds.value > 0);
            }

            return false;
        };

        const clearForm = () => {
            selectedGame.value = null;
            // Note: playerName and linkedinUrl are not cleared to preserve user data
            guessCount.value = null;
            minutes.value = null;
            seconds.value = null;
            ranOutOfGuesses.value = false;
            scoreImage.value = null;
            imagePreview.value = null;
            if (fileUploadRef.value) {
                fileUploadRef.value.clear();
            }
            showErrors.value = false;
        };

        return {
            games,
            selectedGame,
            playerName,
            guessCount,
            minutes,
            seconds,
            ranOutOfGuesses,
            scoreImage,
            imagePreview,
            linkedinUrl,
            isSubmitting,
            showErrors,
            isParsingOCR,
            fileUploadRef,
            submitScore,
            isFormValid,
            clearForm,
            handleImageUpload,
            removeImage,
            parseScoreFromImage,
            autoDetectGameAndScore
        };
    }
};
</script>

<template>
    <Card>
        <template #title>Submit Your Score</template>
        <template #content>
            <form @submit.prevent="submitScore" class="space-y-4">
                <div class="field">
                    <label for="game">Game</label>
                    <Select id="game" v-model="selectedGame" :options="games" optionLabel="name" placeholder="Select a game" class="w-full" :class="{ 'p-invalid': !selectedGame && showErrors }" />
                    <small v-if="!selectedGame && showErrors" class="p-error">Game is required.</small>
                </div>

                <div class="field">
                    <label for="playerName">Player Name</label>
                    <InputText id="playerName" v-model="playerName" placeholder="Enter your name" class="w-full" :class="{ 'p-invalid': !playerName && showErrors }" />
                    <small v-if="!playerName && showErrors" class="p-error">Player name is required.</small>
                </div>

                <div class="field" v-if="selectedGame">
                    <label v-if="selectedGame.scoringType === 1" for="guessCount">Number of Guesses</label>
                    <label v-else-if="selectedGame.scoringType === 2" for="completionTime">Completion Time</label>

                    <div v-if="selectedGame.scoringType === 1" class="space-y-3">
                        <InputNumber
                            id="guessCount"
                            v-model="guessCount"
                            placeholder="Enter number of guesses"
                            class="w-full"
                            :class="{ 'p-invalid': (!guessCount || guessCount <= 0) && !ranOutOfGuesses && showErrors }"
                            :min="1"
                            :disabled="ranOutOfGuesses"
                        />

                        <div class="flex items-center">
                            <Checkbox id="ranOutOfGuesses" v-model="ranOutOfGuesses" :binary="true" />
                            <label for="ranOutOfGuesses" class="ml-2 text-sm font-medium text-gray-700 cursor-pointer">I ran out of guesses (DNF)</label>
                        </div>

                        <small v-if="ranOutOfGuesses" class="text-blue-600 text-sm">This will be recorded as a "Did Not Finish" result</small>
                    </div>

                    <div v-else-if="selectedGame.scoringType === 2" class="flex space-x-2">
                        <InputNumber id="minutes" v-model="minutes" placeholder="Min" class="flex-1" :min="0" suffix=" min" />
                        <InputNumber id="seconds" v-model="seconds" placeholder="Sec" class="flex-1" :class="{ 'p-invalid': (seconds === null || seconds < 0 || seconds >= 60) && showErrors }" :min="0" :max="59" suffix=" sec" />
                    </div>

                    <small v-if="selectedGame.scoringType === 1 && (!guessCount || guessCount <= 0) && !ranOutOfGuesses && showErrors" class="p-error">Number of guesses is required.</small>
                    <small v-if="selectedGame.scoringType === 2 && (seconds === null || seconds < 0 || seconds >= 60) && showErrors" class="p-error">Valid completion time is required.</small>
                </div>

                <div class="field">
                    <label for="linkedinUrl">LinkedIn Profile URL (Optional)</label>
                    <InputText id="linkedinUrl" v-model="linkedinUrl" placeholder="https://linkedin.com/in/yourprofile" class="w-full" />
                </div>

                <div class="field">
                    <label for="scoreImage">Score Screenshot (Optional)</label>
                    <FileUpload ref="fileUploadRef" id="scoreImage" mode="basic" accept="image/jpeg,image/jpg,image/png,image/gif" :maxFileSize="5000000" @select="handleImageUpload" :auto="false" chooseLabel="Choose Image" class="mb-2" />

                    <div v-if="imagePreview" class="mt-2">
                        <div class="flex items-center justify-between mb-2">
                            <span class="text-sm text-gray-600">Image Preview:</span>
                            <Button icon="pi pi-times" class="p-button-rounded p-button-text p-button-danger" size="small" @click="removeImage" aria-label="Remove image" />
                        </div>
                        <img :src="imagePreview" alt="Score preview" class="max-w-full max-h-48 object-contain border rounded" />

                        <div class="mt-3 space-y-2">
                            <Button icon="pi pi-search" label="Auto-detect Game & Score" class="w-full" size="small" :loading="isParsingOCR" @click="autoDetectGameAndScore" />
                            <small class="text-gray-500 text-xs block"> Automatically detects the game name and score from the screenshot </small>

                            <div v-if="selectedGame">
                                <Button icon="pi pi-eye" label="Parse Score Only" class="w-full" size="small" :loading="isParsingOCR" @click="parseScoreFromImage" :disabled="!selectedGame" />
                                <small class="text-gray-500 text-xs block"> Uses OCR to detect score for the selected game </small>
                            </div>
                        </div>
                    </div>
                </div>

                <div class="flex justify-end gap-3 mt-6">
                    <Button type="button" label="Clear" severity="secondary" size="large" class="px-6" @click="clearForm" />
                    <Button type="submit" label="Submit Score" size="large" class="px-6" :loading="isSubmitting" />
                </div>
            </form>
        </template>
    </Card>
</template>

<style scoped>
.field {
    @apply mb-4;
}

.field label {
    @apply block text-sm font-medium mb-2;
}
</style>
