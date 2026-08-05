using System.IO;
using System.Windows;
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

    private readonly MarkupDocument _document = new();
    private BitmapSource? _currentBitmap;

    private CanvasInteractionMode _interactionMode = CanvasInteractionMode.None;
    private AnnotationHandle _activeHandle = AnnotationHandle.None;
    private Point _lastPointer;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void OpenMenuItem_Click(object sender, RoutedEventArgs e) => OpenWithDialog();

    private void SaveAsMenuItem_Click(object sender, RoutedEventArgs e) => SaveWithDialog();

    private void CopyMenuItem_Click(object sender, RoutedEventArgs e) => CopyToClipboard();

    private void ExitMenuItem_Click(object sender, RoutedEventArgs e) => Close();

    private void AddRectangleButton_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureImageLoaded())
        {
            return;
        }

        var bounds = BuildSeedBounds();
        var annotation = new RectangleAnnotation(bounds, DefaultStrokeHex, DefaultStrokeThickness);
        _document.AddAnnotation(annotation);
        _document.SelectAnnotation(annotation.Id);
        RenderDocument();
        StatusTextBlock.Text = "Added rectangle annotation.";
    }

    private void AddEllipseButton_Click(object sender, RoutedEventArgs e)
    {
        if (!EnsureImageLoaded())
        {
            return;
        }

        var bounds = BuildSeedBounds();
        var annotation = new EllipseAnnotation(bounds, DefaultStrokeHex, DefaultStrokeThickness);
        _document.AddAnnotation(annotation);
        _document.SelectAnnotation(annotation.Id);
        RenderDocument();
        StatusTextBlock.Text = "Added ellipse annotation.";
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
            DefaultStrokeHex,
            DefaultStrokeThickness);

        _document.AddAnnotation(annotation);
        _document.SelectAnnotation(annotation.Id);
        RenderDocument();
        StatusTextBlock.Text = "Added line annotation.";
    }

    private void DeleteSelectedButton_Click(object sender, RoutedEventArgs e) => DeleteSelectedAnnotation();

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
            RenderDocument();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Could not open image", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void DeleteSelectedAnnotation()
    {
        if (_document.SelectedAnnotationId is not Guid selectedId)
        {
            return;
        }

        if (_document.RemoveAnnotation(selectedId))
        {
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
        var stroke = BuildBrushForAnnotation(annotation);
        var thickness = annotation switch
        {
            RectangularAnnotationBase rectangular => rectangular.StrokeThickness,
            LineAnnotation line => line.StrokeThickness,
            _ => DefaultStrokeThickness
        };

        if (isSelected)
        {
            thickness += 1;
        }

        return annotation switch
        {
            RectangleAnnotation rectangle => BuildRectangleVisual(rectangle, stroke, thickness),
            EllipseAnnotation ellipse => BuildEllipseVisual(ellipse, stroke, thickness),
            LineAnnotation line => BuildLineVisual(line, stroke, thickness),
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

    private static Brush BuildBrushForAnnotation(IAnnotation annotation)
    {
        var fallback = Brushes.OrangeRed;

        var hex = annotation switch
        {
            RectangularAnnotationBase rectangular => rectangular.StrokeHex,
            LineAnnotation line => line.StrokeHex,
            _ => string.Empty
        };

        if (string.IsNullOrWhiteSpace(hex))
        {
            return fallback;
        }

        return ColorConverter.ConvertFromString(hex) is Color color
            ? new SolidColorBrush(color)
            : fallback;
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

    private AnnotationRect BuildSeedBounds()
    {
        var width = Math.Max(120d, EditorCanvas.Width * 0.25d);
        var height = Math.Max(80d, EditorCanvas.Height * 0.15d);
        var x = Math.Max(0d, (EditorCanvas.Width - width) / 2d);
        var y = Math.Max(0d, (EditorCanvas.Height - height) / 2d);
        return new AnnotationRect(x, y, width, height);
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

    private string BuildDefaultSaveName()
    {
        if (string.IsNullOrWhiteSpace(_document.SourcePath))
        {
            return "markup-shot-output.png";
        }

        var baseName = Path.GetFileNameWithoutExtension(_document.SourcePath);
        return $"{baseName}-annotated.png";
    }

    private enum CanvasInteractionMode
    {
        None,
        Move,
        Resize
    }
}
