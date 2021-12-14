namespace Datadog.Trace
{
    internal interface ISpanParent
    {
        ulong TraceId { get; }

        ulong SpanId { get; }
    }
}
