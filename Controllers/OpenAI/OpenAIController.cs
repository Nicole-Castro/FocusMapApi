using FocusMapApi.Services.OpenAi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FocusMapApi.Controllers.OpenAI
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OpenAIController : ControllerBase
    {
        private readonly IOpenAiService _openAI;
        private readonly IOpenAiUsageGuard _usageGuard;
        private readonly int _maxPromptLength;
        private readonly long _maxAudioBytes;

        public OpenAIController(IOpenAiService openAI, IOpenAiUsageGuard usageGuard, IConfiguration config)
        {
            _openAI = openAI;
            _usageGuard = usageGuard;
            _maxPromptLength = config.GetValue<int?>("OpenAI:MaxPromptLength") ?? 4000;
            _maxAudioBytes = config.GetValue<long?>("OpenAI:MaxAudioBytes") ?? 10 * 1024 * 1024; // 10MB
        }

        [HttpPost("analyze")]
        public async Task<IActionResult> Analyze([FromBody] PromptRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Prompt))
                return BadRequest(new { Success = false, Message = "Prompt vazio." });

            if (req.Prompt.Length > _maxPromptLength)
                return BadRequest(
                    new
                    {
                        Success = false,
                        Message = $"Prompt excede o limite de {_maxPromptLength} caracteres.",
                    }
                );

            if (!_usageGuard.TryConsume(out _))
                return StatusCode(429, BuildLimitMessage());

            var result = await _openAI.AnalyzeContextAsync(req.Prompt);
            return Ok(new { result });
        }

        [HttpPost("transcribe")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> Transcribe(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { Success = false, Message = "Arquivo de áudio inválido." });

            if (file.Length > _maxAudioBytes)
                return BadRequest(
                    new
                    {
                        Success = false,
                        Message = $"Arquivo excede o limite de {_maxAudioBytes / (1024 * 1024)}MB.",
                    }
                );

            if (!_usageGuard.TryConsume(out _))
                return StatusCode(429, BuildLimitMessage());

            using var stream = file.OpenReadStream();
            var result = await _openAI.TranscribeAudioAsync(stream, file.FileName);
            return Ok(new { result });
        }

        private object BuildLimitMessage() =>
            new
            {
                Success = false,
                Message = _usageGuard.IsEnabled
                    ? $"Limite diário de chamadas à OpenAI atingido ({_usageGuard.DailyLimit}). Tente novamente amanhã."
                    : "Chamadas à OpenAI estão desativadas no momento.",
            };
    }

    public class PromptRequest
    {
        public string Prompt { get; set; } = string.Empty;
    }
}
