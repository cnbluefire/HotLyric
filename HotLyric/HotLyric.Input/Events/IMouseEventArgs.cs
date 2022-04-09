namespace HotLyric.Input.Events
{
    public interface IMouseEventArgs
    {
        public MouseButtonState LeftButton { get; }

        public MouseButtonState MiddleButton { get; }

        public MouseButtonState RightButton { get; }

        public MouseButtonState XButton1Button { get; }

        public MouseButtonState XButton2Button { get; }

        public int X { get; }

        public int Y { get; }
    }
}
