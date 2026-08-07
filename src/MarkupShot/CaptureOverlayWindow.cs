using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using DrawingPoint = System.Drawing.Point;
using DrawingRectangle = System.Drawing.Rectangle;

namespace MarkupShot;

internal sealed class CaptureOverlayWindow : Window
{
    private readonly Canvas _overlayCanvas;
    private readonly Border _selectionBorder;

    private bool _isSelecting;
    private Point _startCanvasPoint;
    private DrawingPoint _startScreenPoint;

    public CaptureOverlayWindow()
    {
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        Topmost = true;
        ShowInTaskbar = false;
        Cursor = Cursors.Cross;

        Left = SystemParameters.VirtualScreenLeft;
        Top = SystemParameters.VirtualScreenTop;
        Width = SystemParameters.VirtualScreenWidth;
        Height = SystemParameters.VirtualScreenHeight;

        var root = new Grid
        {
            Background = new SolidColorBrush(Color.FromArgb(56, 0, 0, 0))
        };

        _overlayCanvas = new Canvas
        {
            Background = Brushes.Transparent
        };

        _selectionBorder = new Border
        {
            BorderBrush = Brushes.DeepSkyBlue,
            BorderThickness = new Thickness(2),
            Background = new SolidColorBrush(Color.FromArgb(36, 30, 144, 255)),
            Visibility = Visibility.Collapsed
        };

        _overlayCanvas.Children.Add(_selectionBorder);
        root.Children.Add(_overlayCanvas);
        Content = root;

        Loaded += (_, _) =>
        {
            Activate();
            Focus();
        };

        PreviewKeyDown += CaptureOverlayWindow_PreviewKeyDown;
        _overlayCanvas.MouseLeftButtonDown += OverlayCanvas_MouseLeftButtonDown;
        _overlayCanvas.MouseMove += OverlayCanvas_MouseMove;
        _overlayCanvas.MouseLeftButtonUp += OverlayCanvas_MouseLeftButtonUp;
    }

    public DrawingRectangle? SelectedRegion { get; private set; }

    private void CaptureOverlayWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
        {
            return;
        }

        SelectedRegion = null;
        DialogResult = false;
        Close();
        e.Handled = true;
    }

    private void OverlayCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isSelecting = true;
        _startCanvasPoint = e.GetPosition(_overlayCanvas);
        _startScreenPoint = GetCursorPosition();

        _selectionBorder.Visibility = Visibility.Visible;
        _overlayCanvas.CaptureMouse();
        UpdateSelectionVisual(_startCanvasPoint, _startCanvasPoint);
        e.Handled = true;
    }

    private void OverlayCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isSelecting)
        {
            return;
        }

        var current = e.GetPosition(_overlayCanvas);
        UpdateSelectionVisual(_startCanvasPoint, current);
        e.Handled = true;
    }

    private void OverlayCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isSelecting)
        {
            return;
        }

        _isSelecting = false;
        _overlayCanvas.ReleaseMouseCapture();

        var endScreenPoint = GetCursorPosition();
        var selected = Normalize(_startScreenPoint, endScreenPoint);

        if (selected.Width < 2 || selected.Height < 2)
        {
            SelectedRegion = null;
            DialogResult = false;
            Close();
            return;
        }

        SelectedRegion = selected;
        DialogResult = true;
        Close();
        e.Handled = true;
    }

    private void UpdateSelectionVisual(Point start, Point current)
    {
        var left = Math.Min(start.X, current.X);
        var top = Math.Min(start.Y, current.Y);
        var width = Math.Abs(current.X - start.X);
        var height = Math.Abs(current.Y - start.Y);

        Canvas.SetLeft(_selectionBorder, left);
        Canvas.SetTop(_selectionBorder, top);
        _selectionBorder.Width = width;
        _selectionBorder.Height = height;
    }

    private static DrawingRectangle Normalize(DrawingPoint start, DrawingPoint end)
    {
        var left = Math.Min(start.X, end.X);
        var top = Math.Min(start.Y, end.Y);
        var width = Math.Abs(end.X - start.X);
        var height = Math.Abs(end.Y - start.Y);
        return new DrawingRectangle(left, top, width, height);
    }

    private static DrawingPoint GetCursorPosition()
    {
        if (!GetCursorPos(out var point))
        {
            throw new InvalidOperationException("Unable to read cursor position for capture.");
        }

        return new DrawingPoint(point.X, point.Y);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetCursorPos(out NativePoint lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }
}
