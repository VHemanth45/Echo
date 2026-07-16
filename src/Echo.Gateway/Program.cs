using Echo.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient<NimClient>();
builder.Services.AddSingleton<NimClient>();
var app = builder.Build();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.MapPost("/api/profile", async (ProfileRequest request, NimClient nim, CancellationToken ct) => {
 if (!request.ConsentGranted) return Results.BadRequest(ApiResult<VoiceProfile>.Fail("Cloud consent is required."));
 if (!Validation.ValidSamples(request.Samples)) return Results.BadRequest(ApiResult<VoiceProfile>.Fail("Provide 3 to 10 non-empty samples."));
 var result = await nim.ProfileAsync(request.Samples, ct); return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: 502);
});
app.MapPost("/api/rewrite", async (RewriteRequest request, NimClient nim, CancellationToken ct) => {
 if (!request.ConsentGranted) return Results.BadRequest(ApiResult<string>.Fail("Cloud consent is required."));
 if (Validation.WordCount(request.Text) > 2000) return Results.BadRequest(ApiResult<string>.Fail("Selected text exceeds 2,000 words."));
 if (string.IsNullOrWhiteSpace(request.Text)) return Results.BadRequest(ApiResult<string>.Fail("No text was selected."));
 var result = await nim.RewriteAsync(request.Text, request.Profile, ct); return result.Success ? Results.Ok(result) : Results.Json(result, statusCode: 502);
});
app.Run();

public sealed class NimClient(IHttpClientFactory factory, IConfiguration configuration, ILogger<NimClient> logger) {
 public async Task<ApiResult<VoiceProfile>> ProfileAsync(IReadOnlyList<string> samples, CancellationToken ct) {
  if (string.IsNullOrWhiteSpace(configuration["Nim:ApiKey"])) return ApiResult<VoiceProfile>.Ok(new("clear and direct", "varied sentence lengths", "plain, precise language", "professional but warm", "concise paragraphs", "jargon and unsupported claims"));
  var prompt = """
   Analyze these writing samples and produce a voice profile. Return only one JSON object, with no Markdown or explanation.
   The object must contain these six non-empty string properties: tone, rhythm, vocabulary, formality, recurringPatterns, avoidanceTendencies.
   Samples:
   """ + "\n" + string.Join("\n---\n", samples);
  try {
   var text = await CompleteAsync(prompt, ct);
   var json = ExtractJsonObject(text);
   var profile = JsonSerializer.Deserialize<VoiceProfile>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
   return profile is null || !ValidProfile(profile) ? ApiResult<VoiceProfile>.Fail("NIM did not return a complete voice profile.") : ApiResult<VoiceProfile>.Ok(profile);
  } catch (NimRequestException error) {
   logger.LogWarning("NIM profile request failed with HTTP {StatusCode}: {Response}", (int)error.StatusCode, error.Response);
   return ApiResult<VoiceProfile>.Fail($"NIM profile request failed ({(int)error.StatusCode} {error.StatusCode}).");
  } catch (Exception error) {
   logger.LogWarning(error, "NIM profile response could not be parsed");
   return ApiResult<VoiceProfile>.Fail("NIM returned a response that was not a valid voice profile.");
  }
 }
 public async Task<ApiResult<string>> RewriteAsync(string selected, VoiceProfile profile, CancellationToken ct) {
  if (string.IsNullOrWhiteSpace(configuration["Nim:ApiKey"])) return ApiResult<string>.Ok(selected);
  var prompt = $"Rewrite in this voice: {JsonSerializer.Serialize(profile)}. Preserve meaning, facts, language, links and practical formatting. Do not invent details. Return only rewritten text.\n\n{selected}";
  try { return ApiResult<string>.Ok(await CompleteAsync(prompt, ct)); } catch (Exception) { return ApiResult<string>.Fail("The rewrite provider is unavailable."); }
 }
 async Task<string> CompleteAsync(string prompt, CancellationToken ct) {
  var apiKey = NormalizeBearerToken(configuration["Nim:ApiKey"]);
  var request = new HttpRequestMessage(HttpMethod.Post, configuration["Nim:Endpoint"] ?? throw new InvalidOperationException("Nim endpoint is not configured"));
  request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
  request.Content = JsonContent.Create(new {
   model = configuration["Nim:Model"],
   messages = new[] { new { role = "user", content = prompt } },
   temperature = 1,
   top_p = .95,
   max_tokens = 16384,
   chat_template_kwargs = new { enable_thinking = true },
   reasoning_budget = 16384
  });
  using var response = await factory.CreateClient().SendAsync(request, ct);
  var body = await response.Content.ReadAsStringAsync(ct);
  if (!response.IsSuccessStatusCode) throw new NimRequestException(response.StatusCode, Truncate(body));
  using var doc = JsonDocument.Parse(body);
  var message = doc.RootElement.GetProperty("choices")[0].GetProperty("message");
  if (message.TryGetProperty("content", out var content) && content.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(content.GetString())) return content.GetString()!;
  if (message.TryGetProperty("reasoning_content", out var reasoning) && reasoning.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(reasoning.GetString())) return reasoning.GetString()!;
  throw new InvalidOperationException("NIM returned no assistant content.");
 }
 static string NormalizeBearerToken(string? configuredKey) {
  var value = configuredKey?.Trim() ?? "";
  return value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? value[7..].Trim() : value;
 }
 static string ExtractJsonObject(string text) {
  var start = text.IndexOf('{');
  var end = text.LastIndexOf('}');
  if (start < 0 || end <= start) throw new JsonException("No JSON object was returned.");
  return text[start..(end + 1)];
 }
 static bool ValidProfile(VoiceProfile profile) => new[] { profile.Tone, profile.Rhythm, profile.Vocabulary, profile.Formality, profile.RecurringPatterns, profile.AvoidanceTendencies }.All(value => !string.IsNullOrWhiteSpace(value));
 static string Truncate(string value) => value.Length <= 1000 ? value : value[..1000];
}

public sealed class NimRequestException(HttpStatusCode statusCode, string response) : Exception($"NIM returned {(int)statusCode} {statusCode}.") {
 public HttpStatusCode StatusCode { get; } = statusCode;
 public string Response { get; } = response;
}
