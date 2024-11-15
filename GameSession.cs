using System;
using System.Collections.Generic;
using System.Linq;

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

        const int EmptySpace = 0;

        public static readonly int UserHit = int.MaxValue;

        public static readonly int BotHit = int.MinValue;

        readonly int[,] _gameField;

        int _curX, _curY;

        int _rotateIndex;

        static readonly HashSet<GameSession> CommonGameSessions = new HashSet<GameSession>();

        static readonly List<GameSession> CurrentGameStep = new List<GameSession>();

        static void RotateSession()
        {
            foreach (GameSession gs in CurrentGameStep)
                gs.Rotate();

            CurrentGameStep.Clear();
        }

        public void RotateCurrentSession()
        {
            Winner w = CurrentWinner;

            if (w == Winner.USER || w == Winner.STANDOFF)
                RotateSession();
        }

        public GameSession()
        {
            CurrentGameStep.Clear();

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
            if (gs == null)
                throw new ArgumentNullException();

            _gameField = GameFieldCopy(gs._gameField);

            HitX = gs.HitX;
            HitY = gs.HitY;
        }

        public int HitX { get; private set; } = -1;

        public int HitY { get; private set; } = -1;

        public int this[int x, int y] => _gameField[x, y];

        void Rotate()
        {
            _rotateIndex++;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;

            GameSession gs = obj as GameSession;

            if (gs == null)
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

        public bool MakeUserHit(int x, int y)
        {
            if (_gameField[x, y] != EmptySpace)
                return false;

            _gameField[x, y] = UserHit;

            HitX = x;
            HitY = y;

            return true;
        }

        public bool MakeBotHit()
        {
            if (CommonGameSessions.TryGetValue(this, out GameSession gs))
            {
                _rotateIndex = gs._rotateIndex;
                CurrentGameStep.Add(gs);
            }
            else
            {
                _rotateIndex = 0;
                GameSession t = new GameSession(this);
                CommonGameSessions.Add(t);
                CurrentGameStep.Add(t);
            }

            GameSession p = HowChangeFrame();

            if (p == null)
            {
                RotateSession();
                return false;
            }

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
            if (_rotateIndex < 0)
                throw new InvalidOperationException($@"Значение {nameof(_rotateIndex)} не может быть меньше нуля ({_rotateIndex}).");

            List<(GameSession gs, int ctxLen)> gss = new List<(GameSession gs, int ctxLen)>();

            for (int k = 0, resultLength = int.MaxValue; k < 2; k++)
            {
                while (true)
                {
                    int ctxLength = 0;
                    (GameSession frame, bool end) = NextFrame(true, null, ref ctxLength, k == 0);

                    if (end)
                        break;

                    if (frame == null || ctxLength > resultLength)
                        continue;

                    resultLength = ctxLength;

                    gss.Add((frame, resultLength));
                }

                _curY = _curX = 0;
            }

            if (!gss.Any())
                return null;

            int minCtxLen = gss.Select(v => v.ctxLen).Min();

            List<GameSession> bestGss = new List<GameSession>(gss.Where(v => v.ctxLen == minCtxLen).Select(v => v.gs));

            if (!bestGss.Any())
                throw new InvalidOperationException(@"Массив результата не может быть пустым!");

            return bestGss[_rotateIndex % bestGss.Count];
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
                        HitX = _curX,
                        HitY = _curY
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