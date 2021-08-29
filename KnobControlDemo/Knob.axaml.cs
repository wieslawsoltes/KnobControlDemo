using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace KnobControlDemo
{
    public class Knob : TemplatedControl
    {
        public static readonly StyledProperty<double> ValueProperty = 
            AvaloniaProperty.Register<Knob, double>(nameof(Value));

        private class Digit
        {
            public string BorderName { get; set; }

            public Border? BorderControl { get; set; }

            public string TextName { get; set; }

            public TextBlock? TextControl { get; set; }

            public double Value { get; set; }

            public bool IsSelected { get; set; }

            public double Angle { get; set; }

            public Digit(string borderName, string textName, double value, bool isSelected)
            {
                BorderName = borderName;
                TextName = textName;
                Value = value;
                IsSelected = isSelected;
            }

            public void Select()
            {
                if (TextControl is { })
                {
                    ((IPseudoClasses)(TextControl.Classes)).Set(":selected", true);
                }

                IsSelected = true;
            }
    
            public void Deselect()
            {
                if (TextControl is { })
                {
                    ((IPseudoClasses)(TextControl.Classes)).Set(":selected", false);
                }

                IsSelected = false;
            }
        }

        private Canvas? _canvas;
        private Ellipse? _backgroundEllipse;
        private Ellipse? _digitsEllipse;
        private Ellipse? _valueEllipse;
        private Border? _valueBorder;
        private TextBlock? _valueText;
        private Panel? _minusPanel;
        private Panel? _plusPanel;
        private Panel? _cursorPanel;
        private bool _drag;
        private string _valuePrefix;
        private string _valueUnit;
        private List<Digit> digits;

        public Knob()
        {
            _valuePrefix = "";
            _valueUnit = "V";
            digits = new()
            {
                new Digit("PART_Border1", "PART_Text1", 9.0, false),
                new Digit("PART_Border2", "PART_Text2", 1.0, false),
                new Digit("PART_Border3", "PART_Text3", 2.0, false),
                new Digit("PART_Border4", "PART_Text4", 3.0, false),
                new Digit("PART_Border5", "PART_Text5", 4.0, false),
                new Digit("PART_Border6", "PART_Text6", 5.0, false),
                new Digit("PART_Border7", "PART_Text7", 6.0, false),
                new Digit("PART_Border8", "PART_Text8", 7.0, false),
                new Digit("PART_Border9", "PART_Text9", 8.0, false),
            };
        }

        public double Value
        {
            get => GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == ValueProperty)
            {
                var value = change.NewValue.GetValueOrDefault<double>();
                Invalidate(value);
            }
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            _canvas = e.NameScope.Find<Canvas>("PART_Canvas");
            _backgroundEllipse = e.NameScope.Find<Ellipse>("PART_BackgroundEllipse");
            _digitsEllipse = e.NameScope.Find<Ellipse>("PART_DigitsEllipse");
            _valueEllipse = e.NameScope.Find<Ellipse>("PART_ValueEllipse");
            _valueBorder = e.NameScope.Find<Border>("PART_ValueBorder");
            _valueText = e.NameScope.Find<TextBlock>("PART_ValueText");
            _minusPanel = e.NameScope.Find<Panel>("PART_MinusPanel");
            _plusPanel = e.NameScope.Find<Panel>("PART_PlusPanel");
            _cursorPanel = e.NameScope.Find<Panel>("PART_CursorPanel");

            if (_backgroundEllipse is { })
            {
                _backgroundEllipse.AddHandler(PointerPressedEvent, BackgroundEllipse_PointerPressed, RoutingStrategies.Tunnel | RoutingStrategies.Direct);
                _backgroundEllipse.AddHandler(PointerReleasedEvent, BackgroundEllipse_PointerReleased, RoutingStrategies.Tunnel | RoutingStrategies.Direct);
                _backgroundEllipse.AddHandler(PointerMovedEvent, BackgroundEllipse_PointerMoved, RoutingStrategies.Tunnel | RoutingStrategies.Direct);
            }

            if (_minusPanel is { })
            {
                _minusPanel.PointerPressed += (_, _) => Minus();
            }

            if (_plusPanel is { })
            {
                _plusPanel.PointerPressed += (_, _) => Plus();
            }

            InitializeDigits(e.NameScope);
            InvalidateDigits();
            Invalidate(Value);
        }

        private void BackgroundEllipse_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (_backgroundEllipse is null)
            {
                return;
            }

            var point = e.GetPosition(_canvas);
            var center = GetBounds(_backgroundEllipse).Center;

            InvalidateMove(point, center);

            _drag = true;
        }

        private void BackgroundEllipse_PointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            if (_backgroundEllipse is null)
            {
                return;
            }

            if (_drag)
            {
                _drag = false;
            }
        }

        private void BackgroundEllipse_PointerMoved(object? sender, PointerEventArgs e)
        {
            if (_backgroundEllipse is null)
            {
                return;
            }

            if (_drag)
            {
                var point = e.GetPosition(_canvas);
                var center = GetBounds(_backgroundEllipse).Center;

                InvalidateMove(point, center);
            }
        }

        private void InitializeDigits(INameScope nameScope)
        {
            double startAngle = 0.0;
            double step = 2.0 * Math.PI / digits.Count;

            for (var index = 0; index < digits.Count; index++)
            {
                var digit = digits[index];

                digit.BorderControl = nameScope.Find<Border>(digit.BorderName);
                digit.TextControl = nameScope.Find<TextBlock>(digit.TextName);
                digit.Angle = startAngle;
                digit.BorderControl.PointerPressed += (_, _) => { Value = digit.Value; };

                startAngle += step;
            }
        }

        private void Minus()
        {
            var value = Value;
            value = Math.Round(value - 0.01, 2);
            Value = value;
        }

        private void Plus()
        {
            var value = Value;
            value = Math.Round(value + 0.01, 2);
            Value = value;
        }

        private bool TryToSnap(double value, double precision, out double snapValue)
        {
            for (var index = 0; index < digits.Count; index++)
            {
                var digit = digits[index];

                if (Math.Abs(digit.Value - value) < precision)
                {
                    snapValue = digit.Value;
                    return true;
                }
            }

            snapValue = double.NaN;
            return false;
        }

        private void TryToSnapExact(double value)
        {
            for (var index = 0; index < digits.Count; index++)
            {
                var digit = digits[index];

                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (digit.Value == value)
                {
                    digit.Select();
                    return;
                }
            }
        }

        private double GetAngleFromValue(double value)
        {
            var angle = value * ((2 * Math.PI) / digits.Count);
            return angle;
        }

        private void SetValueFromAngle(double theta)
        {
            var angle = theta;

            var range = 9.0;

            var value = Math.Round(angle / (2 * Math.PI) * range, 2);
            if (value < 1.0)
            {
                value = range + value;
            }

            Value = TryToSnap(value, 0.1, out var snapValue) ? snapValue : value;
        }

        private void InvalidateMove(Point point, Point center)
        {
            var dx = point.X - center.X;
            var dy = point.Y - center.Y;
            var theta =  Math.PI - Math.Atan2(dx, dy);

            SetValueFromAngle(theta);
        }

        private void ResetSelectedDigit()
        {
            foreach (var digit in digits)
            {
                digit.Deselect();
            }
        }

        private Rect GetBounds(Layoutable control)
        {
            var left = Canvas.GetLeft(control);
            var top = Canvas.GetTop(control);
            var width = control.Width;
            var height = control.Height;
            return new Rect(left, top, width, height);
        }

        private void InvalidateDigits()
        {
            if (_digitsEllipse is null)
            {
                return;
            }

            var borderWidth = 25;
            var borderHeight = 25;

            var digitsBounds = GetBounds(_digitsEllipse);
            double width = digitsBounds.Width - borderWidth;
            double height = digitsBounds.Height - borderHeight;
            double rx = width / 2.0;
            double ry = height / 2.0;
            double cx = digitsBounds.Center.X;
            double cy = digitsBounds.Center.Y;
            double startAngle = -Math.PI / 2;
            double step = 2.0 * Math.PI / digits.Count;

            for (int i = 0; i < digits.Count; i++)
            {
                var border = digits[i].BorderControl;
                if (border is { })
                {
                    double x = rx * Math.Cos(startAngle) + cx;
                    double y = ry * Math.Sin(startAngle) + cy;
                    Canvas.SetLeft(border, x - border.Width / 2);
                    Canvas.SetTop(border, y - border.Height / 2);
                }

                var text = digits[i].TextControl;
                if (text is { })
                {
                    text.Text = digits[i].Value.ToString(CultureInfo.InvariantCulture);
                }

                startAngle += step;
            }
        }

        private void InvalidateControls(double angle)
        {
            if (_backgroundEllipse is null)
            {
                return;
            }

            var controlWidth = 25;
            var controlHeight = 25;
            int digitsCount = digits.Count;
            int minusPosition = 8;
            int plusPosition = 1;
            int cursorPosition = 0;

            var backgroundBounds = GetBounds(_backgroundEllipse);
            double width = backgroundBounds.Width - controlWidth;
            double height = backgroundBounds.Height - controlHeight;
            double rx = width / 2.0;
            double ry = height / 2.0;
            double cx = backgroundBounds.Center.X;
            double cy = backgroundBounds.Center.Y;
            double startAngle = -Math.PI / 2 + angle;
            double step = 2.0 * Math.PI / digitsCount;

            double angleMinus = startAngle + step * minusPosition;
            double mpx = rx * Math.Cos(angleMinus) + cx;
            double mpy = ry * Math.Sin(angleMinus) + cy;

            if (_minusPanel is { })
            {
                Canvas.SetLeft(_minusPanel, mpx - _minusPanel.Width / 2);
                Canvas.SetTop(_minusPanel, mpy - _minusPanel.Height / 2);
            }

            double anglePlus = startAngle + step * plusPosition;
            double ppx = rx * Math.Cos(anglePlus) + cx;
            double ppy = ry * Math.Sin(anglePlus) + cy;

            if (_plusPanel is { })
            {
                Canvas.SetLeft(_plusPanel, ppx - _plusPanel.Width / 2);
                Canvas.SetTop(_plusPanel, ppy - _plusPanel.Height / 2);
            }

            double angleCursor = startAngle + step * cursorPosition;
            double cpx = rx * Math.Cos(angleCursor) + cx;
            double cpy = ry * Math.Sin(angleCursor) + cy;

            if (_cursorPanel is { })
            {
                Canvas.SetLeft(_cursorPanel, cpx - _cursorPanel.Width / 2);
                Canvas.SetTop(_cursorPanel, cpy - _cursorPanel.Height / 2);

                _cursorPanel.RenderTransform = new RotateTransform(angle * (180.0 / Math.PI));
            }
        }

        private void InvalidateValueText(double value)
        {
            if (_valueText is null)
            {
                return;
            }

            var havePrefix = string.IsNullOrWhiteSpace(_valuePrefix);
            var haveUnit = string.IsNullOrWhiteSpace(_valueUnit);
            var valueSeparator = !havePrefix && !haveUnit ? "" : " ";

            _valueText.Text = $"{value.ToString("0.###", CultureInfo.InvariantCulture)}{valueSeparator}{_valuePrefix}{_valueUnit}";
        }

        private void Invalidate(double value)
        {
            var angle = GetAngleFromValue(value);
            ResetSelectedDigit();
            TryToSnapExact(value);
            InvalidateControls(angle);
            InvalidateValueText(value);  
        }
    }
}
