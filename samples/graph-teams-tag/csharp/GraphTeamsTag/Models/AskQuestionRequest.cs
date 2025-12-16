// <copyright file="AskQuestionRequest.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.ComponentModel.DataAnnotations;

namespace GraphTeamsTag.Models;

public class AskQuestionRequest : IValidatableObject
{
    public string[] Tags { get; set; }

    public string QuestionTopic { get; set; }

    public string Question { get; set; }

    public string TeamId { get; set; }

    public bool TargetsOnlineUsers { get; set; }

    /// <summary>
    /// Do we want to send an email or use teams
    /// </summary>
    public bool Email { get; set; }

    public QuestionTarget QuestionTarget { get; set; }

    public string RequesterUserId { get; set;  }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Tags.Length == 0)
        {
            yield return new ValidationResult("Tags are required", new string[] { nameof(Tags) });
        }

        if (Tags.Any(string.IsNullOrWhiteSpace))
        {
            yield return new ValidationResult("Tag is required", new string[] { nameof(Tags) });
        }

        if (string.IsNullOrWhiteSpace(QuestionTopic))
        {
            yield return new ValidationResult("Topic is required", new string[] { nameof(QuestionTopic) });
        }

        if (string.IsNullOrWhiteSpace(Question))
        {
            yield return new ValidationResult("Question is required", new string[] { nameof(Question) });
        }

        if (string.IsNullOrWhiteSpace(RequesterUserId))
        {
            yield return new ValidationResult("Requester is required", new string[] { nameof(RequesterUserId) });
        }

        if (TargetsOnlineUsers && Email)
        {
            yield return new ValidationResult("Cannot target online users for emails", new string[] { nameof(Email), nameof(TargetsOnlineUsers) });
        }
    }
}

