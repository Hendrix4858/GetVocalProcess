using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;
using NAudio.CoreAudioApi;
using System.Text.Json;
using CommandLine;

public class AudioSessionInfo
{
    public string process_name { get; set; }
}

public class Options
{
    [Option('p', "pipe", Required = false, HelpText = "启动管道服务器模式")]
    public bool PipeMode { get; set; }

    [Option('v', "version", Required = false, HelpText = "显示版本信息")]
    public bool ShowVersion { get; set; }
}

public class AudioSessionManager
{
    public static List<AudioSessionInfo> GetPlayingSoundSessions()
    {
        var list = new List<AudioSessionInfo>();
        var device = GetDefaultDevice();
        if (device == null) return list;

        var sessions = device.AudioSessionManager.Sessions;
        AddPlayingSessions(list, sessions);

        device.Dispose();
        return list;
    }

    private static void AddPlayingSessions(List<AudioSessionInfo> list, SessionCollection sessions)
    {
        for (int i = 0; i < sessions.Count; i++)
        {
            AddIfPlaying(list, sessions[i]);
        }
    }

    private static MMDevice GetDefaultDevice()
    {
        try
        {
            var en = new MMDeviceEnumerator();
            return en.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
        }
        catch
        {
            return null;
        }
    }

    private static void AddIfPlaying(List<AudioSessionInfo> list, AudioSessionControl s)
    {
        if (!IsPlaying(s)) return;

        var name = GetProcessName((int)s.GetProcessID);
        if (name == null) return;

        list.Add(new AudioSessionInfo { process_name = name });
    }

    private static bool IsPlaying(AudioSessionControl s)
    {
        if (s.SimpleAudioVolume.Mute) return false;
        return s.AudioMeterInformation.MasterPeakValue > 0.001f;
    }

    private static string GetProcessName(int pid)
    {
        if (pid == 0) return null;

        try
        {
            return Process.GetProcessById(pid).ProcessName;
        }
        catch
        {
            return null;
        }
    }
}

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

    private NamedPipeServerStream CreatePipeServer()
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

class Program
{
    static async Task<int> Main(string[] args)
    {
        return await Parser.Default.ParseArguments<Options>(args)
            .MapResult(
                async opts => await RunWithOptionsAsync(opts),
                _ => Task.FromResult(1)
            );
    }

    private static async Task<int> RunWithOptionsAsync(Options opts)
    {
        if (opts.ShowVersion)
        {
            PrintVersion();
            return 0;
        }

        if (opts.PipeMode)
        {
            await StartPipeServerAsync();
            return 0;
        }

        PrintPlayingSessions();
        return 0;
    }

    private static void PrintVersion()
    {
        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        Console.WriteLine($"version: v{version.Major}.{version.Minor}.{version.Build}");
    }

    private static async Task StartPipeServerAsync()
    {
        var server = new PipeServer();
        await server.StartAsync();
    }

    private static void PrintPlayingSessions()
    {
        var sessions = AudioSessionManager.GetPlayingSoundSessions();
        var json = JsonSerializer.Serialize(sessions, new JsonSerializerOptions 
        { 
            WriteIndented = true 
        });
        Console.WriteLine(json);
    }
}