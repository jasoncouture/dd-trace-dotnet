﻿// <copyright file="IDatadogSink.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>
#nullable enable

using System;

namespace Datadog.Trace.Logging.DirectSubmission.Sink
{
    internal interface IDatadogSink : IDisposable
    {
        /// <summary>
        /// Emit the provided log event to the sink. If the sink is being disposed or
        /// the app domain unloaded, then the event is ignored.
        /// </summary>
        /// <param name="logEvent">Log event to emit.</param>
        /// <exception cref="ArgumentNullException">The event is null.</exception>
        void EnqueueLog(DatadogLogEvent logEvent);
    }
}
