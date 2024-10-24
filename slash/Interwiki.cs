using DotNetWikiBot;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using InkataBot.variablesGlobales;
using Newtonsoft.Json;
using System.Net;
using System.Text.RegularExpressions;
using Site = DotNetWikiBot.Site;

namespace InkataBot.slash
{
    internal class Interwiki : ApplicationCommandModule
    {
        string response = "";
        [SlashCommand("interwiki", "Vincula toda la familia Inkipedia en un instante. Conectar comunidades nunca fue tan fácil.")]

        public async Task InterwikiCommand(InteractionContext ctx)
        {

            if (!variablesGlobales.variablesPublicas.interwikiProcesando)
            {
                variablesGlobales.variablesPublicas.interwikiProcesando = true;

                var cts = new CancellationTokenSource();
                variablesGlobales.variablesPublicas.cancellationInterwiki = cts; // Guarda el token en variablesPublicas para detenerlo en caso de ser necesario
                var token = variablesGlobales.variablesPublicas.cancellationInterwiki.Token;

                response = ":map: Has iniciado un proceso interwiki.";
                await EnviarMensajeProcesoInterwiki(ctx, $":airplane_departure: {ctx.User.Mention} ha iniciado un proceso interwiki.");
                await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                            .WithContent(response)
                            .AsEphemeral(true));

                try
                {
                    await Task.Run(async () =>
                    {
                        await RealizarProcesoInterwiki(ctx, cts.Token);
                    }, cts.Token);
                }
                catch (OperationCanceledException) { }
                catch (Exception) { }
                finally
                {
                    variablesGlobales.variablesPublicas.interwikiProcesando = false;
                    variablesGlobales.variablesPublicas.cancellationInterwiki = null;
                }
            }
            else
            {
                response = ":pause_button: Este comando está en uso por otro usuario. Por favor, espere unos minutos antes de intentarlo nuevamente.";
                await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                    .WithContent(response)
                    .AsEphemeral(true));
            }
        }

        private async Task RealizarProcesoInterwiki(InteractionContext ctx, CancellationToken token)
        {
            VariablesPrivadas contrasena = new VariablesPrivadas();

        string[] wikiUrls = {
            "https://es.splatoonwiki.org/w/api.php",
            "https://splatoonwiki.org/w/api.php",
            "https://fr.splatoonwiki.org/w/api.php"
        };
            string[] wikiSites = {
            "https://es.splatoonwiki.org",
            "https://splatoonwiki.org",
            "https://fr.splatoonwiki.org"
        };
            string[] wikiLangs = {
            "es",
            "en",
            "fr"
        };
            string[] wikiCreds = {
            contrasena.ContrasenaEsWiki,
            contrasena.ContrasenaEnWiki,
            contrasena.ContrasenaFrWiki
        };
            string[] botNames = {
            "InkataBot",
            "InkataBot",
            "InkataBot"
        };
        string[] editMessages = {
            $"Edición de bot autorizada por {ctx.User.Username}#{ctx.User.Discriminator} añadiendo interwikis",
            $"Bot edit authorized by {ctx.User.Username}#{ctx.User.Discriminator} adding interwikis",
            $"Bot édition autorisée par {ctx.User.Username}#{ctx.User.Discriminator} ajoutant des interwikis"
        };
            string[] nombreWiki = {
                "Inkipedia ES",
                "Inkipedia",
                "Inkipédia"
        };

            using (HttpClient client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(10);
                int reintentos = 0;

                try
                {
                    List<Site> sites = new List<Site>();
                    for (int i = 0; i < wikiSites.Length; i++)
                    {
                        sites.Add(new Site(wikiSites[i], botNames[i], wikiCreds[i]));
                    }

                    string paginasLista = "action=query&format=json&list=allpages&aplimit=max";
                    List<List<Paginas>> allPaginas = new List<List<Paginas>>();

                    // Obtener todas las páginas de cada wiki
                    for (int i = 0; i < wikiUrls.Length; i++)
                    {
                        string apiUrl = $"{wikiUrls[i]}?{paginasLista}";
                        string servidorRespuesta = await client.GetStringAsync(apiUrl);
                        var resultado = JsonConvert.DeserializeObject<RootObject>(servidorRespuesta);
                        allPaginas.Add(resultado.query.allpages);
                    }

                    // Procesar interwikis
                    for (int i = 0; i < allPaginas.Count; i++)
                    {
                        variablesPublicas.NoCancellationTokenUntilStart = false;
                        token.ThrowIfCancellationRequested();
                        await EnviarMensajeProcesoInterwiki(ctx, $"Analizando {nombreWiki[i]}...");
                        

                        for (int j = 0; j < allPaginas[i].Count; j++)
                        {
                            var pagina = allPaginas[i][j];

                            Page currentPage = new Page(sites[i], pagina.title);
                            List<string> interwikis = currentPage.GetInterLanguageLinks();

                            foreach (var interwiki in interwikis)
                            {
                                for (int k = 0; k < wikiLangs.Length; k++)
                                {
                                    if (interwiki.StartsWith($"{wikiLangs[k]}:") && i != k)
                                    {
                                        bool intentoExitoso = false;
                                        while (!intentoExitoso && reintentos < 5)
                                        {
                                            try
                                            {
                                                token.ThrowIfCancellationRequested();
                                                Page targetPage = new Page(sites[k], interwiki.Replace($"{wikiLangs[k]}:", ""));
                                                targetPage.Load();

                                                // Verificar si la página existe
                                                if (targetPage.Exists())
                                                {
                                                    string correctLangLink = $"[[{wikiLangs[i]}:{pagina.title}]]";  // El enlace interwiki correcto
                                                    string incorrectLangLinkPattern = $@"\[\[{wikiLangs[i]}:([^\]]+)\]\]"; // Patrón para buscar interwikis incorrectos

                                                    // Comprobar si hay un interwiki en ese idioma que no sea el correcto
                                                    var match = Regex.Match(targetPage.text, incorrectLangLinkPattern);

                                                    if (match.Success)
                                                    {
                                                        string currentLangLink = match.Value;

                                                        if (currentLangLink != correctLangLink)
                                                        {
                                                            // Reemplazar el interwiki incorrecto por el correcto
                                                            string newText = targetPage.text.Replace(currentLangLink, correctLangLink);
                                                            token.ThrowIfCancellationRequested();
                                                            targetPage.Save(newText, editMessages[k], true);
                                                        }
                                                    }
                                                    else if (!targetPage.text.Contains(correctLangLink))
                                                    {
                                                        // Añadir el interwiki correcto si no está presente
                                                        string newText = targetPage.text + $"\n{correctLangLink}";
                                                        token.ThrowIfCancellationRequested();
                                                        targetPage.Save(newText, editMessages[k], true);
                                                    }
                                                }
                                                intentoExitoso = true; // Si no hubo excepción, marca como exitoso
                                                reintentos = 0; // Reiniciar los reintentos
                                            }
                                            catch (Exception ex) when (ex is DotNetWikiBot.WikiBotException wikiEx && wikiEx.Message.Contains("Login failed") || ex is WebException webEx && webEx.Response is HttpWebResponse httpResponse && httpResponse.StatusCode == HttpStatusCode.InternalServerError)
                                            {
                                                reintentos++;
                                                await EnviarMensajeProcesoInterwiki(ctx, $":warning: Error en el servidor. Reintentando... ({reintentos}/5)");
                                                await Task.Delay(5000);
                                                if (reintentos >= 5)
                                                {
                                                    await EnviarMensajeProcesoInterwiki(ctx, ":octagonal_sign: El proceso interwiki fue cancelado después de cinco reintentos fallidos.");
                                                    throw new OperationCanceledException(); // Cancelar todo el proceso
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    await EnviarMensajeProcesoInterwiki(ctx, ":airplane_arriving: El proceso de interwiki ha sido completado exitosamente.");
                }
                catch (OperationCanceledException)
                {
                    await EnviarMensajeProcesoInterwiki(ctx, ":octagonal_sign: El proceso interwiki ha sido cancelado.");
                }
                catch (Exception ex)
                {
                    errores.error errorInstance = new errores.error(Program.Client);
                    response = errorInstance.errorCommand(ctx.Member.Username, ex, "interwiki");
                    await EnviarMensajeProcesoInterwiki(ctx, ":knot: Se ha producido un error desconocido; el proceso interwiki ha sido cancelado.");
                }
                finally
                {
                    variablesGlobales.variablesPublicas.interwikiProcesando = false;
                    await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                        .WithContent(response));
                    reintentos = 0;
                    variablesPublicas.NoCancellationTokenUntilStart = true;
                }
            }
        }

        private async Task EnviarMensajeProcesoInterwiki(InteractionContext ctx, string mensaje)
        {
            var canal = await ctx.Client.GetChannelAsync(variablesGlobales.variablesPublicas.mensajeBot);

            if (canal != null)
            {
                await canal.SendMessageAsync(mensaje);
            }
        }

        public class Paginas
        {
            public int pageid { get; set; }
            public int ns { get; set; }
            public string title { get; set; }
        }

        public class Query
        {
            public List<Paginas> allpages { get; set; }
        }

        public class RootObject
        {
            public Query query { get; set; }
        }
    }
}