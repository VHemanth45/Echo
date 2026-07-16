using Echo.Contracts;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;
using System.Net.Http.Json;
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
 var result = await nim.ProfileAsync(request.Samples, ct); return result.Success ? Results.Ok(result) : Results.Problem(result.Error, statusCode: 502);
});
app.MapPost("/api/rewrite", async (RewriteRequest request, NimClient nim, CancellationToken ct) => {
 if (!request.ConsentGranted) return Results.BadRequest(ApiResult<string>.Fail("Cloud consent is required."));
 if (Validation.WordCount(request.Text) > 2000) return Results.BadRequest(ApiResult<string>.Fail("Selected text exceeds 2,000 words."));
 if (string.IsNullOrWhiteSpace(request.Text)) return Results.BadRequest(ApiResult<string>.Fail("No text was selected."));
 var result = await nim.RewriteAsync(request.Text, request.Profile, ct); return result.Success ? Results.Ok(result) : Results.Problem(result.Error, statusCode: 502);
});
app.Run();

public sealed class NimClient(IHttpClientFactory factory, IConfiguration configuration) {
 public async Task<ApiResult<VoiceProfile>> ProfileAsync(IReadOnlyList<string> samples, CancellationToken ct) {
  if (string.IsNullOrWhiteSpace(configuration["Nim:ApiKey"])) return ApiResult<VoiceProfile>.Ok(new("clear and direct", "varied sentence lengths", "plain, precise language", "professional but warm", "concise paragraphs", "jargon and unsupported claims"));
  var prompt = "Analyze these writing samples. Return JSON with tone, rhythm, vocabulary, formality, recurringPatterns, avoidanceTendencies. Samples:\n" + string.Join("\n---\n", samples);
  var text = await CompleteAsync(prompt, ct); try { return ApiResult<VoiceProfile>.Ok(JsonSerializer.Deserialize<VoiceProfile>(text, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!); } catch { return ApiResult<VoiceProfile>.Fail("NIM returned an invalid profile."); }
 }
 public async Task<ApiResult<string>> RewriteAsync(string selected, VoiceProfile profile, CancellationToken ct) {
  if (string.IsNullOrWhiteSpace(configuration["Nim:ApiKey"])) return ApiResult<string>.Ok(selected);
  var prompt = $"Rewrite in this voice: {JsonSerializer.Serialize(profile)}. Preserve meaning, facts, language, links and practical formatting. Do not invent details. Return only rewritten text.\n\n{selected}";
  try { return ApiResult<string>.Ok(await CompleteAsync(prompt, ct)); } catch (Exception) { return ApiResult<string>.Fail("The rewrite provider is unavailable."); }
 }
 async Task<string> CompleteAsync(string prompt, CancellationToken ct) {
  var request = new HttpRequestMessage(HttpMethod.Post, configuration["Nim:Endpoint"] ?? throw new InvalidOperationException("Nim endpoint is not configured"));
  request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", configuration["Nim:ApiKey"]);
  request.Content = JsonContent.Create(new { model = configuration["Nim:Model"], messages = new[] { new { role = "user", content = prompt } }, temperature = .2 });
  using var response = await factory.CreateClient().SendAsync(request, ct); response.EnsureSuccessStatusCode();
  using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct)); return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? throw new InvalidOperationException();
 }
}
