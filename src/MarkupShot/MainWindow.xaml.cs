using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MarkupShot.Core;
using Microsoft.Win32;

namespace MarkupShot;

public partial class MainWindow : Window
{
    private const string DefaultStrokeHex = "#FFFF4F4F";
    private const double DefaultStrokeThickness = 2d;
    private const double DefaultTextFontSize = 18d;
    private const string DefaultTextCallout = "Add note here";

    private readonly MarkupDocument _document = new();
    private BitmapSource? _currentBitmap;

    private CanvasInteractionMode _interactionMode = CanvasInteractionMode.None;
    private AnnotationHandle _activeHandle = AnnotationHandle.None;
    private Point _lastPointer;

    private bool _suppressStyleEvents;
    private bool _suppressTextEvents;

    public MainWindow()
    {
        InitializeComponent();
        TextContentTextBox.Text = DefaultTextCallout;
    }

    private void OpenMenuItem_Click(object sender, RoutedEventArgs e) => OpenWithDialog();

    private void SaveAsMenuItem_Click(object sender, RoutedEventArgs e) => SaveWithDialog();

    private void CopyMenuItem_Click(object sender, RoutedEventArgs e) => CopyToClipboard();

    private void ExitMenuItem_Click(object sender, RoutedEventArgs e) => Close();

