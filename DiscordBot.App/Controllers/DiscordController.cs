using DiscordBot.Data.Models;
using DiscordBot.Models;
using DiscordBot.Services;
using DSharpPlus.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace DiscordBot.Controllers;

[ApiController]
[Authorize]
[Route("[controller]/[action]")]
public class DiscordController : ControllerBase
{
    private readonly DiscordService _discordService;

    public DiscordController(DiscordService discordService)
    {
        _discordService = discordService;
    }

    [HttpGet(Name = "hasContentCreator")]
    public async Task<bool> GetHasContentCreator(ulong id)
    {
        Log.Information("Get hasContentCreator");
        var member = await _discordService.Guild.GetMemberAsync(id);

        if(member == null)
            return false;

        return member.Roles.Any(x => x.Id == _discordService.CreatorRole.Id);
    }

    [HttpGet(Name = "user")]
    public async Task<DiscordUserDto?> GetUser(ulong id)
    {
        Log.Information("Get user");
        var member = await _discordService.Guild.GetMemberAsync(id);

        if (member == null)
            return null;

        return new DiscordUserDto()
        {
            Id = member.Id.ToString(),
            Username = member.Username,
            Discriminator = member.Discriminator,
            AvatarUrl = member.AvatarUrl
        };
    }

    [HttpPost(Name = "grantRole")]
    public async Task PostGrantRole(ulong id, string role)
    {
        Log.Information("Post grantRole");
        var member = await _discordService.Guild.GetMemberAsync(id);

        if (member == null)
            return;

        var discordRole = role switch
        {
            nameof(_discordService.MemberRole) => _discordService.MemberRole,
            nameof(_discordService.CreatorRole) => _discordService.CreatorRole,
            nameof(_discordService.BannedRole) => _discordService.BannedRole,
            nameof(_discordService.ReadOnlyRole) => _discordService.ReadOnlyRole,
            nameof(_discordService.DonatorRole) => _discordService.DonatorRole,
            _ => throw new Exception("Invalid role name")
        };

        await member.GrantRoleAsync(discordRole);
    }

    [HttpPost(Name = "revokeRole")]
    public async Task PostRevokeRole(ulong id, string role)
    {
        Log.Information("Post revokeRole");
        var member = await _discordService.Guild.GetMemberAsync(id);

        if (member == null)
            return;

        var discordRole = role switch
        {
            nameof(_discordService.MemberRole) => _discordService.MemberRole,
            nameof(_discordService.CreatorRole) => _discordService.CreatorRole,
            nameof(_discordService.BannedRole) => _discordService.BannedRole,
            nameof(_discordService.ReadOnlyRole) => _discordService.ReadOnlyRole,
            nameof(_discordService.DonatorRole) => _discordService.DonatorRole,
            _ => throw new Exception("Invalid role name")
        };

        await member.RevokeRoleAsync(discordRole);
    }

    [HttpPost(Name = "updateMemberRoles")]
    public async Task PostUpdateMemberRoles(DiscordMemberRolesDto memberRoles)
    {
        Log.Information("Post updateMemberRoles");
        var members = await _discordService.Guild.GetAllMembersAsync();

        if (members == null)
            return;

        foreach (var member in members)
        {
            var hasVIP = memberRoles.MemberRolesVIP.TryGetValue(member.Id, out var vipId);

            foreach (var role in member.Roles)
            {
                if (memberRoles.RolesVIP.Contains(role.Id))
                {
                    if (!hasVIP|| role.Id != vipId)
                    {
                        await member.RevokeRoleAsync(role);
                    }
                }
            }

            if (hasVIP && !member.Roles.Any((role) => role.Id == vipId))
            {
                var role = _discordService.Guild.GetRole(vipId);
                if (role != null)
                    await member.GrantRoleAsync(role);
            }
        }
    }
}