# Baroda Computers And Sales - Delivery & Logistics Portal

## Overview
Mobile-friendly logistics portal for delivery agents, dispatchers, and drivers of **Baroda Computers And Sales**.

### Key Capabilities
1. **Driver Authentication**:
   - Secure login for verified delivery agents (`delivery_agents` table in Cloudflare D1).
2. **Active Order Dispatch**:
   - View assigned orders with customer phone, delivery address, and order item checklist.
   - Quick "Call Customer" and "Open in Google Maps" shortcuts.
3. **Delivery Lifecycle Updates**:
   - Status updates: `assigned` ➔ `picked_up` ➔ `in_transit` ➔ `delivered`.
   - Customer OTP verification to prevent lost or fraudulent package hand-offs.
   - Real-time sync back to the POS register and Customer Portal.
