using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Samples.Grpc.Services;
using IApplicationLifetime = Microsoft.AspNetCore.Hosting.IApplicationLifetime;

#nullable enable

namespace Samples.Grpc;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IApplicationLifetime _lifetime;
    private static readonly ErrorType[] ErrorTypes = (ErrorType[])Enum.GetValues(typeof(ErrorType));

    public Worker(ILogger<Worker> logger, IApplicationLifetime lifetime)
    {
        _logger = logger;
        _lifetime = lifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!Program.AppListening && !stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Waiting for app started handling requests");
            await Task.Delay(100, stoppingToken);
        }

        if (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Cancellation requested.");
            return;
        }

        var serverAddress = Program.ServerAddress!;
        _logger.LogInformation("App started. Sending requests to {ServerAddress}", serverAddress);

        try
        {
            using var channel = GrpcChannel.ForAddress(serverAddress, GetChannelOptions(serverAddress));
            _logger.LogInformation("Creating GRPC client");
            var client = new Greeter.GreeterClient(channel);

            await SendUnaryRequestAsync(client);
            await SendServerStreamingRequest(client, stoppingToken);
            await SendClientStreamingRequest(client, stoppingToken);
            await SendBothStreamingRequest(client, stoppingToken);
            await SendErrorsAsync(client);

            SendUnaryRequest(client);
            SendErrors(client);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending request");
            Program.ExitCode = 1;
        }

        _logger.LogInformation("Stopping application");

        // trigger app shutdown
        _lifetime.StopApplication();
    }

    private async Task SendUnaryRequestAsync(Greeter.GreeterClient client)
    {
        _logger.LogInformation("Sending unary async request to self");
        var reply = await client.UnaryAsync(
                        new HelloRequest { Name = "GreeterClient" });

        _logger.LogInformation("Received async reply message: {Reply}", reply.Message);
    }

    private void SendUnaryRequest(Greeter.GreeterClient client)
    {
        _logger.LogInformation("Sending unary request to self");
        var reply = client.Unary(new HelloRequest { Name = "GreeterClient" });

        _logger.LogInformation("Received reply message: {Reply}", reply.Message);
    }

    private async Task SendServerStreamingRequest(Greeter.GreeterClient client, CancellationToken ct)
    {
        _logger.LogInformation("Sending streaming server request to self");

        using var messages = client.StreamingFromServer(
            new HelloRequest { Name = "GreeterClient" });

        while (await messages.ResponseStream.MoveNext(ct))
        {
            _logger.LogInformation("Received streaming server message: {Reply}", messages.ResponseStream.Current.Message);
        }

        _logger.LogInformation("Received all streaming server responses");
    }

    private async Task SendClientStreamingRequest(Greeter.GreeterClient client, CancellationToken ct)
    {
        _logger.LogInformation("Sending streaming client requests to self");

        using var call = client.StreamingFromClient();

        for (int i = 0; i < 5; i++)
        {
            var helloRequest = new HelloRequest { Name = "GreeterClient" + i };
            _logger.LogInformation("Sending streaming client message: {Name}", helloRequest.Name);
            await call.RequestStream.WriteAsync(helloRequest);
        }

        await call.RequestStream.CompleteAsync();
        var response = await call;

        _logger.LogInformation("Received final streaming client response {Message}", response.Message);
    }

    private async Task SendBothStreamingRequest(Greeter.GreeterClient client, CancellationToken ct)
    {
        _logger.LogInformation("Sending streaming server request to self");

        using var call = client.StreamingBothWays();

        var readTask = Task.Run(async () =>
        {
            await foreach (var response in call.ResponseStream.ReadAllAsync(ct))
            {
                _logger.LogInformation("Received both streaming message: {Message}", response.Message);
            }
        });

        for (int i = 0; i < 5; i++)
        {
            var helloRequest = new HelloRequest { Name = "GreeterClient" + i };
            _logger.LogInformation("Sending both streaming message: {Name}", helloRequest.Name);
            await call.RequestStream.WriteAsync(helloRequest);
        }

        await call.RequestStream.CompleteAsync();
        await readTask;
        _logger.LogInformation("Both streaming server responses done");
    }

    private void SendErrors(Greeter.GreeterClient client)
    {
        foreach (var errorType in ErrorTypes)
        {
            try
            {
                _logger.LogInformation("Sending erroring request to self with {ErrorType}", errorType);
                var reply = client.ErroringMethod(new CreateErrorRequest { ErrorType = (int)errorType });

                _logger.LogError("Received reply message: {Reply}, but expected exception", reply.Message);
                throw new InvalidOperationException("Expected an exception");
            }
            catch (RpcException ex)
            {
                _logger.LogInformation("Received RPC exception with StatusCode {Status}", ex.Status);
                // expected
            }
        }
    }

    private async Task SendErrorsAsync(Greeter.GreeterClient client)
    {
        foreach (var errorType in ErrorTypes)
        {
            try
            {
                _logger.LogInformation("Sending erroring request to self with {ErrorType}", errorType);
                var reply = await client.ErroringMethodAsync(new CreateErrorRequest { ErrorType = (int)errorType });

                _logger.LogError("Received reply message: {Reply}, but expected exception", reply.Message);
                throw new InvalidOperationException("Expected an exception");
            }
            catch (RpcException ex)
            {
                _logger.LogInformation("Received RPC exception with StatusCode {Status}", ex.Status);
                // expected
            }
        }
    }

    private static GrpcChannelOptions GetChannelOptions(string serverAddress)
    {
#if  !NET5_0_OR_GREATER
        if (serverAddress.StartsWith("http"))
        {
            AppContext.SetSwitch(
                "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
        }
#endif

        return new GrpcChannelOptions()
        {
            HttpHandler = new HttpClientHandler
            {
                // We have to do this to accept self-signed certs
                ServerCertificateCustomValidationCallback
                    = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            }
        };
    }
}
