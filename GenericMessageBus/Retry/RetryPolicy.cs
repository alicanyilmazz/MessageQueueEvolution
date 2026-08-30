namespace General.Retry;

public class RetryPolicy
{
    public int MaxAttempts { get; }

    private readonly Func<int, TimeSpan> _delayProvider;

    private readonly Func<Exception, bool> _shouldRetry;

    public RetryPolicy(int maxAttempts, Func<int, TimeSpan> delayProvider, Func<Exception, bool> shouldRetry = null)
    {
        if (maxAttempts < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxAttempts));
        }

        MaxAttempts = maxAttempts;

        _delayProvider = delayProvider ?? throw new ArgumentNullException(nameof(delayProvider));

        _shouldRetry = shouldRetry ?? (_ => true);
    }

    public bool CanRetry(int currentAttempt, Exception exception)
    {
        return currentAttempt < MaxAttempts && _shouldRetry(exception);
    }

    public TimeSpan GetDelay(int failedAttempt)
    {
        return _delayProvider(failedAttempt);
    }

    // ==========================================
    // NO RETRY
    // ==========================================

    public static RetryPolicy None()
    {
        return new RetryPolicy(1, _ => TimeSpan.Zero);
    }

    // ==========================================
    // FIXED
    // ==========================================

    public static RetryPolicy Fixed(int maxAttempts, TimeSpan delay)
    {
        return new RetryPolicy(maxAttempts, _ => delay);
    }

    // ==========================================
    // EXPONENTIAL
    // ==========================================

    public static RetryPolicy Exponential(int maxAttempts, TimeSpan firstDelay)
    {
        return new RetryPolicy(maxAttempts, attempt =>
        {
            double multiplier = Math.Pow(2, attempt - 1);

            return TimeSpan.FromMilliseconds(firstDelay.TotalMilliseconds * multiplier);
        });
    }
}