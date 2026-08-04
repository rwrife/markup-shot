using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using MarkupShot.Core;
using Microsoft.Win32;

namespace MarkupShot;

public partial class MainWindow : Window
{
    private readonly MarkupDocument _document = new();
    private BitmapSource? _currentBitmap;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void OpenMenuItem_Click(object sender, RoutedEventArgs e) => OpenWithDialog();

    private void SaveAsMenuItem_Click(object sender, RoutedEventArgs e) => SaveWithDialog();

    private void CopyMenuItem_Click(object sender, RoutedEventArgs e) => CopyToClipboard();

    private void ExitMenuItem_Click(object sender, RoutedEventArgs e) => Close();

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
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

        var extension = Path.GetExtension(dialog.FileName).ToLowerInvariant();
        BitmapEncoder encoder = extension is ".jpg" or ".jpeg"
            ? new JpegBitmapEncoder { QualityLevel = 92 }
            : new PngBitmapEncoder();

        encoder.Frames.Add(BitmapFrame.Create(_currentBitmap));

        using var output = File.Create(dialog.FileName);
        encoder.Save(output);

        StatusTextBlock.Text = $"Saved {Path.GetFileName(dialog.FileName)}";
    }

    private void CopyToClipboard()
    {
        if (_currentBitmap is null)
        {
            MessageBox.Show(this, "Open an image before copying.", "markup-shot", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        Clipboard.SetImage(_currentBitmap);
        StatusTextBlock.Text = "Copied image to clipboard.";
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
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Could not open image", MessageBoxButton.OK, MessageBoxImage.Error);
        }
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
}
