// <copyright file="TeamTagController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace GraphTeamsTag.Controllers
{
    using GraphTeamsTag.Helper;
    using GraphTeamsTag.Models;
    using GraphTeamsTag.Provider;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Graph;
    using Microsoft.Graph.Models;
    using System.Threading.Tasks;
    using System.Collections.Generic;

    [Route("api/teamtag")]
    [ApiController]
    public class TeamTagController : Controller
    {
        /// <summary>
        /// Gets app details.
        /// </summary>
        private readonly ILogger<TeamTagController> _logger;

        /// <summary>
        /// Graph helper class using graph api's.
        /// </summary>
        private readonly GraphHelper graphHelper;

        /// <summary>
        /// Graph client factory for creating GraphServiceClient instances.
        /// </summary>
        private readonly IGraphClientFactory _graphClientFactory;

        /// <summary>
        /// Stores the Azure configuration values.
        /// </summary>
        private readonly IConfiguration _configuration;

        /// <summary>
        /// client Id for the application.
        /// </summary>
        private static readonly string ClientIdConfigurationSettingsKey = "AzureAd:ClientId";

        public TeamTagController(ILogger<TeamTagController> logger, IConfiguration configuration,
            GraphHelper graphHelper, IGraphClientFactory graphClientFactory)
        {
            _logger = logger;
            _configuration = configuration;
            this.graphHelper = graphHelper;
            _graphClientFactory = graphClientFactory;
        }

        /// <summary>
        /// Gets app details.
        /// </summary>
        /// <returns>If success return 200 status code, otherwise 500 status code</returns>
        [HttpGet("getAppData")]
        public string GetAppData()
        {
            try
            {
                var clientId = _configuration[ClientIdConfigurationSettingsKey];
                return clientId;
            }
            catch (Exception ex)
            {
                return "Error while fetching app id";
            }
        }

        /// <summary>
        /// Create team tag.
        /// /// </summary>
        /// <param name="teamId">Id of team.</param>
        /// <param name="teamTag">Details of the tag to be created.</param>
        /// <returns>If success return 201 status code, otherwise 500 status code</returns>
        [HttpPost("{teamId}")]
        public async Task<IActionResult> CreateTeamTagAsync([FromRoute] string teamId, [FromBody] TeamTagUpdateDto teamTag)
        {
            try
            {
                await this.graphHelper.CreateTeamworkTagAsync(teamTag, teamId);
                return this.StatusCode(201);
            }
            catch (Exception ex)
            {
                return this.StatusCode(500);
            }
        }

        /// <summary>
        /// Gets the tag details.
        /// </summary>
        /// <param name="ssoToken">Token to be exchanged.</param>
        /// <param name="teamId">Id of team.</param>
        /// <param name="teamTagId">Id of tag.</param>
        /// <returns>If success return 200 status code, otherwise 500 status code</returns>
        [HttpGet("tag")]
        public async Task<IActionResult> GetTeamTagAsync([FromQuery] string ssoToken, string teamId, string teamTagId)
        {
            try
            {
                var graphClient = await _graphClientFactory.CreateGraphClientAsync(ssoToken);
                var teamworkTag = await graphClient.Teams[teamId].Tags[teamTagId]
                .GetAsync();

                var teamwTagDto = new TeamTag
                {
                    Id = teamworkTag.Id,
                    DisplayName = teamworkTag.DisplayName,
                    Description = teamworkTag.Description
                };

                return this.Ok(teamwTagDto);
            }
            catch (Exception ex)
            {
                return this.StatusCode(500);
            }
        }

        /// <summary>
        /// List all the tags for the specified team.
        /// </summary>
        /// <param name="ssoToken">Token to be exchanged.</param>
        /// <param name="teamId">Id of the team.</param>
        /// <returns>If success return list of tags, otherwise 500 status code</returns>
        [HttpGet("list")]
        public async Task<IActionResult> ListTeamTagAsync([FromQuery] string ssoToken, string teamId)
        {
            try
            {
                var graphClient = await _graphClientFactory.CreateGraphClientAsync(ssoToken);

                var tagsResponse = await graphClient.Teams[teamId].Tags.GetAsync();
                var teamworkTagList = new List<TeamTag>();
                
                if (tagsResponse?.Value != null)
                {
                    var pageIterator = PageIterator<TeamworkTag, TeamworkTagCollectionResponse>.CreatePageIterator(
                        graphClient,
                        tagsResponse,
                        (tag) =>
                        {
                            teamworkTagList.Add(new TeamTag
                            {
                                Id = tag.Id,
                                DisplayName = tag.DisplayName,
                                Description = tag.Description,
                                MembersCount = tag.MemberCount ?? 0,
                            });
                            return true;
                        }
                    );

                    await pageIterator.IterateAsync();
                }

                return this.Ok(teamworkTagList);
            }
            catch (Exception e)
            {
                return this.StatusCode(500);
            }
        }

        /// <summary>
        /// Updates the tag.
        /// </summary>
        /// <param name="teamId">Id of team.</param>
        /// <param name="teamTag">Updated details of the tag.</param>
        /// <returns>If success return 204 status code, otherwise 500 status code</returns>
        [HttpPatch("{teamId}/update")]
        public async Task<IActionResult> UpdateTeamTagAsync([FromRoute] string teamId, [FromBody] TeamTagUpdateDto teamTag)
        {
            try
            {
                await this.graphHelper.UpdateTeamworkTagAsync(teamTag, teamId);
                return this.NoContent();
            }
            catch (Exception ex)
            {
                return this.StatusCode(500);
            }
        }

        /// <summary>
        /// Get list of tag's member of the specified tag.
        /// </summary>
        /// <param name="ssoToken">Token to be exchanged.</param>
        /// <param name="teamId">Id of the team.</param>
        /// <param name="tagId">Id of the tag.</param>
        /// <returns>If success return 200 status code, otherwise 500 status code</returns>
        [HttpGet("{teamId}/tag/{tagId}/members")]
        public async Task<IActionResult> GetTeamworkTagMembersAsync([FromQuery] string ssoToken, [FromRoute] string teamId, [FromRoute] string tagId)
        {
            try
            {
                var graphClient = await _graphClientFactory.CreateGraphClientAsync(ssoToken);
                var membersResponse = await graphClient.Teams[teamId].Tags[tagId].Members
                 .GetAsync();

                var tagMemberList = new List<TeamworkTagMember>();

                if (membersResponse?.Value != null)
                {
                    var pageIterator = PageIterator<TeamworkTagMember, TeamworkTagMemberCollectionResponse>.CreatePageIterator(
                        graphClient,
                        membersResponse,
                        (member) =>
                        {
                            tagMemberList.Add(member);
                            return true;
                        }
                    );

                    await pageIterator.IterateAsync();
                }

                return this.Ok(tagMemberList);
            }
            catch (Exception ex)
            {
                return this.StatusCode(500);
            }
        }

        /// <summary>
        /// Deletes existing tag.
        /// </summary>
        /// <param name="ssoToken">Token to be exchanged.</param>
        /// <param name="teamId">Id of team.</param>
        /// <param name="tagId">Id of tag to be deleted.</param>
        /// <returns></returns>
        [HttpDelete("")]
        public async Task<IActionResult> DeleteTeamTagAsync([FromQuery] string ssoToken, [FromQuery] string teamId, [FromQuery] string tagId)
        {
            try
            {
                var graphClient = await _graphClientFactory.CreateGraphClientAsync(ssoToken);
                await graphClient.Teams[teamId].Tags[tagId]
                .DeleteAsync();
                return this.NoContent();
            }
            catch (Exception ex)
            {
                return this.StatusCode(500);
            }
        }

        /// <summary>
        /// Duplicates tag naming it as {tag} (1).
        /// </summary>
        /// <param name="ssoToken">Token to be exchanged.</param>
        /// <param name="teamId">Id of team.</param>
        /// <param name="tagId">Id of tag to be duplicated.</param>
        /// <returns></returns>
        [HttpPost("duplicate")]
        public async Task<IActionResult> DuplicateTag(
            [FromQuery] string ssoToken, 
            [FromQuery] string teamId,
            [FromQuery] string tagId)
        {
            try
            {
                var graphClient = await _graphClientFactory.CreateGraphClientAsync(ssoToken);

                var team = graphClient.Teams[teamId];

                var tag = team.Tags[tagId].GetAsync();
                var tagMembers = team.Tags[tagId].Members.GetAsync();

                await Task.WhenAll(tag, tagMembers);

                if (tag.Result is null)
                {
                    return BadRequest();
                }

                var tagPost = tag.Result;

                tagPost.DisplayName += " (1)";
                tagPost.Description = tag.Result.Description;
                tagPost.Members ??= [];
                tagPost.Members.AddRange(tagMembers.Result.Value);

                await team.Tags.PostAsync(tagPost);

                return NoContent();
            }
            catch (Exception ex)
            {
                return this.StatusCode(500);
            }
        }
    }
}
