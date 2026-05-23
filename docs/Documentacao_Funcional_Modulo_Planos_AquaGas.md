# 5. Documentacao por endpoint/use case

## 5.1 Criacao de plano

### Endpoint

`POST /api/plans/register`

### Objetivo da funcionalidade

Criar um novo plano para um cliente, com itens, valor total, entregas futuras e cobrancas futuras.

### Quando deve ser usada

Quando o cliente esta contratando um novo plano e o sistema precisa iniciar toda a agenda comercial, operacional e financeira.

### Regras de negocio

- o plano e criado com status inicial `Active`
- o sistema gera automaticamente as entregas e cobrancas do contrato
- cada mes do contrato gera um periodo
- o valor total do contrato considera quantidade dos produtos, duracao do contrato e eventual desconto
- desconto so pode ser aplicado por perfil gerencial
- cliente com multa contratual em aberto nao pode contratar novo plano
- cliente com mensalidades vencidas pode gerar alerta antes da criacao
- para plano customizado, a duracao precisa ser informada
- o inicio do plano respeita antecedencia minima operacional de 3 dias

### Validacoes importantes

- `CustomerId` obrigatorio
- `Cycle` obrigatorio
- `DeliveryDay` entre 1 e 31
- `BillingDay` entre 1 e 31
- lista de itens obrigatoria
- nao e permitido produto duplicado na lista
- quantidade dos itens deve ser maior que zero
- `DurationInMonths` so deve ser enviada para ciclo customizado
- em ciclo customizado, a duracao deve estar entre 2 e 60 meses
- se houver desconto e o usuario nao for gerente, a operacao e bloqueada

### Impactos no sistema

- cria o plano
- cria os itens do plano
- cria todas as entregas futuras
- cria todas as cobrancas futuras
- registra o colaborador responsavel pela criacao

### Request de exemplo

```json
{
  "customerId": "acd41b1f-5b9f-47b1-bf6b-69389cb2d53a",
  "cycle": "Annual",
  "discount": 10,
  "deliveryDay": 11,
  "billingDay": 10,
  "durationInMonths": null,
  "ignoreWarnings": false,
  "items": [
    {
      "productId": "f0da26ce-33a8-47f4-850b-d17edde5e03c",
      "quantity": 2
    }
  ]
}
```

### Response de sucesso

```json
{
  "success": true,
  "data": {
    "id": "3b408997-63b4-4fdb-aad6-a7e9a851e713",
    "customerId": "acd41b1f-5b9f-47b1-bf6b-69389cb2d53a",
    "customerName": "Joao Victor",
    "document": "95625989000135",
    "employeeId": "13dbd641-7f0a-4ee0-b8c8-03ccda6d80f7",
    "employeeName": "Gerente Inicial",
    "cycle": "Annual",
    "status": "Active",
    "total": 2400.00,
    "discount": 10,
    "deliveryDay": 11,
    "billingDay": 10,
    "startDate": "2026-06-10T00:00:00Z",
    "endDate": "2027-05-11T00:00:00Z",
    "items": [
      {
        "productId": "f0da26ce-33a8-47f4-850b-d17edde5e03c",
        "productName": "Gas 12kg",
        "quantity": 2
      }
    ],
    "deliveries": [
      {
        "id": "db8435a4-681a-4d9e-957b-f97ede56127a",
        "period": 1,
        "dueDate": "2026-06-11T00:00:00Z",
        "deliveryDate": null,
        "status": "Pending"
      }
    ],
    "billings": [
      {
        "id": "c4df6b29-a42e-4dc5-a8df-0f39b6d1a5e2",
        "period": 1,
        "dueDate": "2026-06-10T00:00:00Z",
        "amount": 200.00,
        "status": "Pending",
        "paidAt": null
      }
    ]
  }
}
```

### Possiveis erros e bloqueios

- cliente nao encontrado
- colaborador responsavel nao encontrado
- produto nao encontrado
- cliente com multa contratual em aberto
- cliente com cobrancas vencidas e `ignoreWarnings = false`
- desconto enviado por usuario sem permissao
- ciclo invalido
- duracao invalida no plano customizado

### Cenarios permitidos

- criar plano anual, trimestral, mensal ou customizado
- criar plano com um ou mais produtos
- criar plano com desconto quando autorizado
- criar plano mesmo com alerta de inadimplencia, desde que o fluxo permita ignorar aviso

### Cenarios nao permitidos

- criar plano para cliente com multa em aberto
- criar plano sem itens
- criar plano com produtos duplicados
- criar plano customizado sem duracao
- criar plano com desconto por perfil nao autorizado

### Observacoes importantes

- a criacao ja deixa o plano pronto para cobranca e entrega
- entregas e cobrancas nascem em sequencia por periodo
- a resposta de sucesso retorna a estrutura do plano com agenda gerada

## 5.2 Consulta de plano por ID

### Endpoint

`GET /api/plans/{id}`

