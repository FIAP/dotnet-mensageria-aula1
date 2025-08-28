# Event Notification - Diagrama de Sequência

```mermaid
sequenceDiagram
    participant App as Aplicacao
    participant Bus as EventBus (InMemory)
    participant EventStore as Event Store
    participant ReadModel as Order Read Model
    participant Analytics as Analytics Projection
    participant Webhook as External Webhook
    participant EventStoreHandler as EventStore Handler

    Note over App: Event Sourcing - Sequência completa de eventos

    App->>Bus: Publish(OrderCreated "ORDER-1001")
    
    par Event Store + Projections
        Bus->>EventStoreHandler: OrderCreated
        EventStoreHandler->>EventStore: Store(ORDER-1001, OrderCreated)
        EventStore-->>EventStoreHandler: Persistido
    and
        Bus->>ReadModel: OrderCreated  
        ReadModel->>ReadModel: Create read model
        Note over ReadModel: Status = "Created", History = ["Created"]
    and
        Bus->>Analytics: OrderCreated
        Analytics->>Analytics: Increment totalOrders, totalRevenue
    end

    App->>Bus: Publish(OrderStatusChanged "Created->PaymentPending")
    
    par
        Bus->>EventStoreHandler: OrderStatusChanged
        EventStoreHandler->>EventStore: Store(ORDER-1001, OrderStatusChanged)
    and  
        Bus->>ReadModel: OrderStatusChanged
        ReadModel->>ReadModel: Update: Status="PaymentPending"
        Note over ReadModel: History = ["Created", "PaymentPending"]
    and
        Bus->>Analytics: OrderStatusChanged
        Analytics->>Analytics: Count status transitions
    end

    Note over App: ... mais mudanças de status ...

    App->>Bus: Publish(OrderStatusChanged "Shipped->Delivered")
    
    par Final State
        Bus->>EventStoreHandler: OrderStatusChanged
        EventStoreHandler->>EventStore: Store(ORDER-1001, OrderStatusChanged)
    and
        Bus->>ReadModel: OrderStatusChanged  
        ReadModel->>ReadModel: Update: Status="Delivered"
        Note over ReadModel: History = ["Created"..."Delivered"]
    and
        Bus->>Analytics: OrderStatusChanged
        Analytics->>Analytics: Track delivery metrics
    and
        Bus->>Webhook: OrderStatusChanged
        Note over Webhook: Trigger para status "Delivered"
        Webhook->>Webhook: Notify external systems
    end

    Note over App: Query Event Store para histórico completo
    App->>EventStore: GetEventsForAggregate("ORDER-1001")
    EventStore-->>App: [5 events] OrderCreated + 4 StatusChanged

    Note over App: Query Read Model para estado atual
    App->>ReadModel: GetOrderState("ORDER-1001")
    ReadModel-->>App: Current: "Delivered", Full History
```

- **Objetivo**: Event Sourcing completo com múltiplas projeções especializadas
- **Funcionalidades Avançadas**:
  - Event Store (persiste todos os eventos para auditoria)
  - CQRS Read Models (estado otimizado para consulta)  
  - Multiple Projections (Analytics, Webhook, Read Model)
  - Event replay capability (reconstrução de estado)
  - External system integration (webhooks condicionais)
