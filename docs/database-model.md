# Modelo de Dados — MongoDB de Vendas

O banco `revenda_vendas` é exclusivo desta API. A coleção de vendas guarda somente `userId`, correspondente ao claim `sub` do JWT; dados cadastrais e credenciais pertencem ao MongoDB de Usuarios.

```mermaid
erDiagram
    VEHICLES {
      string _id PK
      string brand
      string model
      int year
      string color
      decimal price
      string status
      int version
      datetime createdAt
      datetime updatedAt
    }
    SALES {
      string _id PK
      string vehicleId FK
      string userId "JWT sub"
      datetime saleDate
      decimal agreedPrice
      string paymentCode UK
      string paymentStatus
      datetime createdAt
      datetime updatedAt
      datetime paidAt
      datetime canceledAt
    }
    VEHICLES ||--o| SALES : "reserva ou venda"
```

## Coleções e índices

| Coleção | Índices | Regra preservada |
| --- | --- | --- |
| `vehicles` | `{ status: 1, price: 1, _id: 1 }` | Lista veículos disponíveis e vendidos por preço. |
| `sales` | único em `{ vehicleId: 1 }` | Um veículo não pode ter duas vendas. |
| `sales` | único em `{ paymentCode: 1 }` | O código de pagamento identifica uma única transação. |
| `sales` | `{ userId: 1, saleDate: -1 }` | Consulta futura por comprador. |

O campo `version` do veículo apoia atualização otimista. A reserva só ocorre quando o status ainda é `Available`; o processamento do pagamento só finaliza uma venda que ainda está `Pending`.
