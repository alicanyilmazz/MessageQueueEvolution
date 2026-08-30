using Base.Core;

namespace Base.Messages;

public class WorkerThreadExecuteMessage : WorkerThreadBaseMessage
{
    public Action Action { get; set; }

    public WorkerThreadExecuteMessage()
    {
        MessageType = WorkerThreadMessageTypes.Execute;
    }
}
