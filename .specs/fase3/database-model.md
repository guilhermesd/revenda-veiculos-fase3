# Modelo MongoDB — Vendas

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
    PURCHASES {
      string _id PK
      string vehicleId FK
      string userId "JWT sub"
      decimal agreedPrice
      string paymentCode UK
      string paymentStatus "Pending | Paid | Canceled"
      datetime purchasedAt
      datetime paidAt
      datetime canceledAt
    }
    VEHICLES ||--o| PURCHASES : "one completed purchase"
```

Índices: `vehicles { status: 1, price: 1, _id: 1 }`, `purchases { vehicleId: 1 }` único, `purchases { paymentCode: 1 }` único, e `purchases { userId: 1, purchasedAt: -1 }`. A confirmação usa filtro condicional em veículo disponível e versionamento otimista.
