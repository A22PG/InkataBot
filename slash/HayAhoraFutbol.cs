using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using InkataBot.variablesGlobales;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace InkataBot.slash
{
    internal class HayAhoraFutbol : ApplicationCommandModule
    {
        [SlashCommand("bloqueo", "Verifica el estado de bloqueo de Cloudflare en España analizando la red.")]
        public async Task BloqueoCommand(InteractionContext ctx)
        {
            try
            {
                await ctx.DeferAsync();

                var analysis = await RealizarAnalisisBloqueo();

                bool isBloqued = analysis.Status == "blocked";

                var embedBuilder = new DiscordEmbedBuilder()
                {
                    Title = isBloqued ? ":prohibited: Cloudflare bloqueado" : ":white_check_mark: No hay bloqueos",
                    Description = isBloqued
                        ? "La familia *Inkipedia* se puede encontrar temporalmente bloqueada."
                        : "Puedes acceder a la familia *Inkipedia* con normalidad.",
                    Color = isBloqued ? new DiscordColor(221, 46, 68) : new DiscordColor(119, 178, 85)
                };

                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .AddEmbed(embedBuilder));
            }
            catch (Exception ex)
            {
                errores.Error errorInstance = new errores.Error(Program.Client);
                string response = await errorInstance.errorCommand(ctx.Member.Username, ex, "bloqueo");
                var canal = await ctx.Client.GetChannelAsync(variablesPublicas.mensajeBot);
                if (canal != null)
                {
                    await canal.SendMessageAsync(response);
                }
            }
        }

        private async Task<BlockingAnalysis> RealizarAnalisisBloqueo()
        {
            var monitor = new NetworkMonitor("https://hayahora.futbol/estado/data.json"); // Cambia por tu URL real
            try
            {
                var analysis = await monitor.GetNetworkStatusAsync();
                return analysis;
            }
            finally
            {
                monitor.Dispose();
            }
        }
    }

    // Clases del NetworkMonitor (copiadas del código original)
    public class NetworkStatus
    {
        public string lastUpdate { get; set; }
        public List<IpData> data { get; set; }
    }

    public class IpData
    {
        public string ip { get; set; }
        public string isp { get; set; }
        public string description { get; set; }
        public List<StateChange> stateChanges { get; set; }
    }

    public class StateChange
    {
        public DateTime timestamp { get; set; }
        public bool state { get; set; }
    }

    public class BlockingAnalysis
    {
        public string Status { get; set; }
        public int TotalBlockedIps { get; set; }
        public int CloudflareBlockedIps { get; set; }
        public DateTime LastUpdate { get; set; }
        public Dictionary<string, int> IpBlockCounts { get; set; }
        public Dictionary<string, int> CloudflareIpBlockCounts { get; set; }
    }

    public class NetworkMonitor
    {
        private readonly HttpClient _httpClient;
        private readonly string _dataUrl;

        public NetworkMonitor(string dataUrl = "/estado/data.json")
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            _dataUrl = dataUrl;
        }

        public async Task<BlockingAnalysis> GetNetworkStatusAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync(_dataUrl);
                response.EnsureSuccessStatusCode();

                var jsonString = await response.Content.ReadAsStringAsync();
                var networkData = JsonSerializer.Deserialize<NetworkStatus>(jsonString, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return AnalyzeBlockingStatus(networkData);
            }
            catch (Exception)
            {
                return new BlockingAnalysis
                {
                    Status = "unblocked",
                    LastUpdate = DateTime.Now,
                    IpBlockCounts = new Dictionary<string, int>(),
                    CloudflareIpBlockCounts = new Dictionary<string, int>()
                };
            }
        }

        public BlockingAnalysis AnalyzeBlockingStatus(NetworkStatus networkData)
        {
            var ipBlockCounts = new Dictionary<string, int>();
            var cloudflareIpBlockCounts = new Dictionary<string, int>();

            if (networkData?.data != null)
            {
                foreach (var ipData in networkData.data)
                {
                    if (ipData.stateChanges == null || !ipData.stateChanges.Any())
                        continue;

                    var latestState = ipData.stateChanges.Last();

                    if (latestState.state)
                    {
                        if (ipBlockCounts.ContainsKey(ipData.ip))
                            ipBlockCounts[ipData.ip]++;
                        else
                            ipBlockCounts[ipData.ip] = 1;

                        if (ipData.description == "Cloudflare")
                        {
                            if (cloudflareIpBlockCounts.ContainsKey(ipData.ip))
                                cloudflareIpBlockCounts[ipData.ip]++;
                            else
                                cloudflareIpBlockCounts[ipData.ip] = 1;
                        }
                    }
                }
            }

            var totalBlockedIps = ipBlockCounts.Values.Count(count => count > 2);
            var cloudflareBlockedIps = cloudflareIpBlockCounts.Values.Count(count => count > 2);

            var isBlocked = cloudflareBlockedIps > 10;
            var status = isBlocked ? "blocked" : "unblocked";

            var lastUpdate = DateTime.Now;
            if (networkData?.lastUpdate != null && DateTime.TryParse(networkData.lastUpdate, out var parsedDate))
            {
                lastUpdate = parsedDate;
            }

            return new BlockingAnalysis
            {
                Status = status,
                TotalBlockedIps = totalBlockedIps,
                CloudflareBlockedIps = cloudflareBlockedIps,
                LastUpdate = lastUpdate,
                IpBlockCounts = ipBlockCounts,
                CloudflareIpBlockCounts = cloudflareIpBlockCounts
            };
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}