using CommandLine.Text;
using System;
using Windows.Media.Control;

namespace GSMTCHelper
{
    public class Options
    {
        [CommandLine.Option('l', "list", HelpText = "显示所有 Sessions")]
        public bool ShowList { get; set; }

        [CommandLine.Option('d', "detail", HelpText = "列表显示所有属性")]
        public bool ShowDetail { get; set; }

        [CommandLine.Option("aumid", HelpText = "Application User Model ID")]
        public string? Aumid { get; set; }

        [CommandLine.Option("watch", HelpText = "监听 aumid 对应的 Session 属性变化")]
        public string? Watch { get; set; }
    }

    internal class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length == 0) args = ["--help"];

            //args = ["--watch", "--aumid", "17588BrandonWong.LyricEase_13cqesaq5mk46!App"];

            var parserResult = CommandLine.Parser.Default.ParseArguments<Options>(args);
            if (parserResult?.Value != null)
            {
                var manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
                var curSessionAumid = manager.GetCurrentSession()?.SourceAppUserModelId;
                var sessions = manager.GetSessions()
                    .GroupBy(c => c.SourceAppUserModelId)
                    .Select(c => (aumid: c.Key, sessions: c.ToArray(), selected: c.Key == curSessionAumid))
                    .ToArray();

                if (!string.IsNullOrEmpty(parserResult.Value.Watch))
                {
                    var sessionGroup = sessions
                        .FirstOrDefault(c => string.Equals(parserResult.Value.Watch, c.aumid));

                    if (!string.IsNullOrEmpty(sessionGroup.aumid))
                    {
                        var timer = new System.Timers.Timer()
                        {
                            AutoReset = false,
                            Interval = 200,
                            SynchronizingObject = null
                        };

                        var lastTop = 0;

                        timer.Elapsed += async (_s, _a) =>
                        {
                            try
                            {
                                Console.SetCursorPosition(0, 0);
                                await PrintSession(sessionGroup.aumid, sessionGroup.sessions, sessionGroup.selected, true);

                                ClearAndWriteLine();
                                ClearAndWriteLine("按 [Ctrl + C] 键退出");

                                var newTop = Console.CursorTop;
                                if (lastTop > newTop)
                                {
                                    for (var i = 0; i < lastTop - newTop; i++)
                                    {
                                        ClearAndWriteLine();
                                    }
                                }
                                lastTop = newTop;
                            }
                            finally { timer.Start(); }
                        };

                        Console.Clear();

                        timer.Start();
                        var waitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);
                        waitHandle.WaitOne();

                        Console.CancelKeyPress += (_s, _e) =>
                        {
                            timer.Stop();
                            waitHandle.Set();
                        };
                    }
                }
                if (parserResult.Value.ShowList)
                {
                    bool isFirst = true;
                    foreach (var (aumid, children, selected) in sessions)
                    {
                        if (string.IsNullOrEmpty(parserResult.Value.Aumid)
                            || string.Equals(aumid, parserResult.Value.Aumid, StringComparison.OrdinalIgnoreCase))
                        {
                            if (parserResult.Value.ShowDetail)
                            {
                                if (isFirst) isFirst = false;
                                else
                                {
                                    Console.WriteLine();
                                    Console.WriteLine("  ========================");
                                    Console.WriteLine();
                                }
                            }

                            await PrintSession(aumid, children, selected, parserResult.Value.ShowDetail);
                        }
                    }
                }
            }
        }

        public static async Task PrintSession(string aumid, GlobalSystemMediaTransportControlsSession[] sessions, bool selected, bool detail)
        {
            GlobalSystemMediaTransportControlsSessionMediaProperties? mediaProp = null;
            GlobalSystemMediaTransportControlsSessionTimelineProperties? timeline = null;

            if (detail)
            {

                foreach (var child in sessions)
                {
                    if (mediaProp == null)
                    {
                        try
                        {
                            var _mediaProp = await child.TryGetMediaPropertiesAsync();
                            if (!string.IsNullOrEmpty(_mediaProp.Title)
                                || !string.IsNullOrEmpty(_mediaProp.AlbumTitle)
                                || !string.IsNullOrEmpty(_mediaProp.Artist)
                                || !string.IsNullOrEmpty(_mediaProp.AlbumArtist))
                            {
                                mediaProp = _mediaProp;
                            }
                        }
                        catch { }
                    }

                    if (timeline == null)
                    {
                        try
                        {
                            var _timeline = child.GetTimelineProperties();
                            if (_timeline != null)
                            {
                                if (_timeline.StartTime != default
                                    || _timeline.EndTime != default
                                    || _timeline.LastUpdatedTime != default
                                    || _timeline.Position != default)
                                {
                                    timeline = _timeline;
                                }
                            }
                        }
                        catch { }
                    }
                }

                if (timeline == null) timeline = sessions[0].GetTimelineProperties();
            }

            ClearAndWriteLine($"{(selected ? "*" : " ")} {aumid}");

            if (mediaProp != null)
            {
                ClearAndWriteLine();

                ClearAndWriteLine($"    PlaybackType: {mediaProp.PlaybackType}");
                ClearAndWriteLine($"    Title: {mediaProp.Title}");
                ClearAndWriteLine($"    Subtitle: {mediaProp.Subtitle}");
                ClearAndWriteLine($"    Artist: {mediaProp.Artist}");
                ClearAndWriteLine($"    TrackNumber: {mediaProp.TrackNumber}");
                ClearAndWriteLine($"    AlbumTitle: {mediaProp.AlbumTitle}");
                ClearAndWriteLine($"    AlbumArtist: {mediaProp.AlbumArtist}");
                ClearAndWriteLine($"    AlbumTrackCount: {mediaProp.AlbumTrackCount}");
                if (mediaProp.Genres != null && mediaProp.Genres.Count > 0)
                {
                    ClearAndWriteLine($"    Genres:");
                    foreach (var genre in mediaProp.Genres)
                    {
                        ClearAndWriteLine($"        {genre}");
                    }
                }
            }

            if (timeline != null)
            {
                ClearAndWriteLine();

                ClearAndWriteLine($"    StartTime: {timeline.StartTime}");
                ClearAndWriteLine($"    EndTime: {timeline.EndTime}");
                ClearAndWriteLine($"    Position: {timeline.Position}");
                ClearAndWriteLine($"    LastUpdatedTime: {timeline.LastUpdatedTime}");
                ClearAndWriteLine($"    MinSeekTime: {timeline.MinSeekTime}");
                ClearAndWriteLine($"    MaxSeekTime: {timeline.MaxSeekTime}");
            }
        }

        private static void ClearAndWriteLine(string value)
        {
            ClearLine();
            Console.WriteLine(value);
        }

        private static void ClearAndWriteLine()
        {
            ClearLine();
            Console.WriteLine();
        }

        private static void ClearLine()
        {
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, Console.CursorTop);
        }
    }
}
