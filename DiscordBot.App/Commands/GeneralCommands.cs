﻿using DiscordBot.Apis;
using DiscordBot.Services;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Serilog;

namespace DiscordBot.Commands;

public class GeneralCommands : ApplicationCommandModule
{
    private readonly IServerDiscordApi _serverDiscordApi;
    private readonly DiscordService _discordService;

    public GeneralCommands(IServerDiscordApi serverDiscordApi, DiscordService discordService)
    {
        _serverDiscordApi = serverDiscordApi;
        _discordService = discordService;
    }

    [SlashCommand("verify", "help user with verification.")]
    public async Task HelpVerifyCommand(InteractionContext ctx)
    {
        if (ctx.Channel != _discordService.HelpVerifyChannel)
        {
            await ctx.CreateResponseAsync("Invalid channel.");
            return;
        }

        Log.Information("HelpVerifyCommand");
        var message = $"Hello, {ctx.User.Mention}! Check out the {_discordService.VerificationChannel.Mention} channel for detailed steps on how to join our roleplay server and on how to obtain access to our member-only channels.";
        await ctx.CreateResponseAsync(message);
    }

    [SlashCommand("pingserver", "ping server")]
    public async Task PingServerCommand(InteractionContext ctx)
    {
        if (ctx.Channel != _discordService.CommandsChannel)
        {
            await ctx.CreateResponseAsync("Invalid channel.");
            return;
        }

        Log.Information("PingServerCommand");

        var response = await _serverDiscordApi.GetPing();

        if(response)
        {
            Log.Information("PingServerCommand: Success");
            await ctx.CreateResponseAsync("Server is online!");
        }
    }

    [SlashCommand("remove-read-only", "remove read only from user")]
    public async Task RemoveReadOnlyCommand(InteractionContext ctx, [Option("target", "target member to remove read only")] DiscordUser targetMember)
    {
        if (ctx.Channel != _discordService.CommandsChannel)
        {
            await ctx.CreateResponseAsync("Invalid channel.");
            return;
        }
        Log.Information("RemoveReadOnlyCommand");

        var senderMember = ctx.Member;

        if (targetMember == null || senderMember == null)
        {
            await ctx.CreateResponseAsync("```SUCH MEMBERS DO NOT EXIST```");
            return;
        }

        if (targetMember.Id == senderMember.Id)
        {
            await ctx.CreateResponseAsync("```YOU CAN'T DO THIS ACTION ON YOURSELF```");
            return;
        }

        var member = await _discordService.Guild.GetMemberAsync(targetMember.Id);

        if (member.Roles.All(x => x.Id != _discordService.ReadOnlyRole.Id))
        {
            await ctx.CreateResponseAsync("```TARGET DOESN'T HAVE READ-ONLY ROLE```");
            return;
        }

        var result = await _serverDiscordApi.PostRemoveReadOnly(nameof(_discordService.CommandsChannel), ctx.User.Id, ctx.User.Username, targetMember.Id, targetMember.Username);


        if(result)
        {
            await member.RevokeRoleAsync(_discordService.ReadOnlyRole, "Removed read-only");
            await ctx.CreateResponseAsync("Removed read-only role");
            return;
        }

        await ctx.CreateResponseAsync("Failed to remove read-only role");
    }

    [SlashCommand("read-only", "put read only on user")]
    public async Task ReadOnlyCommand(InteractionContext ctx, [Option("target", "target member")] DiscordUser targetMember, [Option("reason", "reason for this action")] string reason)
    {
        if (ctx.Channel != _discordService.CommandsChannel)
            return;
        Log.Information("ReadOnlyCommand");

        var senderMember = ctx.Member;

        if (targetMember == null || senderMember == null)
        {
            await ctx.CreateResponseAsync("```SUCH MEMBERS DO NOT EXIST```");
            return;
        }

        if (targetMember.Id == senderMember.Id)
        {
            await ctx.CreateResponseAsync("```YOU CAN'T DO THIS ACTION ON YOURSELF```");
            return;
        }

        var member = await _discordService.Guild.GetMemberAsync(targetMember.Id);
        if (member.Roles.Any(x => x.Id == _discordService.ReadOnlyRole.Id))
        {
            await ctx.CreateResponseAsync("```TARGET ALREADY HAS READ-ONLY ROLE```");
            return;
        }

        if(await _serverDiscordApi.PostReadOnly(nameof(_discordService.CommandsChannel), ctx.User.Id, ctx.User.Username, targetMember.Id, targetMember.Username, reason))
        {
            await member.GrantRoleAsync(_discordService.ReadOnlyRole);
            await ctx.CreateResponseAsync("Added read-only role");
            return;
        }

        await ctx.CreateResponseAsync("Failed to add read-only role");
    }

