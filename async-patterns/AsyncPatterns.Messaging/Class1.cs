using System.Collections.Concurrent;

namespace AsyncPatterns.Messaging;

public interface IEvent 
{
    string EventId { get; }
    DateTime Timestamp { get; }
}

public interface IMessage { }

public interface IEventHandler<in TEvent> where TEvent : IEvent
{
    Task HandleAsync(TEvent @event, CancellationToken ct = default);
}

public interface IRequest<TResponse> : IMessage { }
public interface IRequestHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    Task<TResponse> HandleAsync(TRequest request, CancellationToken ct = default);
}

// Pub/Sub com Dead Letter Queue e Circuit Breaker
public interface IEventBus
{
    void Subscribe<TEvent>(IEventHandler<TEvent> handler) where TEvent : IEvent;
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default) where TEvent : IEvent;
    IEnumerable<IEvent> GetDeadLetterQueue();
}

public class InMemoryEventBus : IEventBus
{
    private readonly ConcurrentDictionary<Type, List<HandlerWrapper>> _handlers = new();
    private readonly ConcurrentQueue<IEvent> _deadLetterQueue = new();
    private readonly ConcurrentDictionary<string, CircuitBreakerState> _circuitBreakers = new();

    public void Subscribe<TEvent>(IEventHandler<TEvent> handler) where TEvent : IEvent
    {
        var key = typeof(TEvent);
        var wrapper = new HandlerWrapper(handler, typeof(TEvent).Name);
        _handlers.AddOrUpdate(key, _ => new List<HandlerWrapper> { wrapper }, (_, list) => 
        { 
            list.Add(wrapper); 
            return list; 
        });
    }

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default) where TEvent : IEvent
    {
        if (_handlers.TryGetValue(typeof(TEvent), out var handlers))
        {
            var tasks = handlers.Select(async wrapper => 
            {
                var circuitBreakerKey = $"{typeof(TEvent).Name}_{wrapper.HandlerName}";
                
                // Circuit Breaker check
                if (IsCircuitOpen(circuitBreakerKey))
                {
                    Console.WriteLine($"[Circuit Breaker] Handler {wrapper.HandlerName} está em circuito aberto");
                    _deadLetterQueue.Enqueue(@event);
                    return;
                }

                try
                {
                    // Retry policy: 3 tentativas
                    var attempts = 0;
                    var maxAttempts = 3;
                    
                    while (attempts < maxAttempts)
                    {
                        try
                        {
                            var handler = (IEventHandler<TEvent>)wrapper.Handler;
                            await handler.HandleAsync(@event, ct);
                            
                            // Sucesso: reset circuit breaker
                            RecordSuccess(circuitBreakerKey);
                            return;
                        }
                        catch (Exception ex) when (attempts < maxAttempts - 1)
                        {
                            attempts++;
                            Console.WriteLine($"[Retry] Tentativa {attempts} falhou para {wrapper.HandlerName}: {ex.Message}");
                            await Task.Delay(TimeSpan.FromMilliseconds(100 * attempts), ct); // Backoff
                        }
                    }
                    
                    // Todas as tentativas falharam
                    throw new Exception($"Handler {wrapper.HandlerName} falhou após {maxAttempts} tentativas");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Error] Handler {wrapper.HandlerName} falhou: {ex.Message}");
                    RecordFailure(circuitBreakerKey);
                    _deadLetterQueue.Enqueue(@event);
                }
            });
            
            await Task.WhenAll(tasks);
        }
    }

    public IEnumerable<IEvent> GetDeadLetterQueue() => _deadLetterQueue.ToArray();

    private bool IsCircuitOpen(string key)
    {
        if (!_circuitBreakers.TryGetValue(key, out var state))
            return false;

        if (state.State == CircuitState.Open)
        {
            // Verifica se pode tentar half-open
            if (DateTime.UtcNow > state.NextAttemptTime)
            {
                state.State = CircuitState.HalfOpen;
                return false;
            }
            return true;
        }
        
        return false;
    }

    private void RecordSuccess(string key)
    {
        _circuitBreakers.AddOrUpdate(key, 
            new CircuitBreakerState(), 
            (_, state) => 
            {
                state.FailureCount = 0;
                state.State = CircuitState.Closed;
                return state;
            });
    }

    private void RecordFailure(string key)
    {
        _circuitBreakers.AddOrUpdate(key,
            new CircuitBreakerState { FailureCount = 1 },
            (_, state) =>
            {
                state.FailureCount++;
                if (state.FailureCount >= 3) // Threshold
                {
                    state.State = CircuitState.Open;
                    state.NextAttemptTime = DateTime.UtcNow.AddSeconds(30); // Timeout
                    Console.WriteLine($"[Circuit Breaker] Circuito {key} ABERTO por 30 segundos");
                }
                return state;
            });
    }
}

