const Database = require('better-sqlite3');
const path = require('path');
const fs = require('fs');

// Ensure database directory exists inside the app
const dbDir = path.join(__dirname, 'database');
if (!fs.existsSync(dbDir)) {
    fs.mkdirSync(dbDir);
}

// Initialize Local SQLite Database
const dbPath = path.join(dbDir, 'pos_local.db');
const db = new Database(dbPath);
db.pragma('journal_mode = WAL'); // Better performance for reading/writing

// Initialize Tables if they don't exist
function initDB() {
    db.exec(`
        CREATE TABLE IF NOT EXISTS products (
            id TEXT PRIMARY KEY,
            name TEXT NOT NULL,
            sku TEXT UNIQUE NOT NULL,
            category TEXT,
            price REAL NOT NULL,
            cost REAL NOT NULL,
            stock INTEGER DEFAULT 0,
            barcode TEXT,
            created_at DATETIME DEFAULT CURRENT_TIMESTAMP
        );

        CREATE TABLE IF NOT EXISTS customers (
            id TEXT PRIMARY KEY,
            name TEXT NOT NULL,
            phone TEXT UNIQUE,
            email TEXT,
            address TEXT,
            total_spent REAL DEFAULT 0.0,
            created_at DATETIME DEFAULT CURRENT_TIMESTAMP
        );

        CREATE TABLE IF NOT EXISTS sales (
            id TEXT PRIMARY KEY,
            total_amount REAL NOT NULL,
            discount REAL DEFAULT 0,
            payment_method TEXT,
            created_at DATETIME DEFAULT CURRENT_TIMESTAMP
        );
    `);
}

initDB();

// Basic wrapper functions for the frontend to use
module.exports = {
    db,
    
    // Dashboard Stats
    getDashboardStats: () => {
        return {
            totalProducts: db.prepare('SELECT COUNT(*) as count FROM products').get().count,
            outOfStock: db.prepare('SELECT COUNT(*) as count FROM products WHERE stock <= 0').get().count,
            inStock: db.prepare('SELECT SUM(stock) as total FROM products WHERE stock > 0').get().total || 0,
            categories: db.prepare('SELECT COUNT(DISTINCT category) as count FROM products').get().count,
            totalCustomers: db.prepare('SELECT COUNT(*) as count FROM customers').get().count,
            totalSale: 0.00, // Placeholder until sales logic is built
            totalPurchase: 0.00,
            netProfit: 0.00,
            totalOrders: 0
        };
    },

    // Product Functions
    getProducts: (search = '') => {
        if (search) {
            return db.prepare('SELECT * FROM products WHERE name LIKE ? OR sku LIKE ? ORDER BY created_at DESC')
                     .all(`%${search}%`, `%${search}%`);
        }
        return db.prepare('SELECT * FROM products ORDER BY created_at DESC').all();
    },
    
    addProduct: (product) => {
        const stmt = db.prepare(`
            INSERT INTO products (id, name, sku, category, price, cost, stock, barcode) 
            VALUES (@id, @name, @sku, @category, @price, @cost, @stock, @barcode)
        `);
        const id = require('crypto').randomUUID();
        stmt.run({ id, ...product });
        return id;
    }
};
