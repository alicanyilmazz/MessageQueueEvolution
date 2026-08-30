using General.Retry;
namespace General.Options;

public class MessageOptions
{
    public RetryPolicy RetryPolicy { get; set; } = RetryPolicy.None();

    public TimeSpan? Timeout { get; set; }
}