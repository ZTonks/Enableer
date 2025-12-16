using GraphTeamsTag.Helper;
using GraphTeamsTag.Models;
using GraphTeamsTag.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Graph;
using Microsoft.Graph.Me.SendMail;
using Microsoft.Graph.Models;

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
        private readonly LeaderboardService _leaderboardService;
        private readonly QuestionHistoryService _historyService;

        public QuestionController(
            ILogger<QuestionController> logger,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            IHttpContextAccessor httpContextAccessor,
            GraphHelper graphHelper,
            LeaderboardService leaderboardService,
            QuestionHistoryService historyService)
        {
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _leaderboardService = leaderboardService;
            _historyService = historyService;
        }

        [HttpPost("")]
        public async Task<IActionResult> AskQuestion(
            [FromQuery] string ssoToken,
            [FromBody] AskQuestionRequest request)
        {
            var token = await SSOAuthHelper.GetAccessTokenOnBehalfUserAsync(_configuration, _httpClientFactory, _httpContextAccessor, ssoToken);
            var graphClient = SimpleGraphClient.GetGraphClient(token);

            var members = new List<TeamworkTagMember>();

            foreach (var tag in request.Tags)
            {
                var membersResponse = await graphClient.Teams[request.TeamId].Tags[tag].Members.GetAsync();

                if (membersResponse?.Value is null)
                {
                    continue;
                }

                var pageIterator = PageIterator<TeamworkTagMember, TeamworkTagMemberCollectionResponse>.CreatePageIterator(
                    graphClient,
                    membersResponse,
                    (m) =>
                    {
                        if (m.UserId == request.RequesterUserId)
                        {
                            return true; // Skip - requester is always added
                        }

                        members.Add(m);
                        return true;
                    });

                await pageIterator.IterateAsync();
            }

            members = members
                .GroupBy(m => m.UserId)
                .Where(g => g.Count() == request.Tags.Length) // Where matched every tag i.e. AND these together
                .SelectMany(g => g)
                .DistinctBy(m => m.UserId)
                .ToList();

            if (request.TargetsOnlineUsers)
            {
                var onlineMembers = new List<TeamworkTagMember>();

                foreach (var member in members)
                {
                    // TODO, this could probably be improved using a filter to limit the (N+1)-ness of this query
                    // https://stackoverflow.com/questions/77505096/microsoft-graph-api-get-a-list-of-users-by-ids
                    var userPresence = await graphClient.Users[member.UserId].Presence.GetAsync();

                    if (userPresence is null)
                    {
                        continue;
                    }

                    if (userPresence.Availability == "Available")
                    {
                        onlineMembers.Add(member);
                    }
                }

                members = onlineMembers;
            }

            if (members.Count == 0)
            {
                return BadRequest(new
                {
                    Problem = "No members were eligible for all tags",
                });
            }

            // Award points to users who are being asked for help
            foreach (var member in members)
            {
                _leaderboardService.AddPoints(member.UserId, member.DisplayName, 1);
            }

            if (request.Email)
            {
                var userEmails = new List<string>();
                foreach (var member in members)
                {
                    // TODO, this could probably be improved using a filter to limit the (N+1)-ness of this query
                    // https://stackoverflow.com/questions/77505096/microsoft-graph-api-get-a-list-of-users-by-ids
                    var user = await graphClient.Users[member.UserId].GetAsync((options) =>
                    {
                        options.QueryParameters.Select = ["UserPrincipalName"];
                    });

                    if (user != null && user.UserPrincipalName != null)
                    {
                        userEmails.Add(user.UserPrincipalName);
                    }
                }

                var email = new SendMailPostRequestBody
                {
                    Message = new Message
                    {
                        Subject = $"Call for aid - {request.QuestionTopic} - {string.Join(", ", request.Tags)}",
                        Body = new ItemBody
                        {
                            ContentType = BodyType.Text,
                            Content = request.Question,
                        },
                        ToRecipients = userEmails.Select(ue => new Recipient
                        {
                            EmailAddress = new EmailAddress()
                            {
                                Address = ue
                            }
                        }).ToList(),
                    },
                    SaveToSentItems = true,
                };

                await graphClient.Me.SendMail.PostAsync(email);

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

                foreach (var member in members)
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
            }
            else
            {
                // Randomly select a user to ask the question to
                var random = new Random();
                var randomIndex = random.Next(members.Count);

                chat.Members.Add(new AadUserConversationMember
                {
                    AdditionalData = new Dictionary<string, object>()
                    {
                        {"user@odata.bind", $"https://graph.microsoft.com/v1.0/users('{members[randomIndex].UserId}')"}
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
                ResponseUsers = members.Select(m => m.DisplayName ?? "User").ToArray(),
            };

            // Save question history
            _historyService.AddQuestion(new QuestionHistoryEntry
            {
                QuestionTopic = request.QuestionTopic,
                QuestionContent = request.Question,
                Tags = request.Tags,
                ChatId = chatResponse.Id,
                ChatWebUrl = chatResponse.WebUrl,
                RequesterUserId = request.RequesterUserId
            });

            return Ok(reponse);
        }
    }
}
