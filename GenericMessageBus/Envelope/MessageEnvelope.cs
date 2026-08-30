using General.Messages.Base;
using General.Options;
using General.Retry;

namespace General.Envelope;

internal class MessageEnvelope
{
    public Guid Id { get; }

    public IMessage Message { get; }

    public RetryPolicy RetryPolicy { get; }

    public TimeSpan? Timeout { get; }

    public int Attempt { get; set; }

    public DateTime CreatedAt { get; }

    public CancellationTokenSource CancellationSource { get; }

    public TaskCompletionSource<object> CompletionSource { get; }

    public MessageEnvelope(IMessage message, MessageOptions options)
    {
        Id = Guid.NewGuid();

        Message = message;

        RetryPolicy = options?.RetryPolicy ?? RetryPolicy.None();

        Timeout = options?.Timeout;

        CreatedAt = DateTime.UtcNow;

        CancellationSource = new CancellationTokenSource();

        CompletionSource = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}