### Objetivo da funcionalidade

Consultar o detalhe completo de um plano, incluindo itens, entregas, cobrancas e multas.

### Quando deve ser usada

Quando o negocio precisa enxergar o estado consolidado de um plano especifico.

### Regras de negocio

- a consulta retorna o plano com seus relacionamentos funcionais
- permite visualizar o historico operacional atual do contrato

### Validacoes importantes

- o plano precisa existir

### Impactos no sistema

- nao altera dados

### Request de exemplo

Sem corpo de requisicao.

### Response de sucesso

```json
{
  "success": true,
  "data": {
    "id": "3b408997-63b4-4fdb-aad6-a7e9a851e713",
    "customerId": "acd41b1f-5b9f-47b1-bf6b-69389cb2d53a",
    "cycle": "Annual",
    "status": "Active",
    "items": [
      {
        "productId": "f0da26ce-33a8-47f4-850b-d17edde5e03c",
        "productName": "Gas 12kg",
        "quantity": 2
      }
    ],
    "deliveries": [
      {
        "id": "db8435a4-681a-4d9e-957b-f97ede56127a",
        "period": 1,
        "dueDate": "2026-06-11T00:00:00Z",
        "deliveryDate": null,
        "status": "Pending"
      }
    ],
    "billings": [
      {
        "id": "c4df6b29-a42e-4dc5-a8df-0f39b6d1a5e2",
        "period": 1,
        "dueDate": "2026-06-10T00:00:00Z",
        "amount": 200.00,
        "status": "Pending",
        "paidAt": null
      }
    ],
    "penalties": []
  }
}
```

### Possiveis erros e bloqueios

- plano nao encontrado
- cliente vinculado nao encontrado
- colaborador vinculado nao encontrado

### Cenarios permitidos

- consulta de plano ativo
- consulta de plano suspenso
- consulta de plano cancelado

### Cenarios nao permitidos

- consulta de plano inexistente

### Observacoes importantes

- este endpoint e a melhor referencia para apoio operacional e auditoria funcional

## 5.3 Consulta de todos os planos

### Endpoint

`GET /api/plans`

### Objetivo da funcionalidade

Listar todos os planos com sua estrutura funcional consolidada.

### Quando deve ser usada

Para listagens, paines, operacao, filtros e monitoramento.

### Regras de negocio

- retorna lista de planos
- cada item da lista traz estrutura semelhante a consulta detalhada

### Validacoes importantes

- nao possui request body

### Impactos no sistema

- nao altera dados

### Request de exemplo

Sem corpo de requisicao.

### Response de sucesso

```json
{
  "success": true,
  "data": [
    {
      "id": "3b408997-63b4-4fdb-aad6-a7e9a851e713",
      "customerId": "acd41b1f-5b9f-47b1-bf6b-69389cb2d53a",
      "cycle": "Annual",
      "status": "Active",
      "deliveries": [
        {
          "id": "db8435a4-681a-4d9e-957b-f97ede56127a",
          "period": 1,
          "dueDate": "2026-06-11T00:00:00Z",
          "deliveryDate": null,
          "status": "Pending"
        }
      ],
      "billings": [
        {
          "id": "c4df6b29-a42e-4dc5-a8df-0f39b6d1a5e2",
          "period": 1,
          "dueDate": "2026-06-10T00:00:00Z",
          "amount": 200.00,
          "status": "Pending",
          "paidAt": null
        }
      ]
    }
  ]
}
```

### Possiveis erros e bloqueios

- nao foram identificados bloqueios de negocio especificos

### Cenarios permitidos

- listagem geral de planos

### Cenarios nao permitidos

- nao se aplica

### Observacoes importantes

- importante para backlog de dashboards, filtros por status e acompanhamento de carteira

## 5.4 Upgrade de plano

### Endpoint

`PATCH /api/plans/{id}/upgrade`

### Objetivo da funcionalidade

Ampliar o plano, seja aumentando itens, aumentando duracao ou alterando ciclo para um formato superior, sempre com aumento real de valor total.

### Quando deve ser usada

Quando o cliente deseja expandir o contrato, adicionando valor ao plano atual sem encerrar o contrato existente.

### Regras de negocio

- upgrade nao gera multa contratual
- o novo valor total precisa ser maior que o valor atual
- o endpoint aceita:
  - aumento de quantidade de itens existentes
  - inclusao de novos itens
  - alteracao de ciclo
  - extensao de duracao
- nao permite reduzir quantidade de item neste endpoint
- entregas futuras podem ser recalculadas
- cobrancas futuras sao recalculadas em valor
- se o prazo do contrato aumentar, novas cobrancas e novas entregas podem ser criadas
- entregas reagendadas manualmente devem ser preservadas e nao devem ser sobrescritas automaticamente

### Validacoes importantes

