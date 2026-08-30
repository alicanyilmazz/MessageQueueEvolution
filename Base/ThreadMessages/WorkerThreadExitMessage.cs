using Base.Core;

namespace Base.Messages;

public class WorkerThreadExitMessage : WorkerThreadBaseMessage
{
    public WorkerThreadExitMessage()
    {
        MessageType = WorkerThreadMessageTypes.Exit;
    }
}
