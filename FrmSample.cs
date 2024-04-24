using DynamicProcessor;
using System;
using System.Drawing;
using System.Windows.Forms;
using Processor = DynamicParser.Processor;

namespace DynamicSample
{
    public partial class FrmSample : Form
    {
        public static SignValue UserHit => SignValue.MaxValue;

        public static SignValue BotHit => SignValue.MinValue;

        public static SignValue EmptySpace => SignValue.MaxValue.Average(SignValue.MinValue);

        HitCreator _currentSession;

        int _lastHitX = -1, _lastHitY = -1;

        class HitCreator
        {
            readonly SignValue[,] _mainProcessor;

            int _mainX, _mainY;

            int _hitX, _hitY;

            public HitCreator(SignValue[,] map)
            {
                if (map == null)
                    throw new ArgumentNullException(nameof(map));

                _mainProcessor = MapCopy(map, false);
            }

            Processor Processor => new Processor(_mainProcessor, $@"b{_hitX}{_hitY}");

            public Winner CurrentWinner => GetWinner(_mainProcessor);

            static Winner GetWinner(SignValue[,] p)
            {
                bool bu = IsLine(UserHit);
                bool bb = IsLine(BotHit);

                switch (bu)
                {
                    case true when bb:
                        return Winner.STANDOFF;
                    case true:
                        return Winner.USER;
                }

                if (bb)
                    return Winner.BOT;

                for (int y = 0; y < p.GetLength(1); y++)
                    for (int x = 0; x < p.GetLength(0); x++)
                        if (p[x, y] == EmptySpace)
                            return Winner.NOBODY;

                return Winner.STANDOFF;

                bool IsLine(SignValue sv)
                {
                    if (p[0, 0] == sv && p[1, 0] == sv &&
                        p[2, 0] == sv)
                        return true;

                    if (p[0, 1] == sv && p[1, 1] == sv &&
                        p[2, 1] == sv)
                        return true;

                    if (p[0, 2] == sv && p[1, 2] == sv &&
                        p[2, 2] == sv)
                        return true;

                    if (p[0, 0] == sv && p[0, 1] == sv &&
                        p[0, 2] == sv)
                        return true;

                    if (p[1, 0] == sv && p[1, 1] == sv &&
                        p[1, 2] == sv)
                        return true;

                    if (p[2, 0] == sv && p[2, 1] == sv &&
                        p[2, 2] == sv)
                        return true;

                    if (p[0, 0] == sv && p[1, 1] == sv &&
                        p[2, 2] == sv)
                        return true;

                    return p[0, 2] == sv && p[1, 1] == sv &&
                           p[2, 0] == sv;
                }
            }

            public bool MakeUserHit(int x, int y)
            {
                if (_mainProcessor[x, y] != EmptySpace)
                    return false;

                _mainProcessor[x, y] = UserHit;
                return true;
            }

            public void MakeBotHit(int x, int y)
            {
                if (_mainProcessor[x, y] != EmptySpace)
                    throw new Exception($@"Внутренняя ошибка: бот попытался ударить в то место, где уже занято ({x}, {y}).");

                _mainProcessor[x, y] = BotHit;
            }

            static SignValue[,] MapCopy(SignValue[,] map, bool invert)
            {
                if (map == null)
                    throw new ArgumentNullException(nameof(map));

                SignValue[,] sv = new SignValue[map.GetLength(0), map.GetLength(1)];

                for (int y = 0; y < map.GetLength(1); y++)
                    for (int x = 0; x < map.GetLength(0); x++)
                        if (invert)
                        {
                            if (map[x, y] != EmptySpace)
                            {
                                if (map[x, y] == BotHit)
                                    sv[x, y] = UserHit;
                                else
                                {
                                    if (map[x, y] == UserHit)
                                        sv[x, y] = BotHit;
                                    else
                                        throw new Exception(
                                            $@"Неизвестное значение поля на игровой карте ({map[x, y].Value}).");
                                }
                            }
                            else
                                sv[x, y] = map[x, y];
                        }
                        else
                            sv[x, y] = map[x, y];

                return sv;
            }

            (HitCreator hc, bool end) CreateHit(bool isBot, SignValue[,] map, ref uint counter, bool invert)
            {
                bool isStart = map == null;

                counter++;

                if (isStart)
                {
                    map = MapCopy(_mainProcessor, invert);
                    counter = 0;
                }

                uint ct = counter;
                HitCreator lastHc = null;

                for (; _mainY < map.GetLength(1); _mainY++)
                {
                    for (; _mainX < map.GetLength(0); _mainX++)
                    {
                        if (map[_mainX, _mainY] != EmptySpace)
                            continue;

                        HitCreator result = new HitCreator(map)
                        {
                            _mainProcessor =
                                {
                                    [_mainX, _mainY] = isBot ? BotHit : UserHit
                                },
                            _hitX = _mainX,
                            _hitY = _mainY
                        };

                        switch (result.CurrentWinner)
                        {
                            case Winner.BOT:
                                _mainX++;
                                return (result, false);
                            case Winner.USER:
                            case Winner.STANDOFF:
                                _mainX++;
                                return (null, false);
                            case Winner.NOBODY:
                                {
                                    uint ctMin = uint.MaxValue;

                                    do
                                    {
                                        (HitCreator hc, bool end) = result.CreateHit(!isBot, result._mainProcessor, ref counter, invert);

                                        if (end)
                                            break;

                                        if (ctMin > counter)
                                        {
                                            if (hc == null)
                                            {
                                                counter = ct;
                                                continue;
                                            }

                                            ctMin = counter;
                                            lastHc = hc;
                                        }

                                        counter = ct;

                                    } while (true);

                                    if (ctMin != uint.MaxValue)
                                    {
                                        _mainX++;
                                        counter = ctMin;

                                        return (lastHc, false);
                                    }

                                    break;
                                }
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }

                    _mainX = 0;
                }

                counter = ct;
                return (null, true);
            }

            Processor StepByModelIndex(int modelStartIndex, int modelEndIndex = 2)
            {
                uint commonCounter = uint.MaxValue;
                HitCreator commonHitCreator = null;
                int lastK = -1;

                for (int k = modelStartIndex; k < modelEndIndex; k++)
                {
                    while (true)
                    {
                        uint counter = 0;
                        (HitCreator hc, bool end) = CreateHit(true, null, ref counter, k == 0);

                        if (end)
                            break;

                        if (hc == null || counter > commonCounter)
                            continue;

                        if (counter == commonCounter && k <= lastK)
                            continue;

                        lastK = k;
                        commonHitCreator = hc;
                        commonCounter = counter;
                    }

                    _mainY = _mainX = 0;
                }

                return commonHitCreator?.Processor;
            }

            public bool CanUserWin => StepByModelIndex(0, 1) != null;

            public bool CanBotWin => StepByModelIndex(1) != null;

            public Processor NextStep => StepByModelIndex(0);

            public SignValue this[int x, int y] => _mainProcessor[x, y];
        }

        public enum Winner
        {
            USER,
            BOT,
            STANDOFF,
            NOBODY
        }

        public FrmSample()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Изображение игровой карты.
        /// </summary>
        Bitmap _currentCanvas;

        /// <summary>
        /// Поверхность для рисования на игровой карте.
        /// </summary>
        Graphics _currentgrFront;

        /// <summary>
        ///     Задаёт цвет и ширину для рисования в окне создания распознаваемого изображения.
        /// </summary>
        static readonly Pen BlackPen = new Pen(Color.Black, 2.0f);

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