- o plano precisa existir
- o plano nao pode estar cancelado
- o plano nao pode estar finalizado
- o plano nao pode estar suspenso
- deve haver pelo menos uma alteracao informada
- para ciclo customizado, a duracao precisa ser maior que zero
- nao sao permitidos produtos duplicados na lista enviada
- quantidade precisa ser maior que zero
- produto precisa existir e estar ativo
- nao pode haver multa pendente ou vencida
- nao pode haver cobranca atrasada ha mais de 15 dias
- nao pode haver entrega pendente marcada para hoje
- nao pode haver periodo ja entregue com cobranca ainda pendente ou vencida
- nao e permitido upgrade quando todas as mensalidades ja estao pagas
- a nova duracao nao pode ser menor que a duracao atual

### Impactos no sistema

- atualiza valor total do plano
- pode alterar ciclo do plano
- pode estender data final do plano
- recalcula cobrancas futuras
- recalcula datas de entregas futuras
- pode gerar novas cobrancas
- pode gerar novas entregas
- preserva entregas com reagendamento manual

### Request de exemplo

```json
{
  "cycle": "Annual",
  "durationInMonths": null,
  "reason": "Cliente solicitou aumento de consumo",
  "items": [
    {
      "productId": "f0da26ce-33a8-47f4-850b-d17edde5e03c",
      "quantity": 3
    }
  ]
}
```

### Response de sucesso

```json
{
  "success": true,
  "data": {
    "planId": "3b408997-63b4-4fdb-aad6-a7e9a851e713",
    "previousTotal": 2400.00,
    "newTotal": 3600.00,
    "previousCycle": "Annual",
    "cycle": "Annual",
    "updatedBillings": 12,
    "updatedDeliveries": 12,
    "updatedItems": 1,
    "message": "Plan upgraded successfully"
  }
}
```

### Possiveis erros e bloqueios

- plano nao encontrado
- plano cancelado
- plano finalizado
- plano suspenso
- multa pendente ou vencida
- cobranca com atraso superior a 15 dias
- entrega pendente agendada para hoje
- periodo entregue com cobranca ainda aberta
- produto inexistente ou inativo
- tentativa de reduzir item dentro do endpoint de upgrade
- nova duracao menor que a atual
- novo total sem aumento real
- todas as mensalidades ja pagas

### Cenarios permitidos

- aumentar quantidade de item
- adicionar produto novo
- ampliar duracao do contrato
- alterar ciclo para nova configuracao valida

### Cenarios nao permitidos

- reduzir quantidade de item
- executar upgrade com multa em aberto
- executar upgrade com plano suspenso
- executar upgrade quando ja nao existem mensalidades futuras a recalcular

### Observacoes importantes

- upgrade e uma acao de crescimento contratual
- qualquer comportamento de reducao deve ser tratado em fluxo de downgrade, nao neste endpoint
- periodos de entrega e cobranca devem permanecer consistentes apos o upgrade

## 5.5 Downgrade de plano

### Endpoint

`PATCH /api/plans/{id}/downgrade`

### Objetivo da funcionalidade

Reduzir escopo financeiro ou operacional do plano, permitindo diminuir ciclo, reduzir quantidade de produtos existentes ou remover produtos do contrato.

### Quando deve ser usada

Quando o cliente deseja diminuir o contrato atual sem cancelar completamente o plano.

### Regras de negocio

- downgrade pode gerar multa contratual
- o novo valor total do contrato precisa ser menor que o valor atual
- o endpoint aceita:
  - reducao de quantidade de itens existentes
  - remocao de item enviando `quantity = 0`
  - alteracao para um ciclo menor
  - reducao de duracao no caso de ciclo customizado
- nao permite aumentar quantidade de item
- nao permite incluir produto novo
- item nao enviado no request permanece como esta
- nao permite remover todos os itens do plano
- cobrancas pagas nunca sao alteradas
- entregas concluidas nunca sao alteradas
- entregas reagendadas manualmente devem ser preservadas
- periodos ja concluidos ou pagos permanecem intactos
- mensalidades de periodos ja entregues e ainda nao pagos ficam preservadas e fora do recálculo
- se o novo prazo do contrato for menor, cobrancas e entregas futuras excedentes podem ser canceladas
- o downgrade nao remove historico do contrato

### Validacoes importantes

- o plano precisa existir
- o plano nao pode estar cancelado
- o plano nao pode estar finalizado
- o plano nao pode estar suspenso
- o plano nao pode estar em `AwaitingClosure`
- deve haver pelo menos uma alteracao informada
- para ciclo customizado, a duracao precisa ser maior que zero
- nao sao permitidos produtos duplicados na lista enviada
- quantidade precisa ser zero ou maior
- produto precisa ja existir no plano
- nao pode haver multa pendente ou vencida
- nao pode haver cobranca atrasada ha mais de 15 dias
- nao e permitido downgrade quando todas as mensalidades ja estao pagas
- nao pode haver reducao para abaixo de periodos ja pagos ou entregues
- nao pode haver inconsistencia entre periodos futuros de entrega e cobranca
- nao pode haver downgrade sem reducao real de valor
- nao pode gerar multa zero ou negativa

