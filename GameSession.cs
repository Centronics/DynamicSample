using System;
using System.Collections.Generic;
//using System.Runtime.InteropServices;

//using System.Windows.Forms;

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

        static GameSession _lastSolution;
        //static HashSet<GameSession> _lastSet; // REFACTORING!!!

        //static readonly Dictionary<GameSession, HashSet<GameSession>> FallingStates = new Dictionary<GameSession, HashSet<GameSession>>();

        static readonly HashSet<GameSession> FallingStates = new HashSet<GameSession>();

        public GameSession()
        {
            _gameField = new int[3, 3];

            for (int y = 0, mY = _gameField.GetLength(1); y < mY; y++)
                for (int x = 0, mX = _gameField.GetLength(0); x < mX; x++)
                    _gameField[x, y] = EmptySpace;
        }

        GameSession(GameSession gs, bool invert = false)
        {
            if (gs is null)
                throw new ArgumentNullException();

            _gameField = GameFieldCopy(gs._gameField, invert);

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

            if (_lastSolution is null)
                return true;

            switch (CurrentWinner)
            {
                case Winner.BOT:
                    _lastSolution = null;
                    break;
                case Winner.USER:
                case Winner.STANDOFF:
                    //_lastSet?.Add(_lastSolution); // сделать флаг - изменена или нет
                    FallingStates.Add(_lastSolution);
                    _lastSolution = null;
                    break;
            }

            return true;
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

            //if (CurrentWinner == Winner.NOBODY)
                //_lastSolution = new GameSession(this);

            return true;
        }

        GameSession HowChangeFrame()
        {
            _lastSolution = new GameSession(this);

            //HashSet<GameSession> gameSessions;

            //if (!FallingStates.TryGetValue(this, out HashSet<GameSession> v))
            //{
            //    gameSessions = new HashSet<GameSession>();
            //    FallingStates.Add(new GameSession(this), gameSessions); // FallingStates сделать без карт, т.е. просто списком, и проверять его на карты, предшествующие проигрышу, и всё...
            //}
            //else
            //    gameSessions = v;

            //_lastSet = gameSessions;

            GameSession result = null;
            bool cUf = false;

            for (int lk = -1, k = 0, resultLength = int.MaxValue; k < 2; k++)
            {
                while (true)
                {
                    int ctxLength = 0;
                    (GameSession frame, bool end, bool uf) = NextFrame(true, null, ref ctxLength, k == 0, null); //gameSessions

                    if (end)
                        break;

                    if (frame is null)
                        continue;

                    if (uf)
                    {
                        if (cUf)
                        {
                            if (ctxLength > resultLength || (ctxLength == resultLength && lk == k))
                                continue;
                        }
                        else
                        {
                            if (lk > -1)
                                continue;
                        }
                    }
                    else
                    {
                        if (cUf)
                        {
                            // ignored
                        }
                        else
                        {
                            if (ctxLength > resultLength || (ctxLength == resultLength && lk == k))
                                continue;
                        }
                    }

                    resultLength = ctxLength;
                    result = new GameSession(frame, k == 0);

                    if (k == 0)
                        result._gameField[result.HitX, result.HitY] = BotHit;

                    cUf = uf;
                    lk = k;
                }

                _curY = _curX = 0;
            }

            //if (cUf)
            {
                //_lastSolution = new GameSession(this);
                //_lastSet = gameSessions;
                //  FallingStates[this].Clear(); // ПОДУМАТЬ НАД очисткой
            }

            return result;
        }

        (GameSession frame, bool end, bool uf) NextFrame(bool isBot, GameSession map, ref int ctxLength, bool invert, HashSet<GameSession> fStates)
        {
            int ctl = ++ctxLength;

            if (map == null)
            {
                map = new GameSession(this, invert);
                ctl = ctxLength = 0;
                //if (fStates is null)
                //  throw new ArgumentNullException();
            }

            for (int mMainY = map._gameField.GetLength(1); _curY < mMainY; _curY++)
            {
                for (int mMainX = map._gameField.GetLength(0); _curX < mMainX; _curX++)
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

                    switch (ctx.CurrentWinner)
                    {
                        case Winner.BOT:
                            _curX++;
                            return (ctx, false, false); // !invert && FallingStates.Contains(ctx)); //fStates.Contains(ctx));
                        case Winner.USER:
                        case Winner.STANDOFF:
                            _curX++;
                            return (null, false, false);
                        case Winner.NOBODY:
                            {
                                if (!invert && FallingStates.Contains(ctx)) //fStates.Contains(this))
                                {
                                    //_curX++;
                                    return (null, true, false);
                                    //continue;
                                }

                                int ctxMinLength = int.MaxValue;
                                bool cUf = false;

                                //HashSet<GameSession> gameSessions;

                                //if (!FallingStates.TryGetValue(this, out HashSet<GameSession> v))
                                //{
                                //    gameSessions = new HashSet<GameSession>();
                                //    FallingStates.Add(new GameSession(this), gameSessions);
                                //}
                                //else
                                //    gameSessions = v;

                                while (true)
                                {
                                    (GameSession frame, bool end, bool uf) =
                                        ctx.NextFrame(!isBot, ctx, ref ctxLength, invert, null); //gameSessions);

                                    if (end)
                                    {
                                        ctxLength = ctl;
                                        break;
                                    }

                                    if (frame is null)
                                    {
                                        ctxLength = ctl;
                                        continue;
                                    }

                                    if (uf)
                                    {
                                        if (cUf)
                                        {
                                            if (ctxMinLength <= ctxLength)
                                                continue;
                                        }
                                        else
                                        {
                                            if (ctxMinLength != int.MaxValue)
                                                continue;
                                        }
                                    }
                                    else
                                    {
                                        if (cUf)
                                        {
                                            // ignored
                                        }
                                        else
                                        {
                                            if (ctxMinLength <= ctxLength)
                                                continue;
                                        }
                                    }

                                    ctxMinLength = ctxLength;
                                    ctxLength = ctl;
                                    cUf = uf;

                                    if (ctxMinLength != 0)
                                        continue;

                                    cUf = false;
                                    break;
                                }

                                if (ctxMinLength == int.MaxValue)
                                    continue;

                                _curX++;
                                ctxLength = ctxMinLength;

                                //if (cUf || !(_lastSolution is null))
                                return (ctx, false, cUf);

                                //_lastSolution = new GameSession(ctx);
                                //_lastSet = gameSessions;

                                return (ctx, false, false);
                            }
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                _curX = 0;
            }

            ctxLength = ctl;
            return (null, true, false);
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