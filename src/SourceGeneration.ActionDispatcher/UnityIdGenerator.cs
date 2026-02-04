using System.Runtime.CompilerServices;

namespace SourceGeneration.ActionDispatcher;

public class UnityIdGenerator
{
    private const int BusinessIdBits = 4; // 4位，支持16种业务
    private const int MachineIdBits = 3;  // 3位，支持8台机器
    private const int SequenceBits = 14;  // 14位，每个时间片支持16384个ID
    private const long MaxBusinessId = (1L << BusinessIdBits) - 1;
    private const long MaxMachineId = (1L << MachineIdBits) - 1;
    private const long MaxSequence = (1L << SequenceBits) - 1;

    // 预先计算位移量
    private static readonly int TimestampShift = BusinessIdBits + MachineIdBits + SequenceBits;
    private static readonly int BusinessIdShift = MachineIdBits + SequenceBits;
    private static readonly int MachineIdShift = SequenceBits;

    private readonly long _timeUnitMs;
    private readonly long _twepoch;
#if NET9_0_OR_GREATER
    private readonly Lock _lock = new();
#else
    private readonly object _lock = new();
#endif
    private readonly long _prefix; // 业务ID和机器ID合并后的高位
    private readonly int _asyncWaitMs;
    private long _lastTimestamp = -1L;
    private long _sequence = 0L;

    public UnityIdGenerator(byte businessId, byte machineId, int timeUnitMs = 100)
    {
        if (businessId > MaxBusinessId)
            throw new ArgumentException($"businessId must be between 0 and {MaxBusinessId}");
        if (machineId > MaxMachineId)
            throw new ArgumentException($"machineId must be between 0 and {MaxMachineId}");

        _timeUnitMs = timeUnitMs;
        _twepoch = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks / TimeSpan.TicksPerMillisecond / _timeUnitMs;
        _prefix = ((long)businessId << BusinessIdShift) | ((long)machineId << MachineIdShift);
        _asyncWaitMs = (int)Math.Max(10, _timeUnitMs / 10); // 预先计算
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private long GetCurrentTimestamp() => DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond / _timeUnitMs;

    public long NextId()
    {
        lock (_lock)
        {
            var timestamp = GetCurrentTimestamp();
            if (timestamp < _lastTimestamp)
                throw new InvalidOperationException("Clock moved backwards. Refusing to generate id.");

            if (timestamp == _lastTimestamp)
            {
                _sequence = (_sequence + 1) & MaxSequence;
                if (_sequence == 0)
                {
                    timestamp = WaitNextTimestamp(_lastTimestamp);
                    _sequence = 0;
                    _lastTimestamp = timestamp;
                }
            }
            else
            {
                _sequence = 0;
                _lastTimestamp = timestamp;
            }

            return ((timestamp - _twepoch) << TimestampShift)
                 | _prefix
                 | _sequence;
        }
    }

    public async ValueTask<long> NextIdAsync()
    {
        while (true)
        {
            lock (_lock)
            {
                var timestamp = GetCurrentTimestamp();
                if (timestamp < _lastTimestamp)
                    throw new InvalidOperationException("Clock moved backwards. Refusing to generate id.");

                if (timestamp == _lastTimestamp)
                {
                    _sequence = (_sequence + 1) & MaxSequence;
                    if (_sequence == 0)
                    {
                        // 需要异步等待
                    }
                    else
                    {
                        return ((timestamp - _twepoch) << TimestampShift)
                            | _prefix
                            | _sequence;
                    }
                }
                else
                {
                    _sequence = 0;
                    _lastTimestamp = timestamp;
                    return ((timestamp - _twepoch) << TimestampShift)
                        | _prefix
                        | _sequence;
                }
            }
            // 锁外异步等待
            _lastTimestamp = await WaitNextTimestampAsync(_lastTimestamp).ConfigureAwait(false);
        }
    }

    public long[] NextIds(int count)
    {
        if (count <= 0) return [];
        var ids = new long[count];
        lock (_lock)
        {
            int generated = 0;
            while (generated < count)
            {
                var timestamp = GetCurrentTimestamp();
                if (timestamp < _lastTimestamp)
                    throw new InvalidOperationException("Clock moved backwards. Refusing to generate id.");

                if (timestamp != _lastTimestamp)
                {
                    _sequence = 0;
                    _lastTimestamp = timestamp;
                }

                long remain = MaxSequence - _sequence + 1;
                int batch = (int)Math.Min(remain, count - generated);

                long baseId = ((timestamp - _twepoch) << TimestampShift) | _prefix;
                for (int i = 0; i < batch; i++)
                {
                    ids[generated++] = baseId | (_sequence++);
                }

                if (_sequence > MaxSequence)
                {
                    timestamp = WaitNextTimestamp(_lastTimestamp);
                    _sequence = 0;
                    _lastTimestamp = timestamp;
                }
            }
        }
        return ids;
    }

    public ValueTask<long[]> NextIdsAsync(int count)
    {
        if (count <= 0) return new ValueTask<long[]>([]);
        var ids = new long[count];
        int generated = 0;
        return NextIdsAsyncCore(ids, generated, count);

        async ValueTask<long[]> NextIdsAsyncCore(long[] ids, int generated, int total)
        {
            while (generated < total)
            {
                int batch;
                long baseId;
                lock (_lock)
                {
                    var timestamp = GetCurrentTimestamp();
                    if (timestamp < _lastTimestamp)
                        throw new InvalidOperationException("Clock moved backwards. Refusing to generate id.");

                    if (timestamp != _lastTimestamp)
                    {
                        _sequence = 0;
                        _lastTimestamp = timestamp;
                    }

                    long remain = MaxSequence - _sequence + 1;
                    batch = (int)Math.Min(remain, total - generated);
                    baseId = ((timestamp - _twepoch) << TimestampShift) | _prefix;
                    for (int i = 0; i < batch; i++)
                    {
                        ids[generated++] = baseId | (_sequence++);
                    }

                    if (_sequence <= MaxSequence)
                        continue;
                }
                // 锁外异步等待
                await WaitNextTimestampAsync(_lastTimestamp).ConfigureAwait(false);
            }
            return ids;
        }
    }

    private long WaitNextTimestamp(long lastTimestamp)
    {
        // 先短自旋，后让步，降低CPU消耗
        long ts;
        int spin = 0;
        while (true)
        {
            ts = GetCurrentTimestamp();
            if (ts > lastTimestamp)
                return ts;
            if (spin < 100)
            {
                Thread.SpinWait(20);
                spin++;
            }
            else
            {
                Thread.Yield(); // 让出CPU，避免高消耗
            }
        }
    }

    private ValueTask<long> WaitNextTimestampAsync(long lastTimestamp)
    {
        long ts = GetCurrentTimestamp();
        if (ts > lastTimestamp)
            return new ValueTask<long>(ts);

        return new ValueTask<long>(WaitCore(lastTimestamp));

        async Task<long> WaitCore(long lastTs)
        {
            long t = GetCurrentTimestamp();
            while (t <= lastTs)
            {
                await Task.Delay(_asyncWaitMs).ConfigureAwait(false);
                t = GetCurrentTimestamp();
            }
            return t;
        }
    }
}
