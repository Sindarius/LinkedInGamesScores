const API_BASE_URL = '/api';

export class AdminService {
    constructor() {
        this.authToken = localStorage.getItem('adminToken');
    }

    // Authentication Methods
    async authenticate(password) {
        const response = await fetch(`${API_BASE_URL}/admin/authenticate`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ password })
        });

        const result = await response.json();

        if (result.success) {
            this.authToken = result.token;
            localStorage.setItem('adminToken', result.token);
        } else {
            this.authToken = null;
            localStorage.removeItem('adminToken');
        }

        return result;
    }

    async validateToken() {
        if (!this.authToken) {
            return { success: false, message: 'No token available' };
        }

        try {
            const response = await fetch(`${API_BASE_URL}/admin/validate`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({ token: this.authToken })
            });

            const result = await response.json();

            if (!result.success) {
                this.logout();
            }

            return result;
        } catch (error) {
            this.logout();
            return { success: false, message: 'Token validation failed' };
        }
    }

    logout() {
        this.authToken = null;
        localStorage.removeItem('adminToken');
    }

    isAuthenticated() {
        return !!this.authToken;
    }

    getAuthHeaders() {
        return this.authToken
            ? {
                  Authorization: `Bearer ${this.authToken}`,
                  'Content-Type': 'application/json'
              }
            : {
                  'Content-Type': 'application/json'
              };
    }
    // Games Management
    async getAllGames() {
        const response = await fetch(`${API_BASE_URL}/games/all`, {
            headers: this.getAuthHeaders()
        });
        if (!response.ok) {
            if (response.status === 401) {
                throw new Error('Unauthorized: Please login again');
            }
            throw new Error('Failed to fetch games');
        }
        return await response.json();
    }

    async createGame(game) {
        const response = await fetch(`${API_BASE_URL}/games`, {
            method: 'POST',
            headers: this.getAuthHeaders(),
            body: JSON.stringify(game)
        });

        if (!response.ok) {
            if (response.status === 401) {
                throw new Error('Unauthorized: Please login again');
            }
            throw new Error('Failed to create game');
        }
        return await response.json();
    }

    async updateGame(id, game) {
        const response = await fetch(`${API_BASE_URL}/games/${id}`, {
            method: 'PUT',
            headers: this.getAuthHeaders(),
            body: JSON.stringify(game)
        });

        if (!response.ok) {
            if (response.status === 401) {
                throw new Error('Unauthorized: Please login again');
            }
            throw new Error('Failed to update game');
        }
        return response.ok;
    }

    async deleteGame(id) {
        const response = await fetch(`${API_BASE_URL}/games/${id}`, {
            method: 'DELETE',
            headers: this.getAuthHeaders()
        });

        if (!response.ok) {
            if (response.status === 401) {
                throw new Error('Unauthorized: Please login again');
            }
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
            headers: this.getAuthHeaders(),
            body: JSON.stringify(gameScore)
        });

        if (!response.ok) {
            if (response.status === 401) {
                throw new Error('Unauthorized: Please login again');
            }
            throw new Error('Failed to update game score');
        }
        return response.ok;
    }

    async deleteScore(id) {
        const response = await fetch(`${API_BASE_URL}/gamescores/${id}`, {
            method: 'DELETE',
            headers: this.getAuthHeaders()
        });

        if (!response.ok) {
            if (response.status === 401) {
                throw new Error('Unauthorized: Please login again');
            }
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
