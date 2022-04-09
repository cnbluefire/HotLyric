using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace HotLyric.Input.Events
{
    public struct MouseMoveAngleChangedEventArgs
    {
        internal MouseMoveAngleChangedEventArgs(Point startPoint, Point endPoint, double angle)
        {
            StartPoint = startPoint;
            EndPoint = endPoint;
            Angle = angle;
        }

        public Point StartPoint { get; }

        public Point EndPoint { get; }

        public double Angle { get; }
    }
}
