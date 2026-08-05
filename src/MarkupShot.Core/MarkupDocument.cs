using System.Text.Json;

namespace MarkupShot.Core;

public sealed class MarkupDocument
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly List<IAnnotation> _annotations = [];

    public CanvasImage? BaseImage { get; private set; }

    public string? SourcePath { get; private set; }

    public Guid? SelectedAnnotationId { get; private set; }

    public bool HasImage => BaseImage is not null;

    public IReadOnlyList<IAnnotation> Annotations => _annotations;

    public void SetImage(CanvasImage image, string? sourcePath = null)
    {
        BaseImage = image ?? throw new ArgumentNullException(nameof(image));
        SourcePath = sourcePath;
        _annotations.Clear();
        SelectedAnnotationId = null;
    }

    public void LoadFromFile(string path)
    {
        SetImage(CanvasImage.Load(path), path);
    }

    public void SaveCurrentImage(string path)
    {
        if (BaseImage is null)
        {
            throw new InvalidOperationException("No image loaded.");
        }

        BaseImage.Save(path);
    }

    public void AddAnnotation(IAnnotation annotation)
    {
        ArgumentNullException.ThrowIfNull(annotation);
        _annotations.Add(annotation);
    }

    public bool RemoveAnnotation(Guid id)
    {
        var index = FindIndexById(id);
        if (index < 0)
        {
            return false;
        }

        _annotations.RemoveAt(index);
        if (SelectedAnnotationId == id)
        {
            SelectedAnnotationId = null;
        }

        return true;
    }

    public bool TryGetAnnotation(Guid id, out IAnnotation annotation)
    {
        var index = FindIndexById(id);
        if (index < 0)
        {
            annotation = default!;
            return false;
        }

        annotation = _annotations[index];
        return true;
    }

    public IAnnotation? HitTestTopmost(AnnotationPoint point, double tolerance = 6d)
    {
        for (var i = _annotations.Count - 1; i >= 0; i--)
        {
            var candidate = _annotations[i];
            if (candidate.HitTest(point, tolerance))
            {
                return candidate;
            }
        }

        return null;
    }

    public bool SelectByPoint(AnnotationPoint point, double tolerance = 6d)
    {
        var hit = HitTestTopmost(point, tolerance);
        SelectedAnnotationId = hit?.Id;
        return hit is not null;
    }

    public void ClearSelection() => SelectedAnnotationId = null;

    public bool SelectAnnotation(Guid id)
    {
        if (!TryGetAnnotation(id, out _))
        {
            return false;
        }

        SelectedAnnotationId = id;
        return true;
    }

    public bool MoveSelected(double dx, double dy)
    {
        if (SelectedAnnotationId is null || !TryGetAnnotation(SelectedAnnotationId.Value, out var selected))
        {
            return false;
        }

        selected.MoveBy(dx, dy);
        return true;
    }

    public bool ResizeSelected(AnnotationHandle handle, double dx, double dy, double minimumSize = 4d)
    {
        if (SelectedAnnotationId is null || !TryGetAnnotation(SelectedAnnotationId.Value, out var selected))
        {
            return false;
        }

        selected.Resize(handle, dx, dy, minimumSize);
        return true;
    }

    public string SerializeProject()
    {
        var snapshot = new ProjectSnapshot
        {
            SourcePath = SourcePath,
            Annotations = _annotations.Select(annotation => annotation.ToSnapshot()).ToList()
        };

        return JsonSerializer.Serialize(snapshot, SerializerOptions);
    }

    public static MarkupDocument DeserializeProject(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("JSON payload must not be empty.", nameof(json));
        }

        var snapshot = JsonSerializer.Deserialize<ProjectSnapshot>(json, SerializerOptions)
            ?? throw new InvalidOperationException("Failed to parse project JSON.");

        var document = new MarkupDocument
        {
            SourcePath = snapshot.SourcePath
        };

        if (snapshot.Annotations is not null)
        {
            foreach (var annotation in snapshot.Annotations)
            {
                document.AddAnnotation(AnnotationFactory.FromSnapshot(annotation));
            }
        }

        return document;
    }

    private int FindIndexById(Guid id)
    {
        for (var i = 0; i < _annotations.Count; i++)
        {
            if (_annotations[i].Id == id)
            {
                return i;
            }
        }

        return -1;
    }

    private sealed record ProjectSnapshot
    {
        public string? SourcePath { get; init; }

        public List<AnnotationSnapshot>? Annotations { get; init; }
    }
}
