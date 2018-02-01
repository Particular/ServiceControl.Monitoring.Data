namespace ServiceControl.Monitoring.Data.Tests
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
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

            AssertValues(new long[]{1,2,3,4});

            await reporter.Stop();
        }

        [Test]
        public async Task When_flush_size_is_reached_only_maximum_number_is_flushed()
        {
            var reporter = new RawDataReporter(sender.ReportPayload, buffer, WriteEntriesValues, 4, TimeSpan.MaxValue);
            reporter.Start();
            buffer.TryWrite(1);
            buffer.TryWrite(2);
            buffer.TryWrite(3);
            buffer.TryWrite(4);

            AssertValues(new long[]{1,2}, new long[]{3,4});

            await reporter.Stop();
        }

        [Test]
        public async Task When_flushing_should_use_parallel_sends()
        {
            var payloads = new ConcurrentQueue<byte[]>();
            var tcs = new TaskCompletionSource<object>();
            var semaphore = new SemaphoreSlim(0, RawDataReporter.ParallelConsumers);

            Task Report(byte[] payload)
            {
                payloads.Enqueue(payload);
                semaphore.Release();
                return tcs.Task;
            }

            var reporter = new RawDataReporter(Report, buffer, WriteEntriesValues, 1, 1, TimeSpan.MaxValue);
            reporter.Start();
            buffer.TryWrite(1);
            buffer.TryWrite(2);
            buffer.TryWrite(3);
            buffer.TryWrite(4);

            await semaphore.WaitAsync();
            await semaphore.WaitAsync();
            await semaphore.WaitAsync();
            await semaphore.WaitAsync();

            tcs.SetResult(new object());

            await reporter.Stop();

            Assert.AreEqual(RawDataReporter.ParallelConsumers, payloads.Count);
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

            AssertValues(new long[] { 1, 2 });

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

            AssertValues(new long[] { 1, 2 });
        }

        void AssertValues(params long[][] values)
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
                lock (bodies)
                {
                    bodies.Add(body);
                }
                
                return Task.FromResult(0);
            }
        }
    }
}
