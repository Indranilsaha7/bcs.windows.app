const fs = require('fs');
const path = require('path');

const users = JSON.parse(fs.readFileSync(path.join(__dirname, 'users.json')));
let sqlContent = '';

function escapeSql(val) {
    if (val === null || val === undefined) return 'NULL';
    const str = String(val).replace(/'/g, "''");
    return `'${str}'`;
}

for (const [id, user] of Object.entries(users)) {
    // If it's the admin, we map it properly or we insert everything
    // But admin 1 {sanjoy saha} in JSON doesn't have a username field?
    // Let's check:
    const username = user.Username || user.username || id;
    const pwd = user.password || user.PasswordHash || user.password_hash || '';
    const code = user.code || user.CodeHash || user.security_code || '';
    const role = user.Role || user.role || 'employee';
    const permissions = JSON.stringify(user.permissions || []);

    if (!pwd) continue; // Skip if no password because NOT NULL

    sqlContent += `INSERT OR REPLACE INTO users (id, username, password_hash, security_code, device_token, role, permissions) VALUES (${escapeSql(id)}, ${escapeSql(username)}, ${escapeSql(pwd)}, ${escapeSql(code)}, ${escapeSql(user.device_token)}, ${escapeSql(role)}, ${escapeSql(permissions)});\n`;
}

fs.writeFileSync(path.join(__dirname, 'full_users_import.sql'), sqlContent);
console.log('Successfully generated full_users_import.sql');
