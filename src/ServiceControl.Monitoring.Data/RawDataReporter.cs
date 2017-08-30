namespace ServiceControl.Monitoring.Data
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    public delegate void WriteOutput(ArraySegment<RingBuffer.Entry> entries, BinaryWriter outputWriter);

    public class RawDataReporter : IDisposable
    {
        const int DefaultFlushSize = RingBuffer.Size / 2;
        readonly RingBuffer buffer;
        readonly int flushSize;
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
        {
            this.buffer = buffer;
            this.flushSize = flushSize;
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

                    await Consume().ConfigureAwait(false);
                }

                // flush data before ending
                await Consume().ConfigureAwait(false);
            });
        }

        Task Consume()
        {
            var consumed = buffer.Consume(outputWriter);

            if (consumed > 0)
            {
                writer.Flush();
                var body = memoryStream.ToArray(); // if only transport operation allowed ArraySegment<byte>...

                // clean stream
                memoryStream.SetLength(0);

                return sender(body);
            }

            return CompletedTask;
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