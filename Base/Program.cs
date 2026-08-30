// ======================================================
// NORMAL WORKER QUEUE
// ======================================================

using Base.Manager;
using Base.Messages;
using Base.Service;

WorkerQueueManager manager = new WorkerQueueManager();

manager.AddMessage(new WorkerThreadServiceMessage { TransactionCode = "BALANCE" });

manager.AddMessage(new WorkerThreadServiceMessage { TransactionCode = "WITHDRAW" });

manager.AddMessage(new WorkerThreadExecuteMessage { Action = () => { Console.WriteLine("Execute message çalıştı."); } });

manager.AddMessage(new WorkerThreadActivityMessage { LogMessage = "ATM transaction completed." });


// ======================================================
// RETRY QUEUES
// ======================================================

using QueueService queueService = new QueueService();

queueService.Start();


// Settlement
queueService.SettlementQueue.AddSettlement(serialNumber: 1001, amount: 1000, transactionCode: "WITHDRAW");


// Reversal
queueService.ReversalQueue.AddReversal(transactionId: 1002, amount: 500, transactionCode: "WITHDRAW", isDispense: false);


// Refund
queueService.RefundQueue.AddRefund(serialNumber: 1003, amount: 1500, transactionCode: "RECON");

Console.WriteLine();
Console.WriteLine("Messages added to queues.");
Console.WriteLine("Press ENTER to exit.");

Console.ReadLine();

manager.Stop();