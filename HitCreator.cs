using System;
using DynamicProcessor;
using static DynamicSample.FrmSample;
using Processor = DynamicParser.Processor;

namespace DynamicSample
{
    internal class HitCreator
    {
        readonly SignValue[,] _mainProcessor;

        int _hitX, _hitY;

        int _mainX, _mainY;

        public HitCreator(SignValue[,] map)
        {
            if (map == null)
                throw new ArgumentNullException(nameof(map));

            _mainProcessor = MapCopy(map, false);
        }

        Processor Processor => new Processor(_mainProcessor, $@"b{_hitX}{_hitY}");

        public Winner CurrentWinner => GetWinner(_mainProcessor);

        public bool CanUserWin => StepByModelIndex(0, 1) != null;

        public bool CanBotWin => StepByModelIndex(1) != null;

        public Processor NextStep => StepByModelIndex(0);

        public SignValue this[int x, int y] => _mainProcessor[x, y];

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
                throw new Exception(
                    $@"Внутренняя ошибка: бот попытался ударить в то место, где уже занято ({x}, {y}).");

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
                        {
                            sv[x, y] = UserHit;
                        }
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
                    {
                        sv[x, y] = map[x, y];
                    }
                }
                else
                {
                    sv[x, y] = map[x, y];
                }

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
                                (HitCreator hc, bool end) =
                                    result.CreateHit(!isBot, result._mainProcessor, ref counter, invert);

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
    }
}