namespace HotLyric.Input.Events
{
    public struct MouseButtonEventArgs : IMouseEventArgs
    {
        public static MouseButtonEventArgs Create(MouseButton changeButton)
        {
            var args = new MouseButtonEventArgs();
            args.LeftButton = MouseManager.LeftButton;
            args.MiddleButton = MouseManager.MiddleButton;
            args.RightButton = MouseManager.RightButton;
            args.XButton1Button = MouseManager.XButton1;
            args.XButton2Button = MouseManager.XButton2;
            args.X = MouseManager.X;
            args.Y = MouseManager.Y;
            args.ChangeButton = changeButton;

            return args;
        }


        public MouseButtonState LeftButton { get; private set; }

        public MouseButtonState MiddleButton { get; private set; }

        public MouseButtonState RightButton { get; private set; }

        public MouseButtonState XButton1Button { get; private set; }

        public MouseButtonState XButton2Button { get; private set; }

        public int X { get; private set; }

        public int Y { get; private set; }

        public MouseButton ChangeButton { get; private set; }

        public override string ToString()
        {
            return $"{X}, {Y}, {ChangeButton}";
        }
    }
}
