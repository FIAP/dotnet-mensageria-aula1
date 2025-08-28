# Publisher/Subscriber - Diagrama de Sequência

```mermaid
sequenceDiagram
    participant App as Aplicacao
    participant Bus as EventBus (InMemory)
    participant Email as OrderCreatedEmailHandler
    participant Analytics as OrderCreatedAnalyticsHandler
    participant Flaky as FlakyNotificationHandler
    participant CB as Circuit Breaker
    participant DLQ as Dead Letter Queue

    App->>Bus: Publish(OrderCreated)
    Note over Bus: Verifica Circuit Breaker antes de processar

    par Processamento com Resilência
        Bus->>CB: Check circuit state
        CB-->>Bus: Circuit closed (OK)
        Bus->>Email: OrderCreated
        Email-->>Bus: Handle concluido
    and
        Bus->>Analytics: OrderCreated  
        Analytics-->>Bus: Handle concluido
    and
        Bus->>CB: Check circuit for Flaky
        CB-->>Bus: Circuit closed
        Bus->>Flaky: OrderCreated (Tentativa 1)
        Flaky-->>Bus: FALHA
        Note over Bus: Retry Policy ativado
        Bus->>Flaky: OrderCreated (Tentativa 2)
        Flaky-->>Bus: FALHA
        Bus->>Flaky: OrderCreated (Tentativa 3)
        Flaky-->>Bus: FALHA
        Note over CB: Registra falha
        Bus->>CB: Record failure
        CB->>CB: Increment failure count
        Bus->>DLQ: Envia evento para DLQ
    end

    Note over CB: Após 3 falhas, abre circuito
    CB->>CB: Open circuit (30s timeout)
    
    App->>Bus: Publish(OrderCreated #2)
    Bus->>CB: Check circuit for Flaky
    CB-->>Bus: Circuit OPEN
    Note over Bus: Handler em circuito aberto
    Bus->>DLQ: Evento direto para DLQ
    
    Bus-->>App: Publish concluido
```

- **Objetivo**: Distribuição resiliente de eventos para múltiplos consumidores
- **Funcionalidades Avançadas**: 
  - Circuit Breaker (isola handlers problemáticos)
  - Dead Letter Queue (eventos com falha)  
  - Retry Policy (3 tentativas com backoff)
  - Processamento paralelo independente
