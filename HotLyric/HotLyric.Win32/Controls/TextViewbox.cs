using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace HotLyric.Win32.Controls
{
    public class TextViewbox : Border
    {
        public TextViewbox()
        {
            this.IsVisibleChanged += TextViewbox_IsVisibleChanged;
        }

        private void TextViewbox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is true)
            {
                InvalidateMeasure();
                UpdateLayout();
            }
        }

        protected override Size MeasureOverride(Size constraint)
        {
            if (IsVisible && Child is ProgressTextBlock textBlock && textBlock.FontFamily != null)
            {
                // fontSize * LineSpacing + strokeThickness * 2 = height
                // strokeThickness = fontSize / 20

                var height = Math.Floor(constraint.Height - 1);

                if (!double.IsInfinity(height) && !double.IsNaN(height) && height > 0)
                {
                    var strokeThickness1 = textBlock.StrokeThickness1;
                    var strokeThickness2 = textBlock.StrokeThickness2;

                    double fontSize;

                    if (double.IsNaN(strokeThickness1) && double.IsNaN(strokeThickness2))
                    {
                        fontSize = height / (textBlock.FontFamily.LineSpacing + 0.1);
                    }
                    else if (!double.IsNaN(strokeThickness1) && !double.IsNaN(strokeThickness2))
                    {
                        var strokeThickness = Math.Max(Math.Max(strokeThickness1, strokeThickness2), 0);
                        fontSize = (height - strokeThickness * 2) / textBlock.FontFamily.LineSpacing;
                    }
                    else
                    {
                        var strokeThickness = (double.IsNaN(strokeThickness1) ? strokeThickness2 : strokeThickness1);

                        var fontSize1 = height / (textBlock.FontFamily.LineSpacing + 0.1);
                        var tmpStrokeThickness = fontSize1 / 20;

                        if (tmpStrokeThickness >= strokeThickness)
                        {
                            fontSize = fontSize1;
                        }
                        else
                        {
                            fontSize = (height - strokeThickness * 2) / textBlock.FontFamily.LineSpacing;
                        }
                    }

                    textBlock.FontSize = Math.Max(0, fontSize);

                    textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    var width = textBlock.DesiredSize.Width;
                    return new Size(double.IsNaN(width) ? 0 : width, constraint.Height);
                }
            }

            return base.MeasureOverride(constraint);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (IsVisible && Child is ProgressTextBlock textBlock && finalSize.Width > 0 && finalSize.Height > 0 && !double.IsInfinity(finalSize.Width) && !double.IsInfinity(finalSize.Height))
            {
                textBlock.Arrange(new Rect(default, finalSize));
                return finalSize;
            }
            return base.ArrangeOverride(finalSize);
        }
    }
}
