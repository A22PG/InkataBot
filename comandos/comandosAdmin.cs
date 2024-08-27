using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using InkataBot.variablesGlobales;
using System.Threading.Tasks;

namespace InkataBot.commands
{
    internal class comandosAdmin : BaseCommandModule
    {
        [Command("kickInterwiki")]
        [RequirePermissions(DSharpPlus.Permissions.Administrator)]
        public async Task KickInterwiki(CommandContext ctx)
        {
            if (variablesPublicas.interwikiProcesando)
            {
                if (variablesPublicas.cancellationInterwiki != null)
                {
                    variablesPublicas.cancellationInterwiki.Cancel(); // Cancela el token
                    variablesPublicas.cancellationInterwiki = null; // Resetea el token para evitar cancelaciones posteriores
                    variablesPublicas.interwikiProcesando = false;
                }
            }
            await Task.CompletedTask;
        }
    }
}