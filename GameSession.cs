using System;
using System.Collections.Generic;

namespace DynamicSample
{
    internal sealed class GameSession
    {
        public enum Winner
        {
            USER,
            BOT,
            STANDOFF,
            NOBODY
        }

        public enum InterModel
        {
            INVERT,
            FINDSTANDOFF,
            TOTAL
        }

        const int EmptySpace = 0;

        public static readonly int UserHit = int.MaxValue;

        public static readonly int BotHit = int.MinValue;

        readonly int[,] _gameField;

        int _curX, _curY;

        static GameSession _lastBotHit;

        static readonly HashSet<GameSession> CommonSessions = new HashSet<GameSession>();

        public GameSession()
        {
            _gameField = new int[3, 3];

            for (int y = 0, mY = _gameField.GetLength(1); y < mY; y++)
                for (int x = 0, mX = _gameField.GetLength(0); x < mX; x++)
                    _gameField[x, y] = EmptySpace;
        }

        GameSession(int[,] map, InterModel model)
        {
            if (map == null)
                throw new ArgumentNullException(nameof(map));

            _gameField = GameFieldCopy(map);
            CurrentModel = model;
        }

        public int HitX { get; private set; } = -1;

        public int HitY { get; private set; } = -1;

        public InterModel CurrentModel { get; }

        public int this[int x, int y] => _gameField[x, y];

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;

            if (!(obj is GameSession gs))
                return false;

            for (int y = 0, my = gs._gameField.GetLength(1); y < my; y++)
                for (int x = 0, mx = gs._gameField.GetLength(0); x < mx; x++)
                    if (_gameField[x, y] != gs._gameField[x, y])
                        return false;

            return true;
        }

        public override int GetHashCode()
        {
            return HashCreator.GetHash(GetNumbers());

            IEnumerable<int> GetNumbers()
            {
                for (int y = 0, my = _gameField.GetLength(1); y < my; y++)
                    for (int x = 0, mx = _gameField.GetLength(0); x < mx; x++)
                        yield return _gameField[x, y];
            }
        }

        public static bool operator ==(GameSession a, GameSession b)
        {
            if (ReferenceEquals(a, b))
                return true;

            return a?.Equals(b) == true;
        }

        public static bool operator !=(GameSession a, GameSession b) => !(a == b);

