using General.Messages;
using General.Messages.Base;
using System.Collections.Concurrent;

namespace General.Dispatcher;

public class MessageDispatcher
{
    private readonly ConcurrentDictionary<Type, Func<object, CancellationToken, Task<object>>> _handlers = new();

    // ==================================================
    // COMMAND HANDLER
    // ==================================================

    public void RegisterCommandHandler<TCommand>(Func<TCommand, CancellationToken, Task> handler) where TCommand : ICommand
    {
        _handlers[typeof(TCommand)] = async (message, cancellationToken) =>
        {
            await handler((TCommand)message, cancellationToken);

            return null;
        };
    }

    // ==================================================
    // QUERY HANDLER
    // ==================================================

    public void RegisterQueryHandler<TQuery, TResult>(Func<TQuery, CancellationToken, Task<TResult>> handler) where TQuery : IQuery<TResult>
    {
        _handlers[typeof(TQuery)] = async (message, cancellationToken) =>
            {
                return await handler((TQuery)message, cancellationToken);
            };
    }

    // ==================================================
    // DISPATCH
    // ==================================================

    internal async Task<object> DispatchAsync(IMessage message, CancellationToken cancellationToken)
    {
        // Local Execute / Action
        if (message is ActionMessage actionMessage)
        {
            await actionMessage.Action(cancellationToken);

            return null;
        }

        Type messageType = message.GetType();

        if (!_handlers.TryGetValue(messageType, out var handler))
        {
            throw new InvalidOperationException($"Handler not found for {messageType.Name}");
        }

        return await handler(message, cancellationToken);
    }
}
