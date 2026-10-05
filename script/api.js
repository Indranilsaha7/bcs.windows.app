const API_BASE_URL = 'https://backend.bcs.bcsdeveloper.com/api';
const API_KEY = 'backend-bcs-pos';

export async function fetchAPI(endpoint, options = {}) {
    const url = endpoint.startsWith('http') ? endpoint : `${API_BASE_URL}/${endpoint.replace(/^\//, '')}`;
    
    const headers = {
        'x-api-key': API_KEY,
        'Content-Type': 'application/json',
        'x-user-id': localStorage.getItem('loggedInUserId') || '',
        'x-security-code': localStorage.getItem('loggedInUserCodeHash') || '',
        ...options.headers
    };

    const response = await fetch(url, { ...options, headers });
    
    if (!response.ok) {
        throw new Error(`API Error: ${response.status} ${response.statusText}`);
    }

    const data = await response.json();
    return data;
}

export async function getCollection(collectionName) {
    const data = await fetchAPI(`database/data-manager?table=${collectionName}`);
    if (data && data.success && data.data) {
        // Map to mock firebase doc interface
        return {
            docs: data.data.map(item => ({
                id: item.id.toString(),
                firestoreId: item.id.toString(),
                data: () => {
                    const parsed = {...item};
                    if (parsed.buy_price !== undefined) parsed.buyPrice = parsed.buy_price;
                    if (parsed.sell_price !== undefined) parsed.sellPrice = parsed.sell_price;
                    if (parsed.wholesale_price !== undefined) parsed.wholesalePrice = parsed.wholesale_price;
                    if (parsed.shop_sell_price !== undefined) parsed.shopSellPrice = parsed.shop_sell_price;
                    if (parsed.shop_wholesale_price !== undefined) parsed.shopWholesalePrice = parsed.shop_wholesale_price;
                    if (parsed.created_at) parsed.date = { seconds: new Date(parsed.created_at).getTime() / 1000 };
                    
                    // Parse JSON arrays if they are strings
                    ['items', 'lots', 'priceTiers', 'permissions'].forEach(k => {
                        if (typeof parsed[k] === 'string') {
                            try { parsed[k] = JSON.parse(parsed[k]); } catch(e){}
                        }
                    });
                    return parsed;
                }
            }))
        };
    }
    return { docs: [] };
}

export async function updateDocument(collectionName, id, data) {
    return await fetchAPI(`database/data-manager?table=${collectionName}`, {
        method: 'PUT',
        body: JSON.stringify({ id, ...data })
    });
}

export async function deleteDocument(collectionName, id) {
    return await fetchAPI(`database/data-manager?table=${collectionName}&id=${id}`, {
        method: 'DELETE'
    });
}

export async function getDocument(collectionName, id) {
    // hacky way since data-manager get by ID is just a search or array find
    const coll = await getCollection(collectionName);
    const doc = coll.docs.find(d => d.id === id);
    if (doc) return { exists: () => true, data: () => doc.data(), id: doc.id };
    return { exists: () => false };
}
