using DSharpPlus;
using DSharpPlus.Entities;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace InkataBot.errores
{
    public class Error
    {
        private readonly DiscordClient _client;

        public Error(DiscordClient client)
        {
            _client = client;
        }

        public async Task<string> errorCommand(string username, Exception ex, string comando, String infoAdicional)
        {
            Random random = new Random();
            int aleat = random.Next(0, 99999);
            string response = $":name_badge: Se ha producido un error. Por favor, contacte con un administrador y envíele el siguiente código de error: ``{aleat}``\n";
            await EnviarMensajeDeError(aleat, username, comando, ex, infoAdicional);
            return response;
        }

        private async Task EnviarMensajeDeError(int aleat, string username, string comando, Exception ex, String infoAdicional)
        {
            var canal = await _client.GetChannelAsync(variablesGlobales.variablesPublicas.mensajeError); // Usar await para obtener el canal asincrónicamente

            if (canal != null)
            {
                // Crear el archivo de error
                string path = Path.Combine(Directory.GetCurrentDirectory(), $"Error_{aleat}.txt");

                string infoAdicionalMensaje = null;
                if(infoAdicional != null) {
                    infoAdicionalMensaje = $"More information: {infoAdicional}";
                }


                string mensaje = $"User {username} with code {aleat} in command {comando}\n\n{ex}{(infoAdicionalMensaje != null ? $"\n\n{infoAdicionalMensaje}" : "")}\n\nTimestamp: {DateTime.UtcNow} UTC.\n";

                // Escribir el contenido del error en el archivo
                await File.WriteAllTextAsync(path, mensaje, Encoding.UTF8);

                // Crear un MessageBuilder y adjuntar el archivo
                var messageBuilder = new DiscordMessageBuilder()
                    .WithContent($"User ``{username}`` with code ``{aleat}`` in command ``{comando}``");

                // Abrir el archivo como Stream y adjuntarlo
                using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    messageBuilder.AddFile($"Error_{aleat}.txt", fileStream);

                    // Enviar el mensaje con el archivo adjunto
                    await canal.SendMessageAsync(messageBuilder);
                }

                // Eliminar el archivo temporal después de enviarlo
                File.Delete(path);
            }
        }
    }
}