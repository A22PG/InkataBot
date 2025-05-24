using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.SlashCommands;
using InkataBot.commands;
using InkataBot.config;
using InkataBot.slash;
using InkataBot.variablesGlobales;

namespace InkataBot
{
    internal class Program
    {
        public static DiscordClient Client { get; set; }
        private static CommandsNextExtension Commands { get; set; }

        static async Task Main(string[] args)
        {

            var jsonReader = new JSONReader();
            await jsonReader.ReadJSON();
            var discordConfig = new DiscordConfiguration()
            {
                Intents = DiscordIntents.All,
                Token = jsonReader.token,
                TokenType = TokenType.Bot,
                AutoReconnect = true
            };
            Client = new DiscordClient(discordConfig);

            Client.Ready += Client_Ready;
            Client.GuildMemberAdded += Client_GuildMemberAdded;
            DiscordClient bot = Client;

            var commandsConfig = new CommandsNextConfiguration()
            {
                StringPrefixes = new string[] { jsonReader.prefix },
                EnableMentionPrefix = true,
                EnableDms = true,
                EnableDefaultHelp = false
            };

            Commands = Client.UseCommandsNext(commandsConfig);
            var slashCommandsConfig = Client.UseSlashCommands();

            variablesPublicas.interwikiProcesando = false;

            //Comandos
            Commands.RegisterCommands<comandosAdmin>();

            slashCommandsConfig.RegisterCommands<Interwiki>();
            slashCommandsConfig.RegisterCommands<HayAhoraFutbol>();

            await Client.ConnectAsync();
            await Task.Delay(-1);
        }



        private static async Task Client_Ready(DiscordClient sender, ReadyEventArgs args)
        {
            Console.WriteLine($"{sender.CurrentUser.Username} is connected.");
            await Task.CompletedTask;
        }

        private static async Task Client_GuildMemberAdded(DiscordClient sender, GuildMemberAddEventArgs args)
        {
            var guild = args.Guild;
            var channel = guild.GetChannel(variablesPublicas.mensajeBienvenidaCanal);


            if (channel != null)
            {
                var embedBuilder = new DiscordEmbedBuilder()
                {
                    Title = $":squid: ¡Un nuevo usuario se ha unido! :octopus:",
                    Description = $"{args.Member.Mention}, tu habilidad para añadir tinta a las ediciones será fundamental para lograr nuestro objetivo.\n¡Adelante!",
                    Color = new DiscordColor(0, 153, 255)
                };

                embedBuilder.Thumbnail = new DiscordEmbedBuilder.EmbedThumbnail
                {
                    Url = args.Member.AvatarUrl
                };

                var message = new DiscordMessageBuilder()
                    .AddEmbed(embedBuilder);

                await channel.SendMessageAsync(message);

                await args.Member.GrantRoleAsync(guild.GetRole(variablesPublicas.rolUsuario));
            }
        }
    }
}