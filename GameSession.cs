using System;
using System.Collections.Generic;
using System.Linq;

//using System.Linq;

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

        const int EmptySpace = 0;

        public static readonly int UserHit = int.MaxValue;

        public static readonly int BotHit = int.MinValue;

        readonly int[,] _gameField;

        int _curX, _curY;

        int _solutionsCount;

        bool _firstInChain = true;

        GameSession _lastSolution;

        static GameSession LastBotHit;

        static readonly List<GameSession> GameSessionCopy = new List<GameSession>();

        static readonly HashSet<GameSession> FallingStates = new HashSet<GameSession>();

        static readonly Dictionary<GameSession, HashSet<GameSession>> CommonSessions = new Dictionary<GameSession, HashSet<GameSession>>();

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

        GameSession(GameSession gs)
        {
            if (gs is null)
                throw new ArgumentNullException(nameof(gs));

            _gameField = GameFieldCopy(gs._gameField);

            HitX = gs.HitX;
            HitY = gs.HitY;
        }

        public int HitX { get; private set; } = -1;

        public int HitY { get; private set; } = -1;

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

        public Winner CurrentWinner => GetCurrentWinner(false);

        Winner GetCurrentWinner(bool onStep)
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
                        return onStep && FallingStates.Contains(this) ? Winner.USER : Winner.NOBODY;

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

        public bool MakeUserHit(int x, int y)
        {
            if (_gameField[x, y] != EmptySpace)
                return false;

            _gameField[x, y] = UserHit;

            HitX = x;
            HitY = y;

            switch (CurrentWinner)
            {
                case Winner.BOT:
                    LastBotHit = null;
                    GameSessionCopy.Clear();
                    break;
                case Winner.NOBODY:
                    break;
                case Winner.USER:
                case Winner.STANDOFF:
                    AddFall();
                    AddCommon();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return true;

            void AddFall()
            {
                if (LastBotHit is null)
                    return;

                FallingStates.Add(LastBotHit);
                LastBotHit = null;
            }

            void AddCommon()
            {
                for (int k = GameSessionCopy.Count - 1; k >= 0; k--)
                {
                    GameSession gs = GameSessionCopy[k];

                    if (CommonSessions.TryGetValue(gs, out HashSet<GameSession> v))
                    {
                        if (v.Count == _solutionsCount)
                            continue;

                        v.Add(gs._lastSolution);
                        break;
                    }

                    CommonSessions.Add(gs, new HashSet<GameSession>());
                }
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
            try
            {
                if (!CommonSessions.ContainsKey(this))
                    CommonSessions.Add(this, new HashSet<GameSession>());

                GameSessionCopy.Add(this);

                int solutionsCount = 0;

                _lastSolution = null;

                GameSession result = null, reserveResult = null;

                for (int k = 0, resultLength = int.MaxValue; k < 2; k++)
                {
                    while (true)
                    {
                        int ctxLength = 0;
                        (GameSession frame, bool end, bool uf, GameSession endSession) = NextFrame(true, null, ref ctxLength, k == 0, this);

                        if (end)
                            break;

                        if (frame is null)
                            continue;

                        solutionsCount++;

                        if (ctxLength > resultLength)
                            continue;

                        resultLength = ctxLength;

                        switch (uf)
                        {
                            case false:
                                result = frame;
                                _lastSolution = endSession;
                                break;
                            case true when reserveResult is null:
                                reserveResult = frame;
                                break;
                        }

                        if (resultLength == 0)
                            break;
                    }

                    _curY = _curX = 0;
                }

                if (_solutionsCount > 0 && _solutionsCount < solutionsCount)
                    throw new Exception($@"Решения почему-то в разном количестве для одной и той же карты: {solutionsCount} против изначального {_solutionsCount}.");

                _solutionsCount = solutionsCount;

                if (_firstInChain && result is null)
                    throw new InvalidOperationException(@"EXCLAMATION");// очистка кеша

                GameSession gs = result ?? reserveResult;

                LastBotHit = !(gs is null) ? new GameSession(gs) : null;

                return gs;
            }
            finally
            {
                _firstInChain = false;
            }
        }

        (GameSession frame, bool end, bool uf, GameSession endSession) NextFrame(bool isBot, int[,] map, ref int ctxLength, bool invert, GameSession startSession)
        {
            int ctl = ++ctxLength;

            int minLenReserve = int.MaxValue;
            (GameSession frame, bool end, bool uf, GameSession endSession)? reserve = null;

            bool first = map == null;

            if (first)
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
                        HitX = _curX,
                        HitY = _curY
                    };

                    int ctxMinLength = int.MaxValue;

                    minLenReserve = int.MaxValue;
                    GameSession cFrame = null;

                    switch (ctx.GetCurrentWinner(true))
                    {
                        case Winner.BOT:
                            _curX++;

                            if (FallingStates.Contains(this))
                                return (ctx, false, true, new GameSession(this));

                            return CommonSessions.TryGetValue(ctx, out HashSet<GameSession> gss) ? (ctx, false, gss.Contains(ctx), new GameSession(this)) : (ctx, false, false, new GameSession(this));
                        case Winner.USER:
                        case Winner.STANDOFF:
                            _curX++;
                            return (null, false, false, null);
                        case Winner.NOBODY:

                            while (true)
                            {
                                (GameSession frame, bool end, bool uf, GameSession endSession) = ctx.NextFrame(!isBot, ctx._gameField, ref ctxLength, invert, first ? this : startSession);

                                if (end)
                                {
                                    ctxLength = ctl;
                                    break;
                                }
                                // Если окажется так, что второго решения нет (т.е. такого же по длине, как и первое), то вывалить как end...
                                // "думать" должен с учетом индексов...
                                // сделать проверку на "плохие" ситуации
                                // UF не нужен!!
                                switch (uf)
                                {
                                    case false when ctxMinLength > ctxLength:
                                        {
                                            if (frame is null)
                                            {
                                                ctxLength = ctl;
                                                continue;
                                            }

                                            ctxMinLength = ctxLength;
                                            break;
                                        }
                                    case true when minLenReserve > ctxLength:
                                        {
                                            if (frame is null)
                                            {
                                                ctxLength = ctl;
                                                continue;
                                            }

                                            minLenReserve = ctxMinLength;
                                            cFrame = endSession;
                                            break;
                                        }
                                }

                                ctxLength = ctl;
                            }

                            if (ctxMinLength == int.MaxValue && minLenReserve == int.MaxValue)
                                continue;

                            if (reserve == null)
                            {
                                reserve = (ctx, false, true, cFrame); // cFrame нужен для того, чтобы его записать в случае поражения
                                continue;
                            }

                            _curX++;
                            ctxLength = ctxMinLength;

                            return (ctx, false, false, cFrame);
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                _curX = 0;
            }

            if (minLenReserve != int.MaxValue && reserve != null)
            {
                ctxLength = minLenReserve;
                return reserve.Value;
            }

            ctxLength = ctl;
            return (null, true, false, null);
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