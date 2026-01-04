using System;
using System.Drawing;
using System.Windows.Forms;

namespace DynamicSample
{
    public partial class FrmSample : Form
    {
        static readonly Pen BlackPen = new Pen(Color.Black, 2.0f);

        Bitmap _gameCanvas;

        Graphics _gameGrFront;

        GameSession _gameSession;

        public FrmSample()
        {
            InitializeComponent();
        }

        void PbDraw_MouseClick(object sender, MouseEventArgs e)
        {
            try
            {
                Logger.WriteLog(() => $@"{nameof(PbDraw_MouseClick)}: Клик на форме (X = {e.X}, Y = {e.Y}).", Logger.LogLevel.DEBUG);

                int pbDrawCw = pbDraw.Width / 3;
                int pbDrawCh = pbDraw.Height / 3;

                if (!_gameSession.MakeUserHit(e.X / pbDrawCw, e.Y / pbDrawCh))
                {
                    Logger.WriteLog(() => $@"{nameof(PbDraw_MouseClick)}: Клик пользователя ({e.X}, {e.Y}) неудачен.", Logger.LogLevel.DEBUG);
                    return;
                }

                Logger.WriteLog(() => $@"{nameof(PbDraw_MouseClick)}: Клик пользователя ({e.X}, {e.Y}) успешен.", Logger.LogLevel.DEBUG);

                RefreshGameField();

                if (_gameSession.CurrentWinner == GameSession.Winner.NOBODY)
                {
                    (int x, int y) = _gameSession.MakeHitDecision();

                    Logger.WriteLog(() => $@"{nameof(PbDraw_MouseClick)}: Удар бота ({x}, {y}).", Logger.LogLevel.DEBUG);

                    if (_gameSession.MakeBotHit(x, y))
                        RefreshGameField();
                    else
                    {
                        const string s = @"Неожиданная ошибка: Ничья, никто не сможет выиграть!";
                        Logger.WriteLog(() => $@"{nameof(PbDraw_MouseClick)}: {s} Удар бота ({x}, {y}).", Logger.LogLevel.DEBUG);
                        MessageBox.Show($@"{s}{Environment.NewLine}Удар бота ({x}, {y}).");
                        RefreshGameField(true);
                        return;
                    }
                }

                switch (_gameSession.CurrentWinner)
                {
                    case GameSession.Winner.NOBODY:
                        Logger.WriteLog(() => $@"{nameof(PbDraw_MouseClick)}: Никто не выиграл. Игра продолжается.", Logger.LogLevel.DEBUG);
                        return;
                    case GameSession.Winner.STANDOFF:
                        {
                            const string s = @"Ничья!";
                            Logger.WriteLog(() => $@"{nameof(PbDraw_MouseClick)}: {s}", Logger.LogLevel.DEBUG);
                            MessageBox.Show(s);
                            RefreshGameField(true);
                            return;
                        }
                    case GameSession.Winner.USER:
                        {
                            const string s = @"Ты выиграл!";
                            Logger.WriteLog(() => $@"{nameof(PbDraw_MouseClick)}: {s}", Logger.LogLevel.DEBUG);
                            MessageBox.Show(s);
                            RefreshGameField(true);
                            return;
                        }
                    case GameSession.Winner.BOT:
                        {
                            const string s = @"Компьютер выиграл!";
                            Logger.WriteLog(() => $@"{nameof(PbDraw_MouseClick)}: {s}", Logger.LogLevel.DEBUG);
                            MessageBox.Show(s);
                            RefreshGameField(true);
                            return;
                        }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(() => $@"{nameof(PbDraw_MouseClick)}: {ex.Message}", Logger.LogLevel.ERROR);
                MessageBox.Show(ex.Message, @"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
        }

        void FrmSample_Shown(object sender, EventArgs e)
        {
            try
            {
                Logger.WriteLog(() => $@"{nameof(FrmSample_Shown)}: Запуск программы в обычном режиме.");

                pbDraw.Image = _gameCanvas = new Bitmap(pbDraw.Width, pbDraw.Height);
                _gameGrFront = Graphics.FromImage(_gameCanvas);
                RefreshGameField();

                Logger.WriteLog(() => $@"{nameof(FrmSample_Shown)}: Пользовательский интерфейс готов к работе.", Logger.LogLevel.DEBUG);
            }
            catch (Exception ex)
            {
                Logger.WriteLog(() => $@"{nameof(FrmSample_Shown)}: {ex.Message}", Logger.LogLevel.ERROR);
            }
        }

        void FrmSample_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                Logger.WriteLog(() => $@"{nameof(FrmSample_KeyDown)}: Нажатие клавиши ({e.KeyCode}) на форме.", Logger.LogLevel.DEBUG);

                switch (e.KeyCode)
                {
                    case Keys.Escape:
                        Logger.WriteLog(() => $@"{nameof(FrmSample_KeyDown)}: Выход из программы с помощью клавиши ESC.");
                        Application.Exit();
                        return;
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(() => $@"{nameof(FrmSample_KeyDown)}: {ex.Message}", Logger.LogLevel.ERROR);
            }
        }

        void FrmSample_FormClosed(object sender, FormClosedEventArgs e)
        {
            try
            {
                Logger.WriteLog(() => $@"{nameof(FrmSample_FormClosed)}: Форма закрыта.", Logger.LogLevel.DEBUG);

                pbDraw.Image = null;

                _gameGrFront?.Dispose();
                _gameCanvas?.Dispose();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(() => $@"{nameof(FrmSample_FormClosed)}: {ex.Message}", Logger.LogLevel.ERROR);
            }
        }

        void RefreshGameField(bool createNewGame = false)
        {
            if (createNewGame || _gameSession == null)
                _gameSession = new GameSession();

            _gameGrFront.Clear(Color.LightGray);

            int pbDrawCw = pbDraw.Width / 3;
            int pbDrawCh = pbDraw.Height / 3;

            float szVal = (pbDrawCw + pbDrawCh) / 316.0f;
            szVal -= szVal * 0.2f;
            szVal *= 228.0f;

            _gameGrFront.DrawRectangle(BlackPen, pbDrawCw, 0, 2, pbDraw.Height);
            _gameGrFront.DrawRectangle(BlackPen, pbDrawCw * 2, 0, 2, pbDraw.Height);

            _gameGrFront.DrawRectangle(BlackPen, 0, pbDrawCh, pbDraw.Width, 2);
            _gameGrFront.DrawRectangle(BlackPen, 0, pbDrawCh * 2, pbDraw.Width, 2);

            for (int y = 0; y < 3; y++)
                for (int x = 0; x < 3; x++)
                {
                    if (_gameSession[x, y] == GameSession.UserHit)
                        DrawX(x * pbDrawCw, y * pbDrawCh, _gameSession.HitX == x && _gameSession.HitY == y);
                    if (_gameSession[x, y] == GameSession.BotHit)
                        DrawO(x * pbDrawCw, y * pbDrawCh, _gameSession.HitX == x && _gameSession.HitY == y);
                }

            pbDraw.Refresh();

            return;

            void DrawX(int x, int y, bool lastHit)
            {
                if (Launcher.InvertModeEnabled)
                {
                    DrawO(x, y, lastHit);
                    return;
                }

                _gameGrFront.DrawString(@"X",
                    new Font(FontFamily.GenericMonospace, szVal, FontStyle.Italic, GraphicsUnit.Pixel),
                    new SolidBrush(lastHit ? Color.Green : Color.DodgerBlue), x - 36, y - 46);
            }

            void DrawO(int x, int y, bool lastHit)
            {
                if (Launcher.InvertModeEnabled)
                {
                    DrawX(x, y, lastHit);
                    return;
                }

                _gameGrFront.DrawString(@"O",
                    new Font(FontFamily.GenericMonospace, szVal, FontStyle.Italic, GraphicsUnit.Pixel),
                    new SolidBrush(lastHit ? Color.Green : Color.Red), x - 35, y - 43);
            }
        }

        void FrmSample_FormClosing(object sender, FormClosingEventArgs e)
        {
            Logger.WriteLog(() => $@"{nameof(FrmSample_FormClosing)}: Завершение работы программы в обычном режиме...{Environment.NewLine}Причина: {e.CloseReason}.", Logger.LogLevel.DEBUG);

            try
            {
                GameSession.SaveStopSessionsToFile();
            }
            catch (Exception ex)
            {
                Logger.WriteLog(() => $@"{nameof(FrmSample_FormClosing)}: Ошибка: {ex.Message}.", Logger.LogLevel.ERROR);
                e.Cancel = MessageBox.Show(this,
                    $@"Ошибка при сохранении опыта игры: ""{ex.Message}""{Environment.NewLine}Всё равно выйти?",
                    @"Ошибка", MessageBoxButtons.YesNo) != DialogResult.Yes;
            }

            if (e.Cancel)
            {
                Logger.WriteLog(() => $@"{nameof(FrmSample_FormClosing)}: Произошла ошибка при сохранении наработанного опыта, и пользователь отменил выход из программы.", Logger.LogLevel.DEBUG);
                return;
            }

            Logger.WriteLog(() => $@"{nameof(FrmSample_FormClosing)}: Работа в обычном режиме завершена.");
        }
    }
}