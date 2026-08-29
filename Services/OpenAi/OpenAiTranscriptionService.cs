using System.Net.Http.Headers;
using System.Text.Json;

namespace FocusMapApi.Services.OpenAi;

/// <summary>
/// Transcrição via OpenAI (gpt-transcribe / gpt-4o-transcribe / whisper-1, conforme config).
/// Mantida como implementação separada pra poder voltar pra ela a qualquer momento só
/// trocando OpenAI:TranscribeProvider de volta pra "openai" — nada aqui depende do Groq.
/// </summary>
public class OpenAiTranscriptionService : ITranscriptionService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly string _language;

    public OpenAiTranscriptionService(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _apiKey = config["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI:ApiKey não configurada.");
        _model = config["OpenAI:TranscribeModel"] ?? "gpt-transcribe";
        _language = config["OpenAI:TranscribeLanguage"] ?? "pt";
    }

    public async Task<string> TranscribeAsync(Stream audioStream, string fileName)
    {
        var form = new MultipartFormDataContent();

        var audioContent = new StreamContent(audioStream);
        audioContent.Headers.ContentType = new MediaTypeHeaderValue("audio/*");

        form.Add(audioContent, "file", fileName);
        form.Add(new StringContent(_model), "model");
        form.Add(new StringContent(_language), "language");

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://api.openai.com/v1/audio/transcriptions"
        )
        {
            Content = form,
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        var response = await _httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception("Erro na transcrição (OpenAI): " + json);

        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("text").GetString() ?? string.Empty;
    }
}
