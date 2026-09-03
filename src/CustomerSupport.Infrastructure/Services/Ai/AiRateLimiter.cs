using Microsoft.Extensions.Options;

namespace CustomerSupport.Infrastructure.Services.Ai;

public class AiRateLimiter
{
    private readonly SemaphoreSlim _concurrencySemaphore;
    private readonly int _maxTokensPerMinute;
    private readonly object _tokenLock = new();
    private int _tokensUsedThisWindow;
    private DateTime _windowStart;

    public AiRateLimiter(IOptions<AiSettings> settings)
    {
        _concurrencySemaphore = new SemaphoreSlim(settings.Value.MaxConcurrentRequests);
        _maxTokensPerMinute = settings.Value.MaxTokensPerMinute;
        _windowStart = DateTime.UtcNow;
    }

    public async Task AcquireAsync(int estimatedTokens, CancellationToken ct = default)
    {
        if (!await _concurrencySemaphore.WaitAsync(TimeSpan.FromSeconds(30), ct))
            throw new InvalidOperationException("AI rate limit exceeded: too many concurrent requests.");

        lock (_tokenLock)
        {
            if (DateTime.UtcNow - _windowStart > TimeSpan.FromMinutes(1))
            {
                _tokensUsedThisWindow = 0;
                _windowStart = DateTime.UtcNow;
            }

            if (_tokensUsedThisWindow + estimatedTokens > _maxTokensPerMinute)
            {
                _concurrencySemaphore.Release();
                throw new InvalidOperationException("AI rate limit exceeded: token budget exhausted for this minute.");
            }

            _tokensUsedThisWindow += estimatedTokens;
        }
    }

    public void Release(int actualTokens)
    {
        lock (_tokenLock)
        {
            var diff = actualTokens - 0;
            if (diff > 0)
                _tokensUsedThisWindow = Math.Max(0, _tokensUsedThisWindow + diff);
        }
        _concurrencySemaphore.Release();
    }
}
