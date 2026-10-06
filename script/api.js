const API_BASE_URL = 'https://backend.bcs.bcsdeveloper.com/api';

export async function fetchAPI(endpoint, options = {}) {
    const url = endpoint.startsWith('http') ? endpoint : `${API_BASE_URL}/${endpoint.replace(/^\//, '')}`;
    
    // We NO LONGER send the API key by default. 
    // We send the session ID and trusted port number instead!
    const headers = {
        'Content-Type': 'application/json',
        'x-session-id': localStorage.getItem('sessionId') || '',
        'x-session-port': localStorage.getItem('sessionPort') || '',
        ...options.headers
    };

    const response = await fetch(url, { ...options, headers });
    
    if (response.status === 401) {
        // Session is invalid or expired! Auto-logout.
        localStorage.removeItem('isAuthenticated');
        localStorage.removeItem('loggedInUserId');
        localStorage.removeItem('userRole');
        localStorage.removeItem('sessionId');
        localStorage.removeItem('sessionPort');
        window.location.replace('index.html');
        throw new Error("Session expired. Please log in again.");
    }

    if (!response.ok) {
        const err = await response.json().catch(() => ({}));
        throw new Error(err.error || `API Error: ${response.status} ${response.statusText}`);
    }

    return await response.json();
}

export async function getCollection(collectionName) {
    const data = await fetchAPI(`database/data-manager?table=${collectionName}`);
    if (data && data.success && data.data) {
        return {
            docs: data.data.map(item => ({
                id: item.id || item.firestoreId,
                data: () => item
            }))
        };
    }
    return { docs: [] };
}

export async function getDocument(collectionName, id) {
    const coll = await getCollection(collectionName);
    const doc = coll.docs.find(d => d.id === id);
    if (doc) return { exists: () => true, data: () => doc.data(), id: doc.id };
    return { exists: () => false };
}

export async function addDocument(collectionName, data) {
    return await fetchAPI(`database/data-manager?table=${collectionName}`, {
        method: 'POST',
        body: JSON.stringify(data)
    });
}

export async function updateDocument(collectionName, id, data) {
    return await fetchAPI(`database/data-manager?table=${collectionName}&id=${id}`, {
        method: 'PUT',
        body: JSON.stringify(data)
    });
}

export async function deleteDocument(collectionName, id) {
    return await fetchAPI(`database/data-manager?table=${collectionName}&id=${id}`, {
        method: 'DELETE'
    });
}
