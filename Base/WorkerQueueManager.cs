using Base.Core;
using Base.Messages;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Base;

public class WorkerQueueManager
{
    public Queue<WorkerThreadMessageBase> MsgQueue { get; private set; }

    private readonly Thread _workerThread;

    public WorkerQueueManager()
    {
        MsgQueue = new Queue<WorkerThreadMessageBase>();

        _workerThread = new Thread(WorkerThreadRun);

        _workerThread.Name = "WorkerThread";

        _workerThread.Start();
    }

    // ==================================================
    // PRODUCER
    // ==================================================

    public void AddMessage(WorkerThreadMessageBase msg)
    {
        lock (((ICollection)MsgQueue).SyncRoot)
        {
            MsgQueue.Enqueue(msg);

            // Uyuyan WorkerThread'i uyandır.
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
            WorkerThreadMessageBase msg;

            lock (((ICollection)MsgQueue).SyncRoot)
            {
                /*
                 * Queue boşsa thread burada uyur.
                 *
                 * Monitor.Wait:
                 * 1- lock'u bırakır
                 * 2- thread'i uyutur
                 * 3- Pulse gelince uyanır
                 * 4- tekrar lock'u alır
                 */

                while (MsgQueue.Count == 0)
                {
                    Monitor.Wait(((ICollection)MsgQueue).SyncRoot);
                }

                msg = MsgQueue.Dequeue();
            }

            // Exit geldiyse Worker Thread kapanır.
            if (msg.MessageType == WorkerThreadMessageType.Exit)
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

    private void HandleWorkerThreadMessage(WorkerThreadMessageBase msg)
    {
        switch (msg.MessageType)
        {
            case WorkerThreadMessageType.HostQuery:
                {
                    var hostQuery = (WorkerThreadHostQueryMessage)msg;
                    HandleHostQuery(hostQuery);
                    break;
                }

            case WorkerThreadMessageType.Execute:
                {
                    var execute = (WorkerThreadExecuteMessage)msg;
                    execute.Action?.Invoke();
                    break;
                }

            case WorkerThreadMessageType.SendLog:
                {
                    var log = (WorkerThreadLogMessage)msg;
                    Console.WriteLine("LOG : " + log.LogMessage);
                    break;
                }

            case WorkerThreadMessageType.Diagnostics:
                {
                    Console.WriteLine("Diagnostics çalıştırıldı.");
                    break;
                }

            case WorkerThreadMessageType.ScreenAfterLoad:
                {
                    Console.WriteLine("ScreenAfterLoad çalıştırıldı.");
                    break;
                }
        }
    }

    private void HandleHostQuery(WorkerThreadHostQueryMessage message)
    {
        Console.WriteLine("Host Query başladı : " + message.TransactionCode);

        // Burada gerçek host işlemini yaparsın.
        Thread.Sleep(2000);

        Console.WriteLine("Host Query tamamlandı : " + message.TransactionCode);
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