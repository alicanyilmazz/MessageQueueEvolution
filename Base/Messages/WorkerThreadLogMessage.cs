using Base.Core;

namespace Base.Messages;

public class WorkerThreadLogMessage : WorkerThreadMessageBase
{
    public string LogMessage { get; set; }

    public WorkerThreadLogMessage()
    {
        MessageType = WorkerThreadMessageType.SendLog;
    }
}