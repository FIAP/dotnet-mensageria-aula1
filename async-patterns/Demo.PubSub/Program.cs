using AsyncPatterns.Messaging;

Console.WriteLine("Publisher/Subscriber - Demo Avançado");
Console.WriteLine("Demonstra: Circuit Breaker, Dead Letter Queue, Retry Policies");

var bus = new InMemoryEventBus();

// Handlers
bus.Subscribe(new OrderCreatedEmailHandler());
bus.Subscribe(new OrderCreatedAnalyticsHandler());
bus.Subscribe(new FlakyNotificationHandler()); // Handler que falha

Console.WriteLine("\n=== Teste 1: Processamento normal ===");
await bus.PublishAsync(new OrderCreated("ORDER-1001", "CLIENT-42", 199.90m));

Console.WriteLine("\n=== Teste 2: Handler com falhas (demonstra retry + circuit breaker) ===");
await bus.PublishAsync(new OrderCreated("ORDER-1002", "CLIENT-43", 299.90m));

Console.WriteLine("\n=== Teste 3: Após circuit breaker abrir ===");
await bus.PublishAsync(new OrderCreated("ORDER-1003", "CLIENT-44", 399.90m));

// Verifica Dead Letter Queue
var dlqEvents = bus.GetDeadLetterQueue();
Console.WriteLine($"\n=== Dead Letter Queue: {dlqEvents.Count()} eventos ===");
foreach (var evt in dlqEvents)
{
    if (evt is OrderCreated order)
        Console.WriteLine($"DLQ: {order.OrderId} - {order.Total:C}");
}

Console.WriteLine("\nFim Pub/Sub Avançado");

// Tipos
public record OrderCreated(string OrderId, string CustomerId, decimal Total) : IEvent
{
    public string EventId { get; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}

public class OrderCreatedEmailHandler : IEventHandler<OrderCreated>
{
    public async Task HandleAsync(OrderCreated @event, CancellationToken ct = default)
    {
        await Task.Delay(300, ct);
        Console.WriteLine($"Email enviado para {@event.CustomerId} do pedido {@event.OrderId}");
    }
}

public class OrderCreatedAnalyticsHandler : IEventHandler<OrderCreated>
{
    public async Task HandleAsync(OrderCreated @event, CancellationToken ct = default)
    {
        await Task.Delay(200, ct);
        Console.WriteLine($"Analytics: Pedido {@event.OrderId} valor {@event.Total:C}");
    }
}

public class FlakyNotificationHandler : IEventHandler<OrderCreated>
{
    private static int _callCount = 0;

    public async Task HandleAsync(OrderCreated @event, CancellationToken ct = default)
    {
        _callCount++;
        await Task.Delay(100, ct);
        
        // Simula falhas para demonstrar retry e circuit breaker
        if (_callCount <= 6) // Primeiras 6 chamadas falham
        {
            throw new Exception($"Falha simulada na notificação #{_callCount}");
        }
        
        Console.WriteLine($"Notificação push enviada para {@event.OrderId}");
    }
}
