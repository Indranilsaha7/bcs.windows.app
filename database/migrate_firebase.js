require('dotenv').config();
const fs = require('fs');

async function migrate() {
    try {
        const rawData = fs.readFileSync('database.json', 'utf8');
        const database = JSON.parse(rawData);
        
        // Assuming the product data is an array or an object in the JSON
        const products = database.products || database; 
        const items = Array.isArray(products) ? products : Object.values(products);

        for (const item of items) {
            if (!item || !item.name || item.price === undefined) {
                console.warn(`Skipping item missing name or price:`, item);
                continue;
            }

            const payload = {
                name: String(item.name),
                price: Number(item.price)
            };

            try {
                const response = await fetch('https://backend.bcs.bcsdeveloper.com/api/products', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'x-api-key': process.env.API_KEY
                    },
                    body: JSON.stringify(payload)
                });

                if (response.ok) {
                    console.log(`Successfully uploaded product: ${payload.name}`);
                } else {
                    console.error(`Failed to upload product: ${payload.name}. Status: ${response.status} ${response.statusText}`);
                }
            } catch (err) {
                console.error(`Error uploading product: ${payload.name}`, err.message);
            }
        }
    } catch (err) {
        console.error('Error during migration:', err.message);
    }
}

migrate();
