const API_BASE_URL = 'https://localhost:7036/api';

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
            return scores.filter(score => 
                new Date(score.dateAchieved).toDateString() === targetDate
            );
        }
        const response = await fetch(url);
        if (!response.ok) {
            throw new Error('Failed to fetch game scores');
        }
        return await response.json();
    }

    async getLeaderboard(gameId, date = null, top = 10) {
        if (date) {
            // Get filtered scores for specific date
            const scores = await this.getGameScores(gameId, date);
            return scores
                .sort((a, b) => b.score - a.score)
                .slice(0, top);
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
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(gameScore)
        });
        
        if (!response.ok) {
            throw new Error('Failed to submit score');
        }
        return await response.json();
    }

    async getScoresByDateRange(gameId, startDate, endDate) {
        const scores = await this.getGameScores(gameId);
        const start = new Date(startDate);
        const end = new Date(endDate);
        
        return scores.filter(score => {
            const scoreDate = new Date(score.dateAchieved);
            return scoreDate >= start && scoreDate <= end;
        });
    }

    getAvailableDates(scores) {
        const dates = [...new Set(scores.map(score => 
            new Date(score.dateAchieved).toDateString()
        ))];
        return dates.sort((a, b) => new Date(b) - new Date(a));
    }
}