namespace FocusMapApi.Services.OpenAi;

/// <summary>
/// Abstrai QUEM faz a transcrição de áudio (OpenAI, Groq, etc.), pra poder trocar de
/// provedor só via config (OpenAI:TranscribeProvider) sem mexer no resto do pipeline.
/// </summary>
public interface ITranscriptionService
{
    Task<string> TranscribeAsync(Stream audioStream, string fileName);
}
