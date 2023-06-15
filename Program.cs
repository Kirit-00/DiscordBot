using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Lavalink;
using DSharpPlus.Net;
using DSharpPlus.Net.Udp;
using DSharpPlus.SlashCommands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FSocietyBot
{
    public sealed class Program
    {
        //Main Discord Properties

        public static LinkedList<string> playlist = new LinkedList<string>();
        private static DiscordClient Client { get; set; }
        private static CommandsNextExtension Commands { get; set; }
        public static bool loopMusic = false;
        public static LavalinkTrack currenttrack = null;

        static async Task Main(string[] args)
        {
            //Instantiating the class with the Instance property


            //Making a Bot Configuration with our token & additional settings
            var config = new DiscordConfiguration()
            {
                Intents = DiscordIntents.All,
                Token = "YOUR TOKEN",
                TokenType = TokenType.Bot,
                AutoReconnect = true,
            };

            //Initializing the client with this config
            Client = new DiscordClient(config);

            //Setting our default timeout for Interactivity based commands
            Client.UseInteractivity(new InteractivityConfiguration()
            {
                Timeout = TimeSpan.FromMinutes(2)
            });

            //EVENT HANDLERS
            Client.Ready += OnClientReady;
            Client.GuildMemberAdded += UserJoinHandler;

            //Setting up our Commands Configuration with our Prefix
            var commandsConfig = new CommandsNextConfiguration()
            {
                StringPrefixes = new string[] { "!" },
                EnableMentionPrefix = true,
                EnableDms = true,
                EnableDefaultHelp = false,
            };

            //Enabling the use of commands with our config & also enabling use of Slash Commands
            Commands = Client.UseCommandsNext(commandsConfig);
            var slashCommandsConfig = Client.UseSlashCommands();

            //register Commands
            slashCommandsConfig.RegisterCommands<MySlashCommands>();
            Commands.RegisterCommands<MyCommands>();

            //ERROR EVENT HANDLERS
            Commands.CommandErrored += OnCommandError;

            //Lavalink Configuration
            var endpoint = new ConnectionEndpoint
            {
                Hostname = "ssl.horizxon.studio",
                Port = 443,
                Secured = true
            };

            var lavalinkConfig = new LavalinkConfiguration
            {
                Password = "horizxon.studio",
                RestEndpoint = endpoint,
                SocketEndpoint = endpoint
            };

            var lavalink = Client.UseLavalink();

            //Connect to the Client and get the Bot online
            await Client.ConnectAsync();
            await lavalink.ConnectAsync(lavalinkConfig);

            await Task.Delay(-1);
        }
        public static string nextQuery()
        {
            string ret = string.Empty;
            foreach (var item in playlist)
            {
                ret = item;
                playlist.Remove(item);
                break;
            }
            return ret;
        }
        public static async Task CheckMusic(InteractionContext ctx)
        {
            var userVC = ctx.Member.VoiceState.Channel;
            var lavalinkInstance = ctx.Client.GetLavalink();

            var node = lavalinkInstance.ConnectedNodes.Values.First();
            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);
            while (true)
            {
                if (conn != null)
                    if (conn.CurrentState.CurrentTrack == null)
                    {
                        string query = nextQuery();
                        if (query != string.Empty)
                        {
                            var searchQuery = await node.Rest.GetTracksAsync(query);

                            var musicTrack = searchQuery.Tracks.First();

                            await conn.PlayAsync(musicTrack);
                        }
                        else if (loopMusic)
                        {
                            await conn.PlayAsync(currenttrack);
                        }
                        else
                        {
                            await conn.DisconnectAsync();
                        }
                    }
            }
        }
        private static async Task UserJoinHandler(DiscordClient sender, GuildMemberAddEventArgs e)
        {
            var defaultChannel = e.Guild.GetDefaultChannel();

            var welcomeEmbed = new DiscordEmbedBuilder()
            {
                Color = DiscordColor.Gold,
                Title = $"{e.Member.Username} Welcome to FSociety",
                Description = "Hope you enjoy your stay"
            };

            await defaultChannel.SendMessageAsync(embed: welcomeEmbed);
        }

        private static Task OnClientReady(DiscordClient sender, ReadyEventArgs e)
        {
            return Task.CompletedTask;
        }

        private static async Task OnCommandError(CommandsNextExtension sender, CommandErrorEventArgs e)
        {
            if (e.Exception is ChecksFailedException)
            {
                var castedException = (ChecksFailedException)e.Exception; //Casting my ErrorEventArgs as a ChecksFailedException
                string cooldownTimer = string.Empty;

                foreach (var check in castedException.FailedChecks)
                {
                    var cooldown = (CooldownAttribute)check; //The cooldown that has triggered this method
                    TimeSpan timeLeft = cooldown.GetRemainingCooldown(e.Context); //Getting the remaining time on this cooldown
                    cooldownTimer = timeLeft.ToString(@"hh\:mm\:ss");
                }

                var cooldownMessage = new DiscordEmbedBuilder()
                {
                    Title = "Wait for the Cooldown to End",
                    Description = "Remaining Time: " + cooldownTimer,
                    Color = DiscordColor.Red
                };

                await e.Context.Channel.SendMessageAsync(embed: cooldownMessage);
            }
        }
    }
}
