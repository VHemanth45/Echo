namespace Echo.Contracts;

public sealed record VoiceProfile(string Tone, string Rhythm, string Vocabulary, string Formality, string RecurringPatterns, string AvoidanceTendencies);
public sealed record ProfileRequest(bool ConsentGranted, IReadOnlyList<string> Samples);
public sealed record RewriteRequest(bool ConsentGranted, string Text, VoiceProfile Profile);
public sealed record ApiResult<T>(bool Success, T? Value, string? Error) { public static ApiResult<T> Ok(T value) => new(true, value, null); public static ApiResult<T> Fail(string error) => new(false, default, error); }
public static class Validation {
 public static bool ValidSamples(IReadOnlyList<string>? samples) => samples is { Count: >= 3 and <= 10 } && samples.All(s => !string.IsNullOrWhiteSpace(s));
 public static int WordCount(string text) => string.IsNullOrWhiteSpace(text) ? 0 : System.Text.RegularExpressions.Regex.Matches(text, @"\S+").Count;
}
