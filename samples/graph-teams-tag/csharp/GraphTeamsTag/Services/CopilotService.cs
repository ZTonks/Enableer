using System.Text;
using System.Text.Json;
using GraphTeamsTag.Helper;

namespace GraphTeamsTag.Services
{
    public class CopilotService
    {
        private readonly ILogger<CopilotService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CopilotService(
            ILogger<CopilotService> logger, 
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<string> SummarizeChatAsync(string chatId, string ssoToken)
        {
            try
            {
                // 1. Get Graph Token and Client
                var graphToken = await SSOAuthHelper.GetAccessTokenOnBehalfUserAsync(
                    _configuration, 
                    _httpClientFactory, 
                    _httpContextAccessor, 
                    ssoToken, 
                    "https://graph.microsoft.com/User.Read");

                var client = SimpleGraphClient.GetGraphClient(graphToken);

                // 2. Get Chat Messages
                var messages = await client.Chats[chatId].Messages.GetAsync();
                var messageStrings = new List<string>();

                if (messages?.Value != null)
                {
                    var messageList = messages.Value;
                    if (messageList != null)
                    {
                        var messagesToProcess = messageList.ToList();
                        messagesToProcess.Reverse();
                        foreach (var msg in messagesToProcess)
                        {
                            if (!string.IsNullOrEmpty(msg.Body?.Content))
                            {
                                var sender = msg.From?.User?.DisplayName ?? "User";
                                messageStrings.Add($"{sender}: {msg.Body.Content}");
                            }
                        }
                    }
                }

                if (messageStrings.Count == 0)
                {
                    return "No conversation content found to summarize.";
                }

                // 3. Get Flow Token
                var flowToken = await SSOAuthHelper.GetAccessTokenOnBehalfUserAsync(
                    _configuration, 
                    _httpClientFactory, 
                    _httpContextAccessor, 
                    ssoToken, 
                    "https://service.flow.microsoft.com//.default");

                // 4. Call Power Automate Flow
                var flowUrl = "https://default948b6122151a4d28a0dfd1e60b9a42.21.environment.api.powerplatform.com:443/powerautomate/automations/direct/workflows/cfbdbd362e214619ac3ef3cdf2301bd8/triggers/manual/paths/invoke?api-version=1";

                var httpClient = _httpClientFactory.CreateClient();
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", flowToken);

                // Construct payload: { "messages": [...] }
                var payload = new { messages = messageStrings };
                var jsonPayload = JsonSerializer.Serialize(payload);

                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(flowUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    
                    try 
                    {
                        var jsonDoc = JsonDocument.Parse(responseString);
                        if (jsonDoc.RootElement.TryGetProperty("predictionOutput", out var predictionOutput) && 
                            predictionOutput.TryGetProperty("text", out var textElement))
                        {
                            return textElement.GetString() ?? "No summary text returned.";
                        }
                        
                        return responseString; // Fallback to raw response if parsing fails
                    }
                    catch 
                    {
                         return responseString;
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Power Automate Flow failed: {response.StatusCode} - {errorContent}");
                    return $"Failed to generate summary via Power Automate. Status: {response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Power Automate Flow");
                return "An error occurred while generating the summary.";
            }
        }
    }
}
