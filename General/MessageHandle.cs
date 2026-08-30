namespace General;

public class MessageHandle
{
    public Guid Id { get; }

    public Task Completion { get; }

    internal MessageHandle(Guid id, Task completion)
    {
        Id = id;
        Completion = completion;
    }
}
public class MessageHandle<TResult>
{
    public Guid Id { get; }

    public Task<TResult> Completion { get; }

    internal MessageHandle(Guid id, Task<TResult> completion)
    {
        Id = id;
        Completion = completion;
    }
}
