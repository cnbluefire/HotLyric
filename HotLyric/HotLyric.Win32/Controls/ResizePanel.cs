using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Xml.Linq;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI.Input;

namespace HotLyric.Win32.Controls
{
    public enum DragResizeEdge
    {
        Left = 1,
        Right,
        Top,
        TopLeft,
        TopRight,
        Bottom,
        BottomLeft,
        BottomRight,
    }

    public sealed class ResizePanel : Control
    {
        private static Dictionary<string, DragResizeEdge> edges = new Dictionary<string, DragResizeEdge>()
        {
            ["LeftDragger"] = DragResizeEdge.Left,
            ["RightDragger"] = DragResizeEdge.Right,
            ["TopDragger"] = DragResizeEdge.Top,
            ["TopLeftDragger"] = DragResizeEdge.TopLeft,
            ["TopRightDragger"] = DragResizeEdge.TopRight,
            ["BottomDragger"] = DragResizeEdge.Bottom,
            ["BottomLeftDragger"] = DragResizeEdge.BottomLeft,
            ["BottomRightDragger"] = DragResizeEdge.BottomRight,
        };

        private static Dictionary<string, InputSystemCursor> cursors = new Dictionary<string, InputSystemCursor>()
        {
            ["Default"] = InputSystemCursor.Create(InputSystemCursorShape.Arrow),
            ["LeftDragger"] = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast),
            ["RightDragger"] = InputSystemCursor.Create(InputSystemCursorShape.SizeWestEast),
            ["TopDragger"] = InputSystemCursor.Create(InputSystemCursorShape.SizeNorthSouth),
            ["TopLeftDragger"] = InputSystemCursor.Create(InputSystemCursorShape.SizeNorthwestSoutheast),
            ["TopRightDragger"] = InputSystemCursor.Create(InputSystemCursorShape.SizeNortheastSouthwest),
            ["BottomDragger"] = InputSystemCursor.Create(InputSystemCursorShape.SizeNorthSouth),
            ["BottomLeftDragger"] = InputSystemCursor.Create(InputSystemCursorShape.SizeNortheastSouthwest),
            ["BottomRightDragger"] = InputSystemCursor.Create(InputSystemCursorShape.SizeNorthwestSoutheast),
        };

        public ResizePanel()
        {
            this.DefaultStyleKey = typeof(ResizePanel);
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            LeftDragger = GetTemplateChild("LeftDragger") as Rectangle;
            RightDragger = GetTemplateChild("RightDragger") as Rectangle;
            TopDragger = GetTemplateChild("TopDragger") as Rectangle;
            TopLeftDragger = GetTemplateChild("TopLeftDragger") as Rectangle;
            TopRightDragger = GetTemplateChild("TopRightDragger") as Rectangle;
            BottomDragger = GetTemplateChild("BottomDragger") as Rectangle;
            BottomLeftDragger = GetTemplateChild("BottomLeftDragger") as Rectangle;
            BottomRightDragger = GetTemplateChild("BottomRightDragger") as Rectangle;

        }


