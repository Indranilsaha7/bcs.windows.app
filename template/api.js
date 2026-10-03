// Global API Configuration for Cloudflare Pages Backend

const API_CONFIG = {
    // Change this to your deployed Cloudflare Worker URL in production
    BASE_URL: 'http://127.0.0.1:8788', 
    API_KEY: 'backend-bcs-pos'
};

/**
 * Standardized API Fetch Wrapper with Auth Headers
 */
async function fetchAPI(endpoint, options = {}) {
    try {
        const headers = {
            'Content-Type': 'application/json',
            'x-api-key': API_CONFIG.API_KEY,
            ...options.headers
        };

        const response = await fetch(`${API_CONFIG.BASE_URL}${endpoint}`, {
            ...options,
            headers
        });

        const data = await response.json();
        
        if (!response.ok) {
            throw new Error(data.error || 'API Request Failed');
        }

        return data;

    } catch (error) {
        console.error(`API Error on ${endpoint}:`, error);
        showAlert('error', 'Database Error', error.message || 'Failed to connect to backend.');
        return null;
    }
}
