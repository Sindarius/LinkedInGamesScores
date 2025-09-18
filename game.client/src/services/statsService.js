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

    async getDailyChampions(date = null) {
        const params = new URLSearchParams();
        if (date) {
            const d = new Date(date);
            const iso = new Date(Date.UTC(d.getFullYear(), d.getMonth(), d.getDate())).toISOString().slice(0, 10);
            params.append('date', iso);
        }
        const url = params.toString() ? `/api/stats/daily-champions?${params.toString()}` : '/api/stats/daily-champions';
        const response = await fetch(url);
        if (!response.ok) {
            throw new Error('Failed to fetch daily champions');
        }
        return await response.json();
    }
}
