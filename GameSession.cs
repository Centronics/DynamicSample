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

        const int EmptySpace = 0;

        public static readonly int UserHit = int.MaxValue;

        public static readonly int BotHit = int.MinValue;

        readonly int[,] _gameField;

        int _curX, _curY;

        GameSession _lastSolution;

        static GameSession _lastBotHit;

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

        GameSession(int[,] map, bool invert, GameSession lastSolution = null)
        {
            if (map == null)
                throw new ArgumentNullException(nameof(map));

            _gameField = GameFieldCopy(map);
            IsInvert = invert;
            _lastSolution = lastSolution;
        }

        GameSession(GameSession gs)
        {
            if (gs is null)
                throw new ArgumentNullException(nameof(gs));

            _gameField = GameFieldCopy(gs._gameField);

            HitX = gs.HitX;
            HitY = gs.HitY;
            IsInvert = gs.IsInvert;

            _lastSolution = gs._lastSolution;
        }

        public int HitX { get; private set; } = -1;

        public int HitY { get; private set; } = -1;

        public bool IsInvert { get; }

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

            switch (CurrentWinner)
            {
                case Winner.BOT:
                    _lastBotHit = null;
                    GameSessionCopy.Clear();
                    break;
                case Winner.NOBODY:
                    break;
                case Winner.USER:
                case Winner.STANDOFF:
                    AddFall();
                    AddCommon();
                    GameSessionCopy.Clear();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return true;

            void AddFall()
            {
                if (_lastBotHit is null)
                    return;

                FallingStates.Add(_lastBotHit);
                _lastBotHit = null;
            }

            void AddCommon()
            {
                foreach (GameSession gs in GameSessionCopy)
                {
                    if (CommonSessions.TryGetValue(gs, out HashSet<GameSession> v))
                        v.Add(gs._lastSolution);
                    else
                        CommonSessions.Add(gs, new HashSet<GameSession> { gs._lastSolution });
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
            _lastSolution = null;

            GameSession result = null;

            for (int k = 0, pk = -1, resultLength = int.MaxValue; k < 2; k++)
            {
                while (true)
                {
                    int ctxLength = 0;
                    (GameSession frame, bool end, GameSession endSession) =
                        NextFrame(true, null, ref ctxLength, k == 0, this); // new GameSession(GameFieldCopy(_gameField, k==0), k==0));

                    if (end)
                        break;

                    if (frame is null)
                        continue;

                    if (ctxLength > resultLength)
                        continue;

                    if (k == 0 && ctxLength > 0)
                        continue;

                    if (k <= pk && ctxLength == resultLength)
                        continue;

                    pk = k;
                    resultLength = ctxLength;
                    result = frame;
                    _lastSolution = endSession;
                }

                _curY = _curX = 0;
            }

            if (result is null)
            {
                _lastBotHit = null;
                FallingStates.Add(new GameSession(this));
                result = new GameSession(_gameField, false);
                result.HitOnFirstField();
            }
            else if (!result.IsInvert)
            {
                switch (result.CurrentWinner)
                {
                    case Winner.NOBODY:
                        int[,] gf = GameFieldCopy(_gameField);
                        gf[result.HitX, result.HitY] = BotHit;

                        GameSession gs = new GameSession(gf, false)
                        {
                            HitX = result.HitX,
                            HitY = result.HitY
                        };

                        _lastBotHit = gs;
                        break;
                }
            }

            if (!result.IsInvert && !(_lastSolution is null))
                GameSessionCopy.Add(new GameSession(_gameField, false, _lastSolution));

            return result;
        }

        (GameSession frame, bool end, GameSession endSession) NextFrame(bool isBot, int[,] map, ref int ctxLength, bool invert, GameSession startSession)
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

                    GameSession ctx = new GameSession(map, invert)
                    {
                        _gameField =
                        {
                            [_curX, _curY] = isBot ? invert ? UserHit : BotHit : invert ? BotHit : UserHit //isBot ? BotHit : UserHit 
                        },
                        HitX = _curX,
                        HitY = _curY
                    };

                    int ctxMinLength = int.MaxValue;
                    GameSession es = null;

                    switch (ctx.CurrentWinner)
                    {
                        case Winner.BOT:
                            _curX++;

                            if (CommonSessions.TryGetValue(startSession, out HashSet<GameSession> gss) && gss.Contains(ctx))
                                return (null, false, null);

                            return (ctx, false, new GameSession(ctx));
                        case Winner.USER:
                            _curX++;
                            return invert ? (ctx, false, new GameSession(ctx)) : (null, false, null);
                        case Winner.STANDOFF:
                            _curX++;
                            return (null, false, null);
                        case Winner.NOBODY:
                            if (invert || FallingStates.Contains(this))
                                continue;

                            while (true)
                            {
                                (GameSession frame, bool end, GameSession endSession) = ctx.NextFrame(!isBot, ctx._gameField, ref ctxLength, false, startSession);

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
                                    es = endSession;
                                }

                                ctxLength = ctl;
                            }

                            if (ctxMinLength == int.MaxValue)
                                continue;

                            _curX++;
                            ctxLength = ctxMinLength;

                            return (ctx, false, es);
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                _curX = 0;
            }

            ctxLength = ctl;
            return (null, true, null);
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