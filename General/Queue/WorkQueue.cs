using General.Dispatcher;
using General.Envelope;
using General.Messages;
using General.Messages.Base;
using General.Options;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace General.Queue;

public class WorkQueue
{
    private readonly Channel<MessageEnvelope> _channel;

    private readonly MessageDispatcher _dispatcher;

    private readonly ConcurrentDictionary<Guid, MessageEnvelope> _pendingMessages = new();

    private readonly CancellationTokenSource _shutdownSource = new();

    private readonly Task _workerTask;

    public string Name { get; }

    public WorkQueue(string name, MessageDispatcher dispatcher, int capacity = 1000)
    {
        Name = name;

        _dispatcher = dispatcher;

        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,

            SingleReader = true,

            SingleWriter = false
        };

        _channel = Channel.CreateBounded<MessageEnvelope>(options);

        _workerTask = Task.Run(WorkerLoopAsync);
    }

    // ==================================================
    // ENQUEUE
    // ==================================================

    internal async Task<MessageEnvelope> EnqueueAsync(IMessage message, MessageOptions options = null)
    {
        var envelope = new MessageEnvelope(message, options);

        _pendingMessages.TryAdd(envelope.Id, envelope);

        try
        {
            await _channel.Writer.WriteAsync(envelope, _shutdownSource.Token);

            Console.WriteLine($"[{Name}] Enqueued: " + $"{message.GetType().Name} " + $"({envelope.Id})");

            return envelope;
        }
        catch
        {
            _pendingMessages.TryRemove(envelope.Id, out _);

            throw;
        }
    }

    // ==================================================
    // WORKER
    // ==================================================

    private async Task WorkerLoopAsync()
    {
        try
        {
            while (await _channel.Reader.WaitToReadAsync(_shutdownSource.Token))
            {
                while (_channel.Reader.TryRead(out MessageEnvelope envelope))
                {
                    if (envelope.Message is ExitMessage)
                    {
                        Console.WriteLine($"[{Name}] Exit received.");

                        return;
                    }

                    await ProcessMessageAsync(envelope);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Queue stopping.
        }
    }

    // ==================================================
    // PROCESS
    // ==================================================

    private async Task ProcessMessageAsync(MessageEnvelope envelope)
    {
        if (envelope.CancellationSource.IsCancellationRequested)
        {
            CompleteAsCanceled(envelope);

            return;
        }

        envelope.Attempt++;

        Console.WriteLine($"[{Name}] Processing " + $"{envelope.Message.GetType().Name}. " + $"Attempt: {envelope.Attempt}");

        using var attemptCancellation = CancellationTokenSource.CreateLinkedTokenSource(_shutdownSource.Token, envelope.CancellationSource.Token);

        if (envelope.Timeout.HasValue)
        {
            attemptCancellation.CancelAfter(envelope.Timeout.Value);
        }

        try
        {
            object result = await _dispatcher.DispatchAsync(envelope.Message, attemptCancellation.Token);

            CompleteSuccessfully(envelope, result);
        }

        // ==============================================
        // USER CANCEL
        // ==============================================

        catch (OperationCanceledException) when (envelope.CancellationSource.IsCancellationRequested)
        {
            CompleteAsCanceled(envelope);
        }

        // ==============================================
        // TIMEOUT
        // ==============================================

        catch (OperationCanceledException) when (!_shutdownSource.IsCancellationRequested)
        {
            var timeoutException = new TimeoutException($"Message timed out. " + $"Timeout: {envelope.Timeout}");

            await HandleFailureAsync(envelope, timeoutException);
        }

        // ==============================================
        // NORMAL ERROR
        // ==============================================

        catch (Exception ex)
        {
            await HandleFailureAsync(envelope, ex);
        }
    }

    // ==================================================
    // FAILURE / RETRY
    // ==================================================

    private async Task HandleFailureAsync(MessageEnvelope envelope, Exception exception)
    {
        if (envelope.RetryPolicy.CanRetry(envelope.Attempt, exception))
        {
            TimeSpan delay = envelope.RetryPolicy.GetDelay(envelope.Attempt);

            Console.WriteLine($"[{Name}] Failed. " + $"Retrying after {delay}. " + $"Error: {exception.Message}");

            _ = ScheduleRetryAsync(envelope, delay);

            return;
        }

        Console.WriteLine($"[{Name}] Failed permanently. " + $"Error: {exception.Message}");

        CompleteWithError(envelope, exception);
    }

    // ==================================================
    // DELAYED RETRY
    // ==================================================

    private async Task ScheduleRetryAsync(MessageEnvelope envelope, TimeSpan delay)
    {
        try
        {
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(_shutdownSource.Token, envelope.CancellationSource.Token);

            await Task.Delay(delay, linked.Token);

            await _channel.Writer.WriteAsync(envelope, linked.Token);
        }
        catch (OperationCanceledException)
        {
            CompleteAsCanceled(envelope);
        }
    }

    // ==================================================
    // CANCEL MESSAGE
    // ==================================================

    public bool Cancel(Guid messageId)
    {
        if (!_pendingMessages.TryGetValue(messageId, out MessageEnvelope envelope))
        {
            return false;
        }

        Console.WriteLine($"[{Name}] Cancel requested: " + messageId);

        envelope.CancellationSource.Cancel();

        return true;
    }

    // ==================================================
    // COMPLETE
    // ==================================================

    private void CompleteSuccessfully(MessageEnvelope envelope, object result)
    {
        envelope.CompletionSource.TrySetResult(result);

        Cleanup(envelope);

        Console.WriteLine($"[{Name}] Completed: {envelope.Id}");
    }

    private void CompleteWithError(MessageEnvelope envelope, Exception exception)
    {
        envelope.CompletionSource.TrySetException(exception);

        Cleanup(envelope);
    }

    private void CompleteAsCanceled(MessageEnvelope envelope)
    {
        envelope.CompletionSource.TrySetCanceled();

        Cleanup(envelope);

        Console.WriteLine($"[{Name}] Canceled: {envelope.Id}");
    }

    private void Cleanup(MessageEnvelope envelope)
    {
        _pendingMessages.TryRemove(envelope.Id, out _);

        envelope.CancellationSource.Dispose();
    }

    // ==================================================
    // STOP
    // ==================================================

    public async Task StopAsync()
    {
        var exitEnvelope = new MessageEnvelope(new ExitMessage(), new MessageOptions());

        await _channel.Writer.WriteAsync(exitEnvelope);

        await _workerTask;

        _shutdownSource.Cancel();

        _channel.Writer.TryComplete();

        foreach (var item in _pendingMessages)
        {
            item.Value.CancellationSource.Cancel();
        }

        Console.WriteLine($"[{Name}] stopped.");
    }
}