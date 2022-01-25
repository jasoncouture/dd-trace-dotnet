﻿// <copyright file="UpstreamServiceTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using Datadog.Trace.Tagging.PropagatedTags;
using FluentAssertions;
using Xunit;

namespace Datadog.Trace.Tests.Tagging.PropagatedTags;

public class UpstreamServiceTests
{
    [Theory]
    [InlineData("Service1", -1, 2, 0.98769, "U2VydmljZTE|-1|2|0.9877")]
    [InlineData("Service1", 0, 0, 0, "U2VydmljZTE|0|0|0")]
    [InlineData("Service1", 1, 1, 1, "U2VydmljZTE|1|1|1")]
    [InlineData("Service1", 1, 3, null, "U2VydmljZTE|1|3")]
    public void Serialize(string serviceName, int samplingPriority, int samplingMechanism, double? samplingRate, string expected)
    {
        var upstreamService = new UpstreamService(serviceName, samplingPriority, samplingMechanism, samplingRate);
        var actual = upstreamService.Serialize();

        actual.Should().Be(expected);
    }

    [Theory]
    [InlineData("Service1", "U2VydmljZTE")]
    [InlineData("Service12", "U2VydmljZTEy")]
    [InlineData("Service123", "U2VydmljZTEyMw")]
    public void ConvertToBase64String(string serviceName, string expected)
    {
        UpstreamService.ConvertToBase64String(serviceName).Should().Be(expected);
    }
}
