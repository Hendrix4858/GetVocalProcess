using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using CommandLine;
namespace GetVocalProcess;

public class Options
{
    [Option('p', "pipe", Required = false, HelpText = "启动管道服务器模式")]
    public bool PipeMode { get; set; }

    [Option('v', "version", Required = false, HelpText = "显示版本信息")]
    public bool ShowVersion { get; set; }
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