### Impactos no sistema

- atualiza valor total do plano
- pode alterar ciclo do plano
- pode reduzir data final do plano
- recalcula cobrancas futuras elegiveis
- recalcula datas de entregas futuras elegiveis
- pode cancelar cobrancas futuras excedentes
- pode cancelar entregas futuras excedentes
- gera multa contratual do tipo `Downgrade` quando houver reducao financeira real
- registra resumo das alteracoes no historico do plano

### Request de exemplo

```json
{
  "cycle": "Custom",
  "durationInMonths": 4,
  "reason": "Cliente solicitou reducao contratual",
  "items": [
    {
      "productId": "f0da26ce-33a8-47f4-850b-d17edde5e03c",
      "quantity": 1
    }
  ]
}
```

### Response de sucesso

```json
{
  "success": true,
  "data": {
    "planId": "3b408997-63b4-4fdb-aad6-a7e9a851e713",
    "previousTotal": 2400.00,
    "newTotal": 800.00,
    "previousCycle": "Annual",
    "cycle": "Custom",
    "updatedBillings": 4,
    "updatedDeliveries": 4,
    "canceledBillings": 8,
    "canceledDeliveries": 8,
    "updatedItems": 1,
    "removedItems": 0,
    "penalty": {
      "id": "7cb26321-8c7b-48cb-bd8a-e5e3d7c5d111",
      "planId": "3b408997-63b4-4fdb-aad6-a7e9a851e713",
      "type": "Downgrade",
      "status": "PendingPayment",
      "calculatedAmount": 1600.00
    },
    "summary": "Downgrade applied with financial reduction of 1600.00.",
    "message": "Plan downgraded successfully"
  }
}
```

### Possiveis erros e bloqueios

- plano nao encontrado
- plano cancelado
- plano finalizado
- plano suspenso
- plano em `AwaitingClosure`
- multa pendente ou vencida
- cobranca com atraso superior a 15 dias
- produto inexistente no plano
- tentativa de aumentar quantidade
- tentativa de incluir produto novo
- tentativa de remover todos os itens
- nova configuracao sem reducao real de valor
- nova duracao menor que periodos ja concluidos
- inconsistencias entre cobrancas futuras e entregas futuras
- ausencia de mensalidades futuras elegiveis para recálculo

### Cenarios permitidos

- reduzir quantidade de item existente
- remover um item especifico enviando `quantity = 0`
- reduzir ciclo do contrato
- reduzir duracao do contrato em ciclo customizado
- manter itens nao enviados exatamente como estao

### Cenarios nao permitidos

- aumentar quantidade de item
- incluir produto novo
- remover o unico item do plano
- executar downgrade com multa em aberto
- executar downgrade com plano suspenso
- alterar periodos ja pagos ou ja entregues

### Observacoes importantes

- o downgrade preserva historico e atua apenas no futuro elegivel do contrato
- a multa bloqueia novos upgrades e downgrades enquanto estiver pendente ou vencida
- mensalidades de periodos ja entregues e ainda nao pagos nao sao recalculadas automaticamente

## 5.6 Suspensao de plano

### Endpoint

`PATCH /api/plans/{id}/suspend`

### Objetivo da funcionalidade

Pausar temporariamente um plano ativo, interrompendo agenda futura operacional e financeira.

### Quando deve ser usada

Quando o contrato precisa ser interrompido sem cancelamento definitivo.

### Regras de negocio

- apenas planos ativos podem ser suspensos
- entregas futuras nao vinculadas a periodos ja pagos sao canceladas
- cobrancas pendentes e vencidas sao canceladas
- plano passa para status `Suspended`
- se existir entrega pendente para hoje, a suspensao e bloqueada

### Validacoes importantes

- o plano precisa existir
- o plano precisa estar `Active`
- motivo da suspensao e obrigatorio, com minimo de 5 caracteres
- nao pode existir entrega de hoje ainda pendente

### Impactos no sistema

- altera status do plano para `Suspended`
- cancela entregas pendentes/atrasadas de periodos nao pagos
- cancela cobrancas pendentes/atrasadas
- preserva historico de periodos ja pagos

### Request de exemplo

```json
{
  "reason": "Cliente solicitou pausa temporaria"
}
```

### Response de sucesso

```json
{
  "success": true,
  "data": {
    "planId": "3b408997-63b4-4fdb-aad6-a7e9a851e713",
    "status": "Suspended",
    "canceledDeliveries": 5,
    "canceledBillings": 5,
    "reason": "Cliente solicitou pausa temporaria",
    "message": "Plan suspended successfully"
  }
}
```

### Possiveis erros e bloqueios

- plano nao encontrado
- plano nao esta ativo
- entrega pendente agendada para hoje

### Cenarios permitidos

- suspender plano ativo com agenda futura ainda aberta

