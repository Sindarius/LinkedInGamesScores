const API_BASE_URL =
    window.location.hostname === 'localhost' && window.location.port === '3000'
        ? 'http://localhost:5000/api' // Docker environment
        : process.env.NODE_ENV === 'production'
          ? 'http://localhost:5000/api'
          : 'https://localhost:7036/api';

export class AdminService {
    // Games Management
    async getAllGames() {
        const response = await fetch(`${API_BASE_URL}/games/all`);
        if (!response.ok) {
            throw new Error('Failed to fetch games');
        }
        return await response.json();
    }

    async createGame(game) {
        const response = await fetch(`${API_BASE_URL}/games`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(game)
        });

        if (!response.ok) {
            throw new Error('Failed to create game');
        }
        return await response.json();
    }

    async updateGame(id, game) {
        const response = await fetch(`${API_BASE_URL}/games/${id}`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(game)
        });

        if (!response.ok) {
            throw new Error('Failed to update game');
        }
        return response.ok;
    }

    async deleteGame(id) {
        const response = await fetch(`${API_BASE_URL}/games/${id}`, {
            method: 'DELETE'
        });

        if (!response.ok) {
            throw new Error('Failed to delete game');
        }
        return response.ok;
    }

    // Scores Management
    async getAllScores() {
        const response = await fetch(`${API_BASE_URL}/gamescores`);
        if (!response.ok) {
            throw new Error('Failed to fetch game scores');
        }
        return await response.json();
    }

    async getScore(id) {
        const response = await fetch(`${API_BASE_URL}/gamescores/${id}`);
        if (!response.ok) {
            throw new Error('Failed to fetch game score');
        }
        return await response.json();
    }

    async updateScore(id, gameScore) {
        const response = await fetch(`${API_BASE_URL}/gamescores/${id}`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(gameScore)
        });

        if (!response.ok) {
            throw new Error('Failed to update game score');
        }
        return response.ok;
    }

    async deleteScore(id) {
        const response = await fetch(`${API_BASE_URL}/gamescores/${id}`, {
            method: 'DELETE'
        });

        if (!response.ok) {
            throw new Error('Failed to delete game score');
        }
        return response.ok;
    }

    async createScore(gameScore) {
        const response = await fetch(`${API_BASE_URL}/gamescores`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(gameScore)
        });

        if (!response.ok) {
            throw new Error('Failed to create game score');
        }
        return await response.json();
    }
}
