namespace ServiceControl.Monitoring.Data
{
    using System;
    using System.Collections.Concurrent;

    public class Snapshotter : IEntryProvider
    {
        readonly ConcurrentDictionary<int, long> values = new ConcurrentDictionary<int, long>();
        RingBuffer.Entry[] entries = new RingBuffer.Entry[32];

        long IEntryProvider.RoughlyEstimateItemsToConsume()
        {
            return 0;
        }

        int IEntryProvider.Consume(Action<ArraySegment<RingBuffer.Entry>> onChunk)
        {
            var ticks = DateTime.UtcNow.Ticks;

            // foreach is used as the iterator over concurrent dictionary is a nonlocking one, enabling reporting when needed.
            var i = 0;
            foreach (var kvp in values)
            {
                if (i == entries.Length)
                {
                    Array.Resize(ref entries, entries.Length * 2);
                }

                entries[i].Tag = kvp.Key;
                entries[i].Value = kvp.Value;
                entries[i].Ticks = ticks;

                i += 1;
            }

            onChunk(new ArraySegment<RingBuffer.Entry>(entries, 0, i));

            return i;
        }

        public void Write(long value, int tag)
        {
            values[tag] = value;
        }
    }
}