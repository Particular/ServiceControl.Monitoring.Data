namespace ServiceControl.Monitoring.Data
{
    using System;

    // an interface for items that can be consumed with a writer
    public interface IEntryProvider
    {
        long RoughlyEstimateItemsToConsume();
        int Consume(Action<ArraySegment<RingBuffer.Entry>> onChunk);
    }
}