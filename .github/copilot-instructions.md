# Copilot Instructions

## Project Structure
- Place domain entities CreditNote and CreditNoteLine in Cartiva.Domain.
- Reserve Cartiva.Shared.SD for static constants and helpers only.

## Entity Status & EF Core
- Use enums directly on entity status fields (do not use string-based status fields).
- Persist enums as readable strings via centralized EF Core configuration in ApplicationDbContext (e.g., HasConversion or a shared ValueConverter).
- When simplifying helper logic after enum normalization, make helpers operate purely on enums.
- Keep ToValue()/FromValue() only at system boundaries (UI/API/DTOs/external integrations/rare string filters); remove them from helpers, business logic, services, domain logic, and internal controller logic when entities already use enums.

## Project Guidelines
- Do not seed review data in DbInitializer. Reviews should only come from real customer submissions.

## Invoicing & Billing
- Link every order to an invoice; provide direct per-order navigation from each order to its invoice in admin order management.
- Generate invoices on the fly for all orders, including orders paid at checkout.
- Auto-process company invoicing, shipment, and email flows for companies with IsActive = true; process active companies regardless of payment method (upfront or deferred).
- Treat inactive companies as individual customers (do not apply company-specific processing for inactive companies).
- Show invoice status explicitly as "Paid" or "Outstanding/Deferred".
- Include a due date on outstanding/deferred invoices.
- Provide a send-invoice link in invoice management for sending receipts or outstanding notices.