        public Winner CurrentWinner
        {
            get
            {
                bool bb = IsLine(BotHit);

                switch (IsLine(UserHit))
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

        void HitOnFirstField()
        {
            for (int y = 0, mY = _gameField.GetLength(1); y < mY; y++)
                for (int x = 0, mX = _gameField.GetLength(0); x < mX; x++)
                    if (_gameField[x, y] == EmptySpace)
                    {
                        _gameField[x, y] = BotHit;
                        HitX = x;
                        HitY = y;
                        return;
                    }

            throw new Exception(@"Неизвестная ошибка.");
        }

        public bool MakeUserHit(int x, int y)
        {
            if (_gameField[x, y] != EmptySpace)
                return false;

            _gameField[x, y] = UserHit;

            HitX = x;
            HitY = y;

            Winner cw = CurrentWinner;

            switch (cw)
            {
                case Winner.BOT:
                    _lastBotHit = null;
                    break;
                case Winner.NOBODY:
                    break;
                case Winner.USER:
                case Winner.STANDOFF:
                    AddFall(cw);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return true;

            void AddFall(Winner currentWinner)
            {
                if (_lastBotHit is null)
                    return;

                switch (currentWinner)
                {
                    case Winner.USER:
                        CommonSessions.Add(_lastBotHit);
                        break;
                    case Winner.STANDOFF:
                        if (_lastBotHit.CurrentModel == InterModel.TOTAL)
                            CommonSessions.Add(_lastBotHit);
                        break;
                }

                _lastBotHit = null;
            }
        }

        public bool MakeBotHit()
        {
            GameSession p = HowChangeFrame();

            if (p is null)
                return false;

            int x = p.HitX;
            int y = p.HitY;

            if (_gameField[x, y] != EmptySpace)
                throw new Exception(
                    $@"Внутренняя ошибка: бот попытался ударить в то место, где уже занято ({x}, {y}).");

            _gameField[x, y] = BotHit;

            HitX = x;
            HitY = y;

            return true;
        }

        GameSession HowChangeFrame()
        {
            GameSession result = null;

            for (int k = 0, pk = -1, resultLength = int.MaxValue; k < 3; k++)
            {
                while (true)
                {
                    int ctxLength = 0;
                    (GameSession frame, bool end) =
                        NextFrame(true, null, ref ctxLength, (InterModel)k);

                    if (end)
                        break;

                    if (frame is null)
                        continue;

                    if (k == 0 && ctxLength != 0)
                        continue;

                    if (ctxLength > resultLength)
                        continue;

                    if (k == pk && ctxLength == resultLength)
                        continue;

                    pk = k;
                    resultLength = ctxLength;
                    result = frame;
                }

                _curY = _curX = 0;
            }

            if (result is null)
            {
                result = new GameSession(_gameField, InterModel.TOTAL);
                result.HitOnFirstField();
            }
            else if (result.CurrentModel != InterModel.INVERT)
            {
                switch (result.CurrentWinner)
                {
                    case Winner.NOBODY:
                        int[,] gf = GameFieldCopy(_gameField);
                        gf[result.HitX, result.HitY] = BotHit;

                        GameSession gs = new GameSession(gf, InterModel.TOTAL)
                        {
                            HitX = result.HitX,
                            HitY = result.HitY
                        };

                        if (!CommonSessions.Contains(gs))
                            _lastBotHit = gs;

                        break;
                }
            }

            return result;
        }

        (GameSession frame, bool end) NextFrame(bool isBot, int[,] map, ref int ctxLength, InterModel model)
        {
            int ctl = ++ctxLength;

            if (map == null)
            {
                map = GameFieldCopy(_gameField);
                ctl = ctxLength = 0;
            }

            for (int mMainY = map.GetLength(1); _curY < mMainY; _curY++)
            {
                for (int mMainX = map.GetLength(0); _curX < mMainX; _curX++)
                {
                    if (map[_curX, _curY] != EmptySpace)
                        continue;

                    GameSession ctx = new GameSession(map, model)
                    {
                        _gameField =
                        {
                            [_curX, _curY] = isBot ? model == InterModel.INVERT ? UserHit : BotHit : model == InterModel.INVERT ? BotHit : UserHit
                        },
                        HitX = _curX,
                        HitY = _curY
                    };

                    if (model != InterModel.INVERT && CommonSessions.Contains(ctx))
                            continue;

                    int ctxMinLength = int.MaxValue;

                    switch (ctx.CurrentWinner)
                    {
                        case Winner.BOT:
                            _curX++;
                            if (model == InterModel.FINDSTANDOFF)
                                return (null, false);
                            return CommonSessions.Contains(ctx) ? (null, false) : (ctx, false);
                        case Winner.USER:
                            _curX++;
                            return model == InterModel.INVERT ? (ctx, false) : (null, false);
                        case Winner.STANDOFF:
                            _curX++;
                            if (model != InterModel.FINDSTANDOFF)
                                return (null, false);
                            return CommonSessions.Contains(ctx) ? (null, false) : (ctx, false);
                        case Winner.NOBODY:
                            if (model == InterModel.INVERT || CommonSessions.Contains(ctx))
                                continue;

                            while (true)
                            {
                                (GameSession frame, bool end) = ctx.NextFrame(!isBot, ctx._gameField, ref ctxLength, model);

                                if (end)
                                {
                                    ctxLength = ctl;
                                    break;
                                }

                                if (ctxMinLength > ctxLength)
                                {
                                    if (frame is null)
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

        static int[,] GameFieldCopy(int[,] map)
        {
            if (map == null)
                throw new ArgumentNullException(nameof(map));

            int sX = map.GetLength(0), sY = map.GetLength(1);

            int[,] result = new int[sX, sY];

            for (int y = 0; y < sY; y++)
                for (int x = 0; x < sX; x++)
                    result[x, y] = map[x, y];

            return result;
        }
    }
}