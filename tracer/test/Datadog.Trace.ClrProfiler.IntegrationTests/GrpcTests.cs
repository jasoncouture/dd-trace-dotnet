// <copyright file="GrpcTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Linq;
using Datadog.Trace.TestHelpers;
using FluentAssertions;
using FluentAssertions.Execution;
using Xunit;
using Xunit.Abstractions;

#pragma warning disable SA1402 // File may only contain a single class
#pragma warning disable SA1649 // File name must match first type name

namespace Datadog.Trace.ClrProfiler.IntegrationTests
{
    public class GrpcLegacyTests : GrpcTestsBase
    {
        public GrpcLegacyTests(ITestOutputHelper output)
            : base("GrpcLegacy", output)
        {
        }

        [SkippableTheory]
        [InlineData("")]
        [Trait("Category", "EndToEnd")]
        [Trait("RunOnWindows", "True")]
        public void SubmitTraces(string packageVersion)
            => RunSubmitTraces(packageVersion);
    }

#if NETCOREAPP3_0_OR_GREATER
    public class GrpcHttpTests : GrpcTestsBase
    {
        public GrpcHttpTests(ITestOutputHelper output)
            : base("Grpc", output)
        {
            SetEnvironmentVariable("ASPNETCORE_URLS", "http://127.0.0.1:0"); // don't use SSL
        }

        [SkippableTheory]
        [InlineData("")]
        [Trait("Category", "EndToEnd")]
        [Trait("RunOnWindows", "True")]
        public void SubmitTraces(string packageVersion)
            => RunSubmitTraces(packageVersion);
    }

    public class GrpcHttpsTests : GrpcTestsBase
    {
        public GrpcHttpsTests(ITestOutputHelper output)
            : base("Grpc", output)
        {
            SetEnvironmentVariable("ASPNETCORE_URLS", "https://127.0.0.1:0"); // use SSL
        }

        [SkippableTheory]
        [InlineData("")]
        [Trait("Category", "EndToEnd")]
        [Trait("RunOnWindows", "True")]
        public void SubmitTraces(string packageVersion)
            => RunSubmitTraces(packageVersion);
    }
#endif

    public abstract class GrpcTestsBase : TestHelper
    {
        protected GrpcTestsBase(string sampleName, ITestOutputHelper output)
            : base(sampleName, output)
        {
        }

        protected void RunSubmitTraces(string packageVersion)
        {
            // set in dockerfile
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("IsAlpine")))
            {
                Output.WriteLine("Skipping as Grpc.Tools does not support Alpine");
                return;
            }

            const int expectedClientOrServerSpans = 2 // Unary  (sync + async)
                                    + 1 // 1 server streaming
                                    + 1 // 1 client streaming
                                    + 1 // 1 both streaming
                                    + (4 * 2); // 4 Error types (sync + async)
            const int totalExpectedSpans = expectedClientOrServerSpans * 2;

            using var agent = EnvironmentHelper.GetMockAgent();
            using (RunSampleAndWaitForExit(agent, packageVersion: packageVersion, aspNetCorePort: 0))
            {
                var spans = agent.WaitForSpans(totalExpectedSpans, 500);

                using var s = new AssertionScope();

                // basic tests for now, will add verify to this
                spans.Should()
                     .HaveCount(totalExpectedSpans)
                     .And.OnlyContain(x => x.Type == "grpc")
                     .And.OnlyContain(x => x.Name == "grpc.request");

                var clientSpans = spans
                                 .Where(x => x.Tags.ContainsKey(SpanKinds.Client))
                                 .Should()
                                 .HaveCount(expectedClientOrServerSpans);

                var serverSpans = spans
                                 .Where(x => x.Tags.ContainsKey(SpanKinds.Server))
                                 .Should()
                                 .HaveCount(expectedClientOrServerSpans);
            }
        }
    }
}
