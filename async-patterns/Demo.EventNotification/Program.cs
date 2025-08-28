using AsyncPatterns.Messaging;

Console.WriteLine("Event Notification - Demo Avançado");
Console.WriteLine("Demonstra: Event Sourcing, Projections, Read Models");

var bus = new InMemoryEventBus();
var eventStore = new InMemoryEventStore();

// Subscribers para diferentes propósitos
bus.Subscribe<OrderCreated>(new OrderProjection());
bus.Subscribe<OrderStatusChanged>(new OrderProjection());
bus.Subscribe<OrderCreated>(new OrderAnalyticsProjection());
bus.Subscribe<OrderStatusChanged>(new OrderAnalyticsProjection());
bus.Subscribe<OrderStatusChanged>(new ExternalWebhook());
bus.Subscribe<OrderCreated>(new EventStoreHandler(eventStore));
bus.Subscribe<OrderStatusChanged>(new EventStoreHandler(eventStore));

Console.WriteLine("\n=== Event Sourcing: Sequência de eventos de um pedido ===");
await bus.PublishAsync(new OrderCreated("ORDER-1001", "CLIENT-42", 199.90m));
await bus.PublishAsync(new OrderStatusChanged("ORDER-1001", "Created", "PaymentPending"));  
await bus.PublishAsync(new OrderStatusChanged("ORDER-1001", "PaymentPending", "Processing"));
await bus.PublishAsync(new OrderStatusChanged("ORDER-1001", "Processing", "Shipped"));
await bus.PublishAsync(new OrderStatusChanged("ORDER-1001", "Shipped", "Delivered"));

Console.WriteLine("\n=== Event Store: Histórico completo do pedido ===");
var events = eventStore.GetEventsForAggregate("ORDER-1001");
Console.WriteLine($"Total de eventos para ORDER-1001: {events.Count}");
foreach (var evt in events)
{
    Console.WriteLine($"  {evt.Timestamp:HH:mm:ss} - {evt.GetType().Name}");
}

Console.WriteLine("\n=== Read Model: Estado atual do pedido ===");
var currentState = OrderProjection.GetOrderState("ORDER-1001");
if (currentState != null)
{
    Console.WriteLine($"Pedido: {currentState.OrderId}");
    Console.WriteLine($"Status Atual: {currentState.CurrentStatus}");
    Console.WriteLine($"Total de mudanças: {currentState.StatusHistory.Count}");
    Console.WriteLine($"Histórico: {string.Join(" -> ", currentState.StatusHistory)}");
}

Console.WriteLine("\nFim Event Notification Avançado");

// Event Types
public record OrderCreated(string OrderId, string CustomerId, decimal Total) : IEvent
{
    public string EventId { get; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}

public record OrderStatusChanged(string OrderId, string FromStatus, string ToStatus) : IEvent
{
    public string EventId { get; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}

// Event Store simples
public class InMemoryEventStore
{
    private readonly Dictionary<string, List<IEvent>> _events = new();
    
    public void Store(string aggregateId, IEvent @event)
    {
        if (!_events.ContainsKey(aggregateId))
            _events[aggregateId] = new List<IEvent>();
            
        _events[aggregateId].Add(@event);
    }
    
    public List<IEvent> GetEventsForAggregate(string aggregateId)
    {
        return _events.TryGetValue(aggregateId, out var events) ? events : new List<IEvent>();
    }
}

// Read Model para CQRS
public class OrderReadModel
{
    public string OrderId { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string CurrentStatus { get; set; } = string.Empty;
    public List<string> StatusHistory { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}

// Projection que mantém read model
public class OrderProjection : IEventHandler<OrderCreated>, IEventHandler<OrderStatusChanged>
{
    private static readonly Dictionary<string, OrderReadModel> _readModels = new();
    
    public async Task HandleAsync(OrderCreated @event, CancellationToken ct = default)
    {
        await Task.Delay(50, ct);
        
        var readModel = new OrderReadModel
        {
            OrderId = @event.OrderId,
            CustomerId = @event.CustomerId,
            Total = @event.Total,
            CurrentStatus = "Created",
            StatusHistory = { "Created" },
            LastUpdated = @event.Timestamp
        };
        
        _readModels[@event.OrderId] = readModel;
        Console.WriteLine($"[Projection] Read model criado para {readModel.OrderId}");
    }
    
    public async Task HandleAsync(OrderStatusChanged @event, CancellationToken ct = default)
    {
        await Task.Delay(50, ct);
        
        if (_readModels.TryGetValue(@event.OrderId, out var readModel))
        {
            readModel.CurrentStatus = @event.ToStatus;
            readModel.StatusHistory.Add(@event.ToStatus);
            readModel.LastUpdated = @event.Timestamp;
            
            Console.WriteLine($"[Projection] Read model atualizado: {readModel.OrderId} -> {readModel.CurrentStatus}");
        }
    }
    
    public static OrderReadModel? GetOrderState(string orderId)
    {
        return _readModels.TryGetValue(orderId, out var model) ? model : null;
    }
}

// Analytics Projection 
public class OrderAnalyticsProjection : IEventHandler<OrderCreated>, IEventHandler<OrderStatusChanged>
{
    private static int _totalOrders = 0;
    private static decimal _totalRevenue = 0;
    private static readonly Dictionary<string, int> _statusCounts = new();
    
    public async Task HandleAsync(OrderCreated @event, CancellationToken ct = default)
    {
        await Task.Delay(30, ct);
        _totalOrders++;
        _totalRevenue += @event.Total;
        Console.WriteLine($"[Analytics] Total pedidos: {_totalOrders}, Receita: {_totalRevenue:C}");
    }
    
    public async Task HandleAsync(OrderStatusChanged @event, CancellationToken ct = default)
    {
        await Task.Delay(30, ct);
        _statusCounts[@event.ToStatus] = _statusCounts.GetValueOrDefault(@event.ToStatus, 0) + 1;
        Console.WriteLine($"[Analytics] Status '{@event.ToStatus}': {_statusCounts[@event.ToStatus]} ocorrências");
    }
}

// External Integration
public class ExternalWebhook : IEventHandler<OrderStatusChanged>
{
    public async Task HandleAsync(OrderStatusChanged @event, CancellationToken ct = default)
    {
        await Task.Delay(100, ct);
        if (@event.ToStatus is "Delivered" or "Cancelled")
        {
            Console.WriteLine($"[Webhook] Notificando sistema externo: {@event.OrderId} está {(@event.ToStatus == "Delivered" ? "entregue" : "cancelado")}");
        }
    }
}

// Event Store Handler (persiste todos eventos)
public class EventStoreHandler : IEventHandler<OrderCreated>, IEventHandler<OrderStatusChanged>
{
    private readonly InMemoryEventStore _eventStore;
    
    public EventStoreHandler(InMemoryEventStore eventStore)
    {
        _eventStore = eventStore;
    }
    
    public async Task HandleAsync(OrderCreated @event, CancellationToken ct = default)
    {
        await Task.Delay(10, ct);
        _eventStore.Store(@event.OrderId, @event);
        Console.WriteLine($"[EventStore] Persistido: {nameof(OrderCreated)} para {@event.OrderId}");
    }
    
    public async Task HandleAsync(OrderStatusChanged @event, CancellationToken ct = default)
    {
        await Task.Delay(10, ct);
        _eventStore.Store(@event.OrderId, @event);
        Console.WriteLine($"[EventStore] Persistido: {nameof(OrderStatusChanged)} para {@event.OrderId}");
    }
}
