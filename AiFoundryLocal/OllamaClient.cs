using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using CleanCode.Patterns;

namespace AiFoundryLocal
{
    public record OllamaOptions(string BaseUrl = "http://localhost:11434/", int TimeoutSeconds = 60);

    public class OllamaClient : IDisposable
    {
        private readonly HttpClient _http;
        private readonly OllamaOptions _options;

        public OllamaClient(OllamaOptions? options = null, HttpClient? httpClient = null)
        {
            _options = options ?? new OllamaOptions();
            _http = httpClient ?? new HttpClient { BaseAddress = new Uri(_options.BaseUrl), Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds) };
        }

        /// <summary>
        /// Call the Ollama local server `/api/generate` endpoint.
        /// Models and additional parameters are configurable.
        /// Returns the raw response body as a string.
        /// </summary>
        public async Task<Result<string>> GenerateAsync(string model, string prompt, IEnumerable<byte[]?>? images = null, object? parameters = null, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(model)) throw new ArgumentNullException(nameof(model));

            string[]? base64Images = images?.Where(i => i is not null).Select(i => Convert.ToBase64String(i!)).ToArray();

            var request = new Dictionary<string, object?>
            {
                ["model"] = model,
                ["prompt"] = prompt,
            };

            if (base64Images is not null && base64Images.Length > 0)
                request["images"] = base64Images;

            if (parameters is not null)
                request["parameters"] = parameters;

            try
            {
                using var resp = await _http.PostAsJsonAsync("api/generate", request, ct).ConfigureAwait(false);
                var content = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                if (!resp.IsSuccessStatusCode)
                    return Result<string>.Failure($"Ollama returned {(int)resp.StatusCode} ({resp.ReasonPhrase}): {content}");

                return Result<string>.Success(content);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                return Result<string>.Failure("The Ollama request timed out.");
            }
            catch (HttpRequestException exception)
            {
                return Result<string>.Failure($"The Ollama request failed: {exception.Message}");
            }
        }

        public void Dispose()
        {
            _http?.Dispose();
        }
    }
}
