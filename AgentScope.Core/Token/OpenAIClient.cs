using System.Net.Http.Headers;
using System.Text.Json;
using AgentScope.Core.Models;

namespace AgentScope.Core.Token;

/// <summary>
/// Client for fetching Codex/OpenAI token usage via OpenAI's Usage API.
/// </summary>
public class OpenAIClient : IDisposable
{
    private readonly HttpClient _http;
    private const string BaseUrl = "https://api.openai.com/v1";

    public OpenAIClient(string apiKey)
    {
        _http = new HttpClient();
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    /// <summary>Fetch usage for the current date.</summary>
    public async Task<TokenUsage?> GetUsageAsync(CancellationToken ct = default)
    {
        try
        {
            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var response = await _http.GetAsync(
                $"{BaseUrl}/usage?date={today}",
                ct);

            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            var inputTokens = 0;
            var outputTokens = 0;

            if (doc.RootElement.TryGetProperty("data", out var data))
            {
                foreach (var entry in data.EnumerateArray())
                {
                    if (entry.TryGetProperty("n_context_tokens_total", out var input))
                        inputTokens += input.GetInt32();
                    if (entry.TryGetProperty("n_generated_tokens_total", out var output))
                        outputTokens += output.GetInt32();
                }
            }

            return new TokenUsage
            {
                InputTokens = inputTokens,
                OutputTokens = outputTokens,
                LastUpdated = DateTimeOffset.UtcNow,
                IsEstimated = false
            };
        }
        catch
        {
            return null;
        }
    }

    public void Dispose() => _http.Dispose();
}
