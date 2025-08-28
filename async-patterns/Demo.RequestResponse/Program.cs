using AsyncPatterns.Messaging;

Console.WriteLine("Request/Response Assíncrono - Demo Avançado");
Console.WriteLine("Demonstra: Timeouts, Idempotência, Scatter-Gather");

var bus = new InMemoryMessageBus();
bus.RegisterHandler(new GetOrderQueryHandler());
bus.RegisterHandler(new SlowQueryHandler());

Console.WriteLine("\n=== Teste 1: Request/Response normal ===");
var response1 = await bus.SendAsync(new GetOrderQuery("ORDER-1001"));
Console.WriteLine($"Resposta: {response1.OrderId} - {response1.Status}");

Console.WriteLine("\n=== Teste 2: Idempotência (mesma request) ===");
var response2 = await bus.SendAsync(new GetOrderQuery("ORDER-1001"));
Console.WriteLine($"Resposta: {response2.OrderId} - {response2.Status}");

Console.WriteLine("\n=== Teste 3: Request com timeout customizado ===");
try
{
    var response3 = await bus.SendAsync(new SlowQuery("SLOW-QUERY"), TimeSpan.FromMilliseconds(500));
    Console.WriteLine($"Resposta rápida: {response3.Message}");
}
catch (TimeoutException ex)
{
    Console.WriteLine($"Timeout: {ex.Message}");
}

Console.WriteLine("\n=== Teste 4: Scatter-Gather (múltiplas respostas) ===");
var scatterResult = await bus.ScatterGatherAsync(new GetOrderQuery("ORDER-2001"), expectedResponses: 2);
Console.WriteLine($"Scatter-Gather: {scatterResult.Results.Count} sucessos, {scatterResult.Errors.Count} erros");

foreach (var result in scatterResult.Results)
{
    Console.WriteLine($"  {result.OrderId} - {result.Status}");
}

Console.WriteLine("\nFim Request/Response Avançado");

// Tipos
public record GetOrderQuery(string OrderId) : IRequest<GetOrderResponse>;
public record GetOrderResponse(string OrderId, string Status);
public record SlowQuery(string QueryId) : IRequest<SlowResponse>;
public record SlowResponse(string Message);

public class GetOrderQueryHandler : IRequestHandler<GetOrderQuery, GetOrderResponse>
{
    public async Task<GetOrderResponse> HandleAsync(GetOrderQuery request, CancellationToken ct = default)
    {
        Console.WriteLine($"[Handler] Processando query para {request.OrderId}");
        await Task.Delay(300, ct);
        return new GetOrderResponse(request.OrderId, "Completed");
    }
}

public class SlowQueryHandler : IRequestHandler<SlowQuery, SlowResponse>
{
    public async Task<SlowResponse> HandleAsync(SlowQuery request, CancellationToken ct = default)
    {
        Console.WriteLine($"[SlowHandler] Iniciando processamento lento para {request.QueryId}");
        await Task.Delay(2000, ct); // 2 segundos
        return new SlowResponse($"Processamento lento completo para {request.QueryId}");
    }
}
