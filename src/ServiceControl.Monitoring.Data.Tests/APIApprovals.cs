namespace ServiceControl.Monitoring.Data.Tests
{
    using System.IO;
    using System.Runtime.CompilerServices;
    using ApprovalTests;
    using ApprovalTests.Core;
    using ApprovalTests.Writers;
    using NUnit.Framework;
    using PublicApiGenerator;

    [TestFixture]
    public class APIApprovals
    {
        [Test]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Approve()
        {
            var publicApi = ApiGenerator.GeneratePublicApi(typeof(RingBuffer).Assembly);
            Approvals.Verify(WriterFactory.CreateTextWriter(publicApi, "cs"), GetNamer(), Approvals.GetReporter());
        }

        IApprovalNamer GetNamer([CallerFilePath] string path = "")
        {
            var dir = Path.GetDirectoryName(path);
            var name = typeof(RingBuffer).Assembly.GetName().Name;

            return new Namer
            {
                Name = name,
                SourcePath = dir,
            };
        }

        class Namer : IApprovalNamer
        {
            public string SourcePath { get; set; }
            public string Name { get; set; } 
        }
    }
}