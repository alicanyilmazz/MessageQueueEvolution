using Base.Core;

namespace Base.Messages;

public class WorkerThreadExitMessage : WorkerThreadMessageBase
{
    public WorkerThreadExitMessage()
    {
        MessageType = WorkerThreadMessageType.Exit;
    }
}
