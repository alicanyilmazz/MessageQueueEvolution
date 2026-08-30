using Base.Core;
using Base.Messages;
using System.Collections;

namespace Base.Manager;

public class WorkerQueueManager
{
    public Queue<WorkerThreadBaseMessage> MsgQueue { get; private set; }

    private readonly Thread _workerThread;

    public WorkerQueueManager()
    {
        MsgQueue = new Queue<WorkerThreadBaseMessage>();

        _workerThread = new Thread(WorkerThreadRun);

        _workerThread.Name = "WorkerThread";

        _workerThread.Start();
    }

    // ==================================================
    // PRODUCER
    // ==================================================

    public void AddMessage(WorkerThreadBaseMessage msg)
    {
        lock (((ICollection)MsgQueue).SyncRoot)
        {
            MsgQueue.Enqueue(msg);

            // Wake up the sleeping WorkerThread. - Uyuyan WorkerThread'i uyandır.
            Monitor.Pulse(((ICollection)MsgQueue).SyncRoot);
        }
    }

    // ==================================================
    // CONSUMER
    // ==================================================

    private void WorkerThreadRun()
    {
        while (true)
        {
            WorkerThreadBaseMessage msg;

            lock (((ICollection)MsgQueue).SyncRoot)
            {
                /*
                 * If the queue is empty, the thread waits here.
                 *
                 * Monitor.Wait:
                 * 1- Releases the lock - lock u bırakır
                 * 2- Puts the thread to sleep - thread i uykuya alır
                 * 3- Wakes up when Pulse is called - Pulse çağrıldığında uyanır
                 * 4- Reacquires the lock  - lock u tekrar alır
                 */

                while (MsgQueue.Count == 0)
                {
                    Monitor.Wait(((ICollection)MsgQueue).SyncRoot);
                }

                msg = MsgQueue.Dequeue();
            }

            // If the message is an exit message, terminate the worker thread.
            if (msg.MessageType == WorkerThreadMessageTypes.Exit)
            {
                return;
            }

            try
            {
                HandleWorkerThreadMessage(msg);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Worker Thread Error: " + ex);
            }
        }
    }

    // ==================================================
    // MESSAGE HANDLER
    // ==================================================

    private void HandleWorkerThreadMessage(WorkerThreadBaseMessage msg)
    {
        switch (msg.MessageType)
        {
            case WorkerThreadMessageTypes.Service:
                {
                    var hostQuery = (WorkerThreadServiceMessage)msg;
                    HandleHostQuery(hostQuery);
                    break;
                }

            case WorkerThreadMessageTypes.Execute:
                {
                    var execute = (WorkerThreadExecuteMessage)msg;
                    execute.Action?.Invoke();
                    break;
                }

            case WorkerThreadMessageTypes.Activity:
                {
                    var log = (WorkerThreadActivityMessage)msg;
                    Console.WriteLine("LOG : " + log.LogMessage);
                    break;
                }

            case WorkerThreadMessageTypes.System:
                {
                    Console.WriteLine("System message received.");
                    break;
                }
        }
    }

    private void HandleHostQuery(WorkerThreadServiceMessage message)
    {
        Console.WriteLine("Host Query Begin : " + message.TransactionCode);

        // Handling host query logic here...
        Thread.Sleep(2000);

        Console.WriteLine("Host Query Completed : " + message.TransactionCode);
    }

    // ==================================================
    // STOP
    // ==================================================

    public void Stop()
    {
        AddMessage(new WorkerThreadExitMessage());

        _workerThread.Join();
    }
}