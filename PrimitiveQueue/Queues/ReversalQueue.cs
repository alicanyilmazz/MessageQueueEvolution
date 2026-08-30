using Base.QueueMessages;

namespace Base.Queue;

public class ReversalQueue
{
    private readonly List<TransactionReversal> _queue = new();

    private readonly object _lockObj = new();

    private bool _inPooling;

    private readonly int[] _tryPeriods = { 60, 300, 900, 1800, 3600, 7200, 10800, 18000 };

    public void AddReversal(long transactionId, decimal amount, string transactionCode, bool isDispense = false, string issuerScriptResult = null)
    {
        var cancellation = new TransactionReversal { TransactionId = transactionId, Amount = amount, TransactionCode = transactionCode, IsDispense = isDispense, IssuerScriptResult = issuerScriptResult, TryCount = 0, NextTryTime = DateTime.Now };

        lock (_queue)
        {
            _queue.Add(cancellation);
        }

        Console.WriteLine($"[ReversalQueue] Added. TransactionId: {transactionId}");
    }

    public void PoolReversalQueue()
    {
        lock (_lockObj)
        {
            if (_inPooling)
            {
                Console.WriteLine("[ReversalQueue] Pooling already active.");
                return;
            }

            _inPooling = true;
        }

        try
        {
            List<TransactionReversal> currentQueue;

            lock (_queue)
            {
                currentQueue = _queue.Where(x => x.NextTryTime <= DateTime.Now).ToList();
            }

            var itemsToRemove = new List<TransactionReversal>();

            foreach (var item in currentQueue)
            {
                string responseCode;

                try
                {
                    responseCode = SendReversalToService(item);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[ReversalQueue] Host error: " + ex.Message);

                    responseCode = "ERROR";
                }

                if (responseCode == "0" || responseCode == "" || responseCode == "AUTO_REVERSAL_ERROR_CODE")
                {
                    itemsToRemove.Add(item);

                    continue;
                }

                if (item.TryCount >= _tryPeriods.Length)
                {
                    Console.WriteLine($"[ReversalQueue] Max retry reached. TransactionId: {item.TransactionId}");

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
        catch (Exception ex)
        {
            Console.WriteLine("[ReversalQueue] Pool error: " + ex.Message);
        }
        finally
        {
            lock (_lockObj)
            {
                _inPooling = false;
            }
        }
    }

    private void ScheduleNextTry(TransactionReversal item)
    {
        int delay = _tryPeriods[item.TryCount];

        item.TryCount++;

        item.NextTryTime = item.NextTryTime.AddSeconds(delay);

        Console.WriteLine($"[ReversalQueue] Retry scheduled. " + $"TransactionId: {item.TransactionId}, " + $"TryCount: {item.TryCount}, " + $"NextTryTime: {item.NextTryTime}");
    }

    private string SendReversalToService(TransactionReversal item)
    {
        Console.WriteLine($"[ReversalQueue] Reversal sending. " + $"TransactionId: {item.TransactionId}");

         /*
            Request to host for reversal with the following parameters:
         */

        return "0";
    }
}
