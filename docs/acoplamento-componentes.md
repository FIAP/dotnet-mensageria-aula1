# Problema: Acoplamento Forte entre Componentes

## Descrição do Problema

Em arquiteturas tradicionais, os componentes do sistema precisam conhecer e chamar diretamente outros componentes, criando dependências rígidas que dificultam a manutenção, evolução e escalabilidade do sistema.

## Cenário: Sistema de E-commerce - Criação de Pedido

### Arquitetura Tradicional (Sem EDA)

```mermaid
sequenceDiagram
    participant Client as Cliente
    participant OS as OrderService
    participant PS as PaymentService
    participant IS as InventoryService
    participant ES as EmailService
    participant LS as LoggingService

    Client->>OS: Criar Pedido
    Note over OS: Conhece todos os serviços<br/>diretamente
    
    OS->>PS: Processar Pagamento
    PS-->>OS: Pagamento OK
    
    OS->>IS: Verificar Estoque
    IS-->>OS: Estoque OK
    
    OS->>IS: Reservar Itens
    IS-->>OS: Itens Reservados
    
    OS->>ES: Enviar Email Confirmação
    ES-->>OS: Email Enviado
    
    OS->>LS: Registrar Log
    LS-->>OS: Log Registrado
    
    OS-->>Client: Pedido Criado com Sucesso
    
    Note over OS,LS: PROBLEMA: OrderService precisa<br/>conhecer e chamar todos os serviços<br/>Acoplamento FORTE
```

#### Problemas Identificados:
- **Alto Acoplamento**: OrderService precisa conhecer todos os outros serviços
- **Dependências Rígidas**: Mudanças em um serviço afetam o OrderService
- **Processamento Sequencial**: Operações executadas uma após a outra
- **Ponto de Falha Único**: OrderService concentra muita responsabilidade

### Arquitetura com EDA

```mermaid
sequenceDiagram
    participant Client as Cliente
    participant OS as OrderService
    participant EB as Event Bus
    participant PS as PaymentService
    participant IS as InventoryService
    participant ES as EmailService
    participant LS as LoggingService

    Client->>OS: Criar Pedido
    Note over OS: Conhece apenas o Event Bus
    
    OS->>EB: Publicar "OrderCreated"
    OS-->>Client: Pedido Criado (resposta imediata)
    
    par Processamento Paralelo
        EB->>PS: Evento "OrderCreated"
        PS->>EB: Publicar "PaymentProcessed"
    and
        EB->>IS: Evento "OrderCreated"
        IS->>EB: Publicar "InventoryReserved"
    and
        EB->>ES: Evento "OrderCreated"
        ES->>ES: Enviar Email
    and
        EB->>LS: Evento "OrderCreated"
        LS->>LS: Registrar Log
    end
    
    Note over OS,LS: SOLUÇÃO: OrderService só conhece Event Bus<br/>Serviços processam independentemente<br/>Acoplamento BAIXO
```

#### Benefícios Obtidos:
- **Baixo Acoplamento**: OrderService conhece apenas o Event Bus
- **Independência**: Serviços não conhecem uns aos outros
- **Processamento Paralelo**: Múltiplas operações simultâneas
- **Responsividade**: Cliente recebe resposta imediata
- **Flexibilidade**: Fácil adição/remoção de consumidores
