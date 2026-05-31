using System.Net.Http.Headers;
using System.Text.Json;
using AgentScope.Core.Models;

namespace AgentScope.Core.Token;

/// <summary>
/// Client for fetching Claude token usage via Anthropic's Usage API.
/// </summary>
public class AnthropicClient : IDisposable
{
    private readonly HttpClient _http;
    private const string BaseUrl = "https://api.anthropic.com/v1";

    public AnthropicClient(string apiKey)
    {
        _http = new HttpClient();
        _http.DefaultRequestHeaders.Add("x-api-key", apiKey);
        _http.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    /// <summary>Fetch current token usage for the given workspace.</summary>
    public async Task<TokenUsage?> GetUsageAsync(
        string workspaceId,
        CancellationToken ct = default)
    {
        try
        {
            // Anthropic provides usage via the Usage API (billing endpoint)
            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var response = await _http.GetAsync(
                $"{BaseUrl}/organizations/{workspaceId}/usage?start_date={today}&end_date={today}",
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
                    if (entry.TryGetProperty("results", out var results))
                    {
                        foreach (var result in results.EnumerateArray())
                        {
                            inputTokens += GetIntProperty(result, "input_tokens");
                            outputTokens += GetIntProperty(result, "output_tokens");
                        }
                    }
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

    private static int GetIntProperty(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var prop) && prop.TryGetInt32(out var val)
            ? val
            : 0;
    }

    public void Dispose() => _http.Dispose();
}