        private void Dragger_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Rectangle dragger)
            {
                var name = dragger.Name;

                ProtectedCursor = cursors[name];
            }
        }

        private void Dragger_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            ProtectedCursor = cursors["Default"];
        }

        private void Dragger_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {
            ProtectedCursor = cursors["Default"];
        }

        private void Dragger_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Rectangle dragger)
            {
                var name = dragger.Name;
                var edge = edges[name];
                DraggerPointerPressed?.Invoke(this, new ResizePanelDraggerPressedEventArgs(e, edge));
            }
        }

        private void Dragger_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            ProtectedCursor = cursors["Default"];
        }

        public event ResizePanelDraggerPressedEventHandler? DraggerPointerPressed;

        #region Dragger Rects

        private Rectangle? leftDragger;

        private Rectangle? LeftDragger
        {
            get => leftDragger;
            set
            {
                if (leftDragger != value)
                {
                    if (leftDragger != null)
                    {
                        leftDragger.PointerEntered -= Dragger_PointerEntered;
                        leftDragger.PointerExited -= Dragger_PointerExited;
                        leftDragger.PointerCaptureLost -= Dragger_PointerCaptureLost;
                        leftDragger.PointerPressed -= Dragger_PointerPressed;
                        leftDragger.PointerReleased -= Dragger_PointerReleased;
                    }
                    leftDragger = value;
                    if (leftDragger != null)
                    {
                        leftDragger.PointerEntered += Dragger_PointerEntered;
                        leftDragger.PointerExited += Dragger_PointerExited;
                        leftDragger.PointerCaptureLost += Dragger_PointerCaptureLost;
                        leftDragger.PointerPressed += Dragger_PointerPressed;
                        leftDragger.PointerReleased += Dragger_PointerReleased;
                    }
                }
            }
        }

        private Rectangle? rightDragger;

        private Rectangle? RightDragger
        {
            get => rightDragger;
            set
            {
                if (rightDragger != value)
                {
                    if (rightDragger != null)
                    {
                        rightDragger.PointerEntered -= Dragger_PointerEntered;
                        rightDragger.PointerExited -= Dragger_PointerExited;
                        rightDragger.PointerCaptureLost -= Dragger_PointerCaptureLost;
                        rightDragger.PointerPressed -= Dragger_PointerPressed;
                        rightDragger.PointerReleased -= Dragger_PointerReleased;
                    }
                    rightDragger = value;
                    if (rightDragger != null)
                    {
                        rightDragger.PointerEntered += Dragger_PointerEntered;
                        rightDragger.PointerExited += Dragger_PointerExited;
                        rightDragger.PointerCaptureLost += Dragger_PointerCaptureLost;
                        rightDragger.PointerPressed += Dragger_PointerPressed;
                        rightDragger.PointerReleased += Dragger_PointerReleased;
                    }
                }
            }
        }

        private Rectangle? topDragger;

        private Rectangle? TopDragger
        {
            get => topDragger;
            set
            {
                if (topDragger != value)
                {
                    if (topDragger != null)
                    {
                        topDragger.PointerEntered -= Dragger_PointerEntered;
                        topDragger.PointerExited -= Dragger_PointerExited;
                        topDragger.PointerCaptureLost -= Dragger_PointerCaptureLost;
                        topDragger.PointerPressed -= Dragger_PointerPressed;
                        topDragger.PointerReleased -= Dragger_PointerReleased;
                    }
                    topDragger = value;
                    if (topDragger != null)
                    {
                        topDragger.PointerEntered += Dragger_PointerEntered;
                        topDragger.PointerExited += Dragger_PointerExited;
                        topDragger.PointerCaptureLost += Dragger_PointerCaptureLost;
                        topDragger.PointerPressed += Dragger_PointerPressed;
                        topDragger.PointerReleased += Dragger_PointerReleased;
                    }
                }
            }
        }

        private Rectangle? topLeftDragger;

        private Rectangle? TopLeftDragger
        {
            get => topLeftDragger;
            set
            {
                if (topLeftDragger != value)
                {
                    if (topLeftDragger != null)
                    {
                        topLeftDragger.PointerEntered -= Dragger_PointerEntered;
                        topLeftDragger.PointerExited -= Dragger_PointerExited;
                        topLeftDragger.PointerCaptureLost -= Dragger_PointerCaptureLost;
                        topLeftDragger.PointerPressed -= Dragger_PointerPressed;
                        topLeftDragger.PointerReleased -= Dragger_PointerReleased;
                    }
                    topLeftDragger = value;
                    if (topLeftDragger != null)
                    {
                        topLeftDragger.PointerEntered += Dragger_PointerEntered;
                        topLeftDragger.PointerExited += Dragger_PointerExited;
                        topLeftDragger.PointerCaptureLost += Dragger_PointerCaptureLost;
                        topLeftDragger.PointerPressed += Dragger_PointerPressed;
                        topLeftDragger.PointerReleased += Dragger_PointerReleased;
                    }
                }
            }
        }

        private Rectangle? topRightDragger;

        private Rectangle? TopRightDragger
        {
            get => topRightDragger;
            set
            {
                if (topRightDragger != value)
                {
                    if (topRightDragger != null)
                    {
                        topRightDragger.PointerEntered -= Dragger_PointerEntered;
                        topRightDragger.PointerExited -= Dragger_PointerExited;
                        topRightDragger.PointerCaptureLost -= Dragger_PointerCaptureLost;
                        topRightDragger.PointerPressed -= Dragger_PointerPressed;
                        topRightDragger.PointerReleased -= Dragger_PointerReleased;
                    }
                    topRightDragger = value;
                    if (topRightDragger != null)
                    {
                        topRightDragger.PointerEntered += Dragger_PointerEntered;
                        topRightDragger.PointerExited += Dragger_PointerExited;
                        topRightDragger.PointerCaptureLost += Dragger_PointerCaptureLost;
                        topRightDragger.PointerPressed += Dragger_PointerPressed;
                        topRightDragger.PointerReleased += Dragger_PointerReleased;
                    }
                }
            }
        }

        private Rectangle? bottomDragger;

        private Rectangle? BottomDragger
        {
            get => bottomDragger;
            set
            {
                if (bottomDragger != value)
                {
                    if (bottomDragger != null)
                    {
                        bottomDragger.PointerEntered -= Dragger_PointerEntered;
                        bottomDragger.PointerExited -= Dragger_PointerExited;
                        bottomDragger.PointerCaptureLost -= Dragger_PointerCaptureLost;
                        bottomDragger.PointerPressed -= Dragger_PointerPressed;
                        bottomDragger.PointerReleased -= Dragger_PointerReleased;
                    }
                    bottomDragger = value;
                    if (bottomDragger != null)
                    {
                        bottomDragger.PointerEntered += Dragger_PointerEntered;
                        bottomDragger.PointerExited += Dragger_PointerExited;
                        bottomDragger.PointerCaptureLost += Dragger_PointerCaptureLost;
                        bottomDragger.PointerPressed += Dragger_PointerPressed;
                        bottomDragger.PointerReleased += Dragger_PointerReleased;
                    }
                }
            }
        }

        private Rectangle? bottomLeftDragger;

        private Rectangle? BottomLeftDragger
        {
            get => bottomLeftDragger;
            set
            {
                if (bottomLeftDragger != value)
                {
                    if (bottomLeftDragger != null)
                    {
                        bottomLeftDragger.PointerEntered -= Dragger_PointerEntered;
                        bottomLeftDragger.PointerExited -= Dragger_PointerExited;
                        bottomLeftDragger.PointerCaptureLost -= Dragger_PointerCaptureLost;
                        bottomLeftDragger.PointerPressed -= Dragger_PointerPressed;
                        bottomLeftDragger.PointerReleased -= Dragger_PointerReleased;
                    }
                    bottomLeftDragger = value;
                    if (bottomLeftDragger != null)
                    {
                        bottomLeftDragger.PointerEntered += Dragger_PointerEntered;
                        bottomLeftDragger.PointerExited += Dragger_PointerExited;
                        bottomLeftDragger.PointerCaptureLost += Dragger_PointerCaptureLost;
                        bottomLeftDragger.PointerPressed += Dragger_PointerPressed;
                        bottomLeftDragger.PointerReleased += Dragger_PointerReleased;
                    }
                }
            }
        }

        private Rectangle? bottomRightDragger;

        private Rectangle? BottomRightDragger
        {
            get => bottomRightDragger;
            set
            {
                if (bottomRightDragger != value)
                {
                    if (bottomRightDragger != null)
                    {
                        bottomRightDragger.PointerEntered -= Dragger_PointerEntered;
                        bottomRightDragger.PointerExited -= Dragger_PointerExited;
                        bottomRightDragger.PointerCaptureLost -= Dragger_PointerCaptureLost;
                        bottomRightDragger.PointerPressed -= Dragger_PointerPressed;
                        bottomRightDragger.PointerReleased -= Dragger_PointerReleased;
                    }
                    bottomRightDragger = value;
                    if (bottomRightDragger != null)
                    {
                        bottomRightDragger.PointerEntered += Dragger_PointerEntered;
                        bottomRightDragger.PointerExited += Dragger_PointerExited;
                        bottomRightDragger.PointerCaptureLost += Dragger_PointerCaptureLost;
                        bottomRightDragger.PointerPressed += Dragger_PointerPressed;
                        bottomRightDragger.PointerReleased += Dragger_PointerReleased;
                    }
                }
            }
        }

        #endregion Dragger Rects
    }

    public class ResizePanelDraggerPressedEventArgs
    {
        public ResizePanelDraggerPressedEventArgs(PointerRoutedEventArgs pointerEventArgs, DragResizeEdge edge)
        {
            PointerEventArgs = pointerEventArgs;
            Edge = edge;
        }

        public PointerRoutedEventArgs PointerEventArgs { get; }

        public DragResizeEdge Edge { get; }
    }

    public delegate void ResizePanelDraggerPressedEventHandler(ResizePanel sender, ResizePanelDraggerPressedEventArgs args);
}
