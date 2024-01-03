using System.ServiceProcess;

namespace InnoClinic.WindowsServices
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new EmailService()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
