## Async Patterns (.NET) - Nível Avançado

Três demos práticas sobre padrões de comunicação assíncrona:

- [Pub/Sub](./docs/pubsub.md) - Semânticas de entrega, Kafka/RabbitMQ, Transactional Outbox
- [Request/Response](./docs/request-response.md) - Circuit breakers, batching, distributed RPC
- [Event Notification](./docs/event-notification.md) - Event sourcing, projections, schema evolution

### Projetos na Solução

#### AsyncPatterns.Messaging (Class Library)
**Biblioteca enterprise** com infraestrutura avançada para comunicação assíncrona robusta.

**Interfaces Core:**
- `IEvent` - Contratos de eventos com EventId e Timestamp
- `IEventHandler<T>` - Handlers tipados para eventos
- `IEventBus` - Bus com funcionalidades avançadas (DLQ, Circuit Breaker)
- `IRequest<T>` / `IRequestHandler<T,R>` - Request/Response tipado
- `IMessageBus` - Bus enterprise com timeout, cache, scatter-gather

**Implementações Avançadas:**
- `InMemoryEventBus` - Pub/Sub com **Circuit Breaker**, **Dead Letter Queue**, **Retry Policy**
- `InMemoryMessageBus` - Request/Response com **Timeout**, **Idempotência**, **Scatter-Gather**
- **CircuitBreakerState** - Estados: Closed, Open, HalfOpen (padrão enterprise)
- **ScatterGatherResult<T>** - Coleta de múltiplas respostas paralelas

#### Demo.PubSub (Console Application)
**Publisher/Subscriber resiliente** com padrões enterprise de recuperação de falhas.

**Funcionalidades Demonstradas:**
- **Circuit Breaker**: Isola handlers problemáticos após 3 falhas (timeout 30s)
- **Dead Letter Queue**: Captura eventos que falharam para reprocessamento  
- **Retry Policy**: 3 tentativas com backoff exponencial (100ms, 200ms, 300ms)
- **Processamento Paralelo**: Múltiplos handlers executam independentemente

**Handlers Implementados:**
- `OrderCreatedEmailHandler` - Envio de emails transacionais
- `OrderCreatedAnalyticsHandler` - Coleta de métricas e KPIs  
- `FlakyNotificationHandler` - Simula falhas para demonstrar resiliência

**Cenários de Teste:**
1. Processamento normal (todos handlers funcionam)
2. Handler com falhas (ativa retry + circuit breaker)
3. Circuit breaker aberto (eventos vão direto para DLQ)

#### Demo.RequestResponse (Console Application)
**Request/Response enterprise** com funcionalidades de timeout, cache e consultas paralelas.

**Funcionalidades Avançadas:**
- **Timeout Configurável**: Default 30s, customizável por request (ex: 500ms)
- **Idempotência com Cache**: TTL 5 minutos, evita processamento duplicado
- **Scatter-Gather**: Consultas paralelas com agregação de resultados
- **Exception Handling**: TimeoutException específica, logging detalhado

**Handlers Implementados:**
- `GetOrderQueryHandler` - Consulta rápida de pedidos (300ms)
- `SlowQueryHandler` - Simula consulta lenta (2000ms) para testes de timeout

**Cenários de Teste:**
1. Request/Response normal com cache miss
2. Idempotência demonstrada com cache hit  
3. Timeout configurável cancelando request lenta
4. Scatter-Gather coletando múltiplas respostas

#### Demo.EventNotification (Console Application)  
**Event Sourcing completo** com CQRS, múltiplas projeções e Event Store.

**Funcionalidades Enterprise:**
- **Event Store**: Persiste todos eventos para auditoria e replay
- **CQRS Read Models**: Estado otimizado separado do write model
- **Multiple Projections**: Cada projeção serve um propósito específico
- **Event Replay**: Reconstrução de estado a partir do histórico
- **External Integration**: Webhooks condicionais para sistemas externos

**Componentes Implementados:**
- `InMemoryEventStore` - Armazena eventos por agregado (ORDER-1001)
- `OrderProjection` - Mantém read model com estado atual + histórico
- `OrderAnalyticsProjection` - KPIs (total pedidos, receita, status counts)
- `ExternalWebhook` - Notifica sistemas externos em eventos críticos
- `EventStoreHandler` - Persiste todos eventos automaticamente

**Fluxo Completo:**
1. `OrderCreated` - Inicia agregado, cria read model inicial
2. `OrderStatusChanged` - Sequência de mudanças: Created → PaymentPending → Processing → Shipped → Delivered
3. **Event Store Query** - Recupera histórico completo (5 eventos)
4. **Read Model Query** - Estado atual otimizado para consulta

### Pré-requisitos
- .NET SDK

### Como compilar
```bash
cd async-patterns
dotnet build AsyncPatterns.sln
```

### Executar cada demo

