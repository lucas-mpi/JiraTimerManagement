using System;
using System.Windows;

namespace JiraTimerManagement
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            var app = new Application();
            var janela = new TelaPrincipal();
            app.Run(janela);
        }
    }
}
