namespace ServiceControl.Monitoring.Data.Tests
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
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

            AssertValues(new long[] { 1, 2, 3, 4 });

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

            AssertValues(new long[] { 1, 2 }, new long[] { 3, 4 });

            await reporter.Stop();
        }

        [Test]
        public async Task When_flushing_should_use_parallel_sends()
        {
            // this test aims at simulating the consumers that are slow enough to fill parallelism of reporter
            // it asserts that no progress is done till one of them finishes

            const int max = RawDataReporter.MaxParallelConsumers;

            var payloads = new ConcurrentQueue<byte[]>();
            var tcs = new TaskCompletionSource<object>();
            var semaphore = new SemaphoreSlim(0, max);

            var counter = 0;

            Task Report(byte[] payload)
            {
                var value = ReadValues(payload)[0];
                if (value < max)
                {
                    payloads.Enqueue(payload);
                    semaphore.Release();
                    return tcs.Task;
                }

                payloads.Enqueue(payload);
                Interlocked.Increment(ref counter);
                return Task.FromResult(0);
            }

            var reporter = new RawDataReporter(Report, buffer, WriteEntriesValues, flushSize: 1, maxFlushSize: 1, maxSpinningTime: TimeSpan.MaxValue);
            reporter.Start();

            for (var i = 0; i < max; i++)
            {
                buffer.TryWrite(i);
            }

            // additional write
            buffer.TryWrite(max);

            for (var i = 0; i < max; i++)
            {
                await semaphore.WaitAsync();
            }

            // no new reports scheduled before previous complete
            Assert.AreEqual(0, counter);

            // complete all the reports
            tcs.SetResult("");

            await reporter.Stop();

            // should have run the additional one
            Assert.AreEqual(1, counter);

            Assert.AreEqual(max + 1, payloads.Count);
        }

        [Test]
        public async Task When_max_spinning_time_is_reached()
        {
            var maxSpinningTime = TimeSpan.FromMilliseconds(100);

            var reporter = new RawDataReporter(sender.ReportPayload, buffer, WriteEntriesValues, int.MaxValue, maxSpinningTime);
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

        [Test]
        public async Task When_highthrouput_endpoint_shuts_down()
        {
            const int testSize = 10000;
            var queue = new ConcurrentQueue<byte[]>();

            Task Send(byte[] data)
            {
                queue.Enqueue(data);
                return Task.Delay(TimeSpan.FromMilliseconds(100)); // delay to make it realistically long
            }

            var reporter = new RawDataReporter(Send, buffer, WriteEntriesValues);
            reporter.Start();

            var writer = Task.Run(() =>
            {
                for (var i = 0; i < testSize; i++)
                {
                    while (buffer.TryWrite(i) == false)
                    {
                        // spin till it's written
                    }
                }
            });

            var stop = reporter.Stop();
            // await Task.WhenAll(TaskOrDelay(writer,"Writer"), TaskOrDelay(stop, "stop"));

            await Task.WhenAll(writer, stop);

            var values = new HashSet<long>();
            foreach (var current in queue.ToArray().Select(ReadValues))
            {
                foreach (var v in current)
                {
                    values.Add(v);
                }
            }

            Assert.AreEqual(testSize, values.Count);
        }

        async Task TaskOrDelay(Task task, string name)
        {
            var delay = Task.Delay(TimeSpan.FromSeconds(10));
            var returned = await Task.WhenAny(task, delay);
            if (returned == delay)
            {
                throw new Exception($"Task '{name}' was not finished on time");
            }
        }

        void AssertValues(params long[][] values)
        {
            var bodies = sender.bodies.ToArray();
            var i = 0;
            foreach (var body in bodies)
            {
                var encodedValues = ReadValues(body);

                CollectionAssert.AreEqual(values[i], encodedValues);
            }
        }

        static List<long> ReadValues(byte[] body)
        {
            var encodedValues = new List<long>();
            var reader = new BinaryReader(new MemoryStream(body));
            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                encodedValues.Add(reader.ReadInt64());
            }
            return encodedValues;
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
