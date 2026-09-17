using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace CrosshairOverlay.Models
{
    public enum CrosshairStyle
    {
        ClassicCross,
        Dot,
        CrossDot,
        TShape,
        Circle,
        CircleDot,
        Chevron,
        Box
    }

    public class CrosshairConfig : INotifyPropertyChanged
    {
        private CrosshairStyle _style = CrosshairStyle.CrossDot;
        private string _colorHex = "#00FF66";
        private double _opacity = 1.0;
        private double _size = 10.0;
        private double _thickness = 2.0;
        private double _gap = 5.0;
        private double _dotSize = 3.0;
        private bool _hasOutline = true;
        private string _outlineColorHex = "#000000";
        private double _outlineThickness = 1.0;
        private int _offsetX = 0;
        private int _offsetY = 0;
        private bool _isVisible = true;

        public CrosshairStyle Style
        {
            get => _style;
            set { if (_style != value) { _style = value; OnPropertyChanged(); } }
        }

        public string ColorHex
        {
            get => _colorHex;
            set { if (_colorHex != value) { _colorHex = value; OnPropertyChanged(); } }
        }

        public double Opacity
        {
            get => _opacity;
            set { if (Math.Abs(_opacity - value) > 0.001) { _opacity = value; OnPropertyChanged(); } }
        }

        public double Size
        {
            get => _size;
            set { if (Math.Abs(_size - value) > 0.001) { _size = value; OnPropertyChanged(); } }
        }

        public double Thickness
        {
            get => _thickness;
            set { if (Math.Abs(_thickness - value) > 0.001) { _thickness = value; OnPropertyChanged(); } }
        }

        public double Gap
        {
            get => _gap;
            set { if (Math.Abs(_gap - value) > 0.001) { _gap = value; OnPropertyChanged(); } }
        }

        public double DotSize
        {
            get => _dotSize;
            set { if (Math.Abs(_dotSize - value) > 0.001) { _dotSize = value; OnPropertyChanged(); } }
        }

        public bool HasOutline
        {
            get => _hasOutline;
            set { if (_hasOutline != value) { _hasOutline = value; OnPropertyChanged(); } }
        }

        public string OutlineColorHex
        {
            get => _outlineColorHex;
            set { if (_outlineColorHex != value) { _outlineColorHex = value; OnPropertyChanged(); } }
        }

        public double OutlineThickness
        {
            get => _outlineThickness;
            set { if (Math.Abs(_outlineThickness - value) > 0.001) { _outlineThickness = value; OnPropertyChanged(); } }
        }

        public int OffsetX
        {
            get => _offsetX;
            set { if (_offsetX != value) { _offsetX = value; OnPropertyChanged(); } }
        }

        public int OffsetY
        {
            get => _offsetY;
            set { if (_offsetY != value) { _offsetY = value; OnPropertyChanged(); } }
        }

        public bool IsVisible
        {
            get => _isVisible;
            set { if (_isVisible != value) { _isVisible = value; OnPropertyChanged(); } }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void ResetToDefaults()
        {
            Style = CrosshairStyle.CrossDot;
            ColorHex = "#00FF66";
            Opacity = 1.0;
            Size = 10.0;
            Thickness = 2.0;
            Gap = 5.0;
            DotSize = 3.0;
            HasOutline = true;
            OutlineColorHex = "#000000";
            OutlineThickness = 1.0;
            OffsetX = 0;
            OffsetY = 0;
            IsVisible = true;
        }

        /// <summary>
        /// Clamps all numeric values to safe UI ranges.
        /// Called after deserialization to guard against hand-edited or corrupted JSON.
        /// </summary>
        public void Clamp()
        {
            Size            = Math.Clamp(Size, 1, 100);
            Thickness       = Math.Clamp(Thickness, 1, 20);
            Gap             = Math.Clamp(Gap, 0, 50);
            DotSize         = Math.Clamp(DotSize, 1, 30);
            Opacity         = Math.Clamp(Opacity, 0.05, 1.0);
            OutlineThickness = Math.Clamp(OutlineThickness, 0.5, 5.0);
            OffsetX         = Math.Clamp(OffsetX, -500, 500);
            OffsetY         = Math.Clamp(OffsetY, -500, 500);
        }
    }
}
