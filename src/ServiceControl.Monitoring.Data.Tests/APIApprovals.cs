﻿namespace ServiceControl.Monitoring.Data.Tests
{
    using System.IO;
    using System.Runtime.CompilerServices;
    using ApiApprover;
    using NUnit.Framework;

    [TestFixture]
    public class APIApprovals
    {
        [Test]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Approve()
        {
            Directory.SetCurrentDirectory(TestContext.CurrentContext.TestDirectory);
            PublicApiApprover.ApprovePublicApi(typeof(ISender).Assembly);
        }
    }
}