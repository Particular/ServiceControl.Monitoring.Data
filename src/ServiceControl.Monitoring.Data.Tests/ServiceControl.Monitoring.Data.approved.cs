[assembly: System.Runtime.CompilerServices.InternalsVisibleToAttribute("ServiceControl.Monitoring.Data.Tests")]
[assembly: System.Runtime.Versioning.TargetFrameworkAttribute(".NETFramework,Version=v4.5.2", FrameworkDisplayName=".NET Framework 4.5.2")]

namespace ServiceControl.Monitoring.Data
{
    
    public interface ISender
    {
        System.Threading.Tasks.Task ReportPayload(byte[] body);
    }
    public class static LongValueWriter
    {
        public static void Write(System.IO.BinaryWriter outputWriter, System.ArraySegment<ServiceControl.Monitoring.Data.RingBuffer.Entry> chunk) { }
    }
    public class static OccurrenceWriter
    {
        public static void Write(System.IO.BinaryWriter outputWriter, System.ArraySegment<ServiceControl.Monitoring.Data.RingBuffer.Entry> chunk) { }
    }
    public class RawDataReporter : System.IDisposable
    {
        public RawDataReporter(ServiceControl.Monitoring.Data.ISender sender, ServiceControl.Monitoring.Data.RingBuffer buffer, ServiceControl.Monitoring.Data.WriteOutput outputWriter) { }
        public RawDataReporter(ServiceControl.Monitoring.Data.ISender sender, ServiceControl.Monitoring.Data.RingBuffer buffer, ServiceControl.Monitoring.Data.WriteOutput outputWriter, int flushSize, System.TimeSpan maxSpinningTime) { }
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
    public class TaggedLongValueWriter
    {
        public TaggedLongValueWriter() { }
        public int GetTagId(string tagName) { }
        public void Write(System.IO.BinaryWriter outputWriter, System.ArraySegment<ServiceControl.Monitoring.Data.RingBuffer.Entry> chunk) { }
    }
    public delegate void WriteOutput(System.ArraySegment<ServiceControl.Monitoring.Data.RingBuffer.Entry> entries, System.IO.BinaryWriter outputWriter);
}