### Cenarios nao permitidos

- suspender plano cancelado
- suspender plano ja suspenso
- suspender plano com entrega de hoje pendente

### Observacoes importantes

- suspensao nao encerra definitivamente o contrato
- como ha cancelamento de agenda futura, a reativacao precisa recalcular datas

## 5.7 Reativacao de plano

### Endpoint

`PATCH /api/plans/{id}/reactivate`

### Objetivo da funcionalidade

Retomar um plano suspenso e reconstruir a agenda futura de entregas e cobrancas.

### Quando deve ser usada

Quando um cliente ou a operacao decide reativar um contrato que estava pausado.

### Regras de negocio

- apenas planos suspensos podem ser reativados
- o sistema recalcula datas futuras a partir da data atual
- entregas pendentes, atrasadas ou canceladas entram no replanejamento
- cobrancas pendentes, atrasadas ou canceladas entram no replanejamento
- o plano volta para `Active`

### Validacoes importantes

- o plano precisa existir
- o plano precisa estar `Suspended`
- nao pode existir cobranca vencida
- nao pode existir multa em aberto
- o plano precisa ter agenda futura para reativar

### Impactos no sistema

- altera status do plano para `Active`
- reagenda entregas futuras
- reagenda cobrancas futuras
- pode mover datas para frente para adequacao operacional

### Request de exemplo

Sem corpo de requisicao.

### Response de sucesso

```json
{
  "success": true,
  "data": {
    "planId": "3b408997-63b4-4fdb-aad6-a7e9a851e713",
    "status": "Active",
    "rescheduledDeliveries": 5,
    "rescheduledBillings": 5,
    "message": "Plan reactivated successfully"
  }
}
```

### Possiveis erros e bloqueios

- plano nao encontrado
- plano nao esta suspenso
- existem mensalidades vencidas
- existe multa pendente ou vencida
- nao existem agendas futuras para reativar

### Cenarios permitidos

- reativar plano suspenso sem pendencias financeiras impeditivas

### Cenarios nao permitidos

- reativar plano ativo
- reativar plano cancelado
- reativar plano com multa em aberto
- reativar plano com cobrancas vencidas

### Observacoes importantes

- reativacao pode recalcular datas futuras
- este recálculo e parte central da regra de negocio e precisa estar claro em fluxo operacional

## 5.8 Cancelamento de plano

### Endpoint

`PATCH /api/plans/{id}/cancel`

### Objetivo da funcionalidade

Encerrar definitivamente um plano, interrompendo agenda futura e podendo gerar multa contratual.

### Quando deve ser usada

Quando o cliente decide encerrar o contrato antes do fim ou quando a operacao precisa encerrar o plano definitivamente.

### Regras de negocio

- planos cancelados nao podem ser alterados
- cancelamento pode gerar multa contratual
- a multa de cancelamento antecipado considera o saldo de cobrancas ainda abertas
- entregas futuras de periodos nao pagos sao canceladas
- cobrancas pendentes sao canceladas
- se existir multa em aberto, o cancelamento e bloqueado ate resolucao da multa anterior
- pode cancelar plano ativo ou suspenso

### Validacoes importantes

- o plano precisa existir
- o plano nao pode ja estar cancelado
- o plano nao pode estar finalizado
- o plano precisa estar `Active` ou `Suspended`
- nao pode existir multa pendente ou vencida
- nao pode existir entrega pendente agendada para hoje

### Impactos no sistema

- altera status do plano para `Canceled`
- cancela entregas futuras nao vinculadas a periodos pagos
- cancela cobrancas pendentes
- pode gerar multa contratual de cancelamento antecipado

### Request de exemplo

```json
{
  "reason": "Cliente solicitou encerramento do contrato"
}
```

### Response de sucesso

```json
{
  "success": true,
  "data": {
    "planId": "3b408997-63b4-4fdb-aad6-a7e9a851e713",
    "status": "Canceled",
    "canceledDeliveries": 8,
    "canceledBillings": 8,
    "penalty": {
      "id": "7cb26321-8c7b-48cb-bd8a-e5e3d7c5d111",
      "planId": "3b408997-63b4-4fdb-aad6-a7e9a851e713",
      "type": "EarlyCancellation",
      "status": "PendingPayment",
      "calculatedAmount": 1600.00
    },
    "message": "Plan canceled successfully"
  }
}
```

### Possiveis erros e bloqueios

- plano nao encontrado
- plano ja cancelado
- plano finalizado
- plano em status nao permitido
- multa em aberto
- entrega pendente marcada para hoje

### Cenarios permitidos

- cancelar plano ativo
- cancelar plano suspenso
- cancelar plano com geracao de multa
- cancelar plano sem multa quando nao houver saldo financeiro relevante

### Cenarios nao permitidos

- cancelar plano finalizado
- cancelar plano com multa anterior ainda aberta
- cancelar plano com entrega de hoje pendente

