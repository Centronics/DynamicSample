using System;
using System.Linq;
using System.Windows.Forms;

namespace DynamicSample
{
    internal static class Launcher
    {
        public static bool InvertModeEnabled { get; private set; }

        public static bool DebugLogEnabled { get; private set; }

        public static bool ExplicitLogEnabled { get; private set; }

        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                Logger.Initialize();

                try
                {
                    InvertModeEnabled = GetParameter(IsInvertMode);
                    DebugLogEnabled = GetParameter(IsDebugLog);
                    ExplicitLogEnabled = GetParameter(IsExplicitLog);

                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);

                    if (GetParameter(IsBotMode))
                    {
                        Logger.WriteLog(@"Игра началась. Режим бота включен...");
                        Application.Run(new FrmGameBot());
                    }
                    else
                    {
                        Logger.WriteLog(@"Start game. Bot mode is off...");
                        Application.Run(new FrmSample());
                    }
                }
                finally
                {
                    Logger.Deinitialize();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, @"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            return;

            bool GetParameter(Func<string, bool> f) => args?.Any(f) ?? false;

            bool IsInvertMode(string s) => string.Compare(s, @"-invert", StringComparison.OrdinalIgnoreCase) == 0;

            bool IsBotMode(string s) => string.Compare(s, @"-bot", StringComparison.OrdinalIgnoreCase) == 0;

            bool IsDebugLog(string s) => string.Compare(s, @"-debuglog", StringComparison.OrdinalIgnoreCase) == 0;

            bool IsExplicitLog(string s) => string.Compare(s, @"-explicitlog", StringComparison.OrdinalIgnoreCase) == 0;
        }
    }
}