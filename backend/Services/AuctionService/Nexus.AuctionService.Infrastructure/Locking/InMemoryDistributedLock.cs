using Nexus.AuctionService.Application.UseCases.PlaceBid;
using System.Collections.Concurrent;

namespace Nexus.AuctionService.Infrastructure.Locking;

// Distributed lock implementation dùng SemaphoreSlim + ConcurrentDictionary
// Phù hợp cho single-instance deployment (dev/staging, một process duy nhất)
// PRODUCTION WARNING: phải thay bằng Redis-based lock khi scale horizontally
// vì SemaphoreSlim chỉ hoạt động trong cùng một process - nhiều instance sẽ không sync được
public class InMemoryDistributedLock : IDistributedLock
{
    // Static để share giữa các request trong cùng process (không phải per-request instance)
    // ConcurrentDictionary đảm bảo thread-safe khi nhiều thread cùng GetOrAdd cùng key
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphores = new();

    // Interface IDistributedLock không có CancellationToken parameter
    public async Task<IAsyncDisposable> AcquireAsync(string key, TimeSpan timeout)
    {
        // GetOrAdd atomic: tránh race condition khi nhiều thread cùng tạo semaphore cho cùng 1 key
        // SemaphoreSlim(1, 1) = mutex - chỉ 1 thread được vào critical section cùng lúc
        var semaphore = _semaphores.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

        bool acquired = await semaphore.WaitAsync(timeout);
        if (!acquired) throw new TimeoutException($"Could not acquire lock for key '{key}' within {timeout}");

        return new LockHandle(semaphore); // trả về handle để caller giải phóng lock qua `await using`
    }

    // Nested private class - chỉ InMemoryDistributedLock mới tạo được LockHandle
    // IAsyncDisposable để dùng với `await using` - tự động release lock khi ra khỏi scope
    private sealed class LockHandle : IAsyncDisposable
    {
        private readonly SemaphoreSlim _semaphore;
        public LockHandle(SemaphoreSlim semaphore) => _semaphore = semaphore;

        // Release semaphore khi dispose - SemaphoreSlim.Release() là synchronous nên wrap vào ValueTask
        public ValueTask DisposeAsync() { _semaphore.Release(); return ValueTask.CompletedTask; }
    }
}