### Observacoes importantes

- multa de cancelamento antecipado hoje corresponde ao valor em aberto das cobrancas pendentes ou vencidas
- depois de cancelado, o plano nao deve aceitar operacoes de alteracao

## 5.9 Confirmacao de pagamento de mensalidade

### Endpoint

`PATCH /api/plans/confirm-billing-payment`

### Objetivo da funcionalidade

Registrar que uma mensalidade foi paga.

### Quando deve ser usada

Quando o financeiro ou a operacao confirma recebimento de uma cobranca do plano.

### Regras de negocio

- nao e permitido pagar fora da sequencia quando existe mensalidade anterior aberta
- o sistema marca cobrancas pendentes vencidas como atrasadas antes de validar
- plano cancelado bloqueia confirmacao de pagamento

### Validacoes importantes

- `BillingId` obrigatorio
- cobranca precisa existir
- plano vinculado precisa existir
- plano nao pode estar cancelado
- cobranca nao pode estar paga
- nao pode existir cobranca anterior pendente ou atrasada

### Impactos no sistema

- altera status da cobranca para `Paid`
- registra data do pagamento
- registra usuario que confirmou o recebimento

### Request de exemplo

```json
{
  "billingId": "c4df6b29-a42e-4dc5-a8df-0f39b6d1a5e2"
}
```

### Response de sucesso

```json
{
  "success": true,
  "data": {
    "billingId": "c4df6b29-a42e-4dc5-a8df-0f39b6d1a5e2",
    "status": "Paid",
    "paidAt": "2026-06-10T14:00:00Z",
    "receivedBy": "13dbd641-7f0a-4ee0-b8c8-03ccda6d80f7",
    "message": "Payment confirmed successfully"
  }
}
```

### Possiveis erros e bloqueios

- cobranca nao encontrada
- plano vinculado nao encontrado
- plano cancelado
- cobranca ja paga
- existe cobranca anterior em aberto

### Cenarios permitidos

- pagar mensalidade corrente
- pagar mensalidade atrasada seguindo a ordem correta

### Cenarios nao permitidos

- pagar mensalidade de periodo posterior com mensalidade anterior em aberto
- pagar mensalidade de plano cancelado

### Observacoes importantes

- a ordem financeira e obrigatoria e influencia diretamente o fluxo de entregas

## 5.10 Confirmacao de entrega

### Endpoint

`PATCH /api/plans/confirm-delivery`

### Objetivo da funcionalidade

Registrar que a entrega foi realizada e baixar o estoque dos itens do plano.

### Quando deve ser usada

Quando a entrega realmente aconteceu e precisa ser confirmada no sistema.

### Regras de negocio

- entrega nao pode ser confirmada antes da data prevista
- entrega cancelada nao pode ser confirmada
- entrega ja concluida nao pode ser confirmada novamente
- plano cancelado ou suspenso bloqueia a confirmacao
- nao pode haver mensalidade vencida
- nao pode haver entrega anterior ainda pendente ou atrasada
- a confirmacao da entrega reduz estoque dos produtos do plano

### Validacoes importantes

- `DeliveryId` obrigatorio
- entrega precisa existir
- plano precisa existir
- nao pode faltar estoque
- nao pode haver cobranca vencida
- nao pode haver periodo anterior de entrega em aberto

### Impactos no sistema

- altera status da entrega para `Delivered`
- registra data da entrega
- baixa estoque dos produtos vinculados ao plano
- registra movimentacao de estoque de saida

### Request de exemplo

```json
{
  "deliveryId": "db8435a4-681a-4d9e-957b-f97ede56127a"
}
```

### Response de sucesso

```json
{
  "success": true,
  "data": {
    "deliveryId": "db8435a4-681a-4d9e-957b-f97ede56127a",
    "status": "Delivered",
    "deliveryDate": "2026-06-15T15:30:00Z",
    "message": "Delivery confirmed successfully"
  }
}
```

### Possiveis erros e bloqueios

- entrega nao encontrada
- entrega ja concluida
- entrega cancelada
- tentativa de confirmar antes da data
- plano cancelado
- plano suspenso
- existe mensalidade vencida
- existe entrega anterior em aberto
- falta de estoque

### Cenarios permitidos

- confirmar entrega na data prevista
- confirmar entrega atrasada desde que a ordem do plano esteja regular

### Cenarios nao permitidos

- confirmar entrega futura
- confirmar entrega com mensalidade vencida
- confirmar entrega fora da ordem de periodos
- confirmar entrega sem estoque suficiente

### Observacoes importantes

- entregas nao podem ser confirmadas se houver mensalidades vencidas
- esse endpoint mistura controle operacional e controle de estoque

## 5.11 Cancelamento de entrega

### Endpoint

`PATCH /api/plans/cancel-delivery`

### Objetivo da funcionalidade

Cancelar uma entrega futura que ainda nao foi realizada.

### Quando deve ser usada

