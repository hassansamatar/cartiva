# Copilot Instructions

## Project Structure
- Place domain entities CreditNote and CreditNoteLine in Cartiva.Domain.
- Reserve Cartiva.Shared.SD for static constants and helpers only.

## Project Guidelines
- Do not seed review data in DbInitializer. Reviews should only come from real customer submissions.

## Invoicing & Billing
- Link every order to an invoice; provide direct per-order navigation from each order to its invoice in admin order management.
- Generate invoices on the fly for all orders, including orders paid at checkout.
- Show invoice status explicitly as "Paid" or "Outstanding/Deferred".
- Include a due date on outstanding/deferred invoices.
- Provide a send-invoice link in invoice management for sending receipts or outstanding notices.