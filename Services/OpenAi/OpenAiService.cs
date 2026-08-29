using System;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace FocusMapApi.Services.OpenAi;

public class OpenAiService : IOpenAiService
{
    private readonly HttpClient _httpClient;
    private readonly ITranscriptionService _transcriptionService;
    private readonly string _apiKey;
    private readonly string _analyzeModel;

    public OpenAiService(HttpClient httpClient, ITranscriptionService transcriptionService, IConfiguration config)
    {
        _httpClient = httpClient;
        _transcriptionService = transcriptionService;
        _apiKey = config["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI:ApiKey não configurada.");
        _analyzeModel = config["OpenAI:AnalyzeModel"] ?? "gpt-4.1-nano";
    }

    public async Task<string> AnalyzeContextAsync(string prompt)
    {
        var body = new
        {
            model = _analyzeModel,
            messages = new[] { new { role = "user", content = prompt } },
        };

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://api.openai.com/v1/chat/completions"
        )
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        var response = await _httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception("Erro OpenAI: " + json);

        using var doc = JsonDocument.Parse(json);
        return doc
                .RootElement.GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString()
            ?? string.Empty;
    }

    // A classificação de contexto (acima) fica sempre na OpenAI. A transcrição é
    // delegada pro provedor configurado (OpenAI ou Groq, ver OpenAI:TranscribeProvider
    // no appsettings.json e o registro em Program.cs) — trocar de provedor não muda
    // esse método nem o contrato usado pelo OpenAIController.
    public Task<string> TranscribeAudioAsync(Stream audioStream, string fileName) =>
        _transcriptionService.TranscribeAsync(audioStream, fileName);
}
