# Architecture Blueprint: BCSdevloper™ Ecosystem

## WHAT WE HAVE DONE
- Migrated from Firebase to Cloudflare D1 SQL (`bcs_pos_db`).
- Implemented strict 3-Layer Security (App Code via `x-api-key`, User ID, and Argon2id JSON RBAC permissions).
- Stripped Firebase out of the C# Desktop App completely and set up dynamic URL switching (Localhost vs Production).

## WHAT WE ARE BUILDING (The Ecosystem)
- A unified backend powering a C# Desktop POS, Web Customer Portal (E-commerce), and Web Delivery Portal.
- Strict Database Isolation: Employees/Admins use `users` table, Customers use `customers` table.

## UPCOMING AUTOMATIONS
- DB Sync: Main POS DB syncs to Customer DB every 1 hour (save read/write limits).
- Auth Check: Background communication to verify valid sessions.
- Source Check: Auto-refresh frontend if codebase changes.
- POS Connection Checker: Auto-check connection every 2 hours.
