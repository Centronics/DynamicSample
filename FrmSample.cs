using System;
using System.Drawing;
using System.Windows.Forms;
using DynamicProcessor;

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
                        DrawX(x * 161, y * 161, _currentSession.LastHitX == x && _currentSession.LastHitY == y);
                    if (_currentSession[x, y] == BotHit)
                        DrawZero(x * 161, y * 161, _currentSession.LastHitX == x && _currentSession.LastHitY == y);
                }

            pbDraw.Refresh();

            return;

            void DrawX(int x, int y, bool lastHit)
            {
                _currentgrFront.DrawString(@"X",
                    new Font(FontFamily.GenericMonospace, 224.0F, FontStyle.Italic, GraphicsUnit.Pixel),
                    new SolidBrush(lastHit ? Color.Green : Color.DodgerBlue), x - 36, y - 46);
            }

            void DrawZero(int x, int y, bool lastHit)
            {
                _currentgrFront.DrawString(@"O",
                    new Font(FontFamily.GenericMonospace, 224.0F, FontStyle.Italic, GraphicsUnit.Pixel),
                    new SolidBrush(lastHit ? Color.Green : Color.Red), x - 35, y - 43);
            }
        }

        bool HandleGameOver(bool over)
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
                            case false when over:
                                MessageBox.Show(@"Я не смогу выиграть, а ты сможешь! Давай заново!");
                                NewGame();
                                return true;
                            case true when !uw && over:
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
                    MessageBox.Show(@"Ты выиграл!");
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

        bool MakeUserHit(int px, int py)
        {
            int cx = px / 161;
            int cy = py / 161;

            if (!_currentSession.MakeUserHit(cx, cy))
                return false;

            Repaint();

            return true;
        }

        void PbDraw_MouseClick(object sender, MouseEventArgs e)
        {
            try
            {
                if (!MakeUserHit(e.X, e.Y))
                    return;

                if (HandleGameOver(false))
                    return;

                _currentSession.MakeBotHit();

                Repaint();

                HandleGameOver(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, @"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
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
            _currentgrFront?.Dispose();

            DisposeImage(pbDraw);
        }

        void NewGame()
        {
            _currentSession = new HitCreator();

            Repaint();
        }

        /// <summary>
        ///     Освобождает ресурсы, занимаемые изображением в указанном <see cref="PictureBox" />.
        /// </summary>
        /// <param name="pb"><see cref="PictureBox" />, <see cref="PictureBox.Image" /> которого требуется освободить.</param>
        /// <remarks>
        ///     После освобождения <see cref="PictureBox.Image" /> = <see langword="null" />.
        /// </remarks>
        public static void DisposeImage(PictureBox pb)
        {
            if (pb == null)
                throw new ArgumentNullException(nameof(pb), $@"{nameof(DisposeImage)}: {nameof(pb)} = null.");

            Image image = pb.Image;

            if (image == null)
                return;

            pb.Image = null;

            image.Dispose();
        }
    }
}