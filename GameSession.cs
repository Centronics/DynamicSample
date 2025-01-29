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

        static readonly HashSet<GameSession> FallingStatesInverted = new HashSet<GameSession>();

        static readonly Dictionary<GameSession, HashSet<GameSession>> CommonSessions = new Dictionary<GameSession, HashSet<GameSession>>();

        static readonly Dictionary<GameSession, HashSet<GameSession>> CommonSessionsInvert = new Dictionary<GameSession, HashSet<GameSession>>();

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

        int EmptiesCount
        {
            get
            {
                int result = 0;

                for (int y = 0, mY = _gameField.GetLength(1); y < mY; y++)
                    for (int x = 0, mX = _gameField.GetLength(0); x < mX; x++)
                        result += Convert.ToInt32(_gameField[x, y] == EmptySpace);

                return result;
            }
        }

        bool HitOnFirstField()
        {
            for (int y = 0, mY = _gameField.GetLength(1); y < mY; y++)
                for (int x = 0, mX = _gameField.GetLength(0); x < mX; x++)
                    if (_gameField[x, y] == EmptySpace)
                    {
                        _gameField[x, y] = BotHit;
                        HitX = x;
                        HitY = y;
                        return true;
                    }

            return false;
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

                if (_lastBotHit.EmptiesCount > 0)
                {
                    if (_lastBotHit.IsInvert)
                        FallingStatesInverted.Add(_lastBotHit);
                    else
                        FallingStates.Add(_lastBotHit);
                }

                _lastBotHit = null;
            }

            void AddCommon()
            {
                GameSession gf = null;
                foreach (GameSession gs in GameSessionCopy)
                {
                    if (gs.IsInvert)
                    {
                        gf = gs;
                        

                        continue;
                    }

                    if (CommonSessions.TryGetValue(gs, out HashSet<GameSession> v))
                        v.Add(gs._lastSolution);
                    else
                        CommonSessions.Add(gs, new HashSet<GameSession> { gs._lastSolution });
                }

                if (!(gf is null)) // здесь и обучение должно идти наоборот - надо бить так, чтобы не попасть в эти точки.... а не ОТМЕНЯТЬ ИХ!!
                {
                    if (CommonSessionsInvert.TryGetValue(gf, out HashSet<GameSession> vi))
                        vi.Add(gf._lastSolution);
                    else
                        CommonSessionsInvert.Add(gf, new HashSet<GameSession> { gf._lastSolution });
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
            _lastBotHit = null;

            GameSession result = null;

            for (int k = 0, resultLength = int.MaxValue; k < 2; k++)
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

                    if (ctxLength >= resultLength)
                        continue;

                    resultLength = ctxLength;
                    result = frame;
                    //result.IsInvert = k == 0;
                    _lastSolution = endSession;

                    if (resultLength == 0)
                        break;
                }

                _curY = _curX = 0;
            }

            if (result is null)
            {
                result = new GameSession(_gameField, false);
                if (!result.HitOnFirstField())
                    throw new Exception(@"Неизвестная ошибка.");
                _lastSolution = new GameSession(result);
                _lastBotHit = new GameSession(result);
            }
            else if (result.CurrentWinner == Winner.NOBODY)
                _lastBotHit = new GameSession(result);

            GameSessionCopy.Add(new GameSession(_gameField, result.IsInvert, _lastSolution));

            return result;
        }

        (GameSession frame, bool end, GameSession endSession) NextFrame(bool isBot, int[,] map, ref int ctxLength, bool invert, GameSession startSession)
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

                    GameSession ctx = new GameSession(map, invert)
                    {
                        _gameField =
                        {
                            [_curX, _curY] = isBot ? BotHit : UserHit //isBot ? invert ? UserHit : BotHit : invert ? BotHit : UserHit
                        },
                        HitX = _curX,
                        HitY = _curY
                    };

                    int ctxMinLength = int.MaxValue;
                    GameSession es = null;

                    switch (ctx.CurrentWinner)
                    {
                        case Winner.BOT:
                            {
                                _curX++;

                                if (invert)
                                {
                                    if (CommonSessionsInvert.TryGetValue(startSession, // ВЕСЬ УМ здесь))))))
                                            out HashSet<GameSession>
                                                gsi)) // разделить на модели - как насчет того, чтобы поддердать инверсную модель в пункте USER??
                                        if (gsi.Contains(ctx)) // для отладки
                                            return (null, false, null);

                                    return (ctx, false, new GameSession(ctx));
                                }

                                if (CommonSessions.TryGetValue(startSession,
                                        out HashSet<GameSession>
                                            gss)) // разделить на модели - как насчет того, чтобы поддердать инверсную модель в пункте USER??
                                    if (gss.Contains(ctx)) // для отладки
                                        return (null, false, null);

                                return (ctx, false, new GameSession(ctx));
                            }
                        case Winner.USER:
                        //{
                        //    _curX++;

                        //    if (invert)
                        //    {
                        //        if (CommonSessions.TryGetValue(startSession,
                        //                out HashSet<GameSession>
                        //                    gss)) // разделить на модели - как насчет того, чтобы поддердать инверсную модель в пункте USER??
                        //            if (gss.Contains(ctx)) // для отладки
                        //                return (null, false, null);

                        //        return (ctx, false, new GameSession(ctx));
                        //    }

                        //    if (CommonSessionsInvert.TryGetValue(startSession,
                        //            out HashSet<GameSession>
                        //                gsi)) // разделить на модели - как насчет того, чтобы поддердать инверсную модель в пункте USER??
                        //        if (gsi.Contains(ctx)) // для отладки
                        //            return (null, false, null);

                        //    return (ctx, false, new GameSession(ctx));
                        //}
                        case Winner.STANDOFF:
                            _curX++;
                            return (null, false, null);
                        case Winner.NOBODY:
                            if (invert)
                            {
                                if (FallingStatesInverted.Contains(this))
                                {
                                    _curX++;
                                    return (null, true, null);
                                }
                            }
                            else
                            {
                                if (FallingStates.Contains(this))
                                {
                                    _curX++;
                                    return (null, true, null);
                                }
                            }

                            while (true)
                            {
                                (GameSession frame, bool end, GameSession endSession) = ctx.NextFrame(!isBot, ctx._gameField, ref ctxLength, invert, startSession);

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
                                    //break;
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