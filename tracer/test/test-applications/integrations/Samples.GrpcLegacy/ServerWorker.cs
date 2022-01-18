using System;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Samples.Grpc.Services;

namespace Samples.Grpc;

public class ServerWorker
{
    private readonly Logger<ServerWorker> _logger;
    private readonly GreeterService _greeter;

    public ServerWorker(Logger<ServerWorker> logger, GreeterService greeter)
    {
        _logger = logger;
        _greeter = greeter;
    }

    public async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var port = WebServer.GetOpenPort();

        _logger.LogInformation("Starting GRPC server");
        var server = new Server
        {
            Services = { Greeter.BindService(_greeter) },
            Ports = { new ServerPort("localhost", port, ServerCredentials.Insecure) }
        };
        server.Start();

        _logger.LogInformation("Listening on port" + port);

        Program.ServerPort = port;
        Program.AppListening = true;

        try
        {
            await Task.Delay(-1, stoppingToken);
        }
        catch (TaskCanceledException)
        {
            // swallow
        }

        _logger.LogInformation("Cancellation requested, closing server");
        await server.ShutdownAsync();
        _logger.LogInformation("Server shutdown complete");
    }
}
