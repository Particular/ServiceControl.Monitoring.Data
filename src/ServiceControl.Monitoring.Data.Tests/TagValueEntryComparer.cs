namespace ServiceControl.Monitoring.Data.Tests
{
    using System.Collections.Generic;

    class TagValueEntryComparer : IComparer<RingBuffer.Entry>
    {
        public int Compare(RingBuffer.Entry x, RingBuffer.Entry y)
        {
            var result = x.Tag.CompareTo(y.Tag);
            if (result != 0)
            {
                return result;
            }

            return x.Value.CompareTo(y.Value);
        }
    }
}