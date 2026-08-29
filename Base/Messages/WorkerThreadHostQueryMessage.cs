using Base.Core;

namespace Base.Messages;

public class WorkerThreadHostQueryMessage : WorkerThreadMessageBase
{
    public string TransactionCode { get; set; }

    public WorkerThreadHostQueryMessage()
    {
        MessageType = WorkerThreadMessageType.HostQuery;
    }
}