using System;

namespace FocusMapApi.Services.OpenAi;

public interface IOpenAiService
{
    Task<string> AnalyzeContextAsync(string prompt);
    Task<string> TranscribeAudioAsync(Stream audioStream, string fileName);
}
