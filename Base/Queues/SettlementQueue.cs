using Base.QueueMessages;

namespace Base.Queue;

public class SettlementQueue
{
    private readonly List<TransactionSettlement> _queue = new();

    private readonly object _lockObj = new();

    private bool _inPooling;

    private readonly int[] _tryPeriods = { 60, 300, 1800, 7200 };

    public void AddSettlement(long transactionId, decimal amount, string transactionCode)
    {
        var advice = new TransactionSettlement
        {
            TransactionId = transactionId,
            Amount = amount,
            TransactionCode = transactionCode,
            TryCount = 0,
            NextTryTime = DateTime.Now
        };

        lock (_queue)
        {
            _queue.Add(advice);
        }

        Console.WriteLine($"[SettlementQueue] Added. TransactionId: {transactionId}");
    }

    public void PoolSettlementQueue()
    {
        lock (_lockObj)
        {
            if (_inPooling)
            {
                Console.WriteLine("[SettlementQueue] Pooling already active.");

                return;
            }

            _inPooling = true;
        }

        try
        {
            List<TransactionSettlement> currentQueue;

            lock (_queue)
            {
                currentQueue = _queue.Where(x => x.NextTryTime <= DateTime.Now).ToList();
            }

            var itemsToRemove = new List<TransactionSettlement>();

            foreach (var item in currentQueue)
            {
                string responseCode;

                try
                {
                    responseCode = SendSettlementToService(item);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[SettlementQueue] Host error: " + ex.Message);

                    responseCode = "ERROR";
                }

                if (responseCode == "0")
                {
                    itemsToRemove.Add(item);

                    continue;
                }

                if (item.TryCount >= _tryPeriods.Length)
                {
                    Console.WriteLine($"[SettlementQueue] Max retry reached. TransactionId: {item.TransactionId}");

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
            Console.WriteLine("[SettlementQueue] Pool error: " + ex.Message);
        }
        finally
        {
            lock (_lockObj)
            {
                _inPooling = false;
            }
        }
    }

    private void ScheduleNextTry(TransactionSettlement item)
    {
        int delay = _tryPeriods[item.TryCount];

        item.TryCount++;

        item.NextTryTime = item.NextTryTime.AddSeconds(delay);

        Console.WriteLine($"[SettlementQueue] Retry scheduled. " + $"TransactionId: {item.TransactionId}, " + $"TryCount: {item.TryCount}, " + $"NextTryTime: {item.NextTryTime}");
    }

    private string SendSettlementToService(TransactionSettlement item)
    {
        Console.WriteLine($"[SettlementQueue] Sending to host. TransactionId: {item.TransactionId}");

        /*
            Request to host logic here. 
         */

        return "0";
    }
}