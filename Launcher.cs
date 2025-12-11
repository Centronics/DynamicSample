using System;
using System.Linq;
using System.Threading;
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
                try
                {
                    Thread.CurrentThread.Name = @"Main Thread";
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);

                    InvertModeEnabled = GetParameter(IsInvertMode);
                    DebugLogEnabled = GetParameter(IsDebugLog);
                    ExplicitLogEnabled = GetParameter(IsExplicitLog);

                    Logger.Initialize();

                    string invertModeString =
                        InvertModeEnabled ? @"Режим инверсии включен." : @"Режим инверсии выключен.";
                    string debugLogString =
                        DebugLogEnabled ? @"Режим журналирования включен." : @"Режим журналирования выключен.";
                    string explicitLogString =
                        ExplicitLogEnabled ? @"Режим расширенной диагностики включен." : @"Режим расширенной диагностики выключен.";

                    Logger.WriteLog(() => $@"{nameof(Main)}: {invertModeString}");
                    Logger.WriteLog(() => $@"{nameof(Main)}: {debugLogString}");
                    Logger.WriteLog(() => $@"{nameof(Main)}: {explicitLogString}");

                    if (GetParameter(IsBotMode))
                    {
                        Logger.WriteLog(() => $@"{nameof(Main)}: Игра началась. Режим бота включен...");
                        Application.Run(new FrmGameBot());
                        return;
                    }

                    Logger.WriteLog(() => $@"{nameof(Main)}: Игра началась. Режим бота выключен...");
                    Application.Run(new FrmSample());
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