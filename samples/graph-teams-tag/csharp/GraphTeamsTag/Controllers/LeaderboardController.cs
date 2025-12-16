using GraphTeamsTag.Services;
using Microsoft.AspNetCore.Mvc;

namespace GraphTeamsTag.Controllers
{
    [Route("api/leaderboard")]
    [ApiController]
    public class LeaderboardController : Controller
    {
        private readonly LeaderboardService _leaderboardService;

        public LeaderboardController(LeaderboardService leaderboardService)
        {
            _leaderboardService = leaderboardService;
        }

        [HttpGet]
        public IActionResult Get()
        {
            var leaderboard = _leaderboardService.GetLeaderboard()
                .OrderByDescending(e => e.Points)
                .ToList();
            return Ok(leaderboard);
        }
    }
}

