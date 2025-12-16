using GraphTeamsTag.Models;
using GraphTeamsTag.Services;
using Microsoft.AspNetCore.Mvc;

namespace GraphTeamsTag.Controllers
{
    [Route("api/history")]
    [ApiController]
    public class QuestionHistoryController : Controller
    {
        private readonly QuestionHistoryService _historyService;

        public QuestionHistoryController(QuestionHistoryService historyService)
        {
            _historyService = historyService;
        }

        [HttpGet("by-tag/{tagId}")]
        public IActionResult GetByTag(string tagId)
        {
            var questions = _historyService.GetTopQuestionsByTag(tagId);
            return Ok(questions);
        }
    }
}

