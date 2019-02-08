using System.ServiceProcess;

namespace TorrentCreatorUploaderService
{
    internal static class Program
    {
        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        private static void Main()
        {
            var servicesToRun = new ServiceBase[]
            {
                new TorrentCreatorUploaderService()
            };
            ServiceBase.Run(servicesToRun);
        }
    }
}