<template>
    <Card>
        <template #title>Submit Your Score</template>
        <template #content>
            <form @submit.prevent="submitScore" class="space-y-4">
                <div class="field">
                    <label for="game">Game</label>
                    <Dropdown 
                        id="game"
                        v-model="selectedGame" 
                        :options="games" 
                        optionLabel="name" 
                        placeholder="Select a game"
                        class="w-full"
                        :class="{ 'p-invalid': !selectedGame && showErrors }"
                    />
                    <small v-if="!selectedGame && showErrors" class="p-error">Game is required.</small>
                </div>

                <div class="field">
                    <label for="playerName">Player Name</label>
                    <InputText 
                        id="playerName"
                        v-model="playerName" 
                        placeholder="Enter your name"
                        class="w-full"
                        :class="{ 'p-invalid': !playerName && showErrors }"
                    />
                    <small v-if="!playerName && showErrors" class="p-error">Player name is required.</small>
                </div>

                <div class="field">
                    <label for="score">Score</label>
                    <InputNumber 
                        id="score"
                        v-model="score" 
                        placeholder="Enter your score"
                        class="w-full"
                        :class="{ 'p-invalid': (!score || score <= 0) && showErrors }"
                    />
                    <small v-if="(!score || score <= 0) && showErrors" class="p-error">Valid score is required.</small>
                </div>

                <div class="field">
                    <label for="linkedinUrl">LinkedIn Profile URL (Optional)</label>
                    <InputText 
                        id="linkedinUrl"
                        v-model="linkedinUrl" 
                        placeholder="https://linkedin.com/in/yourprofile"
                        class="w-full"
                    />
                </div>

                <div class="flex justify-end space-x-2">
                    <Button 
                        type="button" 
                        label="Clear" 
                        severity="secondary" 
                        @click="clearForm"
                    />
                    <Button 
                        type="submit" 
                        label="Submit Score" 
                        :loading="isSubmitting"
                    />
                </div>
            </form>
        </template>
    </Card>
</template>

<script>
import { GameService } from '@/services/gameService.js';

export default {
    name: 'ScoreSubmissionForm',
    data() {
        return {
            games: [],
            selectedGame: null,
            playerName: '',
            score: null,
            linkedinUrl: '',
            isSubmitting: false,
            showErrors: false,
            gameService: new GameService()
        };
    },
    async mounted() {
        await this.loadGames();
    },
    methods: {
        async loadGames() {
            try {
                this.games = await this.gameService.getGames();
            } catch (error) {
                console.error('Error loading games:', error);
                this.$toast.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: 'Failed to load games'
                });
            }
        },
        async submitScore() {
            this.showErrors = true;
            
            if (!this.isFormValid()) {
                return;
            }

            this.isSubmitting = true;
            
            try {
                const gameScore = {
                    gameId: this.selectedGame.id,
                    playerName: this.playerName,
                    score: this.score,
                    linkedInProfileUrl: this.linkedinUrl || null
                };

                await this.gameService.submitScore(gameScore);
                
                this.$toast.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: 'Score submitted successfully!'
                });
                
                this.clearForm();
                this.$emit('scoreSubmitted');
                
            } catch (error) {
                console.error('Error submitting score:', error);
                this.$toast.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: 'Failed to submit score'
                });
            } finally {
                this.isSubmitting = false;
            }
        },
        isFormValid() {
            return this.selectedGame && 
                   this.playerName.trim() && 
                   this.score && 
                   this.score > 0;
        },
        clearForm() {
            this.selectedGame = null;
            this.playerName = '';
            this.score = null;
            this.linkedinUrl = '';
            this.showErrors = false;
        }
    }
};
</script>

<style scoped>
.field {
    @apply mb-4;
}

.field label {
    @apply block text-sm font-medium mb-2;
}
</style>