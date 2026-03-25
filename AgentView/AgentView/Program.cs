using System.Net.WebSockets;
using NAudio.Wave;

namespace AgentView
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            var view = new MainView();
            var controller = new AgentController(view);

            view.Shown += async (_, __) =>
            {
                await controller.StartAsync();
            };

            Application.Run(view);
        }
    }
}