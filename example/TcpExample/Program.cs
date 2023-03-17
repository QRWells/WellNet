using QRWells.WellNet.Core.Tcp;
using Serilog;

internal class Program
{
    public static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .CreateLogger();
        if (args.Length == 0)
        {
            Log.Error("Please specify either 'server' or 'client' as the first argument");
            return;
        }

        switch (args[0])
        {
            case "server":
                Server(args);
                break;
            case "client":
                Client(args);
                break;
            default:
                Log.Error("Please specify either 'server' or 'client' as the first argument");
                break;
        }
    }

    public static void Server(string[] args)
    {
        using var server = new TcpServer(5050);
        server.OnDataReceived += async (connection, data) =>
        {
            Log.Debug("Received {Data} from {Connection}", data, connection.Id);
            await connection.SendAsync(data);
        };
        server.Start();
        Console.ReadLine();
        Log.Information("Shutting down");
        Log.CloseAndFlush();
    }

    public static async void Client(string[] args)
    {
        using var client = new TcpClient();
        await client.ConnectAsync("localhost", 5050);
        await client.SendAsync("Hello World"u8.ToArray());
        client.OnDataReceived += (connection, data) => { Console.WriteLine($"Received {data} from {connection.Id}"); };
        Console.ReadLine();
    }
}