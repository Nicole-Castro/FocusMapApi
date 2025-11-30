using System;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace FocusMapApi.Services.OpenAi;

public class OpenAiService : IOpenAiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public OpenAiService(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _apiKey = config["OpenAI:ApiKey"];
    }

    public async Task<string> AnalyzeContextAsync(string prompt)
    {
        var body = new
        {
            model = "gpt-4.1-nano",
            messages = new[] { new { role = "user", content = prompt } },
        };

        var content = new StringContent(
            JsonSerializer.Serialize(body),
            Encoding.UTF8,
            "application/json"
        );

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            _apiKey
        );

        var response = await _httpClient.PostAsync(
            "https://api.openai.com/v1/chat/completions",
            content
        );
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception("Erro OpenAI: " + json);

        using var doc = JsonDocument.Parse(json);
        return doc
            .RootElement.GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();
    }

    public async Task<string> TranscribeAudioAsync(Stream audioStream, string fileName)
    {
        var form = new MultipartFormDataContent();

        var audioContent = new StreamContent(audioStream);
        audioContent.Headers.ContentType = new MediaTypeHeaderValue("audio/*");

        form.Add(audioContent, "file", fileName);
        form.Add(new StringContent("whisper-1"), "model");
        form.Add(new StringContent("pt"), "language");

        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            _apiKey
        );

        var response = await _httpClient.PostAsync(
            "https://api.openai.com/v1/audio/transcriptions",
            form
        );

        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception("Erro Whisper: " + json);

        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("text").GetString();
    }
}
