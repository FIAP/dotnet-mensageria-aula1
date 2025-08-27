# Problema: Resiliência e Disponibilidade

## Descrição do Problema

Em arquiteturas tradicionais síncronas, a falha de um único componente pode causar o colapso de todo o processo de negócio, resultando em baixa disponibilidade e experiência ruim para o usuário. Este é conhecido como o problema do "ponto único de falha".

## Cenário: Sistema de E-commerce - Falha no Serviço de Email

### Arquitetura Tradicional - Com Falha

```mermaid
sequenceDiagram
    participant Client as Cliente
    participant OS as OrderService
    participant PS as PaymentService
    participant IS as InventoryService
    participant ES as EmailService
    participant LS as LoggingService

    Client->>OS: Criar Pedido
    
    OS->>PS: Processar Pagamento
    PS-->>OS: Pagamento OK
    
    OS->>IS: Verificar Estoque
    IS-->>OS: Estoque OK
    
    OS->>IS: Reservar Itens
    IS-->>OS: Itens Reservados
    
    OS->>ES: Enviar Email Confirmação
    Note over ES: FALHA NO SERVIÇO<br/>DE EMAIL
    ES--xOS: ERRO - Serviço Indisponível
    
    Note over OS: Todo o processo falha<br/>devido a um único serviço
    OS--xClient: ERRO: Pedido Não Criado
    
    Note over Client,LS: PROBLEMA: Falha em um serviço<br/>derruba todo o processo<br/>Pedido é perdido
```

#### Problemas Identificados:
- **Efeito Cascata**: Falha em um serviço não-crítico derruba todo o processo
- **Perda de Transação**: Pedido completamente perdido devido a falha menor
- **Experiência Ruim**: Cliente não consegue completar compra
- **Rollback Complexo**: Necessário desfazer operações já realizadas

### Arquitetura com EDA - Resiliente a Falhas

```mermaid
sequenceDiagram
    participant Client as Cliente
    participant OS as OrderService
    participant EB as Event Bus
    participant PS as PaymentService
    participant IS as InventoryService
    participant ES as EmailService
    participant LS as LoggingService
    participant DLQ as Dead Letter Queue

    Client->>OS: Criar Pedido
    
    OS->>EB: Publicar "OrderCreated"
    OS-->>Client: Pedido Criado (núcleo funciona)
    
    par Processamento Resiliente
        EB->>PS: Evento "OrderCreated"
        PS->>EB: Publicar "PaymentProcessed"
    and
        EB->>IS: Evento "OrderCreated"
        IS->>EB: Publicar "InventoryReserved"
    and
        EB->>ES: Evento "OrderCreated"
        Note over ES: FALHA NO SERVIÇO<br/>DE EMAIL
        EB->>DLQ: Evento vai para Dead Letter Queue
    and
        EB->>LS: Evento "OrderCreated"
        LS->>LS: Registrar Log
    end
    
    Note over DLQ: Email será reenviado<br/>quando serviço voltar
    
    Note over Client,DLQ: SOLUÇÃO: Falha isolada<br/>Pedido criado com sucesso<br/>Email será processado depois
```
 
