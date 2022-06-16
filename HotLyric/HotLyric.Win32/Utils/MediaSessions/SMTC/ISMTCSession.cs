using System;
using System.Collections.Generic;
using System.Text;
using Windows.Media.Control;

namespace HotLyric.Win32.Utils.MediaSessions.SMTC
{
    public interface ISMTCSession : IMediaSession
    {
        GlobalSystemMediaTransportControlsSession Session { get; }
    }
}
