# BCS Enterprise POS - Project Configuration & Documentation

## What We Have

**Frontend: Electron App**
- **Type**: Desktop POS application (Windows).
- **Core Technology**: Electron, HTML, CSS, Vanilla JavaScript.
- **Views**: `index.html` (Login/Entry), `dashboard.html`, `pos.html`, `add-product.html`, `all-product.html`.
- **Scripts**: 
  - `main.js`: Main Electron process.
  - `script/api.js`: Handles API calls to the backend.
  - `script/auth.js`: Handles user authentication and security.
  - `script/add-alerts.js`, `script/opt-ui.js`: UI logic and alert handling.
  - `script/argon2-bundle.js`: Hashing/encryption logic on the client side.
- **Templates**: Reusable UI components (`sidebar`, `topbar`) and a layout manager (`layout.js`).
- **Build/Release**: Uses `electron-builder` and `.github/workflows` to build and publish updates to GitHub (`Indranilsaha7/bcs.windows.app`).

**Backend: Cloudflare Functions / Workers (D1 Database)**
- **Type**: Serverless API.
- **Location**: `backend/functions/api/`
- **Database**: SQLite using Cloudflare D1 (`context.env.DB`).
- **Core Tables**: `users`, `products`, `customers`, `sales`, `deleted_products`.
- **Key Modules**:
  - `auth/`: Handles session, login, and registration.
  - `database/`: Database operations and queries.
  - `mail/`: Email logic.
  - `cron/`: Scheduled tasks.
  - `init-db.js`: Initializes database tables.

---

## Rules for Editing Files

When collaborating on this project, please adhere to the following rules:

1. **Frontend Changes (Electron)**:
   - Always check the corresponding HTML file before modifying UI JS scripts.
   - Use the centralized `script/api.js` for all backend requests instead of writing raw `fetch` calls scattered across files.
   - Ensure the template structure (e.g., `sidebar`, `topbar`) remains intact when adding new views. Use `template/layout.js` to manage UI component injection.

2. **Backend Changes (Cloudflare Functions)**:
   - All backend API endpoints should reside inside `backend/functions/api/`.
   - Ensure standard CORS headers are attached to all responses (`Access-Control-Allow-Origin: *`, etc.) as seen in `init-db.js`.
   - Use parameterized queries with `context.env.DB.prepare()` to avoid SQL injection when interacting with the database.

3. **General Best Practices**:
   - Do not commit secrets (like `gmail.json` or API keys) directly.
   - Run a quick test of the Electron app (`npm start`) before pushing changes to ensure no breaking runtime errors occur.
   - Document any new database table or column additions in this `config.md` file.

---

## Documentation

### Authentication Flow
- The application uses `argon2` for password hashing and secure token management.
- User sessions are tracked via a `session_id` and verified against a `trusted_port` or security code.

### Database Schema Overview
- **Users**: `id`, `username`, `password_hash`, `security_code`, `session_id`, `trusted_port`, `role`, `permissions`.
- **Products**: `id`, `name`, `category`, `sellPrice`, `quantity`.
- **Customers**: `id`, `name`, `phone`.
- **Sales**: `id`, `total`, `date`.
- **Deleted Products**: Tracks soft-deleted or removed products for auditing (`productId`, `barcode`, `deletedAt`).

### Deployment
- Changes pushed to the GitHub repository automatically trigger a GitHub Actions workflow to build the Electron `.exe` using `electron-builder`.
- Auto-updates are configured via `electron-updater` checking the GitHub Releases page.
