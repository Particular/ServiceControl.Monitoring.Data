namespace ServiceControl.Monitoring.Data
{
    using System.Threading.Tasks;

    interface ISender
    {
        Task ReportPayload(byte[] body);
    }
}