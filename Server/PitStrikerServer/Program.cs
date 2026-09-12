using System;
using System.Threading;
using System.Threading.Tasks;

namespace PitStrikerServer
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            var config = ServerConfiguration.LoadFromEnvironmentAndArgs(args);
            var roomManager = new RoomManager();
            var server = new WebSocketServer(config, roomManager);

            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            await server.StartAsync();

            try
            {
                await Task.Delay(Timeout.Infinite, cts.Token);
            }
            catch (TaskCanceledException)
            {
                // Normal cancellation on Ctrl+C / SIGTERM
            }

            server.Stop();
        }
    }
}
