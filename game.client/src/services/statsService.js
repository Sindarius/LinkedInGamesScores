const API_BASE_URL = '/api';

export class StatsService {
    async getTopWinnersTrend({ days = 7, top = 5, gameId = null } = {}) {
        const params = new URLSearchParams({ days: String(days), top: String(top) });
        if (gameId != null) params.append('gameId', String(gameId));
        const response = await fetch(`${API_BASE_URL}/stats/top-winners?${params.toString()}`);
        if (!response.ok) {
            throw new Error('Failed to fetch top winners trend');
        }
        return await response.json();
    }
}

