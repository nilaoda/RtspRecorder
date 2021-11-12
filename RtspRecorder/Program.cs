// See https://aka.ms/new-console-template for more information
using RtspRecorder;
using System.CommandLine;
using System.CommandLine.Invocation;

var rootCommand = new RootCommand
{
    new Option<string>(
        new string[]{ "--input", "-i" },
        "设置输入rtsp://链接.")
        { IsRequired = true },
    new Option<string>(
        new string[]{ "--duration", "-t" },
        "设置输出长度. [hh:mm:ss]"),
    new Option<string>(
        aliases: new string[]{ "--output", "-o" },
        getDefaultValue: () => "auto",
        description: "设置输出文件. (使用 - 以输出到stdout)"),
    new Option<string>(
        aliases: new string[]{ "--program" },
        getDefaultValue: () => "Record",
        description: "设置流标题. (当--output为auto时生效)"),
    new Option<bool>(
        aliases: new string[]{ "--detail"},
        getDefaultValue: () => false,
        description: "设置是否输出详细交互信息"),
    new Option<string>(
        aliases: new string[]{ "--playback"},
        description: "设置回看, 格式yyyyMMddHHmmss[-<yyyyMMddHHmmss>]"),
};

rootCommand.Description = "适用于 IPTV 的 RTSP 录制工具.";

rootCommand.Handler = CommandHandler.Create<string, string, string, string, bool, string>((input, duration, output, program, detail, playback) =>
{
    try
    {
        string url = input;
        if (!url.StartsWith("rtsp://"))
            throw new ArgumentException("输入链接不是rtsp协议!");
        if (!string.IsNullOrEmpty(playback))
        {
            var arr = playback.Split('-');
            if (Util.CheckTime(arr)) url = Util.GetPlaybackUrl(url, arr);
            else throw new ArgumentException("回看时间输入有误! " + playback);
        }
        var client = new RTSPClient(url);
        client.OutName = output;
        client.StdOut = client.OutName == "-";
        client.Detail = detail;
        client.Program = program;
        if (!string.IsNullOrEmpty(duration))
            client.RecDurLimit = (int)Util.ParseDur(duration).TotalSeconds;
        client.Connect();
        client.DoWork();
        client.Close();
    }
    catch (Exception ex)
    {
        if (detail || ex.GetType() != typeof(IOException))
            Console.Error.WriteLine(Environment.NewLine + ex.Message);
    }
});

return rootCommand.InvokeAsync(args).Result;

