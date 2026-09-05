using System.Text;
using Microsoft.ML.OnnxRuntimeGenAI;

namespace AiFoundryLocal;

public sealed record Phi3VisionOptions(
    string ModelDirectory,
    int MaxNewTokens = 128,
    int ContextLength = 4096);

public sealed record VisionAnalysis(
    string Description,
    IReadOnlyList<string> Tags,
    string RawResponse);

public sealed class Phi3VisionModel : IDisposable
{
    private Model? _model;
    private MultiModalProcessor? _processor;
    private readonly int _maxNewTokens;
    private readonly int _contextLength;
    private readonly SemaphoreSlim _generationLock = new(1, 1);
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private readonly string _modelDirectory;

    public Phi3VisionModel(Phi3VisionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (!Directory.Exists(options.ModelDirectory))
            throw new DirectoryNotFoundException($"Phi-3 Vision model directory was not found: {options.ModelDirectory}");

        _modelDirectory = options.ModelDirectory;
        _maxNewTokens = options.MaxNewTokens;
        _contextLength = options.ContextLength;
    }

    public bool IsLoaded => _model is not null && _processor is not null;

    public async Task LoadAsync(CancellationToken ct = default)
    {
        if (IsLoaded)
            return;

        await _loadLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (IsLoaded)
                return;

            var model = await Task.Run(() => new Model(_modelDirectory), ct).ConfigureAwait(false);
            try
            {
                _model = model;
                _processor = new MultiModalProcessor(model);
            }
            catch
            {
                model.Dispose();
                throw;
            }
        }
        finally
        {
            _loadLock.Release();
        }
    }

    public Task<VisionAnalysis> AnalyzeImageAsync(
        string imagePath,
        string prompt = "Describe this image and return JSON with description and tags.",
        CancellationToken ct = default)
    {
        if (!File.Exists(imagePath))
            throw new FileNotFoundException("Image not found.", imagePath);

        return AnalyzeImageAsync(File.ReadAllBytes(imagePath), prompt, ct);
    }

    public async Task<VisionAnalysis> AnalyzeImageAsync(
        byte[] imageBytes,
        string prompt = "Describe this image and return JSON with description and tags.",
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(imageBytes);
        await LoadAsync(ct).ConfigureAwait(false);
        await _generationLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            return await Task.Run(() => Generate(imageBytes, prompt, ct), ct).ConfigureAwait(false);
        }
        finally
        {
            _generationLock.Release();
        }
    }

    private VisionAnalysis Generate(byte[] imageBytes, string prompt, CancellationToken ct)
    {
        var model = _model ?? throw new InvalidOperationException("The Phi-3 Vision model is not loaded.");
        var processor = _processor ?? throw new InvalidOperationException("The Phi-3 Vision processor is not loaded.");
        using var images = Images.Load(imageBytes);
        using var inputs = processor.ProcessImages(
            $"<|user|>\n<|image_1|>\n{prompt}<|end|>\n<|assistant|>\n",
            images);
        using var parameters = new GeneratorParams(model);
        // max_length is the total prompt plus output length, not the output limit.
        parameters.SetSearchOption("max_length", _contextLength);
        parameters.SetSearchOption("temperature", 0.0f);

        using var generator = new Generator(model, parameters);
        generator.SetInputs(inputs);

        var output = new StringBuilder();
        var generatedTokens = 0;
        while (!generator.IsDone() && generatedTokens < _maxNewTokens)
        {
            ct.ThrowIfCancellationRequested();
            generator.GenerateNextToken();
            output.Append(processor.Decode(generator.GetNextTokens()));
            generatedTokens++;
        }

        var rawResponse = output.ToString().Trim();
        return ParseAnalysis(rawResponse);
    }

    public void Dispose()
    {
        _generationLock.Dispose();
        _loadLock.Dispose();
        _processor?.Dispose();
        _model?.Dispose();
    }

    private static VisionAnalysis ParseAnalysis(string rawResponse)
    {
        var json = ExtractJsonObject(rawResponse);
        if (json.StartsWith("```", StringComparison.Ordinal))
        {
            json = json.Trim('`', ' ', '\r', '\n');
            if (json.StartsWith("json\n", StringComparison.OrdinalIgnoreCase))
                json = json[5..];
        }

        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(json);
            var root = document.RootElement;
            var description = root.TryGetProperty("description", out var descriptionElement)
                ? descriptionElement.GetString() ?? rawResponse
                : rawResponse;
            var tags = root.TryGetProperty("tags", out var tagsElement) && tagsElement.ValueKind == System.Text.Json.JsonValueKind.Array
                ? tagsElement.EnumerateArray()
                    .Select(tag => tag.GetString())
                    .Where(tag => !string.IsNullOrWhiteSpace(tag))
                    .Cast<string>()
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray()
                : [];

            return new VisionAnalysis(description, tags, rawResponse);
        }
        catch (System.Text.Json.JsonException)
        {
            return new VisionAnalysis(rawResponse, [], rawResponse);
        }
    }

    private static string ExtractJsonObject(string response)
    {
        var start = response.IndexOf('{');
        var end = response.LastIndexOf('}');
        if (start >= 0 && end > start)
            return response[start..(end + 1)].Trim();

        return response.Trim().Trim('\uFFFD');
    }
}
