using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace DynamicSample
{
    public partial class FrmSample : Form
    {
        struct SymbolDrawCorrection
        {
            public Point P1 { get; set; }

            public Point P2 { get; set; }

            public Point P3 { get; set; }

            public Point P4 { get; set; }
        }

        static readonly Brush LineBrush = new SolidBrush(Color.Gray);

        static readonly Brush FinishBrush = new SolidBrush(Color.Red);

        static readonly Color FinishSymbolColor = Color.White;

        static readonly Color XColor = Color.DodgerBlue;

        static readonly Color OColor = Color.Green;

        static readonly Color LastHitColor = Color.Red;

        readonly (int styleCode1, int styleCode2, bool active)[,] _screenArray = new (int, int, bool)[3, 3];

        bool _needEmptyClick;

        int _xAppearStyleBaseCode;

        int _xAppearStyleAppCode;

        int _oAppearStyleBaseCode;

        int _oAppearStyleAppCode;

        Bitmap _gameCanvas;

        Graphics _gameGrFront;

        GameSession _gameSession;

        static readonly SymbolDrawCorrection[] X1DrawCorrections =
        {
            new SymbolDrawCorrection(),
            new SymbolDrawCorrection{P1 = new Point(15, -15), P2 = new Point(15, 10), P3 = new Point(10, 1), P4 = new Point(5, -9) },
            new SymbolDrawCorrection{P1 = new Point(0, 0), P2 = new Point(-5, 0), P3 = new Point(5, 20), P4 = new Point(85, 68) },
            new SymbolDrawCorrection{P1 = new Point(-10, -10), P2 = new Point(40, 50), P3 = new Point(-8, 0), P4 = new Point(40, -20) }
        };

        static readonly SymbolDrawCorrection[] X2DrawCorrections =
        {
            new SymbolDrawCorrection(),
            new SymbolDrawCorrection{P1 = new Point(-20, 20), P2 = new Point(-10, -15), P3 = new Point(20, 15), P4 = new Point(20, -15) },
            new SymbolDrawCorrection{P1 = new Point(-20, 55), P2 = new Point(7, -7), P3 = new Point(-10, 6), P4 = new Point(8, 13) },
            new SymbolDrawCorrection{P1 = new Point(-10, -10), P2 = new Point(-20, -30), P3 = new Point(15, 10), P4 = new Point(40, -20) }
        };

        static readonly SymbolDrawCorrection[] O1DrawCorrections =
        {
            new SymbolDrawCorrection(),
            new SymbolDrawCorrection{P1 = new Point(-10, -20), P2 = new Point(-20, -4), P3 = new Point(40, -9), P4 = new Point(50, 39) },
            new SymbolDrawCorrection{P1 = new Point(20, 19), P2 = new Point(15, 23), P3 = new Point(-20, 15), P4 = new Point(-50, -40) },
            new SymbolDrawCorrection{P1 = new Point(30, 10), P2 = new Point(40, -20), P3 = new Point(10, -30), P4 = new Point(-40, -70) }
        };

        static readonly SymbolDrawCorrection[] O2DrawCorrections =
        {
            new SymbolDrawCorrection(),
            new SymbolDrawCorrection{P1 = new Point(1, 3), P2 = new Point(-2, -4), P3 = new Point(-8, 9), P4 = new Point(5, 9) },
            new SymbolDrawCorrection{P1 = new Point(15, 15), P2 = new Point(20, 10), P3 = new Point(-10, -19), P4 = new Point(-10, -10) },
            new SymbolDrawCorrection{P1 = new Point(5, 7), P2 = new Point(15, 5), P3 = new Point(-5, -10), P4 = new Point(-3, 0) }
        };

        public FrmSample()
        {
            _diffEqual = decimal.Parse("1e-18", NumberStyles.Float, CultureInfo.InvariantCulture);

            InitializeComponent();
        }

        void ResetAppearStyle()
        {
            for (int y = 0; y < _screenArray.GetLength(1); y++)
                for (int x = 0; x < _screenArray.GetLength(0); x++)
                    _screenArray[x, y] = (0, 0, false);
        }

        void PbDraw_MouseClick(object sender, MouseEventArgs e)
        {
            try
            {
                Logger.WriteLog(() => $@"{nameof(PbDraw_MouseClick)}: Клик на форме (X = {e.X}, Y = {e.Y}).", Logger.LogLevel.DEBUG);

                if (_needEmptyClick)
                {
                    Logger.WriteLog(() => $@"{nameof(PbDraw_MouseClick)}: <<Пустой клик для начала новой игры>>.", Logger.LogLevel.DEBUG);
                    RefreshGameField(true);
                    ResetAppearStyle();
                    _needEmptyClick = false;
                    return;
                }

                int pbDrawCw = pbDraw.Width / 3;
                int pbDrawCh = pbDraw.Height / 3;

                if (!_gameSession.MakeUserHit(e.X / pbDrawCw, e.Y / pbDrawCh))
                {
                    Logger.WriteLog(() => $@"{nameof(PbDraw_MouseClick)}: Клик пользователя ({e.X}, {e.Y}) неудачен.", Logger.LogLevel.DEBUG);
                    return;
                }

                Logger.WriteLog(() => $@"{nameof(PbDraw_MouseClick)}: Клик пользователя ({e.X}, {e.Y}) успешен.", Logger.LogLevel.DEBUG);

                if (_gameSession.CurrentWinner == GameSession.Winner.NOBODY)
                {
                    (int x, int y) = _gameSession.MakeHitDecision();

                    Logger.WriteLog(() => $@"{nameof(PbDraw_MouseClick)}: Удар бота ({x}, {y}).", Logger.LogLevel.DEBUG);

                    if (!_gameSession.MakeBotHit(x, y))
                    {
                        const string s = @"Неожиданная ошибка: Ничья, никто не сможет выиграть!";
                        Logger.WriteLog(() => $@"{nameof(PbDraw_MouseClick)}: {s} Удар бота ({x}, {y}).", Logger.LogLevel.DEBUG);
                        MessageBox.Show($@"{s}{Environment.NewLine}Удар бота ({x}, {y}).");
                        RefreshGameField(true);
                        return;
                    }

                    Logger.WriteLog(() => $@"{nameof(PbDraw_MouseClick)}: Удар бота успешен.", Logger.LogLevel.DEBUG);
                }

                switch (_gameSession.CurrentWinner)
                {
                    case GameSession.Winner.USER:
                    case GameSession.Winner.BOT:
                        RefreshGameField(false, _gameSession.CurrentWinnerEx.winPts);
                        break;
                    case GameSession.Winner.STANDOFF:
                    case GameSession.Winner.NOBODY:
                    default:
                        RefreshGameField();
                        break;
                }

                switch (_gameSession.CurrentWinner)
                {
                    case GameSession.Winner.NOBODY:
                        Logger.WriteLog(() => $@"{nameof(PbDraw_MouseClick)}: Никто не выиграл. Игра продолжается.", Logger.LogLevel.DEBUG);
                        return;
                    case GameSession.Winner.STANDOFF:
                        Logger.WriteLog(() => $@"{nameof(PbDraw_MouseClick)}: Ничья!");
                        _needEmptyClick = true;
                        return;
                    case GameSession.Winner.USER:
                        Logger.WriteLog(() => $@"{nameof(PbDraw_MouseClick)}: Ты выиграл!");
                        _needEmptyClick = true;
                        return;
                    case GameSession.Winner.BOT:
                        Logger.WriteLog(() => $@"{nameof(PbDraw_MouseClick)}: Компьютер выиграл!");
                        _needEmptyClick = true;
                        return;
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
                _gameGrFront.SmoothingMode = SmoothingMode.AntiAlias;
                _gameGrFront.InterpolationMode = InterpolationMode.HighQualityBicubic;
                _gameGrFront.PixelOffsetMode = PixelOffsetMode.HighQuality;
                RefreshGameField(true);

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

        static int DrawCorrectionsLength
        {
            get
            {
                int xLength = X1DrawCorrections.Length;

                if (xLength != X2DrawCorrections.Length)
                    throw new Exception(@"");

                if (xLength != O1DrawCorrections.Length)
                    throw new Exception(@"123");

                if (xLength != O2DrawCorrections.Length)
                    throw new Exception(@"1234");

                return xLength;
            }
        }

        void RefreshGameField(bool createNewGame = false, Point[] winPts = null)
        {
            if (createNewGame || _gameSession == null)
            {
                int xLength = DrawCorrectionsLength;

                _xAppearStyleBaseCode = Convert.ToInt32(DateTime.Now.Ticks % xLength);
                _xAppearStyleAppCode = Convert.ToInt32(DateTime.Now.Ticks % xLength);
                _oAppearStyleBaseCode = xLength - 1 - _xAppearStyleBaseCode;
                _oAppearStyleAppCode = xLength - 1 - _xAppearStyleAppCode;

                Logger.WriteLog(() =>
                {
                    string s1 = $@"{nameof(_xAppearStyleBaseCode)} = {_xAppearStyleBaseCode}, {nameof(_xAppearStyleAppCode)} = {_xAppearStyleAppCode}";
                    string s2 = $@"{nameof(_oAppearStyleBaseCode)} = {_oAppearStyleBaseCode}, {nameof(_oAppearStyleAppCode)} = {_oAppearStyleAppCode}";
                    return $@"{nameof(RefreshGameField)}: {s1}; {s2}.";
                }, Logger.LogLevel.DEBUG);

                Logger.WriteLog(() => $@"{nameof(RefreshGameField)}: Начало новой игры.");
                _gameSession = new GameSession();
            }

            _gameGrFront.Clear(Color.LightGray);

            int pbDrawCw = pbDraw.Width / 3;
            int pbDrawCh = pbDraw.Height / 3;

            for (int y = 0; y < 3; y++)
                for (int x = 0; x < 3; x++)
                {
                    GameSession.GameFieldHit n = _gameSession[x, y];
                    switch (n)
                    {
                        case GameSession.GameFieldHit.USER:
                            if (Launcher.InvertModeEnabled)
                                DrawO(x, y);
                            else
                                DrawX(x, y);
                            break;
                        case GameSession.GameFieldHit.BOT:
                            if (Launcher.InvertModeEnabled)
                                DrawX(x, y);
                            else
                                DrawO(x, y);
                            break;
                        case GameSession.GameFieldHit.EMPTY:
                            break;
                        default:
                            {
                                string s = $@"Неизвестное значение поля игровой карты ({n}).";
                                Logger.WriteLog(() => $@"{nameof(RefreshGameField)}: {s}", Logger.LogLevel.ERROR);
                                throw new InvalidOperationException(s);
                            }
                    }
                }

            int lineWidth = GetPercentFrom(pbDraw.Width, 964, 4);
            int lineHeight = GetPercentFrom(pbDraw.Height, 927, 4);

            _gameGrFront.FillRectangle(LineBrush, pbDrawCw, 0, lineWidth, pbDraw.Height); // |
            _gameGrFront.FillRectangle(LineBrush, pbDrawCw * 2, 0, lineWidth, pbDraw.Height); //  |

            _gameGrFront.FillRectangle(LineBrush, 0, pbDrawCh, pbDraw.Width, lineHeight); // -
            _gameGrFront.FillRectangle(LineBrush, 0, pbDrawCh * 2, pbDraw.Width, lineHeight); // _

            pbDraw.Refresh();

            return;

            Color SetFieldBackgroundColor(int x, int y, Color desiredColor)
            {
                if (winPts?.All(p => x != p.X || y != p.Y) ?? true)
                    return _gameSession.HitX == x && _gameSession.HitY == y ? LastHitColor : desiredColor;

                _gameGrFront.FillRectangle(FinishBrush, x * pbDrawCw, y * pbDrawCh, pbDrawCw, pbDrawCh);

                return FinishSymbolColor;
            }

            // Расшифровки названий переменных:
            // xNN - увеличение по оси X на NN пикселей.
            // xxNN - увеличение на NN пикселей относительно середины клетки.
            // xdNN - то же самое, что и xNN, но координата указана относительно конца (а не начала).
            // xxdNN - то же самое, что и xxNN, но координата указана относительно конца (а не начала).
            // xx2 - означает середину клетки.
            // Аналогично работает и для оси Y.

            (int, int) GetStyleCodes(int x, int y, ref int xoAppearStyleBaseCode, ref int xoAppearStyleAppCode, bool minus)
            {
                (int styleCode1, int styleCode2, bool active) = _screenArray[x, y];

                if (active)
                    return (styleCode1, styleCode2);

                for (int pBase = 0, pLength = DrawCorrectionsLength; pBase < pLength; pBase++)
                {
                    GetNextStyleCode(ref xoAppearStyleBaseCode);

                    for (int aApp = 0; aApp < pLength; aApp++)
                    {
                        GetNextStyleCode(ref xoAppearStyleAppCode);

                        if (IsExists(xoAppearStyleBaseCode, xoAppearStyleAppCode))
                            continue;

                        _screenArray[x, y] = (xoAppearStyleBaseCode, xoAppearStyleAppCode, true);
                        return (xoAppearStyleBaseCode, xoAppearStyleAppCode);
                    }
                }

                throw new Exception();

                bool IsExists(int sc1, int sc2)
                {
                    for (int py = 0; py < _screenArray.GetLength(1); py++)
                        for (int px = 0; px < _screenArray.GetLength(0); px++)
                        {
                            (int styleCode1, int styleCode2, bool active) v = _screenArray[py, px];

                            if (!v.active)
                                continue;

                            if (v.styleCode1 == sc1 && v.styleCode2 == sc2)
                                return true;
                        }

                    return false;
                }

                void GetNextStyleCode(ref int val)
                {
                    if (minus)
                    {
                        if (--val < 0)
                            val = DrawCorrectionsLength - 1;
                        return;
                    }

                    if (++val > DrawCorrectionsLength - 1)
                        val = 0;
                }
            }

            void DrawX(int x, int y)
            {
                (int sc1, int sc2) = GetStyleCodes(x, y, ref _xAppearStyleBaseCode, ref _xAppearStyleAppCode, false);
                Color col = SetFieldBackgroundColor(x, y, XColor);
                SymbolDrawCorrection sdc1 = X1DrawCorrections[sc1];
                SymbolDrawCorrection sdc2 = X2DrawCorrections[sc2];

                int px = x * pbDrawCw, py = y * pbDrawCh;

                int xd25 = px + pbDrawCw - GetPercentFrom(pbDrawCw, 321, 25);
                int xd35 = px + pbDrawCw - GetPercentFrom(pbDrawCw, 321, 35);
                int xd60 = px + pbDrawCw - GetPercentFrom(pbDrawCw, 321, 60);
                int xd70 = px + pbDrawCw - GetPercentFrom(pbDrawCw, 321, 70);
                int x40 = px + GetPercentFrom(pbDrawCw, 321, 40);
                int x45 = px + GetPercentFrom(pbDrawCw, 321, 45);
                int x55 = px + GetPercentFrom(pbDrawCw, 321, 55);
                int x60 = px + GetPercentFrom(pbDrawCw, 321, 60);
                int xxd40 = px + pbDrawCw / 2 - GetPercentFrom(pbDrawCw / 2, 160.5m, 40);
                int xx50 = px + pbDrawCw / 2 + GetPercentFrom(pbDrawCw / 2, 160.5m, 50);

                int y55 = py + GetPercentFrom(pbDrawCh, 309, 55);
                int y35 = py + GetPercentFrom(pbDrawCh, 309, 35);
                int yy10 = py + pbDrawCh / 2 + GetPercentFrom(pbDrawCh / 2, 154.5m, 10);
                int yy20 = py + pbDrawCh / 2 + GetPercentFrom(pbDrawCh / 2, 154.5m, 20);
                int yd30 = py + pbDrawCh - GetPercentFrom(pbDrawCh, 309, 30);
                int yd50 = py + pbDrawCh - GetPercentFrom(pbDrawCh, 309, 50);
                int yd65 = py + pbDrawCh - GetPercentFrom(pbDrawCh, 309, 65);

                _gameGrFront.FillClosedCurve(new SolidBrush(col), new[]
                {
                    new Point(x40 + sdc1.P1.X, y55 + sdc1.P1.Y), new Point(x55 + sdc1.P1.X, y35 + sdc1.P1.Y),
                    new Point(xx50 + sdc1.P2.X, yy10 + sdc1.P2.Y),
                    new Point(xd25 + sdc1.P3.X, yd65 + sdc1.P3.Y), new Point(xd70 + sdc1.P3.X, yd50 + sdc1.P3.Y),
                    new Point(xxd40 + sdc1.P4.X, yy20 + sdc1.P4.Y)
                });

                _gameGrFront.FillClosedCurve(new SolidBrush(col), new[]
                {
                    new Point(xd60 + sdc2.P1.X, y35 + sdc2.P1.Y), new Point(xd35 + sdc2.P1.X, y55 + sdc2.P1.Y),
                    new Point(xx50 + sdc2.P2.X, yy10 + sdc2.P2.Y),
                    new Point(x60 + sdc2.P3.X, yd30 + sdc2.P3.Y), new Point(x45 + sdc2.P4.X, yd50 + sdc2.P4.Y)
                });
            }

            void DrawO(int x, int y)
            {
                (int sc1, int sc2) = GetStyleCodes(x, y, ref _oAppearStyleBaseCode, ref _oAppearStyleAppCode, true);
                Color col = SetFieldBackgroundColor(x, y, OColor);
                SymbolDrawCorrection sdc1 = O1DrawCorrections[sc1];
                SymbolDrawCorrection sdc2 = O2DrawCorrections[sc2];

                int px = x * pbDrawCw, py = y * pbDrawCh;

                int xx2 = px + pbDrawCw / 2;
                int xxd15 = px + pbDrawCw / 2 - GetPercentFrom(pbDrawCw, 321, 15);
                int xd35 = px + pbDrawCw - GetPercentFrom(pbDrawCw, 321, 35);
                int xd60 = px + pbDrawCw - GetPercentFrom(pbDrawCw, 321, 60);
                int xd100 = px + pbDrawCw - GetPercentFrom(pbDrawCw, 321, 100);
                int x45 = px + GetPercentFrom(pbDrawCw, 321, 45);
                int x60 = px + GetPercentFrom(pbDrawCw, 321, 60);
                int x70 = px + GetPercentFrom(pbDrawCw, 321, 70);
                int x80 = px + GetPercentFrom(pbDrawCw, 321, 80);

                int yy15 = py + pbDrawCh / 2 + GetPercentFrom(pbDrawCh, 309, 15);
                int yd30 = py + pbDrawCh - GetPercentFrom(pbDrawCh, 309, 30);
                int yd50 = py + pbDrawCh - GetPercentFrom(pbDrawCh, 309, 50);
                int yd75 = py + pbDrawCh - GetPercentFrom(pbDrawCh, 309, 75);
                int yd80 = py + pbDrawCh - GetPercentFrom(pbDrawCh, 309, 80);
                int y35 = py + GetPercentFrom(pbDrawCh, 309, 35);
                int y55 = py + GetPercentFrom(pbDrawCh, 309, 55);
                int y95 = py + GetPercentFrom(pbDrawCh, 309, 95);

                _gameGrFront.DrawClosedCurve(new Pen(col, 18), new[]
                {
                    new Point(x80 + sdc1.P1.X, y95 + sdc1.P1.Y), new Point(xx2 + sdc1.P1.X, y55 + sdc1.P1.Y), new Point(xd60 + sdc1.P2.X, y95 + sdc1.P2.Y),
                    new Point(xd100 + sdc1.P2.X, yd75 + sdc1.P2.Y), new Point(xxd15 + sdc1.P3.X, yd50 + sdc1.P3.Y), new Point(x70 + sdc1.P4.X, yd80 + sdc1.P4.Y)
                });

                _gameGrFront.FillClosedCurve(new SolidBrush(col), new[]
                {
                    new Point(xd60 + sdc2.P1.X, y35 + sdc2.P1.Y), new Point(xd35 + sdc2.P1.X, y55 + sdc2.P1.Y),
                    new Point(xx2 + sdc2.P2.X, yy15 + sdc2.P2.Y),
                    new Point(x60 + sdc2.P3.X, yd30 + sdc2.P3.Y), new Point(x45 + sdc2.P4.X, yd50 + sdc2.P4.Y)
                });
            }
        }

        readonly decimal _diffEqual;

        int GetPercentFrom(int number, decimal value, decimal distance)
        {
            decimal percent = distance / value;

            if (IsDiffEqual(percent, 0))
                throw new Exception(@"Почему так мало?");

            decimal r = number * percent;

            if (IsDiffEqual(r, 0))
                throw new Exception(@"Почему так мало?");

            return Convert.ToInt32(Math.Round(r, MidpointRounding.AwayFromZero));

            bool IsDiffEqual(decimal p1, decimal p2)
            {
                if (p1.Equals(p2))
                    return true;

                return Math.Abs(p1 - p2) < _diffEqual;
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