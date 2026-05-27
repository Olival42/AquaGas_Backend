# AquaGás Distribuidora — Backend (Sistema de Gestão Operacional)

O **AquaGás** é um sistema interno de gestão operacional para distribuidoras de **água e gás**, criado para resolver problemas de **operações descentralizadas**, **controle manual** e **baixa visibilidade** sobre **vendas, estoque e contratos recorrentes**.

Este repositório contém o **backend** (API) do sistema, estruturado por domínios/módulos.

## O que o projeto soluciona

Em operações típicas de distribuidoras, é comum encontrar:

- **Dados descentralizados**: clientes, vendas e contratos em planilhas/anotações/memória operacional.
- **Processos manuais suscetíveis a erro**: lançamentos esquecidos/duplicados/inconsistentes.
- **Estoque sem rastreabilidade**: divergência entre estoque físico e registros.
- **Baixa auditabilidade**: dificuldade em responder “quem fez?”, “quando?”, “por quê?”.
- **Risco financeiro**: inconsistência entre vendas, recebimentos e contratos ativos.
- **Ausência de controle de permissões**: ações críticas executadas por qualquer operador.

O AquaGás centraliza as operações, automatiza pontos críticos (principalmente **controle de estoque**), aplica **controle de acesso por perfil (RBAC)** e mantém **integridade/rastreabilidade** com auditoria.

## Visão do produto

O sistema é uma **plataforma web interna** para integrar e controlar processos críticos da distribuidora, com objetivos de:

- **Centralizar informações operacionais**
- **Automatizar processos críticos**
- **Garantir integridade dos dados**
- **Melhorar eficiência operacional**
- **Apoiar decisões estratégicas** com relatórios

## Principais funcionalidades

As funcionalidades abaixo foram organizadas seguindo o escopo do produto (MoSCoW), com foco no MVP.

### Must Have (MVP)

- **Autenticação e controle de acesso** (RBAC)
- **Cadastro de clientes** com **unicidade de CPF/CNPJ**
- **Cadastro de produtos**
- **Controle de estoque** com **baixa automática** e bloqueio de estoque negativo
- **Registro de vendas avulsas**
- **Gestão de planos recorrentes** (contratos por cliente, itens, quantidades, ciclos)

### Should Have

- **Histórico de consumo** por cliente (por produto/quantidade/período)
- **Relatórios operacionais**
- **Auditoria de operações** (logins, alterações e exclusões críticas)

### Could Have (futuro)

- Dashboards gerenciais
- Alertas de estoque
- Exportação de dados

### Fora do escopo (não implementado por definição do produto)

- Portal do cliente
- Pagamentos online
- Acesso externo

## Perfis de usuário (acesso)

- **Gestor**
  - Acesso total às funcionalidades
  - **Exclusivo** para: excluir registros críticos, aplicar descontos, acessar relatórios gerenciais
- **Funcionário**
  - Operação do dia a dia (cadastros, vendas, movimentações)
  - **Sem permissão** para ações críticas (ex.: descontos e exclusões)

## Arquitetura (visão rápida)

- Backend em **C# / .NET** com API Web.
- Banco de dados: **PostgreSQL**.
- Cache/blacklist de tokens: **Redis**.
- Estrutura por domínios/módulos (ex.: **Auth, Customer, Employee, Product, Sale, Plan, Report**).

## Como rodar o projeto

### Pré-requisitos

- **Docker + Docker Compose**
- Opcional para rodar local sem container: **.NET SDK 10**

### Rodando com Docker Compose (recomendado)

1) Crie um arquivo `.env` na raiz do projeto (você pode copiar de `.env.example`).

2) Suba os serviços:

```bash
docker compose up --build
```

3) A API ficará disponível em:

- `http://localhost:5000/swagger` (porta padrão do `.env.example`)

> Observação: a porta externa é controlada por `API_PORT` no `.env`.

### Variáveis de ambiente (Docker)

O `docker-compose.yml` injeta as configurações via variáveis (exemplos em `.env.example`):

- **API**
  - `API_PORT` (porta externa da API)
  - `ASPNETCORE_ENVIRONMENT` (ex.: `Development`)
- **PostgreSQL**
  - `POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PASSWORD`, `POSTGRES_PORT`
  - `CONNECTION_STRING` (usada como `ConnectionStrings__DefaultConnection`)
- **Redis**
  - `PORT_REDIS`
  - `REDIS_CONNECTION` (usada como `ConnectionStrings__Redis`)
- **JWT**
  - `JWT__KEY`, `JWT__ISSUER`, `JWT__AUDIENCE`

### Migrations e seed (importante)

Ao iniciar, a API executa automaticamente:

- **Migrations** dos bancos/contexts dos módulos
- **Seed** inicial (quando aplicável)

Isso ocorre quando o ambiente **não** é `Testing`.

### Rodando localmente (sem Docker)

Você precisa ter um **PostgreSQL** e um **Redis** disponíveis e exportar as variáveis equivalentes às do Compose:

- `ConnectionStrings__DefaultConnection`
- `ConnectionStrings__Redis`
- `Jwt__Key`, `Jwt__Issuer`, `Jwt__Audience` (ou equivalentes com `__`)

Depois execute:

```bash
dotnet run --project src/AquaGas.API/AquaGas.API.csproj
```

Por padrão (launch settings), o Swagger abre em:

- `http://localhost:5217/swagger`

## Rodando testes

Execute os testes com:

```bash
dotnet test
```

## Licença / uso

Sistema de **uso interno** da organização (sem portal para clientes e sem pagamentos online, conforme escopo do produto).

