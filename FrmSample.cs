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
                if (!_gameSession.MakeUserHit(e.X / 161, e.Y / 161))
                    return;

                RefreshGameField();

                if (_gameSession.CurrentWinner == GameSession.Winner.NOBODY)
                {
                    if (!_gameSession.MakeBotHit())
                    {
                        MessageBox.Show(@"Ничья, никто не сможет выиграть!");
                        RefreshGameField(true);
                        return;
                    }

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

            _gameGrFront.DrawRectangle(BlackPen, 161, 0, 2, pbDraw.Height);
            _gameGrFront.DrawRectangle(BlackPen, 322, 0, 2, pbDraw.Height);

            _gameGrFront.DrawRectangle(BlackPen, 0, 161, pbDraw.Width, 2);
            _gameGrFront.DrawRectangle(BlackPen, 0, 322, pbDraw.Width, 2);

            for (int y = 0; y < 3; y++)
            for (int x = 0; x < 3; x++)
            {
                if (_gameSession[x, y] == GameSession.UserHit)
                    DrawX(x * 161, y * 161, _gameSession.LastHitX == x && _gameSession.LastHitY == y);
                if (_gameSession[x, y] == GameSession.BotHit)
                    DrawZero(x * 161, y * 161, _gameSession.LastHitX == x && _gameSession.LastHitY == y);
            }

            pbDraw.Refresh();

            return;

            void DrawX(int x, int y, bool lastHit)
            {
                _gameGrFront.DrawString(@"X",
                    new Font(FontFamily.GenericMonospace, 224.0F, FontStyle.Italic, GraphicsUnit.Pixel),
                    new SolidBrush(lastHit ? Color.Green : Color.DodgerBlue), x - 36, y - 46);
            }

            void DrawZero(int x, int y, bool lastHit)
            {
                _gameGrFront.DrawString(@"O",
                    new Font(FontFamily.GenericMonospace, 224.0F, FontStyle.Italic, GraphicsUnit.Pixel),
                    new SolidBrush(lastHit ? Color.Green : Color.Red), x - 35, y - 43);
            }
        }
    }
}