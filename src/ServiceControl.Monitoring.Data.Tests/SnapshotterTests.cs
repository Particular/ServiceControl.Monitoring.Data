namespace ServiceControl.Monitoring.Data.Tests
{
    using System;
    using System.Collections.Generic;
    using NUnit.Framework;

    public class SnapshotterTests
    {
        Snapshotter snapshotter;
        IEntryProvider provider;

        [SetUp]
        public void SetUp()
        {
            snapshotter = new Snapshotter();
            provider = snapshotter;
        }

        [Test]
        public void Empty_should_report_nothing()
        {
            AssertConsume();
        }

        [Test]
        public void Writing_same_tag_multiples_times_should_report_last_value_once()
        {
            const int tag = 1;

            snapshotter.Write(1, tag);
            snapshotter.Write(2, tag);
            snapshotter.Write(3, tag);

            AssertConsume(new RingBuffer.Entry
            {
                Tag = tag,
                Value = 3
            });
        }

        [Test]
        public void Writing_mutiple_values()
        {
            var entries = new List<RingBuffer.Entry>();
            for (var i = 1; i < 11; i++)
            {
                snapshotter.Write(i, i);
                entries.Add(new RingBuffer.Entry(0, i, i));
            }

            AssertConsume(entries);
        }

        void AssertConsume(params RingBuffer.Entry[] expected)
        {
            provider.Consume(chunk => Assert(expected, chunk));
        }

        void AssertConsume(IEnumerable<RingBuffer.Entry> expected)
        {
            provider.Consume(chunk => Assert(expected, chunk));
        }

        static void Assert(IEnumerable<RingBuffer.Entry> expected, ArraySegment<RingBuffer.Entry> chunk)
        {
            NUnit.Framework.Assert.That(chunk, Is.EquivalentTo(expected).Using(new TagValueEntryComparer()));
        }
    }
}