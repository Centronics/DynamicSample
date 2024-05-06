using System;
using DynamicProcessor;
using static DynamicSample.FrmSample;

namespace DynamicSample
{
    internal class HitCreator
    {
        readonly SignValue[,] _mainProcessor;

        int _mainX, _mainY;

        public HitCreator()
        {
            _mainProcessor = new SignValue[3, 3];

            for (int y = 0, mY = _mainProcessor.GetLength(1); y < mY; y++)
                for (int x = 0, mX = _mainProcessor.GetLength(0); x < mX; x++)
                    _mainProcessor[x, y] = EmptySpace;
        }

        HitCreator(SignValue[,] map)
        {
            if (map == null)
                throw new ArgumentNullException(nameof(map));

            _mainProcessor = MapCopy(map, false);
        }

        public Winner CurrentWinner => GetWinner(_mainProcessor);

        public bool CanUserWin => StepByModelIndex(0, 1) != null;

        public bool CanBotWin => StepByModelIndex(1) != null;

        public int LastHitX { get; private set; } = -1;

        public int LastHitY { get; private set; } = -1;

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

            for (int y = 0, mY = p.GetLength(1); y < mY; y++)
                for (int x = 0, mX = p.GetLength(0); x < mX; x++)
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

            LastHitX = x;
            LastHitY = y;

            return true;
        }

        public void MakeBotHit()
        {
            HitCreator hc = StepByModelIndex(0) ?? throw new Exception(@"Почему-то я не знаю, как ходить - ВНУТРЕННЯЯ ошибка.");

            int x = hc.LastHitX;
            int y = hc.LastHitY;

            if (_mainProcessor[x, y] != EmptySpace)
                throw new Exception(
                    $@"Внутренняя ошибка: бот попытался ударить в то место, где уже занято ({x}, {y}).");

            _mainProcessor[x, y] = BotHit;

            LastHitX = x;
            LastHitY = y;
        }

        static SignValue[,] MapCopy(SignValue[,] map, bool invert)
        {
            if (map == null)
                throw new ArgumentNullException(nameof(map));

            int sX = map.GetLength(0), sY = map.GetLength(1);

            SignValue[,] sv = new SignValue[sX, sY];

            for (int y = 0; y < sY; y++)
                for (int x = 0; x < sX; x++)
                {
                    if (!invert || map[x, y] == EmptySpace)
                    {
                        sv[x, y] = map[x, y];
                        continue;
                    }

                    if (map[x, y] == BotHit)
                    {
                        sv[x, y] = UserHit;
                        continue;
                    }

                    if (map[x, y] != UserHit)
                        throw new Exception(
                            $@"Неизвестное значение поля на игровой карте ({map[x, y].Value}).");

                    sv[x, y] = BotHit;
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

            for (int mMainY = map.GetLength(1); _mainY < mMainY; _mainY++)
            {
                for (int mMainX = map.GetLength(0); _mainX < mMainX; _mainX++)
                {
                    if (map[_mainX, _mainY] != EmptySpace)
                        continue;

                    HitCreator result = new HitCreator(map)
                    {
                        _mainProcessor =
                        {
                            [_mainX, _mainY] = isBot ? BotHit : UserHit
                        },
                        LastHitX = _mainX,
                        LastHitY = _mainY
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

        HitCreator StepByModelIndex(int modelStartIndex, int modelEndIndex = 2)
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

            return commonHitCreator;
        }
    }
}