Quando uma entrega futura precisa ser interrompida antes da execucao.

### Regras de negocio

- apenas entregas futuras podem ser canceladas
- entrega do dia nao pode ser cancelada por este endpoint
- entrega concluida nao pode ser cancelada
- entrega ja cancelada nao pode ser cancelada novamente

### Validacoes importantes

- `DeliveryId` obrigatorio
- motivo obrigatorio com minimo de 5 caracteres
- entrega precisa existir
- a data da entrega precisa ser futura

### Impactos no sistema

- altera status da entrega para `Canceled`
- registra o motivo funcional do cancelamento

### Request de exemplo

```json
{
  "deliveryId": "db8435a4-681a-4d9e-957b-f97ede56127a",
  "reason": "Cliente pediu remarcacao"
}
```

### Response de sucesso

```json
{
  "success": true,
  "data": {
    "deliveryId": "db8435a4-681a-4d9e-957b-f97ede56127a",
    "status": "Canceled",
    "reason": "Cliente pediu remarcacao",
    "message": "Delivery canceled successfully"
  }
}
```

### Possiveis erros e bloqueios

- entrega nao encontrada
- entrega ja realizada
- entrega ja cancelada
- entrega em data passada
- entrega agendada para hoje

### Cenarios permitidos

- cancelar entrega futura ainda pendente

### Cenarios nao permitidos

- cancelar entrega do dia
- cancelar entrega passada
- cancelar entrega ja entregue

### Observacoes importantes

- o cancelamento da entrega e o passo previo para o reagendamento manual

## 5.12 Reagendamento de entrega

### Endpoint

`PATCH /api/plans/reschedule-delivery`

### Objetivo da funcionalidade

Reprogramar uma entrega cancelada para uma nova data dentro de uma janela controlada.

### Quando deve ser usada

Quando uma entrega foi cancelada e precisa ser recolocada na agenda com ajuste operacional.

### Regras de negocio

- apenas entregas canceladas podem ser reagendadas
- entregas entregues nao podem ser reagendadas
- planos cancelados ou suspensos bloqueiam reagendamento
- a nova data nao pode ser anterior a data original
- a nova data deve ficar no maximo 7 dias apos a data original
- o reagendamento manual marca a entrega como agenda customizada
- entregas reagendadas manualmente nao devem ser sobrescritas automaticamente

### Validacoes importantes

- `DeliveryId` obrigatorio
- `NewDate` obrigatoria e futura
- motivo obrigatorio com minimo de 5 caracteres
- plano vinculado precisa existir
- entrega precisa estar cancelada

### Impactos no sistema

- altera a data da entrega
- devolve a entrega para status `Pending`
- registra que a agenda passou a ser customizada

### Request de exemplo

```json
{
  "deliveryId": "db8435a4-681a-4d9e-957b-f97ede56127a",
  "newDate": "2026-06-15T00:00:00Z",
  "reason": "Cliente nao estava no local"
}
```

### Response de sucesso

```json
{
  "success": true,
  "data": {
    "deliveryId": "db8435a4-681a-4d9e-957b-f97ede56127a",
    "previousDate": "2026-06-11T00:00:00Z",
    "newDate": "2026-06-15T00:00:00Z",
    "status": "Pending",
    "message": "Delivery rescheduled successfully"
  }
}
```

### Possiveis erros e bloqueios

- entrega nao encontrada
- plano vinculado nao encontrado
- plano cancelado
- plano suspenso
- entrega nao esta cancelada
- nova data anterior a original
- nova data acima da janela de 7 dias

### Cenarios permitidos

- reagendar entrega cancelada dentro da janela permitida

### Cenarios nao permitidos

- reagendar entrega entregue
- reagendar entrega ainda pendente
- reagendar em plano cancelado
- reagendar para data fora da janela

### Observacoes importantes

- essa regra e central para o dominio
- reagendamento manual preserva a data em futuros upgrades

## 5.13 Confirmacao de pagamento de multa contratual

### Endpoint

`PATCH /api/penalties/{id}/confirm-payment`

### Objetivo da funcionalidade

Registrar o pagamento de uma multa contratual.

### Quando deve ser usada

Quando o cliente quita uma multa aberta.

### Regras de negocio

- apenas multas `PendingPayment` ou `Overdue` podem ser pagas
- multa paga, isenta ou cancelada nao aceita novo pagamento
- multa com valor zero nao pode ser paga
- multas muito antigas deixam de aceitar pagamento

### Validacoes importantes

- multa precisa existir
- multa nao pode estar paga
- multa nao pode estar isenta
- multa nao pode estar cancelada
- multa precisa estar dentro da janela de pagamento permitida

### Impactos no sistema

- altera status da multa para `Paid`
- registra data do pagamento
- registra usuario que confirmou o recebimento

### Request de exemplo

Sem corpo de requisicao.

### Response de sucesso

