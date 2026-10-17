# BCS Enterprise POS - Project Configuration & Documentation

## What We Have

**Frontend: Electron App**
- **Type**: Desktop POS application (Windows).
- **Core Technology**: Electron, HTML, CSS, Vanilla JavaScript.
- **Views**: `index.html` (Login/Entry), `dashboard.html`, `pos.html`, `all-product.html`, `add-product.html`, `add-production.html`, `production-history.html`, `add-production-product.html`, `settings.html`.
- **Scripts**: 
  - `main.js`: Main Electron process and auto-update IPC coordinator.
  - `script/api.js`: Handles API calls to the backend with IndexedDB caching and optimistic updates.
  - `script/auth.js`: Handles user authentication and security.
  - `script/add-alerts.js`, `script/opt-ui.js`: UI logic and alert handling.
  - `script/argon2-bundle.js`: Hashing/encryption logic on the client side.
- **Templates**: Reusable UI components (`sidebar`, `topbar`) and a layout manager (`layout.js`).
- **Build/Release**: Uses `electron-builder` and `.github/workflows/build-release.yml` to build and publish updates to GitHub (`Indranilsaha7/bcs.windows.app`).

**Backend: Cloudflare Functions / Workers (D1 Database)**
- **Type**: Serverless API.
- **Location**: `backend/functions/api/`
- **Database**: SQLite using Cloudflare D1 (`context.env.DB`).
- **Core Tables**: `users`, `products`, `productions`, `customers`, `sales`, `purchases`, `deleted_products`.
- **Key Modules**:
  - `auth/`: Handles session, login, and registration.
  - `database/`: Dynamic microservices (`add.js`, `read.js`, `update.js`, `delete.js`, `data-manager.js`).
  - `mail/`: Email logic queue.
  - `cron/`: Scheduled tasks (e.g. 48h purge of soft-deleted products).
- **more details**: [see](backend/before-action-read-me.md)
---
---
## for deployment
- `frontend` : https://github.com/Indranilsaha7/bcs.windows.app
- `backend`  : https://github.com/Indranilsaha7/backend-bcs-pos

## Rules for Editing Files

When collaborating on this project, please adhere to the following rules:

1. **Frontend Changes (Electron)**:
   - Always check the corresponding HTML file before modifying UI JS scripts.
   - Use the centralized `script/api.js` for all backend requests instead of writing raw `fetch` calls scattered across files.
   - Ensure the template structure (e.g., `sidebar`, `topbar`) remains intact when adding new views. Use `template/layout.js` to manage UI component injection.

2. **Backend Changes (Cloudflare Functions)**:
   - All backend API endpoints should reside inside `backend/functions/api/`.
   - Ensure standard CORS headers are attached to all responses (`Access-Control-Allow-Origin: *`, etc.).
   - Use parameterized queries with `context.env.DB.prepare()` and `.bind()` to prevent SQL injection when interacting with the database.
   - Table names must always be sanitized (`table.replace(/[^a-zA-Z0-9_]/g, '')`).

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
- **Products**: `id`, `name`, `barcode`, `barcodeType`, `unit`, `brand`, `category`, `sellPrice`, `buyPrice`, `wholesalePrice`, `quantity`, `alertQuantity`, `isProduction`, `priceTiers`, `imageUrl`, `description`, `deleteScheduledAt`.
- **Productions**: `id`, `batchNumber`, `finishedProductId`, `productName`, `barcode`, `category`, `quantityProduced`, `unit`, `materialsUsed` (JSON array of consumed items), `materialCost`, `overheadCost`, `totalCost`, `costPerUnit`, `date`, `producedBy`, `notes`, `createdAt`.
- **Customers**: `id`, `name`, `phone`.
- **Sales**: `id`, `total`, `date`.
- **Deleted Products**: Tracks soft-deleted or removed products for auditing (`productId`, `barcode`, `deletedAt`).

### System Update System & Deployment Pipeline
- **Auto-Updater Integration**:
  - `electron-updater` monitors GitHub Releases for `Indranilsaha7/bcs.windows.app`.
  - Background silent check executes on startup (`autoUpdater.checkForUpdatesAndNotify()`).
  - IPC lifecycle events bridge `main.js` with renderer views (`template/topbar/topbar.js` and `settings.html`):
    - `checking_for_update`: Displays spinner indicator.
    - `update_available`: Triggers automated background download.
    - `download_progress`: Relays real-time percentage, speed (MB/s), and transferred bytes to UI progress bars.
    - `update_downloaded`: Activates green "Restart to Install" button and modal actions.
    - `update_not_available`: Confirms latest version status.
    - `update_error`: Displays retry alerts.
- **Publishing a New App Version**:
  1. Increment version in `package.json` (e.g. `1.9.6`).
  2. Commit and tag:
     ```bash
     git commit -am "chore: release v1.9.6"
     git tag v1.9.6
     git push origin main --tags
     ```
  3. GitHub Actions (`.github/workflows/build-release.yml`) builds Windows binaries and publishes the release on GitHub.
  4. Installed client instances automatically download and prompt users to restart.

---

### Windows Defender & SmartScreen Compliance
To prevent Windows Defender / SmartScreen from false-flagging the app:

1. **PE Metadata & Permissions (Configured in `package.json`)**:
   - `requestedExecutionLevel: "asInvoker"`: Runs with standard user rights without demanding UAC elevation, avoiding high-risk heuristic triggers.
   - `perMachine: false`: Installs to `%LOCALAPPDATA%\Programs\` rather than system folders, preventing system modification alerts.
   - Populated Publisher, Legal Trademarks, Copyright, and high-resolution `build/icon.ico`.
   - Excluded internal backend and markdown docs from the packaged `.asar` to keep binary clean and small.

2. **Authenticode Code Signing (Optional / Recommended)**:
   - If you have an Authenticode certificate (`.pfx`), add two repository secrets in GitHub (`Settings -> Secrets and variables -> Actions`):
     - `CSC_LINK`: Base64 encoded content of the `.pfx` file.
     - `CSC_KEY_PASSWORD`: Password for the `.pfx` file.
   - `electron-builder` will automatically sign the `.exe` and the uninstaller.

3. **Microsoft Security Intelligence Whitelisting (Free & Instant)**:
   - For any new release binary, submit the installer `.exe` directly to Microsoft's developer portal:
     👉 [https://www.microsoft.com/en-us/wdsi/filesubmission](https://www.microsoft.com/en-us/wdsi/filesubmission)
   - Select **"Software Developer"** -> Category: **"Incorrectly detected as malware (false positive)"**.
   - Microsoft's automated security pipeline scans the binary, clears Defender definitions, and pushes cloud exclusion within 1–2 hours worldwide.
