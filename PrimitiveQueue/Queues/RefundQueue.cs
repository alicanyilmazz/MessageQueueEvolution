using Base.QueueMessages;

namespace Base.Queue;

public class RefundQueue
{
    private readonly List<TransactionRefund> _queue = new();

    private readonly object _lockObj = new();

    private bool _inPooling;

    private readonly int[] _tryPeriods = { 60, 300, 900, 1800 };

    public void AddRefund(long transactionId, decimal amount, string transactionCode)
    {
        var reconciliation = new TransactionRefund
        {
            TransactionId = transactionId,
            Amount = amount,
            TransactionCode = transactionCode,
            TryCount = 0,
            NextTryTime = DateTime.Now
        };

        lock (_queue)
        {
            _queue.Add(reconciliation);
        }

        Console.WriteLine($"[RefundQueue] Added. TransactionId: {transactionId}");
    }

    public void PoolRefundQueue()
    {
        lock (_lockObj)
        {
            if (_inPooling)
            {
                return;
            }

            _inPooling = true;
        }

        try
        {
            List<TransactionRefund> currentQueue;

            lock (_queue)
            {
                currentQueue = _queue.Where(x => x.NextTryTime <= DateTime.Now).ToList();
            }

            var itemsToRemove = new List<TransactionRefund>();

            foreach (var item in currentQueue)
            {
                string responseCode;

                try
                {
                    responseCode = SendRefundToService(item);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[RefundQueue] Host error: " + ex.Message);
                    responseCode = "ERROR";
                }

                if (responseCode == "0")
                {
                    itemsToRemove.Add(item);

                    continue;
                }

                if (item.TryCount >= _tryPeriods.Length)
                {
                    itemsToRemove.Add(item);

                    continue;
                }

                ScheduleNextTry(item);
            }

            lock (_queue)
            {
                foreach (var item in itemsToRemove)
                {
                    _queue.Remove(item);
                }
            }
        }
        finally
        {
            lock (_lockObj)
            {
                _inPooling = false;
            }
        }
    }

    private void ScheduleNextTry(TransactionRefund item)
    {
        int delay = _tryPeriods[item.TryCount];

        item.TryCount++;

        item.NextTryTime = item.NextTryTime.AddSeconds(delay);
    }

    private string SendRefundToService(TransactionRefund item)
    {
        Console.WriteLine($"[RefundQueue] Sending to service. TransactionId: {item.TransactionId}");

        return "0";
    }
}