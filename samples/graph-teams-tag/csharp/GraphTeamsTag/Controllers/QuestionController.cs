using GraphTeamsTag.Helper;
using GraphTeamsTag.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;

namespace GraphTeamsTag.Controllers;

[Route("api/questions")]
[ApiController]
public class QuestionController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<QuestionController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public QuestionController(
        ILogger<QuestionController> logger,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor httpContextAccessor,
        GraphHelper graphHelper)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    [HttpPost("")]
    public async Task<IActionResult> AskQuestion(
        [FromQuery] string ssoToken,
        [FromBody] AskQuestionRequest request)
    {
        var token = await SSOAuthHelper.GetAccessTokenOnBehalfUserAsync(_configuration, _httpClientFactory, _httpContextAccessor, ssoToken);
        var graphClient = SimpleGraphClient.GetGraphClient(token);

        var members = (await graphClient.Teams[request.TeamId].Tags[request.Tag].Members.Request().GetAsync()).ToList();

        if (request.TargetsOnlineUsers)
        {
            var onlineMembers = new List<TeamworkTagMember>();

            foreach (var member in members)
            {
                var userPresence = await graphClient.Users[member.UserId].Presence.Request().GetAsync();

                if (userPresence.Availability == "Available")
                {
                    onlineMembers.Add(member);
                }
            }

            members = onlineMembers;
        }

        if (request.Email)
        {
            // ZT TODO
            return Ok();
        }

        var chat = new Chat
        {
            ChatType = request.QuestionTarget == QuestionTarget.All ? ChatType.Group : ChatType.OneOnOne,
            Topic = request.QuestionTopic,
            Members = new ChatMembersCollectionPage(),
        };

        foreach (var member in members)
        {
            chat.Members.Add(new AadUserConversationMember
            {
                Roles = new List<string>()
                {
                    "member"
                },
                AdditionalData = new Dictionary<string, object>()
                {
                    {"user@odata.bind", $"https://graph.microsoft.com/v1.0/users('{member.UserId}')"}
                }
            });
        }

        chat.Members.Add(new AadUserConversationMember
        {
            Roles = new List<string>()
            {
                "owner"
            },
            AdditionalData = new Dictionary<string, object>()
            {
                {"user@odata.bind", $"https://graph.microsoft.com/v1.0/users('{request.RequesterUserId}')"}
            }
        });

        chat.Messages.Add(new ChatMessage
        {
             Body = new ItemBody
             {
                 Content = request.Question,
                 ContentType = BodyType.Text,
             }
        });

        var chatResponse = await graphClient.Chats.Request().AddAsync(chat);

        var reponse = new AskQuestionResponse
        {
            ChatId = chatResponse.Id,
            ResponseUsers = members.Select(m => m.DisplayName).ToArray(),
        };

        return Ok(reponse);
    }
}
