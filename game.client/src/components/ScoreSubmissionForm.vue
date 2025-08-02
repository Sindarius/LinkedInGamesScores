<script>
import { ref, onMounted, computed } from 'vue';
import { useToast } from 'primevue/usetoast';
import { usePlayerStore } from '@/stores/playerStore.js';
import { GameService } from '@/services/gameService.js';

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
        const scoreImage = ref(null);
        const imagePreview = ref(null);
        const isSubmitting = ref(false);
        const showErrors = ref(false);

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
                };
                reader.readAsDataURL(file);
            }
        };

        const removeImage = () => {
            scoreImage.value = null;
            imagePreview.value = null;
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
                    gameScore.guessCount = guessCount.value;
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
                    detail: 'Score submitted successfully!'
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
                return guessCount.value && guessCount.value > 0;
            } else if (selectedGame.value.scoringType === 2) {
                return minutes.value !== null && minutes.value >= 0 && seconds.value !== null && seconds.value >= 0 && seconds.value < 60 && (minutes.value > 0 || seconds.value > 0);
            }

            return false;
        };

        const clearForm = () => {
            selectedGame.value = null;
            // Note: playerName and linkedinUrl are not cleared to preserve user data
            guessCount.value = null;
            minutes.value = null;
            seconds.value = null;
            scoreImage.value = null;
            imagePreview.value = null;
            showErrors.value = false;
        };

        return {
            games,
            selectedGame,
            playerName,
            guessCount,
            minutes,
            seconds,
            scoreImage,
            imagePreview,
            linkedinUrl,
            isSubmitting,
            showErrors,
            submitScore,
            isFormValid,
            clearForm,
            handleImageUpload,
            removeImage
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

                    <InputNumber v-if="selectedGame.scoringType === 1" id="guessCount" v-model="guessCount" placeholder="Enter number of guesses" class="w-full" :class="{ 'p-invalid': (!guessCount || guessCount <= 0) && showErrors }" :min="1" />

                    <div v-else-if="selectedGame.scoringType === 2" class="flex space-x-2">
                        <InputNumber id="minutes" v-model="minutes" placeholder="Min" class="flex-1" :class="{ 'p-invalid': (minutes === null || minutes < 0) && showErrors }" :min="0" suffix=" min" />
                        <InputNumber id="seconds" v-model="seconds" placeholder="Sec" class="flex-1" :class="{ 'p-invalid': (seconds === null || seconds < 0 || seconds >= 60) && showErrors }" :min="0" :max="59" suffix=" sec" />
                    </div>

                    <small v-if="selectedGame.scoringType === 1 && (!guessCount || guessCount <= 0) && showErrors" class="p-error">Number of guesses is required.</small>
                    <small v-if="selectedGame.scoringType === 2 && (minutes === null || minutes < 0 || seconds === null || seconds < 0 || seconds >= 60) && showErrors" class="p-error">Valid completion time is required.</small>
                </div>

                <div class="field">
                    <label for="linkedinUrl">LinkedIn Profile URL (Optional)</label>
                    <InputText id="linkedinUrl" v-model="linkedinUrl" placeholder="https://linkedin.com/in/yourprofile" class="w-full" />
                </div>

                <div class="field">
                    <label for="scoreImage">Score Screenshot (Optional)</label>
                    <FileUpload id="scoreImage" mode="basic" accept="image/jpeg,image/jpg,image/png,image/gif" :maxFileSize="5000000" @select="handleImageUpload" :auto="false" chooseLabel="Choose Image" class="mb-2" />

                    <div v-if="imagePreview" class="mt-2">
                        <div class="flex items-center justify-between mb-2">
                            <span class="text-sm text-gray-600">Image Preview:</span>
                            <Button icon="pi pi-times" class="p-button-rounded p-button-text p-button-danger" size="small" @click="removeImage" aria-label="Remove image" />
                        </div>
                        <img :src="imagePreview" alt="Score preview" class="max-w-full max-h-48 object-contain border rounded" />
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
