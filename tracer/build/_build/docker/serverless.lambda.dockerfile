FROM public.ecr.aws/lambda/dotnet:latest
ARG tracerhome

# Add Tracer
COPY . /opt/temp
RUN ls /opt/temp/tracer/src/bin/windows-tracer-home

COPY ./$tracerhome /opt/datadog

# Add Tests
COPY ./tracer/test/test-applications/integrations/Samples.AWS.Lambda/bin/Release/netcoreapp3.1/*.dll /var/task/
COPY ./tracer/test/test-applications/integrations/Samples.AWS.Lambda/bin/Release/netcoreapp3.1/*.deps.json /var/task/

RUN mkdir -p /var/log/datadog/dotnet && chmod a+rwx /var/log/datadog/dotnet

ENV DD_LOG_LEVEL="DEBUG"
ENV DD_TRACE_ENABLED=true
ENV DD_DOTNET_TRACER_HOME="/opt/datadog"
ENV DD_INTEGRATIONS="/opt/datadog/integrations.json"
ENV _DD_EXTENSION_PATH="/opt/datadog/Datadog.Trace.ClrProfiler.Native.so"

ENV CORECLR_ENABLE_PROFILING=1
ENV CORECLR_PROFILER="{846F5F1C-F9AE-4B07-969E-05C26BC060D8}"
ENV CORECLR_PROFILER_PATH="/opt/datadog/Datadog.Trace.ClrProfiler.Native.so"

ENV AWS_LAMBDA_FUNCTION_NAME="my-test-function"
