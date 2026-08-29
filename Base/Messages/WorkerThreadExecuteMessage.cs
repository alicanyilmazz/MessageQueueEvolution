using Base.Core;

namespace Base.Messages;

public class WorkerThreadExecuteMessage : WorkerThreadMessageBase
{
    public Action Action { get; set; }

    public WorkerThreadExecuteMessage()
    {
        MessageType = WorkerThreadMessageType.Execute;
    }
}
