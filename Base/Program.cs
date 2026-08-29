using Base;
using Base.Messages;

WorkerQueueManager manager = new WorkerQueueManager();

manager.AddMessage(new WorkerThreadHostQueryMessage { TransactionCode = "BALANCE" });

manager.AddMessage(new WorkerThreadHostQueryMessage { TransactionCode = "WITHDRAW" });

manager.AddMessage(new WorkerThreadExecuteMessage { Action = () => { Console.WriteLine("Execute message çalıştı."); } });

manager.AddMessage(new WorkerThreadLogMessage { LogMessage = "ATM transaction completed." });

Console.WriteLine("Mesajlar Queue'ya gönderildi.");

Console.ReadLine();

manager.Stop();