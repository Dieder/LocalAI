using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AiFoundryLocal
{
    public interface ILocalModel
    {
        string Name { get; }
        string Path { get; }
        bool IsLoaded { get; }
        Task LoadAsync(CancellationToken ct = default);
    }

    public class LocalModel : ILocalModel
    {
        public string Name { get; init; }
        public string Path { get; init; }
        public bool IsLoaded { get; private set; }

        public LocalModel(string name, string path)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Path = path ?? throw new ArgumentNullException(nameof(path));
        }

        public virtual Task LoadAsync(CancellationToken ct = default)
        {
            // NOTE: This is a lightweight placeholder. Integrate your preferred
            // model runtime here (ONNX Runtime, TorchSharp, Foundry local APIs, etc.)
            // For now we only validate the path and mark it loaded.
            if (!Directory.Exists(Path) && !File.Exists(Path))
                throw new FileNotFoundException("Model path not found", Path);

            IsLoaded = true;
            return Task.CompletedTask;
        }
    }

    public class ModelLoader
    {
        private readonly string _modelsDirectory;
        private readonly Dictionary<string, ILocalModel> _cache = new(StringComparer.OrdinalIgnoreCase);

        public ModelLoader(string modelsDirectory)
        {
            _modelsDirectory = modelsDirectory ?? throw new ArgumentNullException(nameof(modelsDirectory));
        }

        private string ResolveModelPath(string modelName)
        {
            // allow either a direct path or a folder name under the models directory
            if (Path.IsPathRooted(modelName)) return modelName;
            return System.IO.Path.Combine(_modelsDirectory, modelName);
        }

        public async Task<ILocalModel> LoadModelAsync(string modelName, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(modelName)) throw new ArgumentNullException(nameof(modelName));
            if (_cache.TryGetValue(modelName, out var cached)) return cached;

            var path = ResolveModelPath(modelName);
            var model = new LocalModel(modelName, path);
            await model.LoadAsync(ct).ConfigureAwait(false);
            _cache[modelName] = model;
            return model;
        }

        // Convenience helpers for the requested models
        public Task<ILocalModel> LoadPhi3VisionAsync(CancellationToken ct = default) => LoadModelAsync("Phi-3-Vision", ct);
        public Task<ILocalModel> LoadLLaVA1_6Async(CancellationToken ct = default) => LoadModelAsync("LLaVA-1.6", ct);
        public Task<ILocalModel> LoadInternVL2Async(CancellationToken ct = default) => LoadModelAsync("InternVL2", ct);
    }
}
