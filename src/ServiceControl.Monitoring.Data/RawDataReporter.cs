namespace ServiceControl.Monitoring.Data
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    delegate void WriteOutput(ArraySegment<RingBuffer.Entry> entries, BinaryWriter outputWriter);

    class RawDataReporter : IDisposable
    {
        const int DefaultFlushSize = 1028; // for entries written on 16bytes this will give 16kB
        const int MaxDefaultFlushSize = 2048; // for entries written on 16byte this will give 32kB
        internal const int MaxParallelConsumers = 4;
        readonly RingBuffer buffer;
        readonly int flushSize;
        readonly int maxFlushSize;
        readonly Action<ArraySegment<RingBuffer.Entry>> outputWriter;
        readonly BinaryWriter writer;
        readonly MemoryStream memoryStream;
        readonly CancellationTokenSource cancellationTokenSource;
        readonly TimeSpan maxSpinningTime;
        readonly Func<byte[], Task> sender;
        Task reporter;

        static readonly TimeSpan DefaultMaxSpinningTime = TimeSpan.FromSeconds(5);
        static readonly TimeSpan singleSpinningTime = TimeSpan.FromMilliseconds(50);
        static readonly Task CompletedTask = Task.FromResult(0);

        public RawDataReporter(Func<byte[], Task> sender, RingBuffer buffer, WriteOutput outputWriter)
            : this(sender, buffer, outputWriter, DefaultFlushSize, DefaultMaxSpinningTime)
        {
        }

        public RawDataReporter(Func<byte[], Task> sender, RingBuffer buffer, WriteOutput outputWriter, int flushSize, TimeSpan maxSpinningTime)
            : this(sender, buffer, outputWriter, flushSize, MaxDefaultFlushSize, maxSpinningTime)
        {

        }

        public RawDataReporter(Func<byte[], Task> sender, RingBuffer buffer, WriteOutput outputWriter, int flushSize, int maxFlushSize, TimeSpan maxSpinningTime)
        {
            this.buffer = buffer;
            this.flushSize = flushSize;
            this.maxFlushSize = maxFlushSize;
            this.maxSpinningTime = maxSpinningTime;
            this.outputWriter = entries => outputWriter(entries, writer);
            this.sender = sender;
            memoryStream = new MemoryStream();
            writer = new BinaryWriter(memoryStream);
            cancellationTokenSource = new CancellationTokenSource();
        }

        public void Start()
        {
            reporter = Task.Run(async () =>
            {
                var consumers = new List<Task>(MaxParallelConsumers + 1);

                while (cancellationTokenSource.IsCancellationRequested == false)
                {
                    var totalSpinningTime = TimeSpan.Zero;

                    // spin till either MaxSpinningTime is reached OR items to consume are more than FlushSize
                    while (totalSpinningTime < maxSpinningTime)
                    {
                        if (cancellationTokenSource.IsCancellationRequested)
                        {
                            break;
                        }

                        var itemsToConsume = buffer.RoughlyEstimateItemsToConsume();
                        if (itemsToConsume >= flushSize)
                        {
                            break;
                        }

                        totalSpinningTime += singleSpinningTime;
                        await Task.Delay(singleSpinningTime).ConfigureAwait(false);
                    }

                    if (consumers.Count >= MaxParallelConsumers)
                    {
                        var task = await Task.WhenAny(consumers).ConfigureAwait(false);
                        consumers.Remove(task);
                    }

                    consumers.Add(Consume());
                }

                // await all the flushes
                await Task.WhenAll(consumers).ConfigureAwait(false);

                // consume leftovers
                var consumed = buffer.Consume(maxFlushSize, outputWriter);
                while (consumed > 0)
                {
                    await Flush().ConfigureAwait(false);
                    consumed = buffer.Consume(maxFlushSize, outputWriter);
                }
            });
        }

        Task Consume()
        {
            var consumed = buffer.Consume(maxFlushSize, outputWriter);

            if (consumed > 0)
            {
                return Flush();
            }

            return CompletedTask;
        }

        Task Flush()
        {
            writer.Flush();
            // if only transport operation allowed ArraySegment<byte>...
            var body = memoryStream.ToArray();

            // clean stream
            memoryStream.SetLength(0);

            return sender(body);
        }

        public Task Stop()
        {
            cancellationTokenSource.Cancel();
            return reporter;
        }

        public void Dispose()
        {
            writer?.Dispose();
            memoryStream?.Dispose();
            cancellationTokenSource?.Dispose();
        }
    }
}