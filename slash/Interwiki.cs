using DotNetWikiBot;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
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
                variablesGlobales.variablesPublicas.cancellationToken = cts; // Guarda el token en variablesPublicas para detenerlo en caso de ser necesario
                var token = variablesGlobales.variablesPublicas.cancellationToken.Token;

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
                    variablesGlobales.variablesPublicas.cancellationToken = null;
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
            string esWiki = "https://es.splatoonwiki.org/w/api.php"; // URL de es.inkipedia.org
            string enWiki = "https://splatoonwiki.org/w/api.php"; // URL de inkipedia.org
            string frWiki = "https://fr.splatoonwiki.org/w/api.php"; // URL de fr.inkipedia.org
            using (HttpClient client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(10);
                try
                {
                    VariablesPrivadas contrasena = new VariablesPrivadas();
                    Site esWikiLista = new Site("https://es.splatoonwiki.org", "InkataBot", contrasena.ContrasenaEsWiki);
                    Site enWikiLista = new Site("https://splatoonwiki.org", "InkataBot", contrasena.ContrasenaEnWiki);
                    Site frWikiLista = new Site("https://fr.splatoonwiki.org", "InkataBot", contrasena.ContrasenaFrWiki);
                    string paginasLista = "action=query&format=json&list=allpages&aplimit=max";
                    string apiUrl = $"{esWiki}?{paginasLista}";
                    string servidorRespuesta = await client.GetStringAsync(apiUrl);
                    var resultado = JsonConvert.DeserializeObject<RootObject>(servidorRespuesta);
                    List<Paginas> paginasEs = resultado.query.allpages;

                    apiUrl = $"{enWiki}?{paginasLista}";
                    servidorRespuesta = await client.GetStringAsync(apiUrl);
                    resultado = JsonConvert.DeserializeObject<RootObject>(servidorRespuesta);
                    List<Paginas> paginasEn = resultado.query.allpages;

                    apiUrl = $"{frWiki}?{paginasLista}";
                    servidorRespuesta = await client.GetStringAsync(apiUrl);
                    resultado = JsonConvert.DeserializeObject<RootObject>(servidorRespuesta);
                    List<Paginas> paginasFr = resultado.query.allpages;

                    // Recopilar esWiki
                    await EnviarMensajeProcesoInterwiki(ctx, "Analizando Inkipedia ES...");
                    foreach (var pagina in paginasEs)
                    {
                        token.ThrowIfCancellationRequested();
                        Page pES = new Page(esWikiLista, pagina.title);
                        List<string> interwikis = pES.GetInterLanguageLinks();

                        foreach (var interwiki in interwikis)
                        {
                            Site esWikiConexion = new Site("https://es.splatoonwiki.org", "InkataBot", contrasena.ContrasenaEsWiki);
                            if (interwiki.StartsWith("en:"))
                            {
                                token.ThrowIfCancellationRequested();
                                Site enWikiConexion = new Site("https://splatoonwiki.org", "InkataBot", contrasena.ContrasenaEnWiki);
                                string eSinPrefEn = interwiki.Replace("en:", "");
                                Page pEn = new Page(enWikiConexion, eSinPrefEn);
                                try
                                {
                                    pEn.Load();
                                    if (pEn.text != "" && !pEn.text.Contains($"[[es:{pagina.title}]]"))
                                    {
                                        string texto = pEn.text + $"\n[[es:{pagina.title}]]";
                                        token.ThrowIfCancellationRequested();
                                        pEn.Save(texto, $"Bot edit authorized by {ctx.User.Username}#{ctx.User.Discriminator} adding interwikis", true);
                                    }
                                    else if (pEn.text != "" && pEn.text.Contains($"[[es:"))
                                    {
                                        string texto = pEn.text;
                                        string patron = @"\[\[es:(.*?)\]\]";
                                        Match match = Regex.Match(texto, patron);

                                        if (match.Success)
                                        {
                                            string antiguoEnlaceDesconocido = match.Groups[0].Value;
                                            if (!antiguoEnlaceDesconocido.Equals($"[[es:{pagina.title}]]", StringComparison.Ordinal))
                                            {
                                                texto = texto.Replace(antiguoEnlaceDesconocido, $"[[es:{pagina.title}]]");
                                                token.ThrowIfCancellationRequested();
                                                pEn.Save(texto, $"Bot edit authorized by {ctx.User.Username}#{ctx.User.Discriminator} updating interwikis", true);
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex) { Console.WriteLine(ex); }
                            }
                            else if (interwiki.StartsWith("fr:"))
                            {
                                token.ThrowIfCancellationRequested();
                                Site frWikiConexion = new Site("https://fr.splatoonwiki.org", "InkataBot", contrasena.ContrasenaFrWiki);
                                string eSinPrefFr = interwiki.Replace("fr:", "");
                                Page pFr = new Page(frWikiConexion, eSinPrefFr);
                                try
                                {
                                    pFr.Load();

                                    if (pFr.text != "" && !pFr.text.Contains($"[[es:{pagina.title}]]"))
                                    {
                                        string texto = pFr.text + $"\n[[es:{pagina.title}]]";
                                        token.ThrowIfCancellationRequested();
                                        pFr.Save(texto, $"Modification par un robot ajoutant à jour les interwikis autorisé par {ctx.User.Username}#{ctx.User.Discriminator}", true);
                                    }
                                    else if (pFr.text.Contains($"[[es:"))
                                    {
                                        string texto = pFr.text;
                                        string patron = @"\[\[es:(.*?)\]\]";
                                        Match match = Regex.Match(texto, patron);

                                        if (match.Success)
                                        {
                                            string antiguoEnlaceDesconocido = match.Groups[0].Value;

                                            if (!antiguoEnlaceDesconocido.Equals($"[[es:{pagina.title}]]", StringComparison.Ordinal))
                                            {
                                                texto = texto.Replace(antiguoEnlaceDesconocido, $"[[es:{pagina.title}]]");
                                                token.ThrowIfCancellationRequested();
                                                pFr.Save(texto, $"Modification par un robot mettant à jour les interwikis autorisé par {ctx.User.Username}#{ctx.User.Discriminator}", true);
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex) { Console.WriteLine(ex); }

                            }
                            // En este espacio van nuevas wikis si se llegara a dar el caso de aumentar el número de idiomas
                        }
                    }
                    await EnviarMensajeProcesoInterwiki(ctx, "Inkipedia ES analizada");

                    // Recopilar enWiki
                    await EnviarMensajeProcesoInterwiki(ctx, "Analizando Inkipedia...");
                    foreach (var pagina in paginasEn)
                    {
                        token.ThrowIfCancellationRequested();
                        Page pEN = new Page(enWikiLista, pagina.title);
                        List<string> interwikis = pEN.GetInterLanguageLinks();

                        foreach (var interwiki in interwikis)
                        {
                            Site enWikiConexion = new Site("https://splatoonwiki.org", "InkataBot", contrasena.ContrasenaEnWiki);
                            if (interwiki.StartsWith("es:"))
                            {
                                token.ThrowIfCancellationRequested();
                                Site esWikiConexion = new Site("https://es.splatoonwiki.org", "InkataBot", contrasena.ContrasenaEsWiki);
                                string eSinPrefEs = interwiki.Replace("es:", "");
                                Page pEs = new Page(esWikiConexion, eSinPrefEs);
                                try
                                {
                                    pEs.Load();

                                    if (pEs.text != "" && !pEs.text.Contains($"[[en:{pagina.title}]]"))
                                    {
                                        string texto = pEs.text + $"\n[[en:{pagina.title}]]";
                                        token.ThrowIfCancellationRequested();
                                        pEs.Save(texto, $"Edición de bot autorizada por {ctx.User.Username}#{ctx.User.Discriminator} añadiendo interwikis", true);
                                    }
                                    else if (pEs.text != "" && pEs.text.Contains($"[[en:"))
                                    {
                                        string texto = pEs.text;
                                        string patron = @"\[\[en:(.*?)\]\]";
                                        Match match = Regex.Match(texto, patron);

                                        if (match.Success)
                                        {
                                            string antiguoEnlaceDesconocido = match.Groups[0].Value;
                                            if (!antiguoEnlaceDesconocido.Equals($"[[en:{pagina.title}]]", StringComparison.Ordinal))
                                            {
                                                texto = texto.Replace(antiguoEnlaceDesconocido, $"[[en:{pagina.title}]]");
                                                token.ThrowIfCancellationRequested();
                                                pEs.Save(texto, $"Edición de bot autorizada por {ctx.User.Username}#{ctx.User.Discriminator} actualizando interwikis", true);
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex) { Console.WriteLine(ex); }
                            }
                            else if (interwiki.StartsWith("fr:"))
                            {
                                token.ThrowIfCancellationRequested();
                                Site frWikiConexion = new Site("https://fr.splatoonwiki.org", "InkataBot", contrasena.ContrasenaFrWiki);
                                string eSinPrefFr = interwiki.Replace("fr:", "");
                                Page pFr = new Page(frWikiConexion, eSinPrefFr);
                                try
                                {
                                    pFr.Load();

                                    if (pFr.text != "" && !pFr.text.Contains($"[[en:{pagina.title}]]"))
                                    {
                                        string texto = pFr.text + $"\n[[en:{pagina.title}]]";
                                        token.ThrowIfCancellationRequested();
                                        pFr.Save(texto, $"Modification par un robot ajoutant à jour les interwikis autorisé par {ctx.User.Username}#{ctx.User.Discriminator}", true);
                                    }
                                    else if (pFr.text.Contains($"[[en:"))
                                    {
                                        string texto = pFr.text;
                                        string patron = @"\[\[en:(.*?)\]\]";
                                        Match match = Regex.Match(texto, patron);

                                        if (match.Success)
                                        {
                                            string antiguoEnlaceDesconocido = match.Groups[0].Value;

                                            if (!antiguoEnlaceDesconocido.Equals($"[[en:{pagina.title}]]", StringComparison.Ordinal))
                                            {
                                                texto = texto.Replace(antiguoEnlaceDesconocido, $"[[en:{pagina.title}]]");
                                                token.ThrowIfCancellationRequested();
                                                pFr.Save(texto, $"Modification par un robot mettant à jour les interwikis autorisé par {ctx.User.Username}#{ctx.User.Discriminator}", true);
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex) { Console.WriteLine(ex); }


                            }
                            // En este espacio van nuevas wikis si se llegara a dar el caso de aumentar el número de idiomas
                        }
                    }
                    await EnviarMensajeProcesoInterwiki(ctx, "Inkipedia analizada");

                    // Recopilar frWiki
                    await EnviarMensajeProcesoInterwiki(ctx, "Analizando Inkipédia...");
                    foreach (var pagina in paginasEs)
                    {
                        token.ThrowIfCancellationRequested();
                        Page pFR = new Page(frWikiLista, pagina.title);
                        List<string> interwikis = pFR.GetInterLanguageLinks();

                        foreach (var interwiki in interwikis)
                        {
                            Site frWikiConexion = new Site("https://fr.splatoonwiki.org", "InkataBot", contrasena.ContrasenaFrWiki);
                            if (interwiki.StartsWith("en:"))
                            {
                                token.ThrowIfCancellationRequested();
                                Site enWikiConexion = new Site("https://splatoonwiki.org", "InkataBot", contrasena.ContrasenaEnWiki);
                                string eSinPrefEn = interwiki.Replace("en:", "");
                                Page pEn = new Page(enWikiConexion, eSinPrefEn);
                                try
                                {
                                    pEn.Load();

                                    if (pEn.text != "" && !pEn.text.Contains($"[[fr:{pagina.title}]]"))
                                    {
                                        string texto = pEn.text + $"\n[[fr:{pagina.title}]]";
                                        token.ThrowIfCancellationRequested();
                                        pEn.Save(texto, $"Bot edit authorized by {ctx.User.Username}#{ctx.User.Discriminator} with InkataBot adding interwikis", true);
                                    }
                                    else if (pEn.text != "" && pEn.text.Contains($"[[fr:"))
                                    {
                                        string texto = pEn.text;
                                        string patron = @"\[\[fr:(.*?)\]\]";
                                        Match match = Regex.Match(texto, patron);

                                        if (match.Success)
                                        {
                                            string antiguoEnlaceDesconocido = match.Groups[0].Value;
                                            if (!antiguoEnlaceDesconocido.Equals($"[[fr:{pagina.title}]]", StringComparison.Ordinal))
                                            {
                                                texto = texto.Replace(antiguoEnlaceDesconocido, $"[[fr:{pagina.title}]]");
                                                token.ThrowIfCancellationRequested();
                                                pEn.Save(texto, $"Bot edit authorized by {ctx.User.Username}#{ctx.User.Discriminator} with InkataBot adding interwikis", true);
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex) { Console.WriteLine(ex); }
                            }
                            else if (interwiki.StartsWith("es:"))
                            {
                                token.ThrowIfCancellationRequested();
                                Site esWikiConexion = new Site("https://es.splatoonwiki.org", "InkataBot", contrasena.ContrasenaEsWiki);
                                string eSinPrefFr = interwiki.Replace("es:", "");
                                Page pEs = new Page(esWikiConexion, eSinPrefFr);
                                try
                                {
                                    pEs.Load();

                                    if (pEs.text != "" && !pEs.text.Contains($"[[fr:{pagina.title}]]"))
                                    {
                                        string texto = pEs.text + $"\n[[fr:{pagina.title}]]";
                                        token.ThrowIfCancellationRequested();
                                        pEs.Save(texto, $"Edición de bot autorizada por {ctx.User.Username}#{ctx.User.Discriminator} añadiendo interwikis", true);
                                    }
                                    else if (pEs.text.Contains($"[[fr:"))
                                    {
                                        string texto = pEs.text;
                                        string patron = @"\[\[fr:(.*?)\]\]";
                                        Match match = Regex.Match(texto, patron);

                                        if (match.Success)
                                        {
                                            string antiguoEnlaceDesconocido = match.Groups[0].Value;
                                            if (!antiguoEnlaceDesconocido.Equals($"[[fr:{pagina.title}]]", StringComparison.Ordinal))
                                            {
                                                texto = texto.Replace(antiguoEnlaceDesconocido, $"[[fr:{pagina.title}]]");
                                                token.ThrowIfCancellationRequested();
                                                pEs.Save(texto, $"Edición de bot autorizada por {ctx.User.Username}#{ctx.User.Discriminator} actualizando interwikis", true);
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex) { Console.WriteLine(ex); }
                            }
                            // En este espacio van nuevas wikis si se llegara a dar el caso de aumentar el número de idiomas
                        }
                    }
                    await EnviarMensajeProcesoInterwiki(ctx, "Inkipédia analizada...");

                    // En este espacio van nuevas wikis si se llegara a dar el caso de aumentar el número de idiomas

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
                }
                finally
                {
                    variablesGlobales.variablesPublicas.interwikiProcesando = false;
                    await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder()
                        .WithContent(response));
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