    [SlashCommand("apps", "get active applications")]
    public async Task AppsCommand(InteractionContext ctx)
    {
        if (ctx.Channel != _discordService.CommandsChannel)
        {
            await ctx.CreateResponseAsync("Invalid channel.");
            return;
        }
        Log.Information("AppsCommand");

        var waitingIssuers = await _serverDiscordApi.GetApps();
        await ctx.CreateResponseAsync(waitingIssuers);
    }

    [SlashCommand("ann", "post announcement in server")]
    public async Task AnnCommand(InteractionContext ctx, [Option("announcement", "announcement to post on the server")] string text)
    {
        Log.Information("AnnCommand");
        if (ctx.Channel != _discordService.CommandsChannel)
        {
            await ctx.CreateResponseAsync("Invalid channel.");
            return;
        }

        await _serverDiscordApi.PostAnnouncement(text);
        await ctx.CreateResponseAsync("Announcement sent!");
    }

    [SlashCommand("save", "save server state")]
    public async Task SaveCommand(InteractionContext ctx)
    {
        if (ctx.Channel != _discordService.CommandsChannel)
        {
            await ctx.CreateResponseAsync("Invalid channel.");
            return;
        }
        Log.Information("SaveCommand");

        if (await _serverDiscordApi.PostSave())
        {
            await ctx.CreateResponseAsync("Saved to database!");
        }
        else
        {
            await ctx.CreateResponseAsync("Failed to save to database!");
        }
    }

    [SlashCommand("kick", "kick user")]
    public async Task KickCommand(InteractionContext ctx, [Option("target", "target member")] DiscordUser targetMember, [Option("reason", "reason for this action")] string reason)
    {
        if (ctx.Channel != _discordService.CommandsChannel)
        {
            await ctx.CreateResponseAsync("Invalid channel.");
            return;
        }
        Log.Information("KickCommand");

        var senderMember = ctx.Member;

        if (targetMember == null || senderMember == null)
        {
            await ctx.CreateResponseAsync("```SUCH MEMBERS DO NOT EXIST```");
            return;
        }

        if (targetMember.Id == senderMember.Id)
        {
            await ctx.CreateResponseAsync("```YOU CAN'T DO THIS ACTION ON YOURSELF```");
            return;
        }

        if(await _serverDiscordApi.PostKick(nameof(_discordService.CommandsChannel), ctx.User.Id, ctx.User.Username, targetMember.Id, targetMember.Username, reason))
        {
            var member = await _discordService.Guild.GetMemberAsync(targetMember.Id);
            await member.RemoveAsync(reason);
            await ctx.CreateResponseAsync("Kicked user");
            return;
        }

        await ctx.CreateResponseAsync("Failed to kick user");
    }

    [SlashCommand("ban", "ban user")]
    public async Task BanCommand(InteractionContext ctx, [Option("target", "target member")] DiscordUser targetMember, [Option("reason", "reason for this action")] string reason)
    {
        if (ctx.Channel != _discordService.CommandsChannel)
        {
            await ctx.CreateResponseAsync("Invalid channel.");
            return;
        }
        Log.Information("BanCommand");

        var senderMember = ctx.Member;

        if (targetMember == null || senderMember == null)
        {
            await ctx.CreateResponseAsync("```SUCH MEMBERS DO NOT EXIST```");
            return;
        }

        if (targetMember.Id == senderMember.Id)
        {
            await ctx.CreateResponseAsync("```YOU CAN'T DO THIS ACTION ON YOURSELF```");
            return;
        }

        if(await _serverDiscordApi.PostBan(nameof(_discordService.CommandsChannel), ctx.User.Id, ctx.User.Username, targetMember.Id, targetMember.Username, reason))
        {
            await _discordService.Guild.BanMemberAsync(targetMember.Id, 0, reason);
            await ctx.CreateResponseAsync("Banned user");
            return;
        }

        await ctx.CreateResponseAsync("Failed to ban user");
    }
    [SlashCommand("support")]
    public async Task SupportCommand(InteractionContext ctx)
    {
      Log.Information("SupportCommand");
      var message = $"Hello! For general inquiries, verification support, or technical issues, check out the {_discordService.General-supportChannel.Mention}, {_discordService.Technical-supportChannel.Mention}, or {_discordService.Verification-supportChannel.Mention} channels for support and useful links. If your issue does not get resolved, create a ticket using any of these channels.";
      await ctx.CreateResponseAsync(message);
    }
}
