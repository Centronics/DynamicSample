using System;

namespace DynamicSample
{
    internal class GameSession
    {
        public enum Winner
        {
            USER,
            BOT,
            STANDOFF,
            NOBODY
        }

        public static readonly int UserHit = int.MaxValue;

        public static readonly int BotHit = int.MinValue;

        const int EmptySpace = 0;

        readonly int[,] _gameField;

        int _curX, _curY;

        public GameSession()
        {
            _gameField = new int[3, 3];

            for (int y = 0, mY = _gameField.GetLength(1); y < mY; y++)
                for (int x = 0, mX = _gameField.GetLength(0); x < mX; x++)
                    _gameField[x, y] = EmptySpace;
        }

        GameSession(int[,] map)
        {
            if (map == null)
                throw new ArgumentNullException(nameof(map));

            _gameField = GameFieldCopy(map);
        }

        public int LastHitX { get; private set; } = -1;

        public int LastHitY { get; private set; } = -1;

        public int this[int x, int y] => _gameField[x, y];

        public Winner CurrentWinner
        {
            get
            {
                bool bu = IsLine(UserHit);
                bool bb = IsLine(BotHit);

                switch (bu)
                {
                    case true when bb:
                        return Winner.STANDOFF;
                    case true:
                        return Winner.USER;
                    case false when bb:
                        return Winner.BOT;
                }

                for (int y = 0, mY = _gameField.GetLength(1); y < mY; y++)
                    for (int x = 0, mX = _gameField.GetLength(0); x < mX; x++)
                        if (_gameField[x, y] == EmptySpace)
                            return Winner.NOBODY;

                return Winner.STANDOFF;

                bool IsLine(int sv)
                {
                    if (_gameField[0, 0] == sv && _gameField[1, 0] == sv &&
                        _gameField[2, 0] == sv)
                        return true;

                    if (_gameField[0, 1] == sv && _gameField[1, 1] == sv &&
                        _gameField[2, 1] == sv)
                        return true;

                    if (_gameField[0, 2] == sv && _gameField[1, 2] == sv &&
                        _gameField[2, 2] == sv)
                        return true;

                    if (_gameField[0, 0] == sv && _gameField[0, 1] == sv &&
                        _gameField[0, 2] == sv)
                        return true;

                    if (_gameField[1, 0] == sv && _gameField[1, 1] == sv &&
                        _gameField[1, 2] == sv)
                        return true;

                    if (_gameField[2, 0] == sv && _gameField[2, 1] == sv &&
                        _gameField[2, 2] == sv)
                        return true;

                    if (_gameField[0, 0] == sv && _gameField[1, 1] == sv &&
                        _gameField[2, 2] == sv)
                        return true;

                    return _gameField[0, 2] == sv && _gameField[1, 1] == sv &&
                           _gameField[2, 0] == sv;
                }
            }
        }

        public bool MakeUserHit(int x, int y)
        {
            if (_gameField[x, y] != EmptySpace)
                return false;

            _gameField[x, y] = UserHit;

            LastHitX = x;
            LastHitY = y;

            return true;
        }

        public bool MakeBotHit()
        {
            GameSession gs = BuildNextFrame();

            if (gs == null)
                return false;

            int x = gs.LastHitX;
            int y = gs.LastHitY;

            if (_gameField[x, y] != EmptySpace)
                throw new Exception(
                    $@"Внутренняя ошибка: бот попытался ударить в то место, где уже занято ({x}, {y}).");

            _gameField[x, y] = BotHit;

            LastHitX = x;
            LastHitY = y;

            return true;
        }

        GameSession BuildNextFrame()
        {
            GameSession result = null;

            for (int k = 0, lastK = -1, resultLength = int.MaxValue; k < 2; k++)
            {
                while (true)
                {
                    int ctxLength = 0;
                    (GameSession frame, bool end) = NextFrame(true, null, ref ctxLength, k == 0);

                    if (end)
                        break;

                    if (frame == null || ctxLength > resultLength)
                        continue;

                    if (ctxLength == resultLength && k <= lastK)
                        continue;

                    lastK = k;
                    result = frame;
                    resultLength = ctxLength;

                    if (ctxLength == 0)
                        break;
                }

                _curY = _curX = 0;
            }

            return result;
        }

        (GameSession frame, bool end) NextFrame(bool isBot, int[,] map, ref int ctxLength, bool invert)
        {
            int ctl = ++ctxLength;

            if (map == null)
            {
                map = GameFieldCopy(_gameField, invert);
                ctl = ctxLength = 0;
            }

            for (int mMainY = map.GetLength(1); _curY < mMainY; _curY++)
            {
                for (int mMainX = map.GetLength(0); _curX < mMainX; _curX++)
                {
                    if (map[_curX, _curY] != EmptySpace)
                        continue;

                    GameSession ctx = new GameSession(map)
                    {
                        _gameField =
                        {
                            [_curX, _curY] = isBot ? BotHit : UserHit
                        },
                        LastHitX = _curX,
                        LastHitY = _curY
                    };

                    int ctxMinLength = int.MaxValue;

                    switch (ctx.CurrentWinner)
                    {
                        case Winner.BOT:
                            _curX++;
                            return (ctx, false);
                        case Winner.USER:
                        case Winner.STANDOFF:
                            _curX++;
                            return (null, false);
                        case Winner.NOBODY:
                            while (true)
                            {
                                (GameSession frame, bool end) =
                                    ctx.NextFrame(!isBot, ctx._gameField, ref ctxLength, invert);

                                if (end)
                                {
                                    ctxLength = ctl;
                                    break;
                                }

                                if (ctxMinLength > ctxLength)
                                {
                                    if (frame == null)
                                    {
                                        ctxLength = ctl;
                                        continue;
                                    }

                                    ctxMinLength = ctxLength;
                                }

                                ctxLength = ctl;
                            }

                            if (ctxMinLength == int.MaxValue)
                                continue;

                            _curX++;
                            ctxLength = ctxMinLength;
                            return (ctx, false);
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                _curX = 0;
            }

            ctxLength = ctl;
            return (null, true);
        }

        static int[,] GameFieldCopy(int[,] map, bool invert = false)
        {
            if (map == null)
                throw new ArgumentNullException(nameof(map));

            int sX = map.GetLength(0), sY = map.GetLength(1);

            int[,] result = new int[sX, sY];

            for (int y = 0; y < sY; y++)
                for (int x = 0; x < sX; x++)
                {
                    if (!invert || map[x, y] == EmptySpace)
                    {
                        result[x, y] = map[x, y];
                        continue;
                    }

                    if (map[x, y] == BotHit)
                    {
                        result[x, y] = UserHit;
                        continue;
                    }

                    if (map[x, y] != UserHit)
                        throw new Exception(
                            $@"Неизвестное значение поля на игровой карте ({map[x, y]}).");

                    result[x, y] = BotHit;
                }

            return result;
        }
    }
}