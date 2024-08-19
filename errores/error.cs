using DSharpPlus;
using System;

namespace InkataBot.errores
{
    internal class error
    {
        private readonly DiscordClient _client;

        public error(DiscordClient client)
        {
            _client = client;
        }

        public string errorCommand(string username, Exception ex, string comando)
        {
            Random random = new Random();
            int aleat = random.Next(0, 99999);
            string response = $":name_badge: Se ha producido un error. Por favor, contacte con un administrador y envíele el siguiente código de error.\nCódigo de error: ``{aleat}``\n";
            EnviarMensajeDeError(aleat, username, comando, ex);
            return response;
        }

        private async void EnviarMensajeDeError(int aleat, string username, string comando, Exception ex)
        {
            var canal = _client.GetChannelAsync(variablesGlobales.variablesPublicas.mensajeError).Result; // Usar GetChannelAsync para obtener el canal

            if (canal != null)
            {
                string mensaje = $"User ``{username}`` with code ``{aleat}`` in command ``{comando}``\n{ex}";
                Console.Error.WriteLine($"User ``{username}`` with code ``{aleat}`` in command ``{comando}``");
                Console.WriteLine(ex + "\n");
                if (mensaje.Length > 2000)
                {
                    // Si el mensaje es demasiado largo, se recorta para que se ajuste al límite de 2000 caracteres de Discord
                    mensaje = mensaje.Substring(0, 1994) + " [...]";
                }
                await canal.SendMessageAsync(mensaje);
            }
        }
    }
}
