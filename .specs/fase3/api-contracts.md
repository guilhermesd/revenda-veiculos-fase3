# Contratos HTTP

## Compra

`POST /api/v1/vehicles/{vehicleId}/purchases`

Header:

```http
Authorization: Bearer <jwt>
```

Body da compra:

```json
{ "paymentCode": "payment-unique-code" }
```

Resposta `201 Created`:

```json
{
  "id": "purchase-id",
  "vehicleId": "vehicle-id",
  "agreedPrice": 50000.00,
  "paymentCode": "payment-unique-code",
  "paymentStatus": "Pending",
  "purchasedAt": "2026-10-25T12:00:00Z"
}
```

O `userId` não vem do body: é extraído do claim `sub` validado no token.

## Webhook de pagamento

`POST /api/v1/payments/webhook`

Header `X-Webhook-Secret` e body:

```json
{ "paymentCode": "payment-unique-code", "status": "PAID" }
```

Aceita `PAID` e `CANCELED`; chamadas repetidas com o mesmo estado retornam sucesso idempotente.

## JWT aceito

- Assinatura HMAC configurada por `Jwt__Key`.
- Emissor e audiência configurados por `Jwt__Issuer` e `Jwt__Audience`.
- Claims mínimos: `sub`, `email`, `role`, `iss`, `aud`, `exp`.
