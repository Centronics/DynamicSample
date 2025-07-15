using System;
using System.Linq;
using System.Windows.Forms;

namespace DynamicSample
{
    internal static class Launcher
    {
        public static bool InvertModeEnabled { get; private set; }

        public static bool IsDebugLogEnabled { get; private set; }

        [STAThread]
        static void Main(string[] args)
        {
            InvertModeEnabled = GetParameter(IsInvertMode);
            IsDebugLogEnabled = GetParameter(IsDebugLog);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (GetParameter(IsBotMode))
                Application.Run(new FrmGameBot());
            else
                Application.Run(new FrmSample());

            return;

            bool GetParameter(Func<string, bool> f) => args?.Any(f) ?? false;

            bool IsInvertMode(string s) => string.Compare(s, @"-invert", StringComparison.OrdinalIgnoreCase) == 0;

            bool IsBotMode(string s) => string.Compare(s, @"-bot", StringComparison.OrdinalIgnoreCase) == 0;

            bool IsDebugLog(string s) => string.Compare(s, @"-debuglog", StringComparison.OrdinalIgnoreCase) == 0;
        }
    }
}