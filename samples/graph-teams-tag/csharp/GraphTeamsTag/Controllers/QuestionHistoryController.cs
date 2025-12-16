using GraphTeamsTag.Services;
using Microsoft.AspNetCore.Mvc;

namespace GraphTeamsTag.Controllers
{
    [Route("api/history")]
    [ApiController]
    public class QuestionHistoryController : Controller
    {
        private readonly QuestionHistoryService _historyService;
        private readonly CopilotService _copilotService;

        public QuestionHistoryController(
            QuestionHistoryService historyService,
            CopilotService copilotService)
        {
            _historyService = historyService;
            _copilotService = copilotService;
        }

        [HttpGet("by-tag/{tagId}")]
        public IActionResult GetByTag(string tagId)
        {
            var questions = _historyService.GetTopQuestionsByTag(tagId);
            return Ok(questions);
        }

        [HttpPost("{chatId}/summarize")]
        public async Task<IActionResult> Summarize(string chatId, [FromQuery] string ssoToken)
        {
            if (string.IsNullOrEmpty(ssoToken))
            {
                return BadRequest("Token is required");
            }

            // We pass the raw SSO token to the service, which will handle OBO flows for different scopes
            var summary = await _copilotService.SummarizeChatAsync(chatId, ssoToken);

            _historyService.AddSummary(chatId, summary);

            return Ok(new { summary });
        }
    }
}
