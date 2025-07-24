<script setup>
import AdminGamesTable from '@/components/AdminGamesTable.vue';
import AdminScoresTable from '@/components/AdminScoresTable.vue';
</script>

<template>
    <div class="grid">
        <div class="col-12">
            <div class="card">
                <h5>Admin Panel</h5>
                <p class="m-0">Manage games and score entries</p>
            </div>
        </div>

        <div class="col-12">
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
