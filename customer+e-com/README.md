# Baroda Computers And Sales - Customer Portal & E-Commerce (2-in-1)

## Overview
This is the 2-in-1 Web Customer Portal & E-Commerce Storefront for **Baroda Computers And Sales**.

### Key Capabilities
1. **Public E-Commerce Storefront**:
   - Live product browsing connected directly to the store inventory (`products` table in Cloudflare D1).
   - Real-time stock status (in-stock / out-of-stock badges).
   - Shopping cart with instant price and quantity tally.
   - Secure checkout sending sanitized product IDs and quantities to `POST /api/ecommerce/order`.

2. **Customer Portal**:
   - Customer account login & registration.
   - Order history and real-time delivery status tracking.
   - Profile and delivery address management.
   - Digital invoices and receipts.

### Security Architecture
- Connects to `https://backend.bcs.bcsdeveloper.com/api`.
- Never sends prices to the backend during checkout — backend validates and calculates official item prices from D1.
- Public catalog queries use `/api/ecommerce/products`, hiding internal store costs (`buyPrice`, `wholesalePrice`).
