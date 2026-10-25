# Discovery — Tech Challenge Fase 3

## Fontes de verdade

- `docs/Fase 3 - Trabalho Reposição Tech Challenge SOAT.pdf`.
- Especificação operacional fornecida para a Fase 3.
- Implementação da Fase 2 em `../fiap-revenda-veiculos`.

## Requisitos identificados no PDF

1. Cadastrar e editar veículos.
2. Permitir a compra online apenas para pessoas previamente cadastradas.
3. Listar veículos à venda e vendidos por preço crescente.
4. Manter registro/autorização de compradores separado dos dados transacionais.
5. Aplicar CI/CD e Pull Requests para mudanças.
6. Entregar README, código funcional, deploy automatizado, vídeo e PDF de links.

## Decisões da solução

- `revenda-veiculos-fase3`: API de Vendas e MongoDB de Vendas.
- `revenda-veiculos-usuarios`: API de Usuarios/Autenticação e MongoDB de Usuarios.
- Cada serviço acessa exclusivamente sua própria base MongoDB.
- A API de Usuarios emite JWT assinado por segredo configurável; a API de Vendas valida o JWT localmente, sem consultar a API de Usuarios a cada requisição.
- A compra grava o `userId` extraído do token, nunca dados sensíveis do cliente, e inicia o pagamento como pendente.
- O webhook da Fase 2 é mantido: `PAID` efetiva a venda e `CANCELED` devolve o veículo ao estoque.
- A execução local terá exatamente quatro containers: duas APIs e dois MongoDBs.
- Cada repositório é dono do MongoDB que lhe pertence: este Compose declara Vendas e inclui o `compose.yaml` do repositório de Usuarios, que declara Usuarios e seu MongoDB.

## Reutilização da Fase 2

- Domínio de veículo, listagem por preço, MongoDB, Dockerfile multi-stage, ProblemDetails, Swagger e testes serão a base da API de Vendas.
- A venda pendente e o webhook de pagamento da Fase 2 serão preservados e adaptados para associar a compra ao cliente autenticado.

## Riscos

- Segredo JWT inconsistente entre as APIs invalida todos os tokens.
- Imagem Docker de um repositório não estar disponível para o Compose do outro.
- O checkout cruzado de repositório privado no GitHub Actions exige token de leitura em Secret.
- Concorrência pode permitir compra dupla sem atualização condicional do veículo.

## Divergências registradas

O PDF exige deploy automatizado e CI/CD, mas não exige Kubernetes. Portanto Kubernetes não integra a Fase 3. O workflow fará deploy simulado com `docker compose up --no-start`, conforme o direcionamento operacional recebido.
