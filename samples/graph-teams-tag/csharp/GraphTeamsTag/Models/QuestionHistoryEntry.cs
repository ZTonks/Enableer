namespace GraphTeamsTag.Models
{
    public class QuestionHistoryEntry
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string QuestionTopic { get; set; }
        public string QuestionContent { get; set; }
        public string[] Tags { get; set; }
        public string ChatId { get; set; }
        public string ChatWebUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string RequesterUserId { get; set; }
        public string? Summary { get; set; }
    }
}

