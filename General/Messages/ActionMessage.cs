using General.Messages.Base;
namespace General.Messages;

public class ActionMessage : IMessage
{
    public Func<CancellationToken, Task> Action { get; }

    public ActionMessage(Func<CancellationToken, Task> action)
    {
        Action = action;
    }
}