// Request/Response com Timeout e Idempotência
public interface IMessageBus
{
    void RegisterHandler<TRequest, TResponse>(IRequestHandler<TRequest, TResponse> handler)
        where TRequest : IRequest<TResponse>;
    Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, TimeSpan? timeout = null, CancellationToken ct = default);
    Task<ScatterGatherResult<TResponse>> ScatterGatherAsync<TResponse>(IRequest<TResponse> request, int expectedResponses = 1, CancellationToken ct = default);
}

public class InMemoryMessageBus : IMessageBus
{
    private readonly ConcurrentDictionary<Type, object> _handlers = new();
    private readonly ConcurrentDictionary<string, object> _responseCache = new(); // Para idempotência

    public void RegisterHandler<TRequest, TResponse>(IRequestHandler<TRequest, TResponse> handler)
        where TRequest : IRequest<TResponse>
    {
        _handlers[typeof(TRequest)] = handler;
    }

    public async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, TimeSpan? timeout = null, CancellationToken ct = default)
    {
        // Idempotência: verifica cache
        var cacheKey = $"{request.GetType().Name}_{request.GetHashCode()}";
        if (_responseCache.TryGetValue(cacheKey, out var cachedResponse))
        {
            Console.WriteLine($"[Cache] Resposta encontrada em cache para {request.GetType().Name}");
            return (TResponse)cachedResponse;
        }

        var actualTimeout = timeout ?? TimeSpan.FromSeconds(30);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(actualTimeout);

        try
        {
            var type = request.GetType();
            if (!_handlers.TryGetValue(type, out var handler))
            {
                throw new InvalidOperationException($"No handler registered for {type.Name}");
            }

            dynamic dynHandler = handler!;
            dynamic dynRequest = request!;
            TResponse response = await dynHandler.HandleAsync(dynRequest, cts.Token);
            
            // Cache para idempotência (TTL de 5 minutos)
            _responseCache.TryAdd(cacheKey, response);
            _ = Task.Delay(TimeSpan.FromMinutes(5), ct).ContinueWith(_ => _responseCache.TryRemove(cacheKey, out var _));
            
            return response;
        }
        catch (OperationCanceledException) when (cts.Token.IsCancellationRequested && !ct.IsCancellationRequested)
        {
            throw new TimeoutException($"Request {request.GetType().Name} timed out after {actualTimeout}");
        }
    }

    public async Task<ScatterGatherResult<TResponse>> ScatterGatherAsync<TResponse>(IRequest<TResponse> request, int expectedResponses = 1, CancellationToken ct = default)
    {
        var tasks = new List<Task<TResponse>>();
        var results = new List<TResponse>();
        var errors = new List<Exception>();

        // Simula múltiplos handlers (scatter)
        for (int i = 0; i < expectedResponses; i++)
        {
            tasks.Add(SendAsync(request, TimeSpan.FromSeconds(10), ct));
        }

        // Gather results
        foreach (var task in tasks)
        {
            try
            {
                var result = await task;
                results.Add(result);
            }
            catch (Exception ex)
            {
                errors.Add(ex);
            }
        }

        return new ScatterGatherResult<TResponse>(results, errors);
    }
}

// Supporting types
public record ScatterGatherResult<T>(List<T> Results, List<Exception> Errors);

public class HandlerWrapper
{
    public object Handler { get; }
    public string HandlerName { get; }
    
    public HandlerWrapper(object handler, string handlerName)
    {
        Handler = handler;
        HandlerName = handlerName;
    }
}

public class CircuitBreakerState
{
    public CircuitState State { get; set; } = CircuitState.Closed;
    public int FailureCount { get; set; }
    public DateTime NextAttemptTime { get; set; }
}

public enum CircuitState { Closed, Open, HalfOpen }
