using System.Net.Http.Headers;
using System.Text.Json;

namespace FocusMapApi.Services.OpenAi;

/// <summary>
/// Transcrição via Groq (Whisper hospedado na infraestrutura deles — bem mais barato e
/// rápido que os modelos de transcrição da OpenAI). A API do Groq é compatível com o
/// formato da OpenAI (mesmo endpoint /audio/transcriptions), então a implementação é
/// quase idêntica à OpenAiTranscriptionService, só muda URL, chave e modelo.
/// </summary>
public class GroqTranscriptionService : ITranscriptionService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly string _language;

    public GroqTranscriptionService(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _apiKey = config["Groq:ApiKey"] ?? throw new InvalidOperationException("Groq:ApiKey não configurada.");
        _model = config["Groq:TranscribeModel"] ?? "whisper-large-v3-turbo";
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
            "https://api.groq.com/openai/v1/audio/transcriptions"
        )
        {
            Content = form,
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        var response = await _httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception("Erro na transcrição (Groq): " + json);

        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("text").GetString() ?? string.Empty;
    }
}
