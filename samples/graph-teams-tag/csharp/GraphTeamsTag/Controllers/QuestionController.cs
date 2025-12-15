using GraphTeamsTag.Helper;
using GraphTeamsTag.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GraphTeamsTag.Controllers
{
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

            var membersResponse = await graphClient.Teams[request.TeamId].Tags[request.Tag].Members.GetAsync();
            var members = new List<TeamworkTagMember>();
            
            if (membersResponse?.Value != null)
            {
                var pageIterator = PageIterator<TeamworkTagMember, TeamworkTagMemberCollectionResponse>.CreatePageIterator(
                    graphClient,
                    membersResponse,
                    (m) => {
                        members.Add(m);
                        return true;
                    }
                );
                await pageIterator.IterateAsync();
            }

            if (request.TargetsOnlineUsers)
            {
                var onlineMembers = new List<TeamworkTagMember>();

                foreach (var member in members)
                {
                    var userPresence = await graphClient.Users[member.UserId].Presence.GetAsync();

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

            var chatType = request.QuestionTarget == QuestionTarget.All ? ChatType.Group : ChatType.OneOnOne;
            var chat = new Chat
            {
                ChatType = chatType,
                Members = new List<ConversationMember>(),
                AdditionalData = new Dictionary<string, object>()
            };

            if (chatType == ChatType.Group)
            {
                chat.Topic = request.QuestionTopic;
            }

            foreach (var member in members.Where(m => m.UserId != request.RequesterUserId))
            {
                chat.Members.Add(new AadUserConversationMember
                {
                    AdditionalData = new Dictionary<string, object>()
                    {
                        {"user@odata.bind", $"https://graph.microsoft.com/v1.0/users('{member.UserId}')"}
                    },
                    Roles = new List<string> { "owner" }
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

            var chatResponse = await graphClient.Chats.PostAsync(chat);

            var chatMessage = new ChatMessage
            {
                Body = new ItemBody
                {
                    Content = request.Question,
                    ContentType = BodyType.Text,
                }
            };
            
            await graphClient.Chats[chatResponse.Id].Messages.PostAsync(chatMessage);

            var reponse = new AskQuestionResponse
            {
                ChatId = chatResponse.Id,
                ResponseUsers = members.Select(m => m.DisplayName).ToArray(),
            };

            return Ok(reponse);
        }
    }
}
