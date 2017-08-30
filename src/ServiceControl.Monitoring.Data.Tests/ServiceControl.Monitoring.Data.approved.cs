[assembly: System.Runtime.CompilerServices.InternalsVisibleToAttribute("ServiceControl.Monitoring.Data.Tests")]
[assembly: System.Runtime.Versioning.TargetFrameworkAttribute(".NETFramework,Version=v4.5.2", FrameworkDisplayName=".NET Framework 4.5.2")]

namespace ServiceControl.Monitoring.Data
{
    
    public class static LongValueWriterV1
    {
        public static void Write(System.IO.BinaryWriter outputWriter, System.ArraySegment<ServiceControl.Monitoring.Data.RingBuffer.Entry> chunk) { }
    }
    public class static OccurrenceWriterV1
    {
        public static void Write(System.IO.BinaryWriter outputWriter, System.ArraySegment<ServiceControl.Monitoring.Data.RingBuffer.Entry> chunk) { }
    }
    public class RawDataReporter : System.IDisposable
    {
        public RawDataReporter(System.Func<byte[], System.Threading.Tasks.Task> sender, ServiceControl.Monitoring.Data.RingBuffer buffer, ServiceControl.Monitoring.Data.WriteOutput outputWriter) { }
        public RawDataReporter(System.Func<byte[], System.Threading.Tasks.Task> sender, ServiceControl.Monitoring.Data.RingBuffer buffer, ServiceControl.Monitoring.Data.WriteOutput outputWriter, int flushSize, System.TimeSpan maxSpinningTime) { }
        public void Dispose() { }
        public void Start() { }
        public System.Threading.Tasks.Task Stop() { }
    }
    public class RingBuffer
    {
        public RingBuffer() { }
        public bool TryWrite(long value, int tag = 0) { }
        public struct Entry
        {
            public int Tag;
            public long Ticks;
            public long Value;
            public Entry(long ticks, long value, int tag = 0) { }
        }
    }
    public class TaggedLongValueWriterV1
    {
        public TaggedLongValueWriterV1() { }
        public int GetTagId(string tagName) { }
        public void Write(System.IO.BinaryWriter outputWriter, System.ArraySegment<ServiceControl.Monitoring.Data.RingBuffer.Entry> chunk) { }
    }
    public delegate void WriteOutput(System.ArraySegment<ServiceControl.Monitoring.Data.RingBuffer.Entry> entries, System.IO.BinaryWriter outputWriter);
}