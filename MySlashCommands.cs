using DSharpPlus;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using System;
using System.Threading.Tasks;
using DSharpPlus.Lavalink;
using System.Linq;

namespace FSocietyBot
{
    public class MySlashCommands : ApplicationCommandModule
    {
        [SlashCommand("loop", "loop the current music")]
        public async Task loopMusic(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Starting Slash Command"));

            Program.loopMusic = !Program.loopMusic;
            await ctx.Channel.SendMessageAsync("Loop the music " + Program.loopMusic);
        }

        [SlashCommand("play", "Play any music on youtube")]
        public async Task PlayMusic(InteractionContext ctx, [Option("music-name", "query")] string query)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Starting Slash Command"));

            if (ctx.Member.VoiceState == null)
            {
                await ctx.Channel.SendMessageAsync("Please enter a VC!!!");
                return;
            }
            DiscordChannel userVC = ctx.Member.VoiceState.Channel;
            var lavalinkInstance = ctx.Client.GetLavalink();

            //PRE-EXECUTION CHECKS

            if (!lavalinkInstance.ConnectedNodes.Any())
            {
                await ctx.Channel.SendMessageAsync("Connection is not Established!!!");
                return;
            }

            if (userVC.Type != ChannelType.Voice)
            {
                await ctx.Channel.SendMessageAsync("Please enter a valid VC!!!");
                return;
            }

            //Connecting to the VC and playing music
            var node = lavalinkInstance.ConnectedNodes.Values.First();
            await node.ConnectAsync(userVC);

            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);
            if (conn == null)
            {
                await ctx.Channel.SendMessageAsync("Lavalink Failed to connect!!!");
                return;
            }

            var searchQuery = await node.Rest.GetTracksAsync(query);
            if (searchQuery.LoadResultType == LavalinkLoadResultType.NoMatches || searchQuery.LoadResultType == LavalinkLoadResultType.LoadFailed)
            {
                await ctx.Channel.SendMessageAsync($"Failed to find music with query: {query}");
                return;
            }
            var musicTrack = searchQuery.Tracks.First();

            if (conn.CurrentState.CurrentTrack != null)
            {
                Program.playlist.AddLast(query);
                string musicDescription2 = $"Added To playlist your place {Program.playlist.Count}: {musicTrack.Title} \n" +
                                      $"Author: {musicTrack.Author} \n" +
                                      $"URL: {musicTrack.Uri}";

                var nowPlayingEmbed2 = new DiscordEmbedBuilder()
                {
                    Color = DiscordColor.Purple,
                    Title = $"Successfully joined channel {userVC.Name} and playing music",
                    Description = musicDescription2
                };

                await ctx.Channel.SendMessageAsync(embed: nowPlayingEmbed2);
                return;
            }

            await conn.PlayAsync(musicTrack);
            Program.currenttrack = musicTrack;

            string musicDescription = $"Now Playing: {musicTrack.Title} \n" +
                                      $"Author: {musicTrack.Author} \n" +
                                      $"URL: {musicTrack.Uri}";

            var nowPlayingEmbed = new DiscordEmbedBuilder()
            {
                Color = DiscordColor.Purple,
                Title = $"Successfully joined channel {userVC.Name} and playing music",
                Description = musicDescription
            };

