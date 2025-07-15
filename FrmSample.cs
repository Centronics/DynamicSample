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
                int pbDrawCw = pbDraw.Width / 3;
                int pbDrawCh = pbDraw.Height / 3;

                if (!_gameSession.MakeCompetitorHit(e.X / pbDrawCw, e.Y / pbDrawCh))
                    return;

                RefreshGameField();

                if (_gameSession.CurrentWinner == GameSession.Winner.NOBODY)
                {
                    _gameSession.MakeHitDecision();
                    RefreshGameField();
                }

                switch (_gameSession.CurrentWinner)
                {
                    case GameSession.Winner.NOBODY:
                        return;
                    case GameSession.Winner.STANDOFF:
                        MessageBox.Show(@"Ничья!");
                        RefreshGameField(true);
                        return;
                    case GameSession.Winner.USER:
                        MessageBox.Show(@"Ты выиграл!");
                        RefreshGameField(true);
                        return;
                    case GameSession.Winner.BOT:
                        MessageBox.Show(@"Компьютер выиграл!");
                        RefreshGameField(true);
                        return;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, @"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
        }

        void FrmSample_Shown(object sender, EventArgs e)
        {
            pbDraw.Image = _gameCanvas = new Bitmap(pbDraw.Width, pbDraw.Height);
            _gameGrFront = Graphics.FromImage(_gameCanvas);
            RefreshGameField();
        }

        void FrmSample_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Escape:
                    Application.Exit();
                    return;
            }
        }

        void FrmSample_FormClosed(object sender, FormClosedEventArgs e)
        {
            pbDraw.Image = null;

            _gameGrFront?.Dispose();
            _gameCanvas?.Dispose();
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
    }
}