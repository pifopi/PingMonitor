using Discord;
using Discord.WebSocket;
using System.Net.NetworkInformation;

class Program
{
    private enum Channel
    {
        None = 0,
        Google = 1 << 0,
        Jeedom = 1 << 1,
        All = Google | Jeedom,
    }

    private class HostConfig
    {
        public string Name { get; set; }
        public string IP { get; set; }
        public ulong LogChannelId { get; set; }
        public IMessageChannel? LogChannel { get; set; }
    }

    private static readonly Dictionary<Channel, HostConfig> _hostConfigurations = new()
    {
        {
            Channel.Google,
            new HostConfig
            {
                Name = "Google",
                IP = "google.com",
                LogChannelId = 1469384230160171018,

            }
        },
        {
            Channel.Jeedom,
            new HostConfig
            {
                Name = "Jeedom",
                IP = "192.168.1.53",
                LogChannelId = 1469389859599941825,
            }
        }
    };

    private DiscordSocketClient client = new DiscordSocketClient();

    static Task Main(string[] args) => new Program().MainAsync();

    public async Task MainAsync()
    {
        var token = Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN");
        if (string.IsNullOrWhiteSpace(token))
        {
            await Log("Please set the DISCORD_BOT_TOKEN environment variable before running.", Channel.None).ConfigureAwait(false);
            return;
        }

        client.Ready += OnReadyAsync;

        await client.LoginAsync(TokenType.Bot, token).ConfigureAwait(false);
        await client.StartAsync().ConfigureAwait(false);

        await Log("Waiting for bot to be ready.", Channel.None).ConfigureAwait(false);


        Ping ping = new Ping();
        while (true)
        {
            foreach (var (channel, config) in _hostConfigurations)
            {
                PingReply pingResult = await ping.SendPingAsync(config.IP).ConfigureAwait(false);
                if (pingResult.Status == IPStatus.Success)
                {
                    await Log($"Ping to {config.Name} ({config.IP}) successful. Time: {pingResult.RoundtripTime}", channel).ConfigureAwait(false);
                }
                else
                {
                    await Log($"<@282197676982927375> Ping to {config.Name} ({config.IP}) failed. Status: {pingResult.Status}", channel).ConfigureAwait(false);
                }
            }
            await Task.Delay(TimeSpan.FromMinutes(1));
        }
    }

    private async Task OnReadyAsync()
    {
        await Log($"Logged in as {client.CurrentUser}.", Channel.All).ConfigureAwait(false);
        foreach (var (channel, config) in _hostConfigurations)
        {
            if (await client.GetChannelAsync(config.LogChannelId).ConfigureAwait(false) is IMessageChannel logChannel)
            {
                config.LogChannel = logChannel;
            }
            else
            {
                await Log($"Couldn't find channel with ID: {config.LogChannelId}.", channel).ConfigureAwait(false);
            }
        }
        await Log($"Bot is ready.", Channel.All).ConfigureAwait(false);
    }

    private static async Task Log(string message, Channel channel)
    {
        Console.WriteLine($"{channel} - {message}");
        if (channel.HasFlag(Channel.Google))
        {
            if (_hostConfigurations[Channel.Google].LogChannel is IMessageChannel logChannel)
            {
                await logChannel.SendMessageAsync(message).ConfigureAwait(false);
            }
        }
        if (channel.HasFlag(Channel.Jeedom))
        {
            if (_hostConfigurations[Channel.Jeedom].LogChannel is IMessageChannel logChannel)
            {
                await logChannel.SendMessageAsync(message).ConfigureAwait(false);
            }
        }
    }
}