            await ctx.Channel.SendMessageAsync(embed: nowPlayingEmbed);
            await Program.CheckMusic(ctx);
        }

        [SlashCommand("pause", "pause music")]
        public async Task PauseMusic(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Starting Slash Command"));

            var userVC = ctx.Member.VoiceState.Channel;
            var lavalinkInstance = ctx.Client.GetLavalink();

            //PRE-EXECUTION CHECKS
            if (ctx.Member.VoiceState == null || userVC == null)
            {
                await ctx.Channel.SendMessageAsync("Please enter a VC!!!");
                return;
            }

            if (!lavalinkInstance.ConnectedNodes.Any())
            {
                await ctx.Channel.SendMessageAsync("Connection is not Established!!!");
                return;
            }

            if (userVC.Type != ChannelType.Voice)
            {
                await ctx.Channel.SendMessageAsync("Please enter a valid VC!!!");
                return;
            }

            var node = lavalinkInstance.ConnectedNodes.Values.First();
            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (conn == null)
            {
                await ctx.Channel.SendMessageAsync("Lavalink Failed to connect!!!");
                return;
            }

            if (conn.CurrentState.CurrentTrack == null)
            {
                await ctx.Channel.SendMessageAsync("No tracks are playing!!!");
                return;
            }

            await conn.PauseAsync();

            var pausedEmbed = new DiscordEmbedBuilder()
            {
                Color = DiscordColor.Yellow,
                Title = "Track Paused!!"
            };

            await ctx.Channel.SendMessageAsync(embed: pausedEmbed);
        }

        [SlashCommand("skip", "skip music")]
        public async Task SkipMusic(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Starting Slash Command"));

            var lavalinkInstance = ctx.Client.GetLavalink();
            var userVC = ctx.Member.VoiceState.Channel;


            var node = lavalinkInstance.ConnectedNodes.Values.First();
            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);
            string query = Program.nextQuery();
            if (query != string.Empty)
            {
                var searchQuery = await node.Rest.GetTracksAsync(query);

                var musicTrack = searchQuery.Tracks.First();

                await conn.PlayAsync(musicTrack);
                Program.currenttrack = musicTrack;
                string musicDescription2 = $"Now Playing: {musicTrack.Title} \n" +
                      $"Author: {musicTrack.Author} \n" +
                      $"URL: {musicTrack.Uri}";

                var nowPlayingEmbed2 = new DiscordEmbedBuilder()
                {
                    Color = DiscordColor.Purple,
                    Title = $"Successfully joined channel {userVC.Name} and playing music",
                    Description = musicDescription2
                };

                await ctx.Channel.SendMessageAsync(embed: nowPlayingEmbed2);
            }
            else
            {
                var nowPlayingEmbed2 = new DiscordEmbedBuilder()
                {
                    Color = DiscordColor.Red,
                    Title = $"Error",
                    Description = "There is no music in playlist"
                };

                await ctx.Channel.SendMessageAsync(embed: nowPlayingEmbed2);
                await conn.StopAsync();
                await conn.DisconnectAsync();
            }
        }

        [SlashCommand("resume", "resume music")]
        public async Task ResumeMusic(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Starting Slash Command"));

            var userVC = ctx.Member.VoiceState.Channel;
            var lavalinkInstance = ctx.Client.GetLavalink();

            //PRE-EXECUTION CHECKS
            if (ctx.Member.VoiceState == null || userVC == null)
            {
                await ctx.Channel.SendMessageAsync("Please enter a VC!!!");
                return;
            }

            if (!lavalinkInstance.ConnectedNodes.Any())
            {
                await ctx.Channel.SendMessageAsync("Connection is not Established!!!");
                return;
            }

            if (userVC.Type != ChannelType.Voice)
            {
                await ctx.Channel.SendMessageAsync("Please enter a valid VC!!!");
                return;
            }

            var node = lavalinkInstance.ConnectedNodes.Values.First();
            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (conn == null)
            {
                await ctx.Channel.SendMessageAsync("Lavalink Failed to connect!!!");
                return;
            }

            if (conn.CurrentState.CurrentTrack == null)
            {
                await ctx.Channel.SendMessageAsync("No tracks are playing!!!");
                return;
            }

            await conn.ResumeAsync();

            var resumedEmbed = new DiscordEmbedBuilder()
            {
                Color = DiscordColor.Green,
                Title = "Resumed"
            };

            await ctx.Channel.SendMessageAsync(embed: resumedEmbed);
        }

        [SlashCommand("stop", "stop music")]
        public async Task StopMusic(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Starting Slash Command"));

            var userVC = ctx.Member.VoiceState.Channel;
            var lavalinkInstance = ctx.Client.GetLavalink();

            //PRE-EXECUTION CHECKS
            if (ctx.Member.VoiceState == null || userVC == null)
            {
                await ctx.Channel.SendMessageAsync("Please enter a VC!!!");
                return;
            }

            if (!lavalinkInstance.ConnectedNodes.Any())
            {
                await ctx.Channel.SendMessageAsync("Connection is not Established!!!");
                return;
            }

            if (userVC.Type != ChannelType.Voice)
            {
                await ctx.Channel.SendMessageAsync("Please enter a valid VC!!!");
                return;
            }

            var node = lavalinkInstance.ConnectedNodes.Values.First();
            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (conn == null)
            {
                await ctx.Channel.SendMessageAsync("Lavalink Failed to connect!!!");
                return;
            }

            if (conn.CurrentState.CurrentTrack == null)
            {
                await ctx.Channel.SendMessageAsync("No tracks are playing!!!");
                return;
            }

            await conn.StopAsync();
            await conn.DisconnectAsync();

            var stopEmbed = new DiscordEmbedBuilder()
            {
                Color = DiscordColor.Red,
                Title = "Stopped the Track",
                Description = "Successfully disconnected from the VC"
            };

            await ctx.Channel.SendMessageAsync(embed: stopEmbed);
        }
        [SlashCommand("help", "Need Help ?")]
        public async Task HelpCommand(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Starting Slash Command"));
            var helpMessage = new DiscordMessageBuilder()
                .AddEmbed(new DiscordEmbedBuilder()

                .WithColor(DiscordColor.Azure)
                .WithTitle("Help Menu")
                .WithDescription("!penis for get result of your penis size")
                );
            await ctx.Channel.SendMessageAsync(helpMessage);
        }
        //[SlashCommand("test", "This is our first Slash Command")]
        //public async Task TestSlashCommand(InteractionContext ctx, [Choice("Pre-Defined Text", "afhajfjafjdghldghlhg")]
        //                                                           [Option("string", "Type in anything you want")] string text)
        //{
        //    await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
        //                                                                                    .WithContent("Starting Slash Command"));

        //    var embedMessage = new DiscordEmbedBuilder()
        //    {
        //        Title = text,
        //    };

        //    await ctx.Channel.SendMessageAsync(embed: embedMessage);
        //}
        [SlashCommand("poll", "Create your own poll")]
        public async Task PollCommand(InteractionContext ctx, [Option("question", "The main poll subject/question")] string Question,
                                                              [Option("timelimit", "The second set on this poll")] long TimeLimit,
                                                              [Option("option1", "Option 1")] string Option1,
                                                              [Option("option2", "Option 1")] string Option2,
                                                              [Option("option3", "Option 1")] string Option3,
                                                              [Option("option4", "Option 1")] string Option4)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Starting Slash Command"));

            var interactvity = ctx.Client.GetInteractivity(); //Getting the Interactivity Module
            TimeSpan timer = TimeSpan.FromSeconds(TimeLimit); //Converting my time parameter to a timespan variable

            DiscordEmoji[] optionEmojis = { DiscordEmoji.FromName(ctx.Client, ":one:", false),
                                            DiscordEmoji.FromName(ctx.Client, ":two:", false),
                                            DiscordEmoji.FromName(ctx.Client, ":three:", false),
                                            DiscordEmoji.FromName(ctx.Client, ":four:", false) }; //Array to store discord emojis

            string optionsString = optionEmojis[0] + " | " + Option1 + "\n" +
                                   optionEmojis[1] + " | " + Option2 + "\n" +
                                   optionEmojis[2] + " | " + Option3 + "\n" +
                                   optionEmojis[3] + " | " + Option4; //String to display each option with its associated emojis

            var pollMessage = new DiscordMessageBuilder()
                .AddEmbed(new DiscordEmbedBuilder()

                .WithColor(DiscordColor.Azure)
                .WithTitle(string.Join(" ", Question))
                .WithDescription(optionsString)
                ); //Making the Poll message

            var putReactOn = await ctx.Channel.SendMessageAsync(pollMessage); //Storing the await command in a variable

            foreach (var emoji in optionEmojis)
            {
                await putReactOn.CreateReactionAsync(emoji); //Adding each emoji from the array as a reaction on this message
            }

            var result = await interactvity.CollectReactionsAsync(putReactOn, timer); //Collects all the emoji's and how many peopele reacted to those emojis

            int count1 = 0; //Counts for each emoji
            int count2 = 0;
            int count3 = 0;
            int count4 = 0;

            foreach (var emoji in result) //Foreach loop to go through all the emojis in the message and filtering out the 4 emojis we need
            {
                if (emoji.Emoji == optionEmojis[0])
                {
                    count1++;
                }
                if (emoji.Emoji == optionEmojis[1])
                {
                    count2++;
                }
                if (emoji.Emoji == optionEmojis[2])
                {
                    count3++;
                }
                if (emoji.Emoji == optionEmojis[3])
                {
                    count4++;
                }
            }

            int totalVotes = count1 + count2 + count3 + count4;

            string resultsString = optionEmojis[0] + ": " + count1 + " Votes \n" +
                       optionEmojis[1] + ": " + count2 + " Votes \n" +
                       optionEmojis[2] + ": " + count3 + " Votes \n" +
                       optionEmojis[3] + ": " + count4 + " Votes \n\n" +
                       "The total number of votes is " + totalVotes; //String to show the results of the poll

            var resultsMessage = new DiscordMessageBuilder()
                .AddEmbed(new DiscordEmbedBuilder()

                .WithColor(DiscordColor.Green)
                .WithTitle("Results of Poll")
                .WithDescription(resultsString)
                );

            await ctx.Channel.SendMessageAsync(resultsMessage); //Making the embed and sending it off            
        }
        [RequirePermissions(Permissions.ManageMessages)]
        [SlashCommand("purge", "Delete specific messages in a channel")]
        public async Task PurgeCommand(InteractionContext ctx, [Option("amount", "The number of messages to delete")] long amount)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Starting Slash Command"));

            if (amount <= 0 || amount > 100)
            {
                await ctx.Channel.SendMessageAsync("Please enter a valid amount between 1 and 100.");
                return;
            }

            var messages = await ctx.Channel.GetMessagesAsync((int)(amount + 1)); // +1 because we want to include the command message as well

            foreach (var message in messages)
            {
                await message.DeleteAsync();
            }

            await ctx.Channel.SendMessageAsync($"Deleted {amount} messages from {ctx.Channel.Mention}.");
        }

        //[SlashCommand("caption", "Give any image a Caption")]
        //public async Task CaptionCommand(InteractionContext ctx, [Option("caption", "The caption you want the image to have")] string caption,
        //                                                         [Option("image", "The image you want to upload")] DiscordAttachment picture)
        //{
        //    await ctx.DeferAsync();

        //    var captionMessage = new DiscordMessageBuilder()
        //        .AddEmbed(new DiscordEmbedBuilder()
        //            .WithColor(DiscordColor.Azure)
        //            .WithFooter(caption)
        //            .WithImageUrl(picture.Url)
        //            );

        //    await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(captionMessage.Embed));
        //}

        //User Requested Commands

        //[SlashCommand("create-VC", "Creates a voice channel")]
        //public async Task CreateVC(InteractionContext ctx, [Option("channel-name", "Name of this Voice Channel")] string channelName,
        //                                                   [Option("member-limit", "Adds a user limit to this channel")] string channelLimit = null)
        //{
        //    await ctx.DeferAsync();

        //    var channelUsersParse = int.TryParse(channelLimit, out int channelUsers);

        //    //Create the Voice Channel with the channel limit
        //    if (channelLimit != null && channelUsersParse == true)
        //    {
        //        await ctx.Guild.CreateVoiceChannelAsync(channelName, null, null, channelUsers);

        //        var success = new DiscordEmbedBuilder()
        //        {
        //            Title = "Created Voice Channel " + channelName,
        //            Description = "The channel was created with a user limit of " + channelLimit.ToString(),
        //            Color = DiscordColor.Azure
        //        };

        //        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(success));
        //    }
        //    //No User Limit
        //    else if (channelLimit == null)
        //    {
        //        await ctx.Guild.CreateVoiceChannelAsync(channelName);

        //        var success = new DiscordEmbedBuilder()
        //        {
        //            Title = "Created Voice Channel " + channelName,
        //            Color = DiscordColor.Azure
        //        };

        //        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(success));
        //    }
        //    //Invalid input parsed in
        //    else if (channelUsersParse == false)
        //    {
        //        var fail = new DiscordEmbedBuilder()
        //        {
        //            Title = "Please provide a valid number for the user limit",
        //            Color = DiscordColor.Red
        //        };

        //        await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(fail));
        //    }
        //}
    }
}
