# Plano de implementação

1. Preparar a solução de Vendas reutilizando a Fase 2 e remover infraestrutura Kubernetes da entrega Fase 3.
2. Implementar a solução de Usuarios e autenticação JWT.
3. Adaptar o domínio de Vendas para compra autenticada, pagamento pendente e webhook idempotente.
4. Proteger endpoints e validar JWT localmente.
5. Implementar testes unitários e de integração.
6. Criar Dockerfiles e Compose de quatro serviços.
7. Criar workflow GitHub Actions com build, `up --no-start`, validação explícita de quatro containers e limpeza.
8. Executar validações locais e atualizar READMEs.
