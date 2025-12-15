// <copyright file="AskQuestionRequest.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

using System.ComponentModel.DataAnnotations;

namespace GraphTeamsTag.Models;

public class AskQuestionRequest : IValidatableObject
{
    public string Tag { get; set; }

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
        if (string.IsNullOrWhiteSpace(Tag))
        {
            yield return new ValidationResult("Tag is required", new string[] { nameof(Tag) });
        }

        if (string.IsNullOrWhiteSpace(Question))
        {
            yield return new ValidationResult("Question is required", new string[] { nameof(Question) });
        }
    }
}


