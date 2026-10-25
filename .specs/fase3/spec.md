# Spec funcional e técnica — API de Vendas

## Casos de uso

- Criar, consultar e editar veículo disponível.
- Listar veículos disponíveis por preço crescente.
- Iniciar compra de veículo autenticado e receber o resultado de pagamento por webhook.
- Listar veículos vendidos por preço crescente.

## Regras de negócio

- Apenas usuário autenticado pode comprar.
- Um veículo `Available` pode ter somente uma compra efetivada.
- A compra de veículo `Available` cria uma venda `Pending`, grava `userId`, data UTC, snapshot do preço e `paymentCode`; o veículo passa para `PaymentPending`.
- O webhook `PAID` altera a venda para `Paid` e o veículo para `Sold`.
- O webhook `CANCELED` altera a venda para `Canceled` e devolve o veículo para `Available`.
- Veículo vendido ou com pagamento pendente não pode ser editado ou recomprado.
- Listas usam `price ASC, id ASC`; paginação é obrigatória.
- A API não armazena nome, senha, CPF ou coleção de usuarios.

## Endpoints

| Método | Rota | Autenticação | Resultado |
|---|---|---|---|
| POST | `/api/v1/vehicles` | JWT | Cria veículo |
| GET | `/api/v1/vehicles/{id}` | JWT | Consulta veículo |
| PUT | `/api/v1/vehicles/{id}` | JWT | Edita veículo disponível |
| GET | `/api/v1/vehicles/available` | JWT | Lista estoque ordenado |
| POST | `/api/v1/vehicles/{id}/purchases` | JWT | Efetiva compra para o `sub` do token |
| GET | `/api/v1/vehicles/sold` | JWT | Lista vendas ordenadas |
| POST | `/api/v1/payments/webhook` | segredo do webhook | Confirma ou cancela pagamento |

## Erros

- `400`: entrada/identificador inválido.
- `401`: token ausente, inválido ou expirado.
- `404`: veículo inexistente.
- `409`: veículo indisponível, vendido ou concorrência.
- `500`: erro inesperado, sem detalhes internos.

## Critérios de aceite

- Swagger documenta todos os endpoints e respostas.
- Compra sem JWT recebe 401; token válido inicia no máximo uma venda pendente por veículo.
- Duas compras concorrentes do mesmo veículo resultam em no máximo uma venda pendente.
- Repetição do mesmo webhook é idempotente; evento final conflitante retorna 409.
- Testes unitários não dependem de MongoDB; testes de integração validam endpoints relevantes.
