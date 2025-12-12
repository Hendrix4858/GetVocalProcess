using System.IO.Pipes;
using System.Text;
using System.Text.Json;

namespace GetVocalProcess;

public class PipeServer
{
    private const string PIPE_NAME = "AudioSessionPipe";
    private bool _running = true;

    public async Task StartAsync()
    {
        Console.WriteLine($"服务器启动,监听管道: {PIPE_NAME}");

        while (_running)
        {
            await HandleClientAsync();
        }
    }

    private async Task HandleClientAsync()
    {
        using (var server = CreatePipeServer())
        {
            await server.WaitForConnectionAsync();
            await ProcessRequestAsync(server);
        }
    }

    private static NamedPipeServerStream CreatePipeServer()
    {
        return new NamedPipeServerStream(
            PIPE_NAME,
            PipeDirection.InOut,
            1,
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous
        );
    }

    private async Task ProcessRequestAsync(NamedPipeServerStream server)
    {
        var request = await ReadRequestAsync(server);
        
        if (request == "GET_SESSIONS")
        {
            var response = GetSessionsJson();
            await WriteResponseAsync(server, response);
        }
        else if (request == "STOP")
        {
            _running = false;
            await WriteResponseAsync(server, "STOPPED");
        }
    }

    private async Task<string> ReadRequestAsync(Stream stream)
    {
        var buffer = new byte[1024];
        var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
        return Encoding.UTF8.GetString(buffer, 0, bytesRead);
    }

    private string GetSessionsJson()
    {
        var sessions = AudioSessionManager.GetPlayingSoundSessions();
        return JsonSerializer.Serialize(sessions);
    }

    private async Task WriteResponseAsync(Stream stream, string response)
    {
        var bytes = Encoding.UTF8.GetBytes(response);
        await stream.WriteAsync(bytes, 0, bytes.Length);
        await stream.FlushAsync();
    }

    public void Stop()
    {
        _running = false;
    }
}