#### Demo.PubSub - Resilience Patterns
```bash
dotnet run --project Demo.PubSub/Demo.PubSub.csproj
```
Saída demonstrando Circuit Breaker, DLQ e Retry:
```
Publisher/Subscriber - Demo Avançado
Demonstra: Circuit Breaker, Dead Letter Queue, Retry Policies

=== Teste 1: Processamento normal ===
[Retry] Tentativa 1 falhou para OrderCreated: Falha simulada na notificação #1
Analytics: Pedido ORDER-1001 valor $199.90
Email enviado para CLIENT-42 do pedido ORDER-1001
[Retry] Tentativa 2 falhou para OrderCreated: Falha simulada na notificação #2
[Error] Handler OrderCreated falhou: Falha simulada na notificação #3

=== Teste 2: Handler com falhas (demonstra retry + circuit breaker) ===
[Retry] Tentativa 1 falhou para OrderCreated: Falha simulada na notificação #4
Analytics: Pedido ORDER-1002 valor $299.90
Email enviado para CLIENT-43 do pedido ORDER-1002
[Retry] Tentativa 2 falhou para OrderCreated: Falha simulada na notificação #5
[Error] Handler OrderCreated falhou: Falha simulada na notificação #6

=== Teste 3: Após circuit breaker abrir ===
Notificação push enviada para ORDER-1003
Analytics: Pedido ORDER-1003 valor $399.90
Email enviado para CLIENT-44 do pedido ORDER-1003

=== Dead Letter Queue: 2 eventos ===
DLQ: ORDER-1001 - $199.90
DLQ: ORDER-1002 - $299.90

Fim Pub/Sub Avançado
```

#### Demo.RequestResponse - Enterprise Patterns
```bash
dotnet run --project Demo.RequestResponse/Demo.RequestResponse.csproj
```
Saída demonstrando Timeout, Cache e Scatter-Gather:
```
Request/Response Assíncrono - Demo Avançado
Demonstra: Timeouts, Idempotência, Scatter-Gather

=== Teste 1: Request/Response normal ===
[Handler] Processando query para ORDER-1001
Resposta: ORDER-1001 - Completed

=== Teste 2: Idempotência (mesma request) ===
[Cache] Resposta encontrada em cache para GetOrderQuery
Resposta: ORDER-1001 - Completed

=== Teste 3: Request com timeout customizado ===
[SlowHandler] Iniciando processamento lento para SLOW-QUERY
Timeout: Request SlowQuery timed out after 00:00:00.5000000

=== Teste 4: Scatter-Gather (múltiplas respostas) ===
[Handler] Processando query para ORDER-2001
[Handler] Processando query para ORDER-2001
Scatter-Gather: 2 sucessos, 0 erros
  ORDER-2001 - Completed
  ORDER-2001 - Completed

Fim Request/Response Avançado
```

#### Demo.EventNotification - Event Sourcing & CQRS
```bash
dotnet run --project Demo.EventNotification/Demo.EventNotification.csproj
```
Saída demonstrando Event Store, Projections e Read Models:
```
Event Notification - Demo Avançado
Demonstra: Event Sourcing, Projections, Read Models

=== Event Sourcing: Sequência de eventos de um pedido ===
[EventStore] Persistido: OrderCreated para ORDER-1001
[Analytics] Total pedidos: 1, Receita: $199.90
[Projection] Read model criado para ORDER-1001
[EventStore] Persistido: OrderStatusChanged para ORDER-1001
[Analytics] Status 'PaymentPending': 1 ocorrências
[Projection] Read model atualizado: ORDER-1001 -> PaymentPending
[EventStore] Persistido: OrderStatusChanged para ORDER-1001
[Analytics] Status 'Processing': 1 ocorrências
[Projection] Read model atualizado: ORDER-1001 -> Processing
[EventStore] Persistido: OrderStatusChanged para ORDER-1001
[Analytics] Status 'Shipped': 1 ocorrências
[Projection] Read model atualizado: ORDER-1001 -> Shipped
[EventStore] Persistido: OrderStatusChanged para ORDER-1001
[Analytics] Status 'Delivered': 1 ocorrências
[Projection] Read model atualizado: ORDER-1001 -> Delivered
[Webhook] Notificando sistema externo: ORDER-1001 está entregue

=== Event Store: Histórico completo do pedido ===
Total de eventos para ORDER-1001: 5
  00:06:19 - OrderCreated
  00:06:19 - OrderStatusChanged
  00:06:19 - OrderStatusChanged
  00:06:19 - OrderStatusChanged
  00:06:19 - OrderStatusChanged

=== Read Model: Estado atual do pedido ===
Pedido: ORDER-1001
Status Atual: Delivered
Total de mudanças: 5
Histórico: Created -> PaymentPending -> Processing -> Shipped -> Delivered

Fim Event Notification Avançado
```