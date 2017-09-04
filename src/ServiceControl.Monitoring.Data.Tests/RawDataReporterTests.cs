namespace ServiceControl.Monitoring.Data.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using NUnit.Framework;

    public class RawDataReporterTests
    {
        RingBuffer buffer;
        MockSender sender;

        [SetUp]
        public void SetUp()
        {
            buffer = new RingBuffer();
            sender = new MockSender();
        }

        [Test]
        public async Task When_flush_size_is_reached()
        {
            var reporter = new RawDataReporter(sender.ReportPayload, buffer, WriteEntriesValues, 4, TimeSpan.MaxValue);
            reporter.Start();
            buffer.TryWrite(1);
            buffer.TryWrite(2);
            buffer.TryWrite(3);
            buffer.TryWrite(4);

            Assert(new long[]{1,2,3,4});

            await reporter.Stop();
        }

        [Test]
        public async Task When_max_spinning_time_is_reached()
        {
            var maxSpinningTime = TimeSpan.FromMilliseconds(100);

            var reporter = new RawDataReporter(sender.ReportPayload,  buffer, WriteEntriesValues, int.MaxValue, maxSpinningTime);
            reporter.Start();
            buffer.TryWrite(1);
            buffer.TryWrite(2);
            await Task.Delay(maxSpinningTime.Add(TimeSpan.FromMilliseconds(200)));

            Assert(new long[] { 1, 2 });

            await reporter.Stop();
        }

        [Test]
        public async Task When_stopped()
        {
            var reporter = new RawDataReporter(sender.ReportPayload, buffer, WriteEntriesValues, int.MaxValue, TimeSpan.MaxValue);
            reporter.Start();
            buffer.TryWrite(1);
            buffer.TryWrite(2);

            await reporter.Stop();

            Assert(new long[] { 1, 2 });
        }

        void Assert(params long[][] values)
        {
            var bodies = sender.bodies.ToArray();
            var i = 0;
            foreach (var body in bodies)
            {
                var encodedValues = new List<long>();
                var reader = new BinaryReader(new MemoryStream(body));
                while (reader.BaseStream.Position < reader.BaseStream.Length)
                {
                    encodedValues.Add(reader.ReadInt64());
                }

                CollectionAssert.AreEqual(values[i], encodedValues);
            }
        }

        static void WriteEntriesValues(ArraySegment<RingBuffer.Entry> entries, BinaryWriter writer)
        {
            foreach (var entry in entries)
            {
                writer.Write(entry.Value);
            }
        }

        class MockSender
        {
            public List<byte[]> bodies = new List<byte[]>();

            public Task ReportPayload(byte[] body)
            {
                bodies.Add(body);
                return Task.FromResult(0);
            }
        }
    }
}
