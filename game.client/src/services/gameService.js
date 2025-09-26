const API_BASE_URL = '/api';

export class GameService {
    async getGames() {
        const response = await fetch(`${API_BASE_URL}/games`);
        if (!response.ok) {
            throw new Error('Failed to fetch games');
        }
        return await response.json();
    }

    async getGameScores(gameId, date = null) {
        let url = `${API_BASE_URL}/gamescores/game/${gameId}`;
        if (date) {
            // Filter by date on the client side for now
            const response = await fetch(url);
            if (!response.ok) {
                throw new Error('Failed to fetch game scores');
            }
            const scores = await response.json();
            const targetDate = new Date(date).toDateString();
            return scores.filter((score) => new Date(score.dateAchieved).toDateString() === targetDate);
        }
        const response = await fetch(url);
        if (!response.ok) {
            throw new Error('Failed to fetch game scores');
        }
        return await response.json();
    }

    async getLeaderboard(gameId, date = null, top = 10) {
        if (date) {
            const d = new Date(date);
            const iso = new Date(Date.UTC(d.getFullYear(), d.getMonth(), d.getDate())).toISOString().slice(0, 10);
            const response = await fetch(`${API_BASE_URL}/gamescores/game/${gameId}/leaderboard/day?date=${iso}&top=${top}`);
            if (!response.ok) {
                throw new Error('Failed to fetch daily leaderboard');
            }
            return await response.json();
        }

        const response = await fetch(`${API_BASE_URL}/gamescores/game/${gameId}/leaderboard?top=${top}`);
        if (!response.ok) {
            throw new Error('Failed to fetch leaderboard');
        }
        return await response.json();
    }

    async submitScore(gameScore) {
        const response = await fetch(`${API_BASE_URL}/gamescores`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(gameScore)
        });

        if (!response.ok) {
            throw new Error('Failed to submit score');
        }
        return await response.json();
    }

    async submitScoreWithImage(gameScoreData, imageFile) {
        const formData = new FormData();
        formData.append('GameId', gameScoreData.gameId);
        formData.append('PlayerName', gameScoreData.playerName);

        if (gameScoreData.guessCount !== undefined && gameScoreData.guessCount !== null) {
            formData.append('GuessCount', gameScoreData.guessCount);
        }

        if (gameScoreData.completionTime) {
            formData.append('CompletionTime', gameScoreData.completionTime);
        }

        if (gameScoreData.linkedInProfileUrl) {
            formData.append('LinkedInProfileUrl', gameScoreData.linkedInProfileUrl);
        }

        if (imageFile) {
            formData.append('ScoreImage', imageFile);
        }

        const response = await fetch(`${API_BASE_URL}/gamescores/with-image`, {
            method: 'POST',
            body: formData
        });

        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(errorText || 'Failed to submit score with image');
        }
        return await response.json();
    }

    async getScoreImage(scoreId) {
        const response = await fetch(`${API_BASE_URL}/gamescores/${scoreId}/image`);
        if (!response.ok) {
            if (response.status === 404) {
                return null; // No image available
            }
            throw new Error('Failed to fetch score image');
        }
        return response.blob();
    }

    async getScoreImageThumbnail(scoreId, width = 200, height = 150) {
        const response = await fetch(`${API_BASE_URL}/gamescores/${scoreId}/image/thumbnail?width=${width}&height=${height}`);
        if (!response.ok) {
            if (response.status === 404) {
                return null; // No image available
            }
            throw new Error('Failed to fetch score image thumbnail');
        }
        return response.blob();
    }

    async getScoresByDateRange(gameId, startDate, endDate) {
        const scores = await this.getGameScores(gameId);
        const start = new Date(startDate);
        const end = new Date(endDate);

        return scores.filter((score) => {
            const scoreDate = new Date(score.dateAchieved);
            return scoreDate >= start && scoreDate <= end;
        });
    }

    getAvailableDates(scores) {
        const dates = [...new Set(scores.map((score) => new Date(score.dateAchieved).toDateString()))];
        return dates.sort((a, b) => new Date(b) - new Date(a));
    }

    // Analytics endpoints
    async getCloseCalls(days = 7) {
        const response = await fetch(`${API_BASE_URL}/analytics/close-calls?days=${days}`);
        if (!response.ok) {
            throw new Error('Failed to fetch close calls data');
        }
        return await response.json();
    }

    async getComebackKings(days = 14) {
        const response = await fetch(`${API_BASE_URL}/analytics/comeback-kings?days=${days}`);
        if (!response.ok) {
            throw new Error('Failed to fetch comeback kings data');
        }
        return await response.json();
    }

    async getConsistencyChampions(days = 30, minScores = 5) {
        const response = await fetch(`${API_BASE_URL}/analytics/consistency-champions?days=${days}&minScores=${minScores}`);
        if (!response.ok) {
            throw new Error('Failed to fetch consistency champions data');
        }
        return await response.json();
    }

    async getScoreDistribution(scoringType, days = 30) {
        const response = await fetch(`${API_BASE_URL}/analytics/distribution/${scoringType}?days=${days}`);
        if (!response.ok) {
            throw new Error('Failed to fetch score distribution data');
        }
        return await response.json();
    }

    async getPhotoFinishes(date = null) {
        let url = `${API_BASE_URL}/analytics/photo-finish`;
        if (date) {
            const d = new Date(date);
            const iso = new Date(Date.UTC(d.getFullYear(), d.getMonth(), d.getDate())).toISOString().slice(0, 10);
            url += `?date=${iso}`;
        }
        const response = await fetch(url);
        if (!response.ok) {
            throw new Error('Failed to fetch photo finish data');
        }
        return await response.json();
    }

    async getPlayerTemperature(playerName, days = 7) {
        const encodedPlayerName = encodeURIComponent(playerName);
        const response = await fetch(`${API_BASE_URL}/analytics/player-temperature/${encodedPlayerName}?days=${days}`);
        if (!response.ok) {
            throw new Error('Failed to fetch player temperature data');
        }
        return await response.json();
    }
}
