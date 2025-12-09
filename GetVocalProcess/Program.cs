using System;
using System.Collections.Generic;
using System.Diagnostics;
using NAudio.CoreAudioApi;

public class AudioSessionInfo
{
    public string process_name { get; set; }
}

public class AudioSessionManager
{
    public static List<AudioSessionInfo> GetPlayingSoundSessions()
    {
        var list = new List<AudioSessionInfo>();
        var device = GetDefaultDevice();
        var sessions = device.AudioSessionManager.Sessions;

        for (int i = 0; i < sessions.Count; i++)
        {
            AddIfPlaying(list, sessions[i]);
        }

        device.Dispose();
        return list;
    }

    private static MMDevice GetDefaultDevice()
    {
        var en = new MMDeviceEnumerator();
        return en.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
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
// 使用示例
class Program
{
    static void Main()
    {
        var sessions = AudioSessionManager.GetPlayingSoundSessions();
        
        foreach (var session in sessions)
        {
            Console.WriteLine($"{session.process_name}");
        }
    }
}