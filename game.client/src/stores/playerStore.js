import { defineStore } from 'pinia';

export const usePlayerStore = defineStore('player', {
    state: () => ({
        playerName: '',
        linkedInProfileUrl: ''
    }),

    actions: {
        setPlayerData(name, linkedInUrl = '') {
            this.playerName = name;
            this.linkedInProfileUrl = linkedInUrl;
            this.saveToStorage();
        },

        setPlayerName(name) {
            this.playerName = name;
            this.saveToStorage();
        },

        setLinkedInUrl(url) {
            this.linkedInProfileUrl = url;
            this.saveToStorage();
        },

        clearPlayerData() {
            this.playerName = '';
            this.linkedInProfileUrl = '';
            this.saveToStorage();
        },

        loadFromStorage() {
            try {
                const savedData = localStorage.getItem('playerData');
                if (savedData) {
                    const data = JSON.parse(savedData);
                    this.playerName = data.playerName || '';
                    this.linkedInProfileUrl = data.linkedInProfileUrl || '';
                }
            } catch (error) {
                console.warn('Failed to load player data from localStorage:', error);
            }
        },

        saveToStorage() {
            try {
                const data = {
                    playerName: this.playerName,
                    linkedInProfileUrl: this.linkedInProfileUrl
                };
                localStorage.setItem('playerData', JSON.stringify(data));
            } catch (error) {
                console.warn('Failed to save player data to localStorage:', error);
            }
        }
    },

    getters: {
        hasPlayerData: (state) => Boolean(state.playerName.trim()),
        hasLinkedInUrl: (state) => Boolean(state.linkedInProfileUrl.trim())
    }
});
