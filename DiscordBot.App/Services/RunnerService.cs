﻿using DiscordBot.Apis;
using DSharpPlus.Entities;
using Serilog;

namespace DiscordBot.Services;

public class RunnerService : BackgroundService
{
    private readonly DiscordService _discordService;
    private readonly GuildJoinService _guildJoinService;
    private readonly IServerDiscordApi _serverApi;


    public RunnerService(DiscordService discordService, GuildJoinService guildJoinService, IServerDiscordApi serverApi)
    {
        _discordService = discordService;
        _guildJoinService = guildJoinService;
        _serverApi = serverApi;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var status = await _serverApi.GetPing();

                if(!status)
                {
                    throw new Exception("Server is down.");
                }
            }
            // A simplified, more dev-friendly error if we hit a rather standard error.
            catch(HttpRequestException httpException)
            {
                await _discordService.SetPresence(ActivityType.Watching, "server status: DOWN !");
                // A warning is more accurate to something that may cause issues if not resolved.
                Log.Warning($"RunnerService: Caught HttpRequestException from serverApi.GetPing: {httpException.Message}", "RunnerService");
            }
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
        Console.WriteLine("Running...");
    }
}
