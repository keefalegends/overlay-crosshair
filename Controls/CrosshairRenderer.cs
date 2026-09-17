using System;
using System.Windows;
using System.Windows.Media;
using CrosshairOverlay.Models;

namespace CrosshairOverlay.Controls
{
    public class CrosshairRenderer : FrameworkElement
    {
        public static readonly DependencyProperty ConfigProperty =
            DependencyProperty.Register(
                nameof(Config),
                typeof(CrosshairConfig),
                typeof(CrosshairRenderer),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnConfigChanged)
            );

        public static readonly DependencyProperty IsPreviewProperty =
            DependencyProperty.Register(
                nameof(IsPreview),
                typeof(bool),
                typeof(CrosshairRenderer),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender)
            );

        public CrosshairConfig? Config
        {
            get => (CrosshairConfig?)GetValue(ConfigProperty);
            set => SetValue(ConfigProperty, value);
        }

        public bool IsPreview
        {
            get => (bool)GetValue(IsPreviewProperty);
            set => SetValue(IsPreviewProperty, value);
        }

        // Brush cache — avoids allocating new BrushConverter + Brush on every OnRender call.
        // Only re-parses when the hex string actually changes.
        private string _cachedColorHex = "";
        private string _cachedOutlineHex = "";
        private Brush _cachedMainBrush = Brushes.LimeGreen;
        private Brush _cachedOutlineBrush = Brushes.Black;

        private Brush GetMainBrush(string hex)
        {
            if (hex == _cachedColorHex) return _cachedMainBrush;
            try
            {
                var b = (Brush)(new BrushConverter().ConvertFromString(hex) ?? Brushes.LimeGreen);
                if (b.CanFreeze) b.Freeze();
                _cachedColorHex = hex;
                return _cachedMainBrush = b;
            }
            catch { return _cachedMainBrush = Brushes.LimeGreen; }
        }

        private Brush GetOutlineBrush(string hex)
        {
            if (hex == _cachedOutlineHex) return _cachedOutlineBrush;
            try
            {
                var b = (Brush)(new BrushConverter().ConvertFromString(hex) ?? Brushes.Black);
                if (b.CanFreeze) b.Freeze();
                _cachedOutlineHex = hex;
                return _cachedOutlineBrush = b;
            }
            catch { return _cachedOutlineBrush = Brushes.Black; }
        }

        public CrosshairRenderer()
        {
            SnapsToDevicePixels = true;
            UseLayoutRounding = true;
        }

        private static void OnConfigChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CrosshairRenderer renderer)
            {
                if (e.OldValue is CrosshairConfig oldConfig)
                {
                    oldConfig.PropertyChanged -= renderer.OnConfigPropertyChanged;
                }
                if (e.NewValue is CrosshairConfig newConfig)
                {
                    newConfig.PropertyChanged += renderer.OnConfigPropertyChanged;
                }
                renderer.InvalidateVisual();
            }
        }

        private void OnConfigPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            var config = Config;
            if (config == null || (!config.IsVisible && !IsPreview))
                return;

            double width = ActualWidth;
            double height = ActualHeight;
            if (width <= 0 || height <= 0)
                return;

            double centerBaseX = Math.Floor(width / 2.0);
            double centerBaseY = Math.Floor(height / 2.0);

            if (IsPreview)
            {
                // Subtle esports reticle guide lines (crisp 1px aligned to device pixel grid)
                var guideBrush = new SolidColorBrush(Color.FromRgb(0x28, 0x2C, 0x38));
                if (guideBrush.CanFreeze) guideBrush.Freeze();
                var guidePen = new Pen(guideBrush, 1.0);
                if (guidePen.CanFreeze) guidePen.Freeze();

                double guideX = centerBaseX + 0.5;
                double guideY = centerBaseY + 0.5;

                // Horizontal center line across viewport
                dc.DrawLine(guidePen, new Point(0, guideY), new Point(width, guideY));
                // Vertical center line across viewport
                dc.DrawLine(guidePen, new Point(guideX, 0), new Point(guideX, height));
            }

            // In preview mode, allow reticle to move relative to center base guide lines if offset is set
            double cx = centerBaseX + (IsPreview ? config.OffsetX : 0);
            double cy = centerBaseY + (IsPreview ? config.OffsetY : 0);

            // Use cached brushes — only re-parses hex string when color actually changes
            Brush mainBrush = GetMainBrush(config.ColorHex);
            Brush outlineBrush = GetOutlineBrush(config.OutlineColorHex);

            dc.PushOpacity(Math.Clamp(config.Opacity, 0.0, 1.0));

            double size = Math.Max(1.0, config.Size);
            double thickness = Math.Max(1.0, config.Thickness);
            double gap = Math.Max(0.0, config.Gap);
            double dotSize = Math.Max(1.0, config.DotSize);
            double outlineThick = config.HasOutline ? Math.Max(0.5, config.OutlineThickness) : 0.0;

            switch (config.Style)
            {
                case CrosshairStyle.ClassicCross:
                    DrawCross(dc, cx, cy, size, thickness, gap, outlineThick, mainBrush, outlineBrush, drawTop: true);
                    break;

                case CrosshairStyle.Dot:
                    DrawDot(dc, cx, cy, dotSize, outlineThick, mainBrush, outlineBrush);
                    break;

                case CrosshairStyle.CrossDot:
                    DrawCross(dc, cx, cy, size, thickness, gap, outlineThick, mainBrush, outlineBrush, drawTop: true);
                    DrawDot(dc, cx, cy, dotSize, outlineThick, mainBrush, outlineBrush);
                    break;

                case CrosshairStyle.TShape:
                    DrawCross(dc, cx, cy, size, thickness, gap, outlineThick, mainBrush, outlineBrush, drawTop: false);
                    break;

                case CrosshairStyle.Circle:
                    DrawCircle(dc, cx, cy, gap + size, thickness, outlineThick, mainBrush, outlineBrush);
                    break;

                case CrosshairStyle.CircleDot:
                    DrawCircle(dc, cx, cy, gap + size, thickness, outlineThick, mainBrush, outlineBrush);
                    DrawDot(dc, cx, cy, dotSize, outlineThick, mainBrush, outlineBrush);
                    break;

                case CrosshairStyle.Chevron:
                    DrawChevron(dc, cx, cy, size, thickness, outlineThick, mainBrush, outlineBrush);
                    break;

                case CrosshairStyle.Box:
                    DrawBox(dc, cx, cy, gap + size, thickness, outlineThick, mainBrush, outlineBrush);
                    break;
            }

            dc.Pop();
        }

        private void DrawCross(
            DrawingContext dc,
            double cx, double cy,
            double size, double thickness, double gap, double outlineThick,
            Brush mainBrush, Brush outlineBrush,
            bool drawTop)
        {
            var innerPen = new Pen(mainBrush, thickness) { StartLineCap = PenLineCap.Square, EndLineCap = PenLineCap.Square };
            if (innerPen.CanFreeze) innerPen.Freeze();

            Pen? outerPen = null;
            if (outlineThick > 0)
            {
                outerPen = new Pen(outlineBrush, thickness + (outlineThick * 2))
                {
                    StartLineCap = PenLineCap.Square,
                    EndLineCap = PenLineCap.Square
                };
                if (outerPen.CanFreeze) outerPen.Freeze();
            }

            void RenderLines(Pen pen)
            {
                // Left arm
                dc.DrawLine(pen, new Point(cx - gap - size, cy), new Point(cx - gap, cy));
                // Right arm
                dc.DrawLine(pen, new Point(cx + gap, cy), new Point(cx + gap + size, cy));
                // Bottom arm
                dc.DrawLine(pen, new Point(cx, cy + gap), new Point(cx, cy + gap + size));
                // Top arm
                if (drawTop)
                {
                    dc.DrawLine(pen, new Point(cx, cy - gap - size), new Point(cx, cy - gap));
                }
            }

            if (outerPen != null)
            {
                RenderLines(outerPen);
            }
            RenderLines(innerPen);
        }

        private void DrawDot(
            DrawingContext dc,
            double cx, double cy,
            double dotSize, double outlineThick,
            Brush mainBrush, Brush outlineBrush)
        {
            double radius = dotSize / 2.0;
            if (outlineThick > 0)
            {
                dc.DrawEllipse(outlineBrush, null, new Point(cx, cy), radius + outlineThick, radius + outlineThick);
            }
            dc.DrawEllipse(mainBrush, null, new Point(cx, cy), radius, radius);
        }

        private void DrawCircle(
            DrawingContext dc,
            double cx, double cy,
            double radius, double thickness, double outlineThick,
            Brush mainBrush, Brush outlineBrush)
        {
            if (outlineThick > 0)
            {
                var outerPen = new Pen(outlineBrush, thickness + (outlineThick * 2));
                if (outerPen.CanFreeze) outerPen.Freeze();
                dc.DrawEllipse(null, outerPen, new Point(cx, cy), radius, radius);
            }

            var innerPen = new Pen(mainBrush, thickness);
            if (innerPen.CanFreeze) innerPen.Freeze();
            dc.DrawEllipse(null, innerPen, new Point(cx, cy), radius, radius);
        }

        private void DrawChevron(
            DrawingContext dc,
            double cx, double cy,
            double size, double thickness, double outlineThick,
            Brush mainBrush, Brush outlineBrush)
        {
            var innerPen = new Pen(mainBrush, thickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
            if (innerPen.CanFreeze) innerPen.Freeze();

            Pen? outerPen = null;
            if (outlineThick > 0)
            {
                outerPen = new Pen(outlineBrush, thickness + (outlineThick * 2))
                {
                    StartLineCap = PenLineCap.Round,
                    EndLineCap = PenLineCap.Round
                };
                if (outerPen.CanFreeze) outerPen.Freeze();
            }

            void RenderChevron(Pen pen)
            {
                dc.DrawLine(pen, new Point(cx - size, cy + size), new Point(cx, cy));
                dc.DrawLine(pen, new Point(cx + size, cy + size), new Point(cx, cy));
            }

            if (outerPen != null)
            {
                RenderChevron(outerPen);
            }
            RenderChevron(innerPen);
        }

        private void DrawBox(
            DrawingContext dc,
            double cx, double cy,
            double halfSize, double thickness, double outlineThick,
            Brush mainBrush, Brush outlineBrush)
        {
            var rect = new Rect(cx - halfSize, cy - halfSize, halfSize * 2, halfSize * 2);

            if (outlineThick > 0)
            {
                var outerPen = new Pen(outlineBrush, thickness + (outlineThick * 2));
                if (outerPen.CanFreeze) outerPen.Freeze();
                dc.DrawRectangle(null, outerPen, rect);
            }

            var innerPen = new Pen(mainBrush, thickness);
            if (innerPen.CanFreeze) innerPen.Freeze();
            dc.DrawRectangle(null, innerPen, rect);
        }
    }
}
