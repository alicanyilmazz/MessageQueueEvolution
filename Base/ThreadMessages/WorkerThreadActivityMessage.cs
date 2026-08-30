using Base.Core;

namespace Base.Messages;

public class WorkerThreadActivityMessage : WorkerThreadBaseMessage
{
    public string LogMessage { get; set; }

    public WorkerThreadActivityMessage()
    {
        MessageType = WorkerThreadMessageTypes.Activity;
    }
}