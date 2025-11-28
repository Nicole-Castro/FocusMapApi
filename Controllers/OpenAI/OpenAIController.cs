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
        private readonly OpenAiService _openAI;

        public OpenAIController(OpenAiService openAI)
        {
            _openAI = openAI;
        }

        [HttpPost("analyze")]
        public async Task<IActionResult> Analyze([FromBody] PromptRequest req)
        {
            var result = await _openAI.AnalyzeContextAsync(req.Prompt);
            return Ok(new { result });
        }

        [HttpPost("transcribe")]
        public async Task<IActionResult> Transcribe(IFormFile file)
        {
            using var stream = file.OpenReadStream();
            var result = await _openAI.TranscribeAudioAsync(stream, file.FileName);
            return Ok(new { result });
        }
    }

    public class PromptRequest
    {
        public string Prompt { get; set; }
    }
}
