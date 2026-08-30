using General.Dispatcher;
using General.Envelope;
using General.Messages;
using General.Options;
using General.Queue;
using System.Collections.Concurrent;

namespace General;

public class MessageBus
{
    private readonly ConcurrentDictionary<string, WorkQueue> _queues = new();

    public MessageDispatcher Dispatcher { get; }

    public MessageBus()
    {
        Dispatcher = new MessageDispatcher();
    }

    // ==================================================
    // CREATE CUSTOM QUEUE
    // ==================================================

    public WorkQueue CreateQueue(string queueName, int capacity = 1000)
    {
        return _queues.GetOrAdd(queueName, name => new WorkQueue(name, Dispatcher, capacity));
    }

    // ==================================================
    // COMMAND
    // ==================================================

    public async Task<MessageHandle> SendAsync<TCommand>(string queueName, TCommand command, MessageOptions options = null) where TCommand : ICommand
    {
        WorkQueue queue = GetQueue(queueName);

        MessageEnvelope envelope = await queue.EnqueueAsync(command, options);

        return new MessageHandle(envelope.Id, envelope.CompletionSource.Task);
    }

    // ==================================================
    // QUERY
    // ==================================================

    public async Task<MessageHandle<TResult>> QueryAsync<TQuery, TResult>(string queueName, TQuery query, MessageOptions options = null) where TQuery : IQuery<TResult>
    {
        WorkQueue queue = GetQueue(queueName);

        MessageEnvelope envelope = await queue.EnqueueAsync(query, options);

        async Task<TResult> WaitResultAsync()
        {
            object result = await envelope.CompletionSource.Task;

            return (TResult)result;
        }

        return new MessageHandle<TResult>(envelope.Id, WaitResultAsync());
    }

    // ==================================================
    // EXECUTE ACTION
    // ==================================================

    public async Task<MessageHandle> ExecuteAsync(string queueName, Func<CancellationToken, Task> action, MessageOptions options = null)
    {
        WorkQueue queue = GetQueue(queueName);

        var message = new ActionMessage(action);

        MessageEnvelope envelope = await queue.EnqueueAsync(message, options);

        return new MessageHandle(envelope.Id, envelope.CompletionSource.Task);
    }

    // ==================================================
    // CANCEL
    // ==================================================

    public bool Cancel(string queueName, Guid messageId)
    {
        return GetQueue(queueName).Cancel(messageId);
    }

    private WorkQueue GetQueue(string queueName)
    {
        if (!_queues.TryGetValue(queueName, out WorkQueue queue))
        {
            throw new InvalidOperationException($"Queue not found: {queueName}");
        }

        return queue;
    }

    // ==================================================
    // STOP ALL
    // ==================================================

    public async Task StopAsync()
    {
        foreach (WorkQueue queue in _queues.Values)
        {
            await queue.StopAsync();
        }
    }
}