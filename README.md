# API de Vendas — Tech Challenge Fase 3

API .NET 10 de veículos e vendas autenticadas. A compra usa o `sub` do JWT emitido pela API de Usuarios, cria pagamento pendente e é finalizada pelo webhook `PAID` ou `CANCELED`.

## Regras de acesso

Todas as rotas de veículos exigem JWT válido. `ADM` pode cadastrar, editar e listar veículos; `COMPRADOR` pode listar e iniciar compras. Cadastro/edição exigem `ADM`; compra exige `COMPRADOR`.

### Swagger com Bearer

Faça login na API de Usuários, copie `accessToken`, abra o Swagger de Vendas, clique em **Authorize** e cole somente o JWT no esquema **Bearer**. O Swagger enviará `Authorization: Bearer <token>`.

## Execução local

Os dois repositórios devem ser diretórios irmãos:

```text
fiap-revenda-veiculos-fase3/
fiap-revenda-veiculos-usuarios/
```

Na raiz deste repositório, o Compose inclui o arquivo de Usuarios. O MongoDB de Vendas é definido aqui; o MongoDB de Usuarios é definido no repositório de Usuarios.

```powershell
Copy-Item .env.example .env
docker compose up -d --build
```

Serviços: Vendas `http://localhost:8080/swagger`, Usuarios `http://localhost:8081/swagger`; MongoDBs não expõem portas públicas.

Caso outro ambiente local já use essas portas, como a Fase 2, utilize `docker compose -f compose.yaml -f compose.local-ports.yaml up -d`. Nesse caso, os endereços serão `http://localhost:8180/swagger` e `http://localhost:8181/swagger`.

## CI/CD

`.github/workflows/deploy.yml` é acionado em push na `main` ou manualmente. Ele testa as APIs, constrói imagens, cria exatamente quatro containers com `docker compose up --no-start`, verifica o estado `created` e remove os recursos. É uma simulação de deploy no runner do GitHub Actions, não um deploy remoto de produção.

## Testes

```powershell
dotnet test Fiap.RevendaVeiculos.slnx
```

> Para executar os testes de integração da categoria `Docker`, o Compose deve estar ativo. Caso não esteja, o teste exibirá o comando necessário: `docker compose -f compose.yaml -f compose.local-ports.yaml up -d --build`.

### Teste de integração de papéis

O teste de integração valida o ambiente completo: cadastro/login de usuários, autorização de `ADM` e `COMPRADOR`, cadastro, consulta, edição e listagens de veículos, compra e os webhooks `PAID` e `CANCELED`. Ele usa as APIs reais em `8180` e `8181`.

```powershell
docker compose -f compose.yaml -f compose.local-ports.yaml up -d --build
dotnet test Fiap.RevendaVeiculos.slnx --filter "Category=Docker"
```

Se o Compose não estiver em execução, o teste falhará com uma mensagem orientando o comando acima. Para encerrar o ambiente ao final: `docker compose -f compose.yaml -f compose.local-ports.yaml down`.

Cobertura do ciclo: cadastro e login de `ADM`/`COMPRADOR`, uso de JWT, criação, consulta, edição e listagem de veículos, bloqueio de criação por comprador, compra, webhook `PAID`, listagem de vendidos, webhook `CANCELED` e liberação do veículo.

## SDD

Os requisitos, arquitetura, contratos, modelagem, integração, plano e tarefas estão em `.specs/fase3/`.

## Documentação de API e banco

- [Design system da API de Vendas](docs/api-design-system.md): convenções HTTP, autenticação, endpoints e ciclo da compra.
- [Modelo de dados MongoDB de Vendas](docs/database-model.md): coleções, campos, índices e regras de integridade.

## Etapas do CI/CD

O workflow [deploy.yml](.github/workflows/deploy.yml) é ativado automaticamente a cada `push` na branch `main`. Também pode ser iniciado manualmente em **Actions > ci-cd-simulado > Run workflow**.

1. Faz checkout dos repositórios de Vendas e Usuarios como diretórios irmãos.
2. Instala o .NET 10 e executa os testes das duas soluções.
3. Cria o `.env`, valida a composição Docker, constrói as imagens e obtém as imagens MongoDB.
4. Simula o deploy com `docker compose up --no-start`.
5. Confirma que existem exatamente quatro containers no estado `created`.
6. Executa a limpeza com `docker compose down --remove-orphans`, inclusive em caso de falha.

Se o repositório de Usuarios for privado, configure o secret `CLIENTES_REPOSITORY_TOKEN` neste repositório para permitir o checkout durante o workflow. O pipeline é uma validação/deploy simulado no runner do GitHub Actions; ele não publica em ambiente remoto.
