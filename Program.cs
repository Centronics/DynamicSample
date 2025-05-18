using System;
using System.Windows.Forms;

namespace DynamicSample
{
    internal static class Program
    {
        public static bool IsDebugLogEnabled { get; private set; }

        /// <summary>
        ///     Главная точка входа для приложения.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            bool bot = false;

            int argsCount = args.Length;

            if (argsCount > 0 && argsCount < 3)
            {
                bot = IsBot(args[0]) || (argsCount > 1 && IsBot(args[1]));
                IsDebugLogEnabled = IsDebugLog(args[0]) || (argsCount > 1 && IsDebugLog(args[1]));
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (bot)
                Application.Run(new FrmGameBot());
            else
                Application.Run(new FrmSample());

            return;

            bool IsBot(string s) => s == @"-bot";

            bool IsDebugLog(string s) => s == @"-debuglog";
        }
    }
}