// Compilation:
// csc /optimize+ /debug- /platform:x64 /out:rportfwd.exe Program.cs
// 
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

class ReversePortForwarder
{
    static async Task<int> Main(string[] args)
    {
        if (args.Length < 2 || !args[1].Contains(":"))
        {
            Console.WriteLine("Usage: rportfwd.exe <LPORT> <RHOST:RPORT> [KILL_SWITCH_PORT]");
            return 1;
        }

        int listenPort = int.Parse(args[0]);
        string[] targetParts = args[1].Split(':');
        string targetHost = targetParts[0];
        int targetPort = int.Parse(targetParts[1]);

        CancellationTokenSource cts = new CancellationTokenSource();
        CancellationToken token = cts.Token;

        // Start kill switch listener only if specified
        if (args.Length >= 3)
        {
            int killSwitchPort = int.Parse(args[2]);
            _ = Task.Run(() => StartKillSwitchListener(killSwitchPort, cts));
            Console.WriteLine($"[*] Kill switch active on port {killSwitchPort}");
        }

        TcpListener listener;
        try
        {
            listener = new TcpListener(IPAddress.Any, listenPort);
            listener.Start();
        }
        catch (SocketException ex)
        {
            Console.Error.WriteLine($"[!] Failed to bind to port {listenPort}: {ex.Message}");
            return 1;
        }
        Console.WriteLine($"[*] Listening on 0.0.0.0:{listenPort} -> forwarding to {targetHost}:{targetPort}");

        try
        {
            while (!token.IsCancellationRequested)
            {
                if (listener.Pending())
                {
                    var localClient = await listener.AcceptTcpClientAsync();
                    _ = Task.Run(() => HandleConnection(localClient, targetHost, targetPort));
                }
                await Task.Delay(100);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[!] Listener error: {ex.Message}");
        }
        finally
        {
            listener.Stop();
        }

        return 0;
    }

    static async Task HandleConnection(TcpClient localClient, string targetHost, int targetPort)
    {
        using var remoteClient = new TcpClient();
        try
        {
            await remoteClient.ConnectAsync(targetHost, targetPort);
            Console.WriteLine($"[+] Connection forwarded to {targetHost}:{targetPort}");

            var localStream = localClient.GetStream();
            var remoteStream = remoteClient.GetStream();

            _ = Pipe(localStream, remoteStream);
            _ = Pipe(remoteStream, localStream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[!] Connection failed: {ex.Message}");
            localClient.Close();
        }
    }

    static async Task Pipe(NetworkStream input, NetworkStream output)
    {
        byte[] buffer = new byte[8192];
        try
        {
            int read;
            while ((read = await input.ReadAsync(buffer, 0, buffer.Length)) > 0)
                await output.WriteAsync(buffer, 0, read);
        }
        catch { /* Ignore disconnections */ }
    }

    static void StartKillSwitchListener(int port, CancellationTokenSource cts)
    {
        TcpListener killListener = new TcpListener(IPAddress.Any, port);
        killListener.Start();
        Console.WriteLine($"[!] Kill switch armed. Connect to port {port} to terminate.");
        killListener.AcceptTcpClient();
        Console.WriteLine("[!] Kill switch triggered. Shutting down.");
        cts.Cancel();
        killListener.Stop();
    }
}
