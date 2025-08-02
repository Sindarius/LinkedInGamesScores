<script setup>
import { ref, onMounted } from 'vue';
import AdminGamesTable from '@/components/AdminGamesTable.vue';
import AdminScoresTable from '@/components/AdminScoresTable.vue';
import { AdminService } from '@/services/adminService.js';

const adminService = new AdminService();
const isAuthenticated = ref(false);
const password = ref('');
const loginError = ref('');
const isLoading = ref(false);

const authenticate = async () => {
    if (!password.value.trim()) {
        loginError.value = 'Please enter a password';
        return;
    }

    isLoading.value = true;
    loginError.value = '';

    try {
        const result = await adminService.authenticate(password.value);

        if (result.success) {
            isAuthenticated.value = true;
            password.value = '';
        } else {
            loginError.value = result.message || 'Authentication failed';
        }
    } catch (error) {
        loginError.value = 'Authentication failed. Please try again.';
    } finally {
        isLoading.value = false;
    }
};

const logout = () => {
    adminService.logout();
    isAuthenticated.value = false;
    password.value = '';
    loginError.value = '';
};

onMounted(async () => {
    if (adminService.isAuthenticated()) {
        const validation = await adminService.validateToken();
        isAuthenticated.value = validation.success;
    }
});
</script>

<template>
    <div class="grid">
        <div class="col-12">
            <div class="card">
                <div class="flex justify-content-between align-items-center">
                    <div>
                        <h5>Admin Panel</h5>
                        <p class="m-0">Manage games and score entries</p>
                    </div>
                    <div v-if="isAuthenticated">
                        <Button label="Logout" icon="pi pi-sign-out" class="p-button-outlined p-button-danger" @click="logout" />
                    </div>
                </div>
            </div>
        </div>

        <!-- Login Form -->
        <div v-if="!isAuthenticated" class="col-12">
            <div class="card">
                <div class="flex justify-content-center">
                    <div class="w-full md:w-6 lg:w-4">
                        <h6 class="text-center mb-4">Admin Authentication Required</h6>

                        <div class="field">
                            <label for="password" class="block text-900 font-medium mb-2">Password</label>
                            <Password id="password" v-model="password" :feedback="false" toggleMask class="w-full" placeholder="Enter admin password" @keyup.enter="authenticate" :class="{ 'p-invalid': loginError }" />
                            <small v-if="loginError" class="p-error">{{ loginError }}</small>
                        </div>

                        <Button label="Login" icon="pi pi-sign-in" class="w-full" @click="authenticate" :loading="isLoading" :disabled="!password.trim()" />
                    </div>
                </div>
            </div>
        </div>

        <!-- Admin Panel Content -->
        <div v-if="isAuthenticated" class="col-12">
            <div class="card">
                <TabView>
                    <TabPanel header="Games Management">
                        <AdminGamesTable />
                    </TabPanel>
                    <TabPanel header="Scores Management">
                        <AdminScoresTable />
                    </TabPanel>
                </TabView>
            </div>
        </div>
    </div>
</template>
