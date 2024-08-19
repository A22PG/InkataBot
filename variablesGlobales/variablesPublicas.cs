using System;
using System.Threading;

namespace InkataBot.variablesGlobales
{
    internal class variablesPublicas
    {
        //Prevención de dos o más interwikis
        public static Boolean interwikiProcesando { get; set; }

        //Mensajes de Discord
        public static ulong mensajeBienvenidaCanal { get; } = 1099346502079479848;
        public static ulong mensajeError { get; } = 1100052280822214697;
        public static ulong mensajeBot { get; } = 1100791919518433331;
        public static CancellationTokenSource cancellationToken { get; internal set; }
    }
}