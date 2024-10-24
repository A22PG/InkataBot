namespace InkataBot.variablesGlobales
{
    internal class variablesPublicas
    {
        //Prevenciones
        public static Boolean interwikiProcesando { get; set; }

        //Canales de Discord
        public static ulong mensajeBienvenidaCanal { get; } = 1099346502079479848;
        public static ulong mensajeError { get; } = 1100052280822214697;
        public static ulong mensajeBot { get; } = 1100791919518433331;

        //Roles de Discord
        public static ulong rolUsuario { get; } = 1145817920878940311;

        //Administración
        public static CancellationTokenSource cancellationInterwiki { get; internal set; }
        public static Boolean NoCancellationTokenUntilStart { get; internal set; }
    }
}