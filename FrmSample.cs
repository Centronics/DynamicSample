using System;
using System.Drawing;
using System.Windows.Forms;
using DynamicProcessor;
using Processor = DynamicParser.Processor;

namespace DynamicSample
{
    public partial class FrmSample : Form
    {
        public enum Winner
        {
            USER,
            BOT,
            STANDOFF,
            NOBODY
        }

        static readonly Pen BlackPen = new Pen(Color.Black, 2.0f);

        /// <summary>
        ///     Изображение игровой карты.
        /// </summary>
        Bitmap _currentCanvas;

        /// <summary>
        ///     Поверхность для рисования на игровой карте.
        /// </summary>
        Graphics _currentgrFront;

        HitCreator _currentSession;

        int _lastHitX = -1, _lastHitY = -1;

        public FrmSample()
        {
            InitializeComponent();
        }

        public static SignValue UserHit => SignValue.MaxValue;

        public static SignValue BotHit => SignValue.MinValue;

        public static SignValue EmptySpace => SignValue.MaxValue.Average(SignValue.MinValue);

        void FrmSample_Shown(object sender, EventArgs e)
        {
            _currentCanvas = new Bitmap(pbDraw.Width, pbDraw.Height);
            _currentgrFront = Graphics.FromImage(_currentCanvas);
            pbDraw.Image = _currentCanvas;

            NewGame();
        }

        void Repaint()
        {
            _currentgrFront.Clear(Color.LightGray);

            _currentgrFront.DrawRectangle(BlackPen, 161, 0, 2, pbDraw.Height);
            _currentgrFront.DrawRectangle(BlackPen, 322, 0, 2, pbDraw.Height);

            _currentgrFront.DrawRectangle(BlackPen, 0, 161, pbDraw.Width, 2);
            _currentgrFront.DrawRectangle(BlackPen, 0, 322, pbDraw.Width, 2);

            for (int y = 0; y < 3; y++)
            for (int x = 0; x < 3; x++)
            {
                if (_currentSession[x, y] == UserHit)
                    DrawX(x * 161, y * 161, _lastHitX == x && _lastHitY == y);
                if (_currentSession[x, y] == BotHit)
                    DrawZero(x * 161, y * 161, _lastHitX == x && _lastHitY == y);
            }

            pbDraw.Refresh();

            return;

            void DrawX(int x, int y, bool lastHit)
            {
                _currentgrFront.DrawString(@"X",
                    new Font(FontFamily.GenericMonospace, 224.0F, FontStyle.Italic, GraphicsUnit.Pixel),
                    new SolidBrush(lastHit ? Color.Green : Color.DodgerBlue), x - 40, y - 50);
            }

            void DrawZero(int x, int y, bool lastHit)
            {
                _currentgrFront.DrawString(@"O",
                    new Font(FontFamily.GenericMonospace, 224.0F, FontStyle.Italic, GraphicsUnit.Pixel),
                    new SolidBrush(lastHit ? Color.Green : Color.Red), x - 35, y - 43);
            }
        }

        bool IsGameOver()
        {
            switch (_currentSession.CurrentWinner)
            {
                case Winner.NOBODY:
                {
                    bool bw = _currentSession.CanBotWin;
                    bool uw = _currentSession.CanUserWin;

                    switch (bw)
                    {
                        case false when !uw:
                            MessageBox.Show(@"Ничья! Никому не удастся выиграть!");
                            NewGame();
                            return true;
                        case false:
                            MessageBox.Show(@"Не вижу смысла продолжать игру! Не смогу победить!");
                            NewGame();
                            return true;
                        case true when !uw:
                            MessageBox.Show(@"У тебя не получится меня победить!");
                            NewGame();
                            return true;
                    }

                    return false;
                }
                case Winner.STANDOFF:
                    MessageBox.Show(@"Ничья!");
                    NewGame();
                    return true;
                case Winner.USER:
                    MessageBox.Show(@"Вы выиграли!");
                    NewGame();
                    return true;
                case Winner.BOT:
                    MessageBox.Show(@"Компьютер выиграл!");
                    NewGame();
                    return true;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        bool MakeBotHit()
        {
            Processor botHit = _currentSession.NextStep;

            if (botHit == null)
            {
                MessageBox.Show(@"Сдаюсь! Победить не смогу!");
                NewGame();
                return false;
            }

            int bx = Convert.ToInt32(botHit.Tag[1].ToString());
            int by = Convert.ToInt32(botHit.Tag[2].ToString());

            _currentSession.MakeBotHit(bx, by);

            _lastHitX = bx;
            _lastHitY = by;

            Repaint();

            return true;
        }

        bool MakeUserHit(int px, int py)
        {
            int cx = px / 161;
            int cy = py / 161;

            if (!_currentSession.MakeUserHit(cx, cy))
                return false;

            _lastHitX = cx;
            _lastHitY = cy;

            Repaint();

            return true;
        }

        void PbDraw_MouseClick(object sender, MouseEventArgs e)
        {
            if (!MakeUserHit(e.X, e.Y))
                return;

            if (IsGameOver())
                return;

            if (!MakeBotHit())
                return;

            IsGameOver();
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

        void NewGame()
        {
            SignValue[,] map = new SignValue[3, 3];

            for (int y = 0; y < map.GetLength(1); y++)
            for (int x = 0; x < map.GetLength(0); x++)
                map[x, y] = EmptySpace;

            _currentSession = new HitCreator(map);

            _lastHitY = _lastHitX = -1;

            Repaint();
        }
    }
}