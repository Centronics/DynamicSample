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

        int _lastBotHitX = -1, _lastBotHitY = -1;

        class HitCreator
        {
            readonly SignValue[,] _mainProcessor;

            int _mainX, _mainY;

            int _hitX, _hitY;

            public HitCreator(SignValue[,] map)
            {
                if (map == null)
                    throw new ArgumentNullException(nameof(map));

                _mainProcessor = MapCopy(map);
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

            static SignValue[,] MapCopy(SignValue[,] map)
            {
                if (map == null)
                    throw new ArgumentNullException(nameof(map));

                SignValue[,] sv = new SignValue[map.GetLength(0), map.GetLength(1)];

                for (int y = 0; y < map.GetLength(1); y++)
                    for (int x = 0; x < map.GetLength(0); x++)
                        sv[x, y] = map[x, y];

                return sv;
            }

            (HitCreator hc, bool end) CreateHit(bool isBot, SignValue[,] map, ref uint counter)
            {
                bool isStart = map == null;

                if (isStart)
                {
                    map = MapCopy(_mainProcessor);
                    counter = 0;
                }

                uint ct = counter;
                HitCreator lastHc = null;

                counter++;

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
                                        (HitCreator hc, bool end) = result.CreateHit(!isBot, result._mainProcessor, ref counter);

                                        if (end)
                                        {
                                            counter = ct;
                                            break;
                                        }

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

            public Processor NextStep
            {
                get
                {
                    uint commonCounter = uint.MaxValue;
                    HitCreator commonHitCreator = null;

                    while (true)
                    {
                        uint counter = 0;
                        (HitCreator hc, bool end) = CreateHit(true, null, ref counter);

                        if (end)
                            break;

                        if (hc == null || counter >= commonCounter)
                            continue;

                        commonHitCreator = hc;
                        commonCounter = counter;
                    }

                    _mainY = _mainX = 0;

                    return commonHitCreator?.Processor;
                }
            }

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
        /// Изображение карты.
        /// </summary>
        Bitmap _currentCanvas;
        /// <summary>
        /// Поверхность для рисования.
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
                        DrawX(x * 161, y * 161);
                    if (_currentSession[x, y] == BotHit)
                        DrawZero(x * 161, y * 161, _lastBotHitX == x && _lastBotHitY == y);
                }

            pbDraw.Refresh();

            return;

            void DrawX(int x, int y)
            {
                _currentgrFront.DrawString(@"X",
                    new Font(FontFamily.GenericMonospace, 224.0F, FontStyle.Italic, GraphicsUnit.Pixel),
                    new SolidBrush(Color.DodgerBlue), x - 40, y - 50);
            }

            void DrawZero(int x, int y, bool lastHit)
            {
                _currentgrFront.DrawString(@"O",
                    new Font(FontFamily.GenericMonospace, 224.0F, FontStyle.Italic, GraphicsUnit.Pixel),
                    new SolidBrush(lastHit ? Color.Green : Color.Red), x - 35, y - 43);
            }
        }

        void PbDraw_MouseUp(object sender, MouseEventArgs e)
        {
            int cx = e.X / 161;
            int cy = e.Y / 161;

            if (!_currentSession.MakeUserHit(cx, cy))
                return;

            Processor botHit = _currentSession.NextStep;

            if (botHit == null)
            {
                MessageBox.Show(@"Вы выиграли! Не знаю, как ходить!");
                NewGame();
                return;
            }

            int bx = Convert.ToInt32(botHit.Tag[1].ToString());
            int by = Convert.ToInt32(botHit.Tag[2].ToString());

            _currentSession.MakeBotHit(bx, by);

            _lastBotHitX = bx;
            _lastBotHitY = by;

            Repaint();

            switch (_currentSession.CurrentWinner)
            {
                case Winner.NOBODY:
                    return;
                case Winner.STANDOFF:
                    MessageBox.Show(@"Ничья!");
                    NewGame();
                    return;
                case Winner.USER:
                    MessageBox.Show(@"Вы выиграли!");
                    NewGame();
                    return;
                case Winner.BOT:
                    MessageBox.Show(@"Компьютер выиграл!");
                    NewGame();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        void NewGame()
        {
            SignValue[,] map = new SignValue[3, 3];

            for (int y = 0; y < map.GetLength(1); y++)
                for (int x = 0; x < map.GetLength(0); x++)
                    map[x, y] = EmptySpace;

            _currentSession = new HitCreator(map);

            _lastBotHitY = _lastBotHitX = -1;

            Repaint();
        }
    }
}
