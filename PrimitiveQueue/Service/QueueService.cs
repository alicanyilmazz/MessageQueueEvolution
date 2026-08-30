using Base.Queue;

namespace Base.Service;

public class QueueService : IDisposable
{
    public SettlementQueue SettlementQueue { get; }
    public ReversalQueue ReversalQueue { get; }
    public RefundQueue RefundQueue { get; }

    private readonly System.Timers.Timer _settlementTimer;
    private readonly System.Timers.Timer _reversalTimer;
    private readonly System.Timers.Timer _refundTimer;

    public QueueService()
    {
        SettlementQueue = new SettlementQueue();
        ReversalQueue = new ReversalQueue();
        RefundQueue = new RefundQueue();

        // ----------------------------------------------
        // SETTLEMENT
        // ----------------------------------------------

        _settlementTimer = new System.Timers.Timer(13000);
        _settlementTimer.Elapsed += (_, _) => SettlementQueue.PoolSettlementQueue();

        // ----------------------------------------------
        // REVERSAL
        // ----------------------------------------------

        _reversalTimer = new System.Timers.Timer(12000);
        _reversalTimer.Elapsed += (_, _) => ReversalQueue.PoolReversalQueue();

        // ----------------------------------------------
        // REFUND
        // ----------------------------------------------

        _refundTimer = new System.Timers.Timer(13000);
        _refundTimer.Elapsed += (_, _) => RefundQueue.PoolRefundQueue();
    }

    public void Start()
    {
        _settlementTimer.Start();
        _reversalTimer.Start();
        _refundTimer.Start();

        Console.WriteLine("Retry queues started.");
    }

    public void Stop()
    {
        _settlementTimer.Stop();
        _reversalTimer.Stop();
        _refundTimer.Stop();

        Console.WriteLine("Retry queues stopped.");
    }

    public void Dispose()
    {
        Stop();

        _settlementTimer.Dispose();
        _reversalTimer.Dispose();
        _refundTimer.Dispose();
    }
}