<script setup>
import { ref, onMounted } from 'vue';
import { useToast } from 'primevue/usetoast';
import { AdminService } from '@/services/adminService';

const toast = useToast();
const adminService = new AdminService();

const scores = ref([]);
const games = ref([]);
const editingRows = ref([]);
const loading = ref(false);
const newScoreDialog = ref(false);
const deleteScoreDialog = ref(false);
const selectedScore = ref(null);
const newScoreTimeInput = ref('');

const newScore = ref({
    gameId: null,
    playerName: '',
    guessCount: null,
    completionTime: null,
    dateAchieved: new Date(),
    linkedInProfileUrl: ''
});

onMounted(() => {
    loadScores();
    loadGames();
});

const loadScores = async () => {
    try {
        loading.value = true;
        scores.value = await adminService.getAllScores();
    } catch (error) {
        toast.add({ severity: 'error', summary: 'Error', detail: 'Failed to load scores', life: 3000 });
    } finally {
        loading.value = false;
    }
};

const loadGames = async () => {
    try {
        games.value = await adminService.getAllGames();
    } catch (error) {
        toast.add({ severity: 'error', summary: 'Error', detail: 'Failed to load games', life: 3000 });
    }
};

const onRowEditSave = async (event) => {
    try {
        const { newData, index } = event;
        await adminService.updateScore(newData.id, newData);
        scores.value[index] = newData;
        toast.add({ severity: 'success', summary: 'Success', detail: 'Score updated successfully', life: 3000 });
    } catch (error) {
        toast.add({ severity: 'error', summary: 'Error', detail: 'Failed to update score', life: 3000 });
    }
};

const onRowEditCancel = (event) => {
    const { index } = event;
    delete editingRows.value[scores.value[index].id];
};

const updateGameName = (data) => {
    const game = games.value.find((g) => g.id === data.gameId);
    if (game) {
        data.gameName = game.name;
    }
};

