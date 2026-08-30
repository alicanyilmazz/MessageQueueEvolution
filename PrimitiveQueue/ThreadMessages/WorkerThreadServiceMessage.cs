using Base.Core;

namespace Base.Messages;

public class WorkerThreadServiceMessage : WorkerThreadBaseMessage
{
    public string TransactionCode { get; set; }

    public WorkerThreadServiceMessage()
    {
        MessageType = WorkerThreadMessageTypes.Service;
    }
}