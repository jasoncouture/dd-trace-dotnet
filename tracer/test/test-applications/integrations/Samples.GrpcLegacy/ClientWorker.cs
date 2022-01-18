using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Samples.Grpc.Services;
#nullable enable

namespace Samples.Grpc;

public class ClientWorker
{
    private readonly Logger<ClientWorker> _logger;
    private static readonly ErrorType[] ErrorTypes = (ErrorType[])Enum.GetValues(typeof(ErrorType));

    public ClientWorker(Logger<ClientWorker> logger)
    {
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken stoppingToken)
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

        var serverAddress = $"127.0.0.1:{Program.ServerPort}";
        _logger.LogInformation("App started. Sending requests to " + serverAddress);

        try
        {
            var channel = new Channel(serverAddress, ChannelCredentials.Insecure);
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
    }

    private async Task SendUnaryRequestAsync(Greeter.GreeterClient client)
    {
        _logger.LogInformation("Sending unary async request to self");
        var reply = await client.UnaryAsync(
                        new HelloRequest { Name = "GreeterClient" });

        _logger.LogInformation("Received async reply message: "+ reply.Message);
    }

    private void SendUnaryRequest(Greeter.GreeterClient client)
    {
        _logger.LogInformation("Sending unary request to self");
        var reply = client.Unary(new HelloRequest { Name = "GreeterClient" });

        _logger.LogInformation("Received reply message: " + reply.Message);
    }

    private async Task SendServerStreamingRequest(Greeter.GreeterClient client, CancellationToken ct)
    {
        _logger.LogInformation("Sending streaming server request to self");

        using var messages = client.StreamingFromServer(
            new HelloRequest { Name = "GreeterClient" });

        while (await messages.ResponseStream.MoveNext(ct))
        {
            _logger.LogInformation("Received streaming server message: " + messages.ResponseStream.Current.Message);
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
            _logger.LogInformation("Sending streaming client message: " + helloRequest.Name);
            await call.RequestStream.WriteAsync(helloRequest);
        }

        await call.RequestStream.CompleteAsync();
        var response = await call;

        _logger.LogInformation("Received final streaming client response " + response.Message);
    }

    private async Task SendBothStreamingRequest(Greeter.GreeterClient client, CancellationToken ct)
    {
        _logger.LogInformation("Sending streaming server request to self");

        using var call = client.StreamingBothWays();

        var readTask = Task.Run(async () =>
        {
            while (await call.ResponseStream.MoveNext())
            {
                var response = call.ResponseStream.Current;
                _logger.LogInformation("Received both streaming message: " + response.Message);
            }
        });

        for (int i = 0; i < 5; i++)
        {
            var helloRequest = new HelloRequest { Name = "GreeterClient" + i };
            _logger.LogInformation("Sending both streaming message: " + helloRequest.Name);
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
                _logger.LogInformation("Sending erroring request to self with " + errorType);
                var reply = client.ErroringMethod(new CreateErrorRequest { ErrorType = (int)errorType });

                _logger.LogError("Received reply message: " +  reply.Message + "but expected exception");
                throw new InvalidOperationException("Expected an exception");
            }
            catch (RpcException ex)
            {
                _logger.LogInformation("Received RPC exception with StatusCode " + ex.Status);
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
                _logger.LogInformation("Sending erroring request to self with " + errorType);
                var reply = await client.ErroringMethodAsync(new CreateErrorRequest { ErrorType = (int)errorType });

                _logger.LogError("Received reply message: " +  reply.Message + "but expected exception");
                throw new InvalidOperationException("Expected an exception");
            }
            catch (RpcException ex)
            {
                _logger.LogInformation("Received RPC exception with StatusCode " + ex.Status);
                // expected
            }
        }
    }
}