const formatTime = (timeString) => {
    if (!timeString) return '-';
    // Handle TimeSpan format from .NET
    const time = timeString.toString();
    if (time.includes(':')) {
        return time;
    }
    // Convert seconds to HH:MM:SS format
    const totalSeconds = parseInt(time);
    const hours = Math.floor(totalSeconds / 3600);
    const minutes = Math.floor((totalSeconds % 3600) / 60);
    const seconds = totalSeconds % 60;
    return `${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;
};

const formatTimeForEdit = (timeString) => {
    if (!timeString) return '';
    // Return formatted time for editing
    return formatTime(timeString) === '-' ? '' : formatTime(timeString);
};

const updateTimeFromInput = (data, value) => {
    // Update the data directly when input changes
    if (!value || value === '') {
        // Allow clearing the time
        data.completionTime = null;
        return;
    }

    // Handle HH:MM:SS format
    if (value.match(/^\d{1,2}:\d{2}:\d{2}$/)) {
        const parts = value.split(':');
        const hours = parseInt(parts[0]);
        const minutes = parseInt(parts[1]);
        const seconds = parseInt(parts[2]);

        if (minutes < 60 && seconds < 60) {
            const formattedValue = `${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;
            data.completionTime = formattedValue;
            return;
        }
    }

    // Handle MM:SS format (assume 0 hours)
    if (value.match(/^\d{1,2}:\d{2}$/)) {
        const parts = value.split(':');
        const minutes = parseInt(parts[0]);
        const seconds = parseInt(parts[1]);

        if (minutes < 60 && seconds < 60) {
            const formattedValue = `00:${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;
            data.completionTime = formattedValue;
            return;
        }
    }

    // Keep the raw input for validation feedback
    data.completionTime = value;
};

const isInvalidTimeFormat = (timeString) => {
    if (!timeString) return false;

    // Valid formats: HH:MM:SS or MM:SS
    const validHHMMSS = /^\d{1,2}:\d{2}:\d{2}$/.test(timeString);
    const validMMSS = /^\d{1,2}:\d{2}$/.test(timeString);

    if (!validHHMMSS && !validMMSS) return true;

    const parts = timeString.split(':');
    if (validHHMMSS) {
        parseInt(parts[0]); // hours (validation only)
        const minutes = parseInt(parts[1]);
        const seconds = parseInt(parts[2]);
        return minutes >= 60 || seconds >= 60;
    }

    if (validMMSS) {
        const minutes = parseInt(parts[0]);
        const seconds = parseInt(parts[1]);
        return minutes >= 60 || seconds >= 60;
    }

    return false;
};

const openNewScoreDialog = () => {
    newScore.value = {
        gameId: null,
        playerName: '',
        guessCount: null,
        completionTime: null,
        dateAchieved: new Date(),
        linkedInProfileUrl: ''
    };
    newScoreTimeInput.value = '';
    newScoreDialog.value = true;
};

const hideNewScoreDialog = () => {
    newScoreDialog.value = false;
};

const saveNewScore = async () => {
    try {
        // Convert time input to TimeSpan format if provided
        if (newScoreTimeInput.value && newScoreTimeInput.value.match(/^\d{2}:\d{2}:\d{2}$/)) {
            newScore.value.completionTime = newScoreTimeInput.value;
        }

        const createdScore = await adminService.createScore(newScore.value);
        scores.value.push(createdScore);
        newScoreDialog.value = false;
        toast.add({ severity: 'success', summary: 'Success', detail: 'Score created successfully', life: 3000 });
    } catch (error) {
        toast.add({ severity: 'error', summary: 'Error', detail: 'Failed to create score', life: 3000 });
    }
};

const confirmDeleteScore = (score) => {
    selectedScore.value = score;
    deleteScoreDialog.value = true;
};

const deleteScore = async () => {
    try {
        await adminService.deleteScore(selectedScore.value.id);
        scores.value = scores.value.filter((s) => s.id !== selectedScore.value.id);
        deleteScoreDialog.value = false;
        selectedScore.value = null;
        toast.add({ severity: 'success', summary: 'Success', detail: 'Score deleted successfully', life: 3000 });
    } catch (error) {
        toast.add({ severity: 'error', summary: 'Error', detail: 'Failed to delete score', life: 3000 });
    }
};

const formatDate = (dateString) => {
    return new Date(dateString).toLocaleDateString();
};
</script>

<template>
    <div>
        <div class="flex justify-content-between align-items-center mb-4">
            <h6 class="m-0">Scores Management</h6>
            <Button label="Add Score" icon="pi pi-plus" class="px-4 py-2" @click="openNewScoreDialog" />
        </div>

        <DataTable v-model:editingRows="editingRows" :value="scores" editMode="row" dataKey="id" @row-edit-save="onRowEditSave" @row-edit-cancel="onRowEditCancel" :loading="loading" :paginator="true" :rows="20" class="p-datatable-gridlines">
            <Column field="id" header="ID" style="width: 80px">
                <template #body="{ data }">
                    {{ data.id }}
                </template>
            </Column>

            <Column field="gameName" header="Game" style="min-width: 120px">
                <template #body="{ data }">
                    {{ data.gameName }}
                </template>
                <template #editor="{ data }">
                    <Dropdown v-model="data.gameId" :options="games" optionLabel="name" optionValue="id" @change="updateGameName(data)" />
                </template>
            </Column>

            <Column field="playerName" header="Player" style="min-width: 150px">
                <template #body="{ data }">
                    {{ data.playerName }}
                </template>
                <template #editor="{ data, field }">
                    <InputText v-model="data[field]" />
                </template>
            </Column>

            <Column field="guesscount" header="Guesses" style="width: 100px">
                <template #body="{ data }">
                    {{ data.guessCount || '-' }}
                </template>
                <template #editor="{ data }">
                    <InputNumber v-model="data.guessCount" />
                </template>
            </Column>

            <Column field="completionTime" header="Time" style="width: 120px">
                <template #body="{ data }">
                    {{ formatTime(data.completionTime) }}
                </template>
                <template #editor="{ data }">
                    <InputText :model-value="formatTimeForEdit(data.completionTime)" placeholder="HH:MM:SS or MM:SS" @update:model-value="updateTimeFromInput(data, $event)" :class="{ 'p-invalid': isInvalidTimeFormat(data.completionTime) }" />
                </template>
            </Column>

            <Column field="score" header="Score" style="width: 100px">
                <template #body="{ data }">
                    {{ data.score }}
                </template>
            </Column>

            <Column field="dateAchieved" header="Date" style="width: 120px">
                <template #body="{ data }">
                    {{ formatDate(data.dateAchieved) }}
                </template>
                <template #editor="{ data }">
                    <Calendar v-model="data.dateAchieved" dateFormat="yy-mm-dd" />
                </template>
            </Column>

            <Column field="linkedInProfileUrl" header="LinkedIn" style="min-width: 200px">
                <template #body="{ data }">
                    <a v-if="data.linkedInProfileUrl" :href="data.linkedInProfileUrl" target="_blank" class="text-primary"> Profile </a>
                    <span v-else>-</span>
                </template>
                <template #editor="{ data, field }">
                    <InputText v-model="data[field]" />
                </template>
            </Column>

            <Column :rowEditor="true" style="width: 10%; min-width: 8rem" bodyStyle="text-align:center"></Column>

            <Column style="width: 80px">
                <template #body="{ data }">
                    <Button icon="pi pi-trash" class="p-button-rounded p-button-danger p-button-text" size="small" @click="confirmDeleteScore(data)" aria-label="Delete score" />
                </template>
            </Column>
        </DataTable>

        <!-- New Score Dialog -->
        <Dialog v-model:visible="newScoreDialog" header="Add New Score" :modal="true" class="p-fluid">
            <div class="field">
                <label for="gameId">Game</label>
                <Dropdown id="gameId" v-model="newScore.gameId" :options="games" optionLabel="name" optionValue="id" required />
            </div>
            <div class="field">
                <label for="playerName">Player Name</label>
                <InputText id="playerName" v-model="newScore.playerName" required />
            </div>
            <div class="field">
                <label for="guessCount">Guess Count</label>
                <InputNumber id="guessCount" v-model="newScore.guessCount" />
            </div>
            <div class="field">
                <label for="completionTime">Completion Time (HH:MM:SS)</label>
                <InputText id="completionTime" v-model="newScoreTimeInput" placeholder="00:01:30" />
            </div>
            <div class="field">
                <label for="dateAchieved">Date Achieved</label>
                <Calendar id="dateAchieved" v-model="newScore.dateAchieved" dateFormat="yy-mm-dd" />
            </div>
            <div class="field">
                <label for="linkedInUrl">LinkedIn Profile URL</label>
                <InputText id="linkedInUrl" v-model="newScore.linkedInProfileUrl" />
            </div>

            <template #footer>
                <Button label="Cancel" icon="pi pi-times" @click="hideNewScoreDialog" class="p-button-text" />
                <Button label="Save" icon="pi pi-check" @click="saveNewScore" />
            </template>
        </Dialog>

        <!-- Delete Confirmation Dialog -->
        <Dialog v-model:visible="deleteScoreDialog" header="Confirm" :modal="true">
            <div class="flex align-items-center justify-content-center">
                <i class="pi pi-exclamation-triangle mr-3" style="font-size: 2rem" />
                <span v-if="selectedScore"
                    >Are you sure you want to delete the score for <b>{{ selectedScore.playerName }}</b
                    >?</span
                >
            </div>
            <template #footer>
                <Button label="No" icon="pi pi-times" @click="deleteScoreDialog = false" class="p-button-text" />
                <Button label="Yes" icon="pi pi-check" @click="deleteScore" />
            </template>
        </Dialog>
    </div>
</template>