    private void AddArrowButton_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureImageLoaded())
        {
            return;
        }

        var bounds = BuildSeedBounds();
        var annotation = new ArrowAnnotation(
            new AnnotationPoint(bounds.Left, bounds.Center.Y),
            new AnnotationPoint(bounds.Right, bounds.Center.Y),
            GetSelectedStrokeHex(),
            GetSelectedStrokeThickness());

        AddAndSelectAnnotation(annotation, "Added arrow annotation.");
    }

    private void AddLineButton_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureImageLoaded())
        {
            return;
        }

        var bounds = BuildSeedBounds();
        var annotation = new LineAnnotation(
            new AnnotationPoint(bounds.Left, bounds.Top),
            new AnnotationPoint(bounds.Right, bounds.Bottom),
            GetSelectedStrokeHex(),
            GetSelectedStrokeThickness());

        AddAndSelectAnnotation(annotation, "Added line annotation.");
    }

    private void AddRectangleButton_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureImageLoaded())
        {
            return;
        }

        var annotation = new RectangleAnnotation(
            BuildSeedBounds(),
            GetSelectedStrokeHex(),
            GetSelectedStrokeThickness());

        AddAndSelectAnnotation(annotation, "Added rectangle annotation.");
    }

    private void AddEllipseButton_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureImageLoaded())
        {
            return;
        }

        var annotation = new EllipseAnnotation(
            BuildSeedBounds(),
            GetSelectedStrokeHex(),
            GetSelectedStrokeThickness());

        AddAndSelectAnnotation(annotation, "Added ellipse annotation.");
    }

    private void AddInkButton_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureImageLoaded())
        {
            return;
        }

        var annotation = new InkAnnotation(
            BuildSeedInkPoints(),
            GetSelectedStrokeHex(),
            GetSelectedStrokeThickness());

        AddAndSelectAnnotation(annotation, "Added freehand ink annotation.");
    }

    private void AddTextButton_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureImageLoaded())
        {
            return;
        }

        var text = string.IsNullOrWhiteSpace(TextContentTextBox.Text)
            ? DefaultTextCallout
            : TextContentTextBox.Text;

        var annotation = new TextAnnotation(
            BuildSeedBounds(),
            text,
            GetSelectedFontSize(),
            GetSelectedStrokeHex(),
            Math.Max(1d, GetSelectedStrokeThickness() / 2d));

        AddAndSelectAnnotation(annotation, "Added text callout annotation.");
    }

    private void AddHighlighterButton_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureImageLoaded())
        {
            return;
        }

        var annotation = new HighlighterAnnotation(
            BuildSeedBounds(),
            GetSelectedStrokeHex(),
            Math.Max(1d, GetSelectedStrokeThickness() / 2d),
            fillOpacity: 0.35d);

        AddAndSelectAnnotation(annotation, "Added highlighter annotation.");
    }

    private void DeleteSelectedButton_Click(object sender, RoutedEventArgs e) => DeleteSelectedAnnotation();

    private void StyleControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressStyleEvents || !IsLoaded)
        {
            return;
        }

        if (_document.SelectedAnnotationId is not Guid selectedId
            || !_document.TryGetAnnotation(selectedId, out var selectedAnnotation))
        {
            return;
        }

        selectedAnnotation.SetStroke(GetSelectedStrokeHex(), GetSelectedStrokeThickness());
        RenderDocument();
    }

    private void FontSizeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressTextEvents || !IsLoaded)
        {
            return;
        }

        if (_document.SelectedAnnotationId is not Guid selectedId
            || !_document.TryGetAnnotation(selectedId, out var selectedAnnotation)
            || selectedAnnotation is not TextAnnotation textAnnotation)
        {
            return;
        }

        textAnnotation.SetFontSize(GetSelectedFontSize());
        RenderDocument();
    }

    private void TextContentTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppressTextEvents || !IsLoaded)
        {
            return;
        }

        if (_document.SelectedAnnotationId is not Guid selectedId
            || !_document.TryGetAnnotation(selectedId, out var selectedAnnotation)
            || selectedAnnotation is not TextAnnotation textAnnotation)
        {
            return;
        }

        textAnnotation.SetText(TextContentTextBox.Text);
        RenderDocument();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete)
        {
            DeleteSelectedAnnotation();
            e.Handled = true;
            return;
        }

        if ((Keyboard.Modifiers & ModifierKeys.Control) == 0)
        {
            return;
        }

        if (e.Key == Key.O)
        {
            OpenWithDialog();
            e.Handled = true;
        }
        else if (e.Key == Key.S)
        {
            SaveWithDialog();
            e.Handled = true;
        }
        else if (e.Key == Key.C)
        {
            CopyToClipboard();
            e.Handled = true;
        }
    }

    private void Window_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            return;
        }

        if (e.Data.GetData(DataFormats.FileDrop) is not string[] files || files.Length == 0)
        {
            return;
        }

        TryLoadImage(files[0]);
    }

    private void EditorCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (!_document.HasImage)
        {
            return;
        }

        var point = e.GetPosition(EditorCanvas);

        if (_document.SelectedAnnotationId is Guid selectedId
            && _document.TryGetAnnotation(selectedId, out var selectedAnnotation))
        {
            var handle = selectedAnnotation.HitTestHandle(ToAnnotationPoint(point), handleRadius: 8d);
            if (handle != AnnotationHandle.None)
            {
                _interactionMode = CanvasInteractionMode.Resize;
                _activeHandle = handle;
                _lastPointer = point;
                EditorCanvas.CaptureMouse();
                e.Handled = true;
                return;
            }
        }

        if (_document.SelectByPoint(ToAnnotationPoint(point), tolerance: 6d))
        {
            _interactionMode = CanvasInteractionMode.Move;
            _lastPointer = point;
            EditorCanvas.CaptureMouse();
        }
        else
        {
            _document.ClearSelection();
            _interactionMode = CanvasInteractionMode.None;
            _activeHandle = AnnotationHandle.None;
        }

        SyncControlsFromSelection();
        RenderDocument();
        e.Handled = true;
    }

    private void EditorCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (!EditorCanvas.IsMouseCaptured || _interactionMode == CanvasInteractionMode.None)
        {
            return;
        }

        var point = e.GetPosition(EditorCanvas);
        var dx = point.X - _lastPointer.X;
        var dy = point.Y - _lastPointer.Y;

        if (Math.Abs(dx) < 0.001 && Math.Abs(dy) < 0.001)
        {
            return;
        }

        if (_interactionMode == CanvasInteractionMode.Move)
        {
            _document.MoveSelected(dx, dy);
        }
        else if (_interactionMode == CanvasInteractionMode.Resize)
        {
            _document.ResizeSelected(_activeHandle, dx, dy);
        }

        _lastPointer = point;
        RenderDocument();
        e.Handled = true;
    }

    private void EditorCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (EditorCanvas.IsMouseCaptured)
        {
            EditorCanvas.ReleaseMouseCapture();
        }

        _interactionMode = CanvasInteractionMode.None;
        _activeHandle = AnnotationHandle.None;
        e.Handled = true;
    }

    private void OpenWithDialog()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Image files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) == true)
        {
            TryLoadImage(dialog.FileName);
        }
    }

    private void SaveWithDialog()
    {
        if (_currentBitmap is null || !_document.HasImage)
        {
            MessageBox.Show(this, "Open an image before saving.", "markup-shot", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "PNG image (*.png)|*.png|JPEG image (*.jpg;*.jpeg)|*.jpg;*.jpeg",
            DefaultExt = "png",
            AddExtension = true,
            FileName = BuildDefaultSaveName()
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        var rendered = RenderDocumentBitmap(includeSelectionHandles: false);
        if (rendered is null)
        {
            return;
        }

        var extension = Path.GetExtension(dialog.FileName).ToLowerInvariant();
        BitmapEncoder encoder = extension is ".jpg" or ".jpeg"
            ? new JpegBitmapEncoder { QualityLevel = 92 }
            : new PngBitmapEncoder();

        encoder.Frames.Add(BitmapFrame.Create(rendered));

        using var output = File.Create(dialog.FileName);
        encoder.Save(output);

        RenderDocument();
        StatusTextBlock.Text = $"Saved {Path.GetFileName(dialog.FileName)}";
    }

    private void CopyToClipboard()
    {
        if (_currentBitmap is null || !_document.HasImage)
        {
            MessageBox.Show(this, "Open an image before copying.", "markup-shot", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var rendered = RenderDocumentBitmap(includeSelectionHandles: false);
        if (rendered is null)
        {
            return;
        }

        Clipboard.SetImage(rendered);
        RenderDocument();
        StatusTextBlock.Text = "Copied annotated image to clipboard.";
    }

    private void TryLoadImage(string path)
    {
        try
        {
            var image = CanvasImage.Load(path);
            _document.SetImage(image, path);

            using var input = new MemoryStream(image.Bytes.ToArray(), writable: false);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
            bitmap.StreamSource = input;
            bitmap.EndInit();
            bitmap.Freeze();

            _currentBitmap = bitmap;
            BaseImageElement.Source = bitmap;
            BaseImageElement.Width = image.Width;
            BaseImageElement.Height = image.Height;
            EditorCanvas.Width = image.Width;
            EditorCanvas.Height = image.Height;

            Title = $"markup-shot — {Path.GetFileName(path)}";
            StatusTextBlock.Text = $"Loaded {Path.GetFileName(path)} ({image.Width}x{image.Height})";
            SyncControlsFromSelection();
            RenderDocument();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Could not open image", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void AddAndSelectAnnotation(IAnnotation annotation, string statusMessage)
    {
        _document.AddAnnotation(annotation);
        _document.SelectAnnotation(annotation.Id);
        SyncControlsFromSelection();
        RenderDocument();
        StatusTextBlock.Text = statusMessage;
    }

    private void DeleteSelectedAnnotation()
    {
        if (_document.SelectedAnnotationId is not Guid selectedId)
        {
            return;
        }

        if (_document.RemoveAnnotation(selectedId))
        {
            SyncControlsFromSelection();
            RenderDocument();
            StatusTextBlock.Text = "Deleted selected annotation.";
        }
    }

    private void RenderDocument(bool includeSelectionHandles = true)
    {
        EditorCanvas.Children.Clear();
        EditorCanvas.Children.Add(BaseImageElement);

        foreach (var annotation in _document.Annotations)
        {
            var isSelected = _document.SelectedAnnotationId == annotation.Id;
            EditorCanvas.Children.Add(BuildAnnotationVisual(annotation, isSelected));
        }

        if (includeSelectionHandles
            && _document.SelectedAnnotationId is Guid selectedId
            && _document.TryGetAnnotation(selectedId, out var selectedAnnotation))
        {
            foreach (var handlePoint in GetHandlePoints(selectedAnnotation))
            {
                var handleRect = new Rectangle
                {
                    Width = 8,
                    Height = 8,
                    Fill = Brushes.White,
                    Stroke = Brushes.DodgerBlue,
                    StrokeThickness = 1,
                    IsHitTestVisible = false
                };

                Canvas.SetLeft(handleRect, handlePoint.X - 4);
                Canvas.SetTop(handleRect, handlePoint.Y - 4);
                EditorCanvas.Children.Add(handleRect);
            }
        }
    }

    private UIElement BuildAnnotationVisual(IAnnotation annotation, bool isSelected)
    {
        var color = ParseColor(GetStrokeHex(annotation), Colors.OrangeRed);
        var strokeBrush = new SolidColorBrush(color);
        var thickness = Math.Max(1d, GetStrokeThickness(annotation));

        if (isSelected)
        {
            thickness += 1d;
        }

        return annotation switch
        {
            RectangleAnnotation rectangle => BuildRectangleVisual(rectangle, strokeBrush, thickness),
            EllipseAnnotation ellipse => BuildEllipseVisual(ellipse, strokeBrush, thickness),
            LineAnnotation line => BuildLineVisual(line, strokeBrush, thickness),
            ArrowAnnotation arrow => BuildArrowVisual(arrow, strokeBrush, thickness),
            InkAnnotation ink => BuildInkVisual(ink, strokeBrush, thickness),
            TextAnnotation text => BuildTextVisual(text, color, thickness, isSelected),
            HighlighterAnnotation highlighter => BuildHighlighterVisual(highlighter, color, thickness, isSelected),
            _ => throw new InvalidOperationException($"Unknown annotation type: {annotation.GetType().Name}")
        };
    }

    private static UIElement BuildRectangleVisual(RectangleAnnotation rectangle, Brush stroke, double thickness)
    {
        var shape = new Rectangle
        {
            Width = rectangle.Bounds.Width,
            Height = rectangle.Bounds.Height,
            Stroke = stroke,
            StrokeThickness = thickness,
            Fill = Brushes.Transparent
        };

        Canvas.SetLeft(shape, rectangle.Bounds.X);
        Canvas.SetTop(shape, rectangle.Bounds.Y);
        return shape;
    }

    private static UIElement BuildEllipseVisual(EllipseAnnotation ellipse, Brush stroke, double thickness)
    {
        var shape = new Ellipse
        {
            Width = ellipse.Bounds.Width,
            Height = ellipse.Bounds.Height,
            Stroke = stroke,
            StrokeThickness = thickness,
            Fill = Brushes.Transparent
        };

        Canvas.SetLeft(shape, ellipse.Bounds.X);
        Canvas.SetTop(shape, ellipse.Bounds.Y);
        return shape;
    }

    private static UIElement BuildLineVisual(LineAnnotation line, Brush stroke, double thickness) =>
        new Line
        {
            X1 = line.Start.X,
            Y1 = line.Start.Y,
            X2 = line.End.X,
            Y2 = line.End.Y,
            Stroke = stroke,
            StrokeThickness = thickness,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round
        };

    private static UIElement BuildArrowVisual(ArrowAnnotation arrow, Brush stroke, double thickness)
    {
        var container = new Canvas { IsHitTestVisible = false };

        var line = new Line
        {
            X1 = arrow.Start.X,
            Y1 = arrow.Start.Y,
            X2 = arrow.End.X,
            Y2 = arrow.End.Y,
            Stroke = stroke,
            StrokeThickness = thickness,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round
        };

        var directionX = arrow.End.X - arrow.Start.X;
        var directionY = arrow.End.Y - arrow.Start.Y;
        var length = Math.Sqrt(directionX * directionX + directionY * directionY);

        if (length < 0.001)
        {
            directionX = 1d;
            directionY = 0d;
            length = 1d;
        }

        directionX /= length;
        directionY /= length;

        var arrowLength = Math.Clamp(thickness * 5d, 10d, 26d);
        var arrowHalfWidth = arrowLength * 0.45d;

        var baseX = arrow.End.X - directionX * arrowLength;
        var baseY = arrow.End.Y - directionY * arrowLength;

        var perpX = -directionY;
        var perpY = directionX;

        var left = new Point(baseX + perpX * arrowHalfWidth, baseY + perpY * arrowHalfWidth);
        var right = new Point(baseX - perpX * arrowHalfWidth, baseY - perpY * arrowHalfWidth);

        var head = new Polygon
        {
            Fill = stroke,
            Stroke = stroke,
            StrokeThickness = Math.Max(1d, thickness / 2d),
            Points = new PointCollection
            {
                new(arrow.End.X, arrow.End.Y),
                left,
                right
            }
        };

        container.Children.Add(line);
        container.Children.Add(head);
        return container;
    }

    private static UIElement BuildInkVisual(InkAnnotation ink, Brush stroke, double thickness)
    {
        if (ink.Points.Count == 1)
        {
            var point = ink.Points[0];
            var dotDiameter = Math.Max(thickness, 2d);
            var ellipse = new Ellipse
            {
                Width = dotDiameter,
                Height = dotDiameter,
                Fill = stroke,
                Stroke = stroke,
                StrokeThickness = 1
            };

            Canvas.SetLeft(ellipse, point.X - dotDiameter / 2d);
            Canvas.SetTop(ellipse, point.Y - dotDiameter / 2d);
            return ellipse;
        }

        return new Polyline
        {
            Stroke = stroke,
            StrokeThickness = thickness,
            StrokeLineJoin = PenLineJoin.Round,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            Points = new PointCollection(ink.Points.Select(point => new Point(point.X, point.Y)))
        };
    }

    private static UIElement BuildTextVisual(TextAnnotation text, Color strokeColor, double thickness, bool isSelected)
    {
        var border = new Border
        {
            Width = Math.Max(60d, text.Bounds.Width),
            Height = Math.Max(text.FontSize + 14d, text.Bounds.Height),
            Background = new SolidColorBrush(Color.FromArgb(96, 0, 0, 0)),
            BorderBrush = isSelected
                ? Brushes.DodgerBlue
                : new SolidColorBrush(Color.FromArgb(180, strokeColor.R, strokeColor.G, strokeColor.B)),
            BorderThickness = new Thickness(Math.Max(1d, thickness / 2d)),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(6)
        };

        border.Child = new TextBlock
        {
            Text = text.Text,
            Foreground = new SolidColorBrush(strokeColor),
            FontSize = text.FontSize,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Top
        };

        Canvas.SetLeft(border, text.Bounds.X);
        Canvas.SetTop(border, text.Bounds.Y);
        return border;
    }

    private static UIElement BuildHighlighterVisual(HighlighterAnnotation highlighter, Color strokeColor, double thickness, bool isSelected)
    {
        var fillAlpha = (byte)Math.Round(Math.Clamp(highlighter.FillOpacity, 0d, 1d) * 255d);
        var fill = new SolidColorBrush(Color.FromArgb(fillAlpha, strokeColor.R, strokeColor.G, strokeColor.B));

        var borderAlpha = isSelected ? (byte)255 : (byte)200;
        var stroke = new SolidColorBrush(Color.FromArgb(borderAlpha, strokeColor.R, strokeColor.G, strokeColor.B));

        var shape = new Rectangle
        {
            Width = highlighter.Bounds.Width,
            Height = highlighter.Bounds.Height,
            Fill = fill,
            Stroke = stroke,
            StrokeThickness = Math.Max(1d, thickness / 2d),
            RadiusX = 2,
            RadiusY = 2
        };

        Canvas.SetLeft(shape, highlighter.Bounds.X);
        Canvas.SetTop(shape, highlighter.Bounds.Y);
        return shape;
    }

    private BitmapSource? RenderDocumentBitmap(bool includeSelectionHandles)
    {
        if (!_document.HasImage)
        {
            return null;
        }

        RenderDocument(includeSelectionHandles);

        var pixelWidth = Math.Max(1, (int)Math.Ceiling(EditorCanvas.Width));
        var pixelHeight = Math.Max(1, (int)Math.Ceiling(EditorCanvas.Height));

        EditorCanvas.Measure(new Size(EditorCanvas.Width, EditorCanvas.Height));
        EditorCanvas.Arrange(new Rect(0, 0, EditorCanvas.Width, EditorCanvas.Height));

        var renderTarget = new RenderTargetBitmap(pixelWidth, pixelHeight, 96, 96, PixelFormats.Pbgra32);
        renderTarget.Render(EditorCanvas);
        renderTarget.Freeze();
        return renderTarget;
    }

    private bool EnsureImageLoaded()
    {
        if (_document.HasImage)
        {
            return true;
        }

        MessageBox.Show(this, "Open an image before adding annotations.", "markup-shot", MessageBoxButton.OK, MessageBoxImage.Information);
        return false;
    }

    private void SyncControlsFromSelection()
    {
        _suppressStyleEvents = true;
        _suppressTextEvents = true;

        try
        {
            if (_document.SelectedAnnotationId is Guid selectedId
                && _document.TryGetAnnotation(selectedId, out var annotation))
            {
                SelectComboBoxItemByTag(ColorComboBox, GetStrokeHex(annotation));
                SelectNearestNumericComboBoxItem(StrokeThicknessComboBox, GetStrokeThickness(annotation));

                if (annotation is TextAnnotation textAnnotation)
                {
                    TextContentTextBox.Text = textAnnotation.Text;
                    SelectNearestNumericComboBoxItem(FontSizeComboBox, textAnnotation.FontSize);
                }
            }
        }
        finally
        {
            _suppressStyleEvents = false;
            _suppressTextEvents = false;
        }
    }

    private AnnotationRect BuildSeedBounds()
    {
        var width = Math.Max(120d, EditorCanvas.Width * 0.25d);
        var height = Math.Max(80d, EditorCanvas.Height * 0.15d);
        var x = Math.Max(0d, (EditorCanvas.Width - width) / 2d);
        var y = Math.Max(0d, (EditorCanvas.Height - height) / 2d);
        return new AnnotationRect(x, y, width, height);
    }

    private IReadOnlyList<AnnotationPoint> BuildSeedInkPoints()
    {
        var seed = BuildSeedBounds();

        return
        [
            new AnnotationPoint(seed.Left, seed.Bottom - seed.Height * 0.2d),
            new AnnotationPoint(seed.Left + seed.Width * 0.2d, seed.Top + seed.Height * 0.15d),
            new AnnotationPoint(seed.Left + seed.Width * 0.45d, seed.Bottom - seed.Height * 0.1d),
            new AnnotationPoint(seed.Left + seed.Width * 0.7d, seed.Top + seed.Height * 0.2d),
            new AnnotationPoint(seed.Right, seed.Center.Y)
        ];
    }

    private string BuildDefaultSaveName()
    {
        if (string.IsNullOrWhiteSpace(_document.SourcePath))
        {
            return "markup-shot-output.png";
        }

        var baseName = Path.GetFileNameWithoutExtension(_document.SourcePath);
        return $"{baseName}-annotated.png";
    }

    private string GetSelectedStrokeHex() =>
        GetSelectedTagValue(ColorComboBox) ?? DefaultStrokeHex;

    private double GetSelectedStrokeThickness() =>
        ParseTagDouble(StrokeThicknessComboBox, DefaultStrokeThickness);

    private double GetSelectedFontSize() =>
        ParseTagDouble(FontSizeComboBox, DefaultTextFontSize);

    private static string GetStrokeHex(IAnnotation annotation) => annotation switch
    {
        RectangularAnnotationBase rectangular => rectangular.StrokeHex,
        LineAnnotation line => line.StrokeHex,
        ArrowAnnotation arrow => arrow.StrokeHex,
        InkAnnotation ink => ink.StrokeHex,
        _ => DefaultStrokeHex
    };

    private static double GetStrokeThickness(IAnnotation annotation) => annotation switch
    {
        RectangularAnnotationBase rectangular => rectangular.StrokeThickness,
        LineAnnotation line => line.StrokeThickness,
        ArrowAnnotation arrow => arrow.StrokeThickness,
        InkAnnotation ink => ink.StrokeThickness,
        _ => DefaultStrokeThickness
    };

    private static string? GetSelectedTagValue(ComboBox comboBox) =>
        (comboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();

    private static double ParseTagDouble(ComboBox comboBox, double fallback)
    {
        var tag = GetSelectedTagValue(comboBox);
        return double.TryParse(tag, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : fallback;
    }

    private static Color ParseColor(string hex, Color fallback)
    {
        if (ColorConverter.ConvertFromString(hex) is Color color)
        {
            return color;
        }

        return fallback;
    }

    private static void SelectComboBoxItemByTag(ComboBox comboBox, string tag)
    {
        foreach (var item in comboBox.Items.OfType<ComboBoxItem>())
        {
            if (string.Equals(item.Tag?.ToString(), tag, StringComparison.OrdinalIgnoreCase))
            {
                comboBox.SelectedItem = item;
                return;
            }
        }
    }

    private static void SelectNearestNumericComboBoxItem(ComboBox comboBox, double value)
    {
        ComboBoxItem? bestItem = null;
        var bestDistance = double.MaxValue;

        foreach (var item in comboBox.Items.OfType<ComboBoxItem>())
        {
            if (!double.TryParse(item.Tag?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var itemValue))
            {
                continue;
            }

            var distance = Math.Abs(itemValue - value);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestItem = item;
            }
        }

        if (bestItem is not null)
        {
            comboBox.SelectedItem = bestItem;
        }
    }

    private static AnnotationPoint ToAnnotationPoint(Point point) =>
        new(point.X, point.Y);

    private static IEnumerable<AnnotationPoint> GetHandlePoints(IAnnotation annotation)
    {
        if (annotation is LineAnnotation line)
        {
            yield return line.Start;
            yield return line.End;
            yield break;
        }

        if (annotation is ArrowAnnotation arrow)
        {
            yield return arrow.Start;
            yield return arrow.End;
            yield break;
        }

        var bounds = annotation.Bounds;
        var center = bounds.Center;
        yield return new AnnotationPoint(bounds.Left, bounds.Top);
        yield return new AnnotationPoint(center.X, bounds.Top);
        yield return new AnnotationPoint(bounds.Right, bounds.Top);
        yield return new AnnotationPoint(bounds.Right, center.Y);
        yield return new AnnotationPoint(bounds.Right, bounds.Bottom);
        yield return new AnnotationPoint(center.X, bounds.Bottom);
        yield return new AnnotationPoint(bounds.Left, bounds.Bottom);
        yield return new AnnotationPoint(bounds.Left, center.Y);
    }

    private enum CanvasInteractionMode
    {
        None,
        Move,
        Resize
    }
}
