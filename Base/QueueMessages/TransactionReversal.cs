namespace Base.QueueMessages;

public class TransactionReversal : ITransactionId
{
    public long TransactionId { get; set; }

    public decimal Amount { get; set; }

    public string TransactionCode { get; set; }

    public string IssuerScriptResult { get; set; }

    public bool IsDispense { get; set; }

    public string ReversalResponseCode { get; set; }

    public int TryCount { get; set; }

    public DateTime NextTryTime { get; set; }
}