namespace ServiceControl.Monitoring.Data
{
    using System.Threading.Tasks;

    public interface ISender
    {
        Task ReportPayload(byte[] body);
    }
}