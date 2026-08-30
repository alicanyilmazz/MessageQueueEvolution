using General;
using General.Commands;
using General.Options;
using General.Retry;

MessageBus bus = new MessageBus();

// =====================================================
// CUSTOM QUEUES
// =====================================================

bus.CreateQueue("commands");

bus.CreateQueue("queries");

bus.CreateQueue("background");


// =====================================================
// COMMAND HANDLER
// =====================================================

bus.Dispatcher.RegisterCommandHandler<CreateOrderCommand>(async (command, cancellationToken) =>
        {
            Console.WriteLine($"CreateOrder API çağrısı başladı. " + $"Customer: {command.CustomerId}");

            await Task.Delay(1000, cancellationToken);

            /*
             * real: await httpClient.PostAsync(...)
             */

            Console.WriteLine("Order oluşturuldu.");
        });


// =====================================================
// QUERY HANDLER
// =====================================================

bus.Dispatcher.RegisterQueryHandler<GetOrderQuery, OrderResponse>(async (query, cancellationToken) =>
        {
            Console.WriteLine($"Order API sorgulanıyor: " + query.OrderId);

            await Task.Delay(500, cancellationToken);

            /*
             * real:
             *
             * return await httpClient.GetFromJsonAsync<OrderResponse>(...)
             */

            return new OrderResponse(query.OrderId, "Completed", 1500);
        });


// =====================================================
// COMMAND + RETRY
// =====================================================

MessageHandle commandHandle = await bus.SendAsync("commands", new CreateOrderCommand(100, 2500),

        new MessageOptions
        {
            RetryPolicy = RetryPolicy.Exponential(maxAttempts: 5, firstDelay: TimeSpan.FromSeconds(1)),
            Timeout = TimeSpan.FromSeconds(10)
        });


// =====================================================
// QUERY
// =====================================================

MessageHandle<OrderResponse> queryHandle = await bus.QueryAsync<GetOrderQuery, OrderResponse>("queries", new GetOrderQuery(123), new MessageOptions
{
    RetryPolicy = RetryPolicy.Fixed(3, TimeSpan.FromSeconds(2)),
    Timeout = TimeSpan.FromSeconds(5)
});


OrderResponse order = await queryHandle.Completion;

Console.WriteLine($"Order: {order.Id} - {order.Status}");


// =====================================================
// EXECUTE
// =====================================================

MessageHandle actionHandle = await bus.ExecuteAsync("background",
        async cancellationToken =>
        {
            Console.WriteLine("Background işlem başladı.");

            await Task.Delay(2000, cancellationToken);

            Console.WriteLine("Background işlem bitti.");
        });

await actionHandle.Completion;


// =====================================================
// STOP
// =====================================================

await bus.StopAsync();