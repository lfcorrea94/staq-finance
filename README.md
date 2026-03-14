# Visão Executiva do MVP

## Objetivo do Produto
O MVP tem como objetivo principal...

## Principais Decisões Arquitetônicas
1. **Monólito Modular + Arquitetura Limpa**: A estrutura da aplicação foi projetada para ser modular e seguir os princípios da arquitetura limpa.
2. **Controles**: Os controladores gerenciam a lógica de negócios e a interação com o usuário.
3. **Slug do Workspace na Rota**: Implementamos um slug para o workspace, permitindo uma melhor organização nas rotas da API.
4. **Isolamento Multiusuário**: A arquitetura permite o isolamento seguro de dados entre múltiplos usuários.

## Escopo do Core MVP
O escopo do MVP inclui:
- **Contas**
- **Categorias**
- **Transações**
- **Recorrência**
- **Endpoint de Execução**

## Stack Tecnológica
- **.NET**: Utilizado como a base do desenvolvimento da aplicação.
- **PostgreSQL**: O banco de dados relacional escolhido para armazenar dados de forma eficaz.
- **EF Core**: Para o mapeamento objeto-relacional e manipulação de dados.
- **JWT**: Para autenticação e segurança na troca de informações.

## Notas de Implantação
A aplicação será implantada utilizando **Render** para o ambiente de execução e **Postgres** para a base de dados.