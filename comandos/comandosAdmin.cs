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
                if (variablesPublicas.cancellationToken != null)
                {
                    variablesPublicas.cancellationToken.Cancel(); // Cancela el token
                    variablesPublicas.cancellationToken = null; // Resetea el token para evitar cancelaciones posteriores
                    variablesPublicas.interwikiProcesando = false;
                }
            }
            await Task.CompletedTask;
        }
    }
}