```json
{
  "success": true,
  "data": {
    "penaltyId": "7cb26321-8c7b-48cb-bd8a-e5e3d7c5d111",
    "status": "Paid",
    "paidAt": "2026-06-20T12:00:00Z",
    "amount": 1600.00,
    "message": "Contract penalty payment confirmed successfully."
  }
}
```

### Possiveis erros e bloqueios

- multa nao encontrada
- multa ja paga
- multa isenta
- multa cancelada
- multa fora da janela permitida
- multa com valor zero

### Cenarios permitidos

- pagar multa pendente
- pagar multa vencida ainda dentro do periodo aceito

### Cenarios nao permitidos

- pagar multa isenta
- pagar multa cancelada
- pagar multa muito antiga

### Observacoes importantes

- multa paga pode desbloquear operacoes como upgrade, reativacao e cancelamento

## 5.14 Isencao de multa contratual

### Endpoint

`PATCH /api/penalties/{id}/waive`

### Objetivo da funcionalidade

Registrar isencao total da multa contratual.

### Quando deve ser usada

Quando o negocio decide perdoar a multa por criterio comercial, operacional ou excepcional.

### Regras de negocio

- apenas multas pendentes ou vencidas podem ser isentadas
- multa paga nao pode ser isentada
- multa cancelada nao pode ser isentada
- motivo e obrigatorio e precisa ter pelo menos 10 caracteres
- multas muito antigas nao aceitam mais isencao

### Validacoes importantes

- multa precisa existir
- motivo obrigatorio
- multa nao pode estar paga
- multa nao pode estar isenta
- multa nao pode estar cancelada
- multa precisa estar dentro da janela de isencao permitida

### Impactos no sistema

- altera status da multa para `Waived`
- registra motivo da isencao
- registra data e usuario responsavel

### Request de exemplo

```json
{
  "reason": "Ajuste comercial aprovado pela gestao"
}
```

### Response de sucesso

```json
{
  "success": true,
  "data": {
    "penaltyId": "7cb26321-8c7b-48cb-bd8a-e5e3d7c5d111",
    "status": "Waived",
    "reason": "Ajuste comercial aprovado pela gestao",
    "amount": 1600.00,
    "waivedAt": "2026-06-20T12:00:00Z",
    "message": "Contract penalty waived successfully by Gerente Inicial"
  }
}
```

### Possiveis erros e bloqueios

- multa nao encontrada
- multa ja paga
- multa ja isenta
- multa cancelada
- motivo invalido
- multa fora da janela de isencao

### Cenarios permitidos

- isentar multa pendente
- isentar multa vencida dentro da janela permitida

### Cenarios nao permitidos

- isentar multa paga
- isentar multa ja isenta
- isentar multa muito antiga

### Observacoes importantes

- a isencao tambem desbloqueia operacoes que dependem da inexistencia de multa em aberto

## 5.15 Cancelamento de multa contratual

### Endpoint

`PATCH /api/penalties/{id}/cancel`

### Objetivo da funcionalidade

Cancelar administrativamente uma multa contratual.

### Quando deve ser usada

Quando a multa foi gerada de forma indevida ou precisa ser invalidada por decisao operacional.

### Regras de negocio

- apenas multas pendentes ou vencidas podem ser canceladas
- multa paga nao pode ser cancelada
- multa isenta nao pode ser cancelada
- multa cancelada nao pode ser cancelada novamente
- motivo e obrigatorio e precisa ter pelo menos 10 caracteres
- multas muito antigas nao aceitam cancelamento

### Validacoes importantes

- multa precisa existir
- motivo obrigatorio
- multa nao pode estar paga
- multa nao pode estar isenta
- multa nao pode estar cancelada
- multa precisa estar dentro da janela de cancelamento permitida

### Impactos no sistema

- altera status da multa para `Canceled`
- registra motivo do cancelamento
- registra data e usuario responsavel

### Request de exemplo

```json
{
  "reason": "Multa gerada em duplicidade por erro operacional"
}
```

### Response de sucesso

```json
{
  "success": true,
  "data": {
    "penaltyId": "7cb26321-8c7b-48cb-bd8a-e5e3d7c5d111",
    "status": "Canceled",
    "amount": 1600.00,
    "canceledAt": "2026-06-20T12:00:00Z",
    "reason": "Multa gerada em duplicidade por erro operacional",
    "message": "Contract penalty canceled successfully"
  }
}
```

### Possiveis erros e bloqueios

- multa nao encontrada
- multa paga
- multa isenta
- multa ja cancelada
- motivo invalido
- multa fora da janela de cancelamento

### Cenarios permitidos

- cancelar multa pendente
- cancelar multa vencida dentro da janela permitida

### Cenarios nao permitidos

- cancelar multa paga
- cancelar multa isenta
- cancelar multa muito antiga

### Observacoes importantes

- cancelamento de multa e diferente de isencao
- funcionalmente, ambos retiram o bloqueio da multa em aberto, mas com significado de negocio distinto

---
