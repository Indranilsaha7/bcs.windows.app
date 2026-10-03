const fs = require('fs');
const path = require('path');

const outputSqlFile = path.join(__dirname, 'migration.sql');
let sqlContent = '';

// Helper to escape SQL strings
function escapeSql(val) {
    if (val === null || val === undefined) return 'NULL';
    if (typeof val === 'number') return val;
    if (typeof val === 'object') {
        // Handle Firebase Timestamp
        if (val._seconds) {
            const d = new Date(val._seconds * 1000);
            return `'${d.toISOString().replace('T', ' ').substring(0, 19)}'`;
        }
        // Fallback for arrays/objects
        val = JSON.stringify(val);
    }
    // Escape single quotes by doubling them
    const str = String(val).replace(/'/g, "''");
    return `'${str}'`;
}

// 1. Users
sqlContent += `
CREATE TABLE IF NOT EXISTS users (
    id TEXT PRIMARY KEY,
    username TEXT NOT NULL,
    password_hash TEXT NOT NULL,
    security_code TEXT,
    device_token TEXT,
    role TEXT DEFAULT 'employee',
    permissions TEXT
);
`;

if (fs.existsSync(path.join(__dirname, 'users.json'))) {
    const users = JSON.parse(fs.readFileSync(path.join(__dirname, 'users.json')));
    for (const [id, user] of Object.entries(users)) {
        sqlContent += `INSERT OR IGNORE INTO users (id, username, password_hash, security_code, device_token, role, permissions) VALUES (${escapeSql(id)}, ${escapeSql(user.Username || user.username)}, ${escapeSql(user.PasswordHash || user.password_hash)}, ${escapeSql(user.CodeHash || user.security_code)}, ${escapeSql(user.device_token)}, ${escapeSql(user.Role || user.role)}, ${escapeSql(user.permissions)});\n`;
    }
}

// 2. Customers
sqlContent += `
CREATE TABLE IF NOT EXISTS customers (
    id TEXT PRIMARY KEY,
    displayId TEXT,
    name TEXT,
    firstName TEXT,
    lastName TEXT,
    language TEXT,
    dob TEXT,
    email TEXT,
    phone TEXT,
    cardNumber TEXT,
    customerType TEXT,
    password TEXT,
    code TEXT,
    points INTEGER DEFAULT 0,
    createdAt DATETIME
);
`;

if (fs.existsSync(path.join(__dirname, 'customers.json'))) {
    const customers = JSON.parse(fs.readFileSync(path.join(__dirname, 'customers.json')));
    for (const [id, c] of Object.entries(customers)) {
        sqlContent += `INSERT OR IGNORE INTO customers (id, displayId, name, firstName, lastName, language, dob, email, phone, cardNumber, customerType, password, code, points, createdAt) VALUES (${escapeSql(id)}, ${escapeSql(c.displayId)}, ${escapeSql(c.name)}, ${escapeSql(c.firstName)}, ${escapeSql(c.lastName)}, ${escapeSql(c.language)}, ${escapeSql(c.dob)}, ${escapeSql(c.email)}, ${escapeSql(c.phone)}, ${escapeSql(c.cardNumber)}, ${escapeSql(c.customerType)}, ${escapeSql(c.password)}, ${escapeSql(c.code)}, ${escapeSql(c.points)}, ${escapeSql(c.createdAt)});\n`;
    }
}

// 3. Suppliers
sqlContent += `
CREATE TABLE IF NOT EXISTS suppliers (
    id TEXT PRIMARY KEY,
    name TEXT,
    shopName TEXT,
    email TEXT,
    phone TEXT,
    address TEXT
);
`;

if (fs.existsSync(path.join(__dirname, 'suppliers.json'))) {
    const suppliers = JSON.parse(fs.readFileSync(path.join(__dirname, 'suppliers.json')));
    for (const [id, s] of Object.entries(suppliers)) {
        sqlContent += `INSERT OR IGNORE INTO suppliers (id, name, shopName, email, phone, address) VALUES (${escapeSql(id)}, ${escapeSql(s.name)}, ${escapeSql(s.shopName)}, ${escapeSql(s.email)}, ${escapeSql(s.phone)}, ${escapeSql(s.address)});\n`;
    }
}

// 4. Daily Reports
sqlContent += `
CREATE TABLE IF NOT EXISTS daily_reports (
    id TEXT PRIMARY KEY,
    date DATETIME,
    openingCash REAL DEFAULT 0,
    totalWork REAL DEFAULT 0,
    totalSales REAL DEFAULT 0,
    totalDuePaid REAL DEFAULT 0,
    walletPayments REAL DEFAULT 0,
    walletDeposits REAL DEFAULT 0,
    totalCashInDrawer REAL DEFAULT 0,
    cashSellReturn REAL DEFAULT 0,
    walletSellReturn REAL DEFAULT 0,
    returnToOwner REAL DEFAULT 0,
    finalCash REAL DEFAULT 0,
    usedProducts TEXT,
    totalUsedProductCost REAL DEFAULT 0,
    note TEXT,
    savedBy TEXT,
    savedAt DATETIME
);
`;

if (fs.existsSync(path.join(__dirname, 'daily_reports.json'))) {
    const reports = JSON.parse(fs.readFileSync(path.join(__dirname, 'daily_reports.json')));
    for (const [id, r] of Object.entries(reports)) {
        sqlContent += `INSERT OR IGNORE INTO daily_reports (id, date, openingCash, totalWork, totalSales, totalDuePaid, walletPayments, walletDeposits, totalCashInDrawer, cashSellReturn, walletSellReturn, returnToOwner, finalCash, usedProducts, totalUsedProductCost, note, savedBy, savedAt) VALUES (${escapeSql(id)}, ${escapeSql(r.date)}, ${escapeSql(r.openingCash)}, ${escapeSql(r.totalWork)}, ${escapeSql(r.totalSales)}, ${escapeSql(r.totalDuePaid)}, ${escapeSql(r.walletPayments)}, ${escapeSql(r.walletDeposits)}, ${escapeSql(r.totalCashInDrawer)}, ${escapeSql(r.cashSellReturn)}, ${escapeSql(r.walletSellReturn)}, ${escapeSql(r.returnToOwner)}, ${escapeSql(r.finalCash)}, ${escapeSql(r.usedProducts)}, ${escapeSql(r.totalUsedProductCost)}, ${escapeSql(r.note)}, ${escapeSql(r.savedBy)}, ${escapeSql(r.savedAt)});\n`;
    }
}

// 5. Products
sqlContent += `
CREATE TABLE IF NOT EXISTS products (
    id TEXT PRIMARY KEY,
    name TEXT,
    barcode TEXT,
    barcodeType TEXT,
    unit TEXT,
    brand TEXT,
    category TEXT,
    quantity REAL DEFAULT 0,
    buy_price REAL DEFAULT 0,
    sell_price REAL DEFAULT 0,
    wholesale_price REAL DEFAULT 0,
    shop_sell_price REAL DEFAULT 0,
    shop_wholesale_price REAL DEFAULT 0,
    alertQuantity REAL,
    description TEXT,
    packetPerBox REAL,
    pcsPerBox REAL,
    pcsPerPacket REAL,
    imageUrl TEXT,
    lots TEXT,
    created_at DATETIME,
    updated_at DATETIME
);
`;

if (fs.existsSync(path.join(__dirname, 'products.json'))) {
    const products = JSON.parse(fs.readFileSync(path.join(__dirname, 'products.json')));
    for (const [id, p] of Object.entries(products)) {
        let buy = 0, sell = 0, ws = 0, sws = 0, ss = 0;
        if (p.priceTiers && p.priceTiers.pcs) {
            buy = p.priceTiers.pcs.buy || 0;
            sell = p.priceTiers.pcs.sell || 0;
            ws = p.priceTiers.pcs.wholesale || 0;
            sws = p.priceTiers.pcs.shop_wholesale || 0;
            ss = p.priceTiers.pcs.shop_sell || 0;
        }
        
        sqlContent += `INSERT OR IGNORE INTO products (id, name, barcode, barcodeType, unit, brand, category, quantity, buy_price, sell_price, wholesale_price, shop_sell_price, shop_wholesale_price, alertQuantity, description, packetPerBox, pcsPerBox, pcsPerPacket, imageUrl, lots, created_at, updated_at) VALUES (${escapeSql(id)}, ${escapeSql(p.name)}, ${escapeSql(p.barcode)}, ${escapeSql(p.barcodeType)}, ${escapeSql(p.unit)}, ${escapeSql(p.brand)}, ${escapeSql(p.category)}, ${escapeSql(p.quantity)}, ${escapeSql(buy)}, ${escapeSql(sell)}, ${escapeSql(ws)}, ${escapeSql(ss)}, ${escapeSql(sws)}, ${escapeSql(p.alertQuantity)}, ${escapeSql(p.description)}, ${escapeSql(p.packetPerBox)}, ${escapeSql(p.pcsPerBox)}, ${escapeSql(p.pcsPerPacket)}, ${escapeSql(p.imageUrl)}, ${escapeSql(p.lots)}, ${escapeSql(p.createdAt)}, ${escapeSql(p.updatedAt)});\n`;
    }
}

fs.writeFileSync(outputSqlFile, sqlContent);
console.log('Successfully generated migration.sql');
