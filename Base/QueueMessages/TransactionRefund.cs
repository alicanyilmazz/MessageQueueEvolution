namespace Base.QueueMessages;

public class TransactionRefund : ITransactionId
{
    public long TransactionId { get; set; }

    public decimal Amount { get; set; }

    public string TransactionCode { get; set; }

    public int TryCount { get; set; }

    public DateTime NextTryTime { get; set; }
}