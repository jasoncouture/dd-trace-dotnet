// <copyright file="CommonTracer.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using Datadog.Trace.Sampling;

namespace Datadog.Trace.ClrProfiler
{
    /// <summary>
    /// This class contains methods implemented by both the automatic and manual tracer.
    /// It is used for duplex communication.
    /// </summary>
    internal abstract class CommonTracer : ICommonTracer2
    {
        public int? GetSamplingPriority()
        {
            return (int?)Tracer.Instance.InternalActiveScope?.Span.Context?.TraceContext?.SamplingDecision?.Priority;
        }

        public void SetSamplingPriority(int? samplingPriority)
        {
            if (samplingPriority is null)
            {
                ClearSamplingDecision();
            }
            else
            {
                SetSamplingDecision((int)samplingPriority, (int)SamplingMechanism.Unknown, rate: null);
            }
        }

        public void ClearSamplingDecision()
        {
            Tracer.Instance.InternalActiveScope?.Span.Context?.TraceContext
                 ?.SetSamplingDecision(samplingDecision: null, notifyDistributedTracer: false);
        }

        public void SetSamplingDecision(int priority, int mechanism, float? rate)
        {
            var samplingDecision = new SamplingDecision((SamplingPriority)priority, (SamplingMechanism)mechanism, rate);

            Tracer.Instance.InternalActiveScope?.Span.Context?.TraceContext
                 ?.SetSamplingDecision(samplingDecision, notifyDistributedTracer: false);
        }
    }
}
