# Integração entre APIs

```mermaid
sequenceDiagram
    participant C as Cliente
    participant A as API Usuarios
    participant V as API Vendas
    C->>A: POST /api/v1/auth/login
    A-->>C: JWT assinado
    C->>V: POST /vehicles/{id}/purchases + Bearer JWT
    V->>V: valida assinatura, issuer, audience e exp
    V->>V: usa sub como userId e cria pagamento Pending
    V-->>C: 201 compra pendente
    participant P as Processador de Pagamento
    P->>V: webhook PAID ou CANCELED
    V->>V: efetiva ou cancela a venda
```

Falhas de token retornam 401. A API de Vendas não faz chamada síncrona à API de Usuarios para validar token; isso reduz acoplamento e mantém operação disponível enquanto o token for válido.
