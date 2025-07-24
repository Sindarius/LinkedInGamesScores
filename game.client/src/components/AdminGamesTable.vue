<script setup>
import { ref, onMounted } from 'vue';
import { useToast } from 'primevue/usetoast';
import { AdminService } from '@/services/adminService';

const toast = useToast();
const adminService = new AdminService();

const games = ref([]);
const editingRows = ref([]);
const loading = ref(false);
const newGameDialog = ref(false);
const deleteGameDialog = ref(false);
const selectedGame = ref(null);

const newGame = ref({
    name: '',
    description: '',
    scoringType: 1,
    isActive: true
});

const scoringTypes = ref([
    { label: 'Guesses', value: 1 },
    { label: 'Time', value: 2 }
]);

onMounted(() => {
    loadGames();
});

const loadGames = async () => {
    try {
        loading.value = true;
        games.value = await adminService.getAllGames();
    } catch (error) {
        toast.add({ severity: 'error', summary: 'Error', detail: 'Failed to load games', life: 3000 });
    } finally {
        loading.value = false;
    }
};

const onRowEditSave = async (event) => {
    try {
        const { newData, index } = event;
        await adminService.updateGame(newData.id, newData);
        games.value[index] = newData;
        toast.add({ severity: 'success', summary: 'Success', detail: 'Game updated successfully', life: 3000 });
    } catch (error) {
        toast.add({ severity: 'error', summary: 'Error', detail: 'Failed to update game', life: 3000 });
    }
};

const onRowEditCancel = (event) => {
    const { index } = event;
    delete editingRows.value[games.value[index].id];
};

const openNewGameDialog = () => {
    newGame.value = {
        name: '',
        description: '',
        scoringType: 1,
        isActive: true
    };
    newGameDialog.value = true;
};

const hideNewGameDialog = () => {
    newGameDialog.value = false;
};

const saveNewGame = async () => {
    try {
        const createdGame = await adminService.createGame(newGame.value);
        games.value.push(createdGame);
        newGameDialog.value = false;
        toast.add({ severity: 'success', summary: 'Success', detail: 'Game created successfully', life: 3000 });
    } catch (error) {
        toast.add({ severity: 'error', summary: 'Error', detail: 'Failed to create game', life: 3000 });
    }
};

const confirmDeleteGame = (game) => {
    selectedGame.value = game;
    deleteGameDialog.value = true;
};

const deleteGame = async () => {
    try {
        await adminService.deleteGame(selectedGame.value.id);
        games.value = games.value.filter((g) => g.id !== selectedGame.value.id);
        deleteGameDialog.value = false;
        selectedGame.value = null;
        toast.add({ severity: 'success', summary: 'Success', detail: 'Game deleted successfully', life: 3000 });
    } catch (error) {
        toast.add({ severity: 'error', summary: 'Error', detail: 'Failed to delete game', life: 3000 });
    }
};

const getScoringTypeLabel = (type) => {
    return type === 1 ? 'Guesses' : 'Time';
};

const formatDate = (dateString) => {
    return new Date(dateString).toLocaleDateString();
};
</script>

<template>
    <div>
        <div class="flex justify-content-between align-items-center mb-4">
            <h6 class="m-0">Games Management</h6>
            <Button label="Add Game" icon="pi pi-plus" @click="openNewGameDialog" />
        </div>

        <DataTable v-model:editingRows="editingRows" :value="games" editMode="row" dataKey="id" @row-edit-save="onRowEditSave" @row-edit-cancel="onRowEditCancel" :loading="loading" class="p-datatable-gridlines">
            <Column field="id" header="ID" style="width: 80px">
                <template #body="{ data }">
                    {{ data.id }}
                </template>
            </Column>

            <Column field="name" header="Name" style="min-width: 200px">
                <template #body="{ data }">
                    {{ data.name }}
                </template>
                <template #editor="{ data, field }">
                    <InputText v-model="data[field]" />
                </template>
            </Column>

            <Column field="description" header="Description" style="min-width: 300px">
                <template #body="{ data }">
                    {{ data.description }}
                </template>
                <template #editor="{ data, field }">
                    <Textarea v-model="data[field]" rows="2" />
                </template>
            </Column>

            <Column field="scoringType" header="Scoring Type" style="width: 150px">
                <template #body="{ data }">
                    {{ getScoringTypeLabel(data.scoringType) }}
                </template>
                <template #editor="{ data, field }">
                    <Dropdown v-model="data[field]" :options="scoringTypes" optionLabel="label" optionValue="value" />
                </template>
            </Column>

            <Column field="isActive" header="Active" style="width: 100px">
                <template #body="{ data }">
                    <Tag :value="data.isActive ? 'Active' : 'Inactive'" :severity="data.isActive ? 'success' : 'danger'" />
                </template>
                <template #editor="{ data, field }">
                    <Checkbox v-model="data[field]" :binary="true" />
                </template>
            </Column>

            <Column field="createdDate" header="Created" style="width: 150px">
                <template #body="{ data }">
                    {{ formatDate(data.createdDate) }}
                </template>
            </Column>

            <Column :rowEditor="true" style="width: 10%; min-width: 8rem" bodyStyle="text-align:center"></Column>

            <Column style="width: 80px">
                <template #body="{ data }">
                    <Button icon="pi pi-trash" class="p-button-rounded p-button-danger p-button-text" @click="confirmDeleteGame(data)" />
                </template>
            </Column>
        </DataTable>

        <!-- New Game Dialog -->
        <Dialog v-model:visible="newGameDialog" header="Add New Game" :modal="true" class="p-fluid">
            <div class="field">
                <label for="name">Name</label>
                <InputText id="name" v-model="newGame.name" required />
            </div>
            <div class="field">
                <label for="description">Description</label>
                <Textarea id="description" v-model="newGame.description" rows="3" />
            </div>
            <div class="field">
                <label for="scoringType">Scoring Type</label>
                <Dropdown id="scoringType" v-model="newGame.scoringType" :options="scoringTypes" optionLabel="label" optionValue="value" />
            </div>

            <template #footer>
                <Button label="Cancel" icon="pi pi-times" @click="hideNewGameDialog" class="p-button-text" />
                <Button label="Save" icon="pi pi-check" @click="saveNewGame" />
            </template>
        </Dialog>

        <!-- Delete Confirmation Dialog -->
        <Dialog v-model:visible="deleteGameDialog" header="Confirm" :modal="true">
            <div class="flex align-items-center justify-content-center">
                <i class="pi pi-exclamation-triangle mr-3" style="font-size: 2rem" />
                <span v-if="selectedGame"
                    >Are you sure you want to delete <b>{{ selectedGame.name }}</b
                    >?</span
                >
            </div>
            <template #footer>
                <Button label="No" icon="pi pi-times" @click="deleteGameDialog = false" class="p-button-text" />
                <Button label="Yes" icon="pi pi-check" @click="deleteGame" />
            </template>
        </Dialog>
    </div>
</template>
