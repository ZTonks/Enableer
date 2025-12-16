using System.Text.Json;
using GraphTeamsTag.Models;

namespace GraphTeamsTag.Services
{
    public class QuestionHistoryService
    {
        private const string FilePath = "question_history.json";
        private readonly object _lock = new object();

        public List<QuestionHistoryEntry> GetHistory()
        {
            lock (_lock)
            {
                if (!File.Exists(FilePath)) return new List<QuestionHistoryEntry>();
                var json = File.ReadAllText(FilePath);
                try
                {
                    return JsonSerializer.Deserialize<List<QuestionHistoryEntry>>(json) ?? new List<QuestionHistoryEntry>();
                }
                catch
                {
                    return new List<QuestionHistoryEntry>();
                }
            }
        }

        public void AddQuestion(QuestionHistoryEntry question)
        {
            lock (_lock)
            {
                var history = GetHistory();
                history.Add(question);
                File.WriteAllText(FilePath, JsonSerializer.Serialize(history, new JsonSerializerOptions { WriteIndented = true }));
            }
        }

        public void AddSummary(string chatId, string summary)
        {
            lock (_lock)
            {
                var history = GetHistory();
                var question = history.FirstOrDefault(q => q.ChatId == chatId);
                if (question != null)
                {
                    question.Summary = summary;
                    File.WriteAllText(FilePath, JsonSerializer.Serialize(history, new JsonSerializerOptions { WriteIndented = true }));
                }
            }
        }

        public List<QuestionHistoryEntry> GetTopQuestionsByTag(string tagId, int count = 5)
        {
             var history = GetHistory();
             return history
                .Where(q => q.Tags != null && q.Tags.Select(t => t.Id).Contains(tagId))
                .OrderByDescending(q => q.CreatedAt)
                .Take(count)
                .ToList();
        }
    }
}

