# Request/Response Assíncrono - Diagrama de Sequência

```mermaid
sequenceDiagram
    participant Client as Cliente
    participant Bus as MessageBus (InMemory)
    participant Cache as Response Cache
    participant Handler as GetOrderQueryHandler
    participant SlowHandler as SlowQueryHandler
    participant Timeout as Timeout Manager

    Note over Client: Cenário 1: Request normal
    Client->>Bus: Send(GetOrderQuery "ORDER-1001")
    Bus->>Cache: Check cache for key
    Cache-->>Bus: Cache MISS
    Bus->>Handler: GetOrderQuery
    Handler-->>Bus: GetOrderResponse
    Bus->>Cache: Store response (TTL=5min)
    Bus-->>Client: GetOrderResponse

    Note over Client: Cenário 2: Idempotência (mesma request)
    Client->>Bus: Send(GetOrderQuery "ORDER-1001")
    Bus->>Cache: Check cache for key
    Cache-->>Bus: Cache HIT
    Bus-->>Client: GetOrderResponse (from cache)

    Note over Client: Cenário 3: Request com timeout customizado
    Client->>Bus: Send(SlowQuery, timeout=500ms)
    Bus->>Cache: Check cache for key
    Cache-->>Bus: Cache MISS
    Bus->>Timeout: Start timeout timer (500ms)
    Bus->>SlowHandler: SlowQuery
    Note over SlowHandler: Processing takes 2000ms
    Timeout-->>Bus: TIMEOUT EXPIRED
    Bus-->>Client: TimeoutException

    Note over Client: Cenário 4: Scatter-Gather (múltiplas respostas)
    Client->>Bus: ScatterGather(GetOrderQuery, expectedResponses=2)
    
    par Scatter para múltiplos handlers
        Bus->>Handler: GetOrderQuery (instância 1)
        Handler-->>Bus: GetOrderResponse
    and  
        Bus->>Handler: GetOrderQuery (instância 2)
        Handler-->>Bus: GetOrderResponse
    end
    
    Note over Bus: Gather results
    Bus-->>Client: ScatterGatherResult(2 sucessos, 0 erros)
```

- **Objetivo**: Comunicação assíncrona com retorno garantido e funcionalidades enterprise
- **Funcionalidades Avançadas**:
  - Timeout configurável (default 30s, customizável)
  - Idempotência com cache TTL (evita processamento duplicado)
  - Scatter-Gather para consultas paralelas
  - Exception handling robusto

