using System;
using System.Collections.Generic;
using System.IO;
using DynamicParser;
using DynamicProcessor;

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
            NULL,
            STANDOFF,
            INVERT,
            TOTAL
        }

        public static readonly int EmptyHit = 0;

        public static readonly int UserHit = int.MaxValue;

        public static readonly int BotHit = int.MinValue;

        readonly int[,] _gameField;

        int _curX, _curY;

        static GameSession _lastBotHit;

        static readonly HashSet<GameSession> SessionsStandoff = new HashSet<GameSession>();

        static readonly HashSet<GameSession> SessionsTotal = new HashSet<GameSession>();

        public GameSession()
        {
            _gameField = new int[3, 3];

            for (int y = 0, mY = _gameField.GetLength(1); y < mY; y++)
                for (int x = 0, mX = _gameField.GetLength(0); x < mX; x++)
                    _gameField[x, y] = EmptyHit;
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
                        if (_gameField[x, y] == EmptyHit)
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
            if (_gameField[x, y] != EmptyHit)
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

                    if (_lastBotHit is null)
                        return true;

                    switch (_lastBotHit.CurrentModel)
                    {
                        case InterModel.STANDOFF:
                            if (cw != Winner.STANDOFF)
                                SessionsStandoff.Add(_lastBotHit);
                            break;
                        case InterModel.TOTAL:
                            SessionsTotal.Add(_lastBotHit);
                            if (cw == Winner.USER)
                                SessionsStandoff.Add(_lastBotHit);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    _lastBotHit = null;

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return true;
        }

        public void MakeBotHit()
        {
            GameSession p = HowChangeFrame() ?? throw new InvalidDataException();

            int x = p.HitX;
            int y = p.HitY;

            if (_gameField[x, y] != EmptyHit)
                throw new Exception(
                    $@"Внутренняя ошибка: бот попытался ударить в то место, где уже занято ({x}, {y}).");

            _gameField[x, y] = BotHit;

            HitX = x;
            HitY = y;
        }

        GameSession HowChangeFrame()
        {
            GameSession result = null;
            int resultLength = int.MaxValue;

            for (InterModel k = 0, pk = 0; k < (InterModel)4; k++)
            {
                while (true)
                {
                    int ctxLength = 0;
                    (GameSession frame, bool end) =
                        NextFrame(true, null, ref ctxLength, k);

                    if (end)
                    {
                        if (!(frame is null))
                            result = frame;
                        break;
                    }

                    if (frame is null)
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
                throw new InvalidOperationException($@"{nameof(result)} почему-то null...");

            if (result.CurrentModel == InterModel.NULL || result.CurrentModel == InterModel.INVERT)
                return result;
            
            int[,] gf = GameFieldCopy(_gameField);
            gf[result.HitX, result.HitY] = BotHit;

            GameSession gs = new GameSession(gf, result.CurrentModel)
            {
                HitX = result.HitX,
                HitY = result.HitY
            };

            switch (result.CurrentModel)
            {
                case InterModel.STANDOFF:
                    if (!SessionsStandoff.Contains(gs))
                        _lastBotHit = gs;
                    break;
                case InterModel.TOTAL:
                    if (!SessionsTotal.Contains(gs))
                        _lastBotHit = gs;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
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
                    if (map[_curX, _curY] != EmptyHit)
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

                    switch (model)
                    {
                        case InterModel.NULL:
                            return (ctx, true);
                        case InterModel.STANDOFF:
                            if (SessionsStandoff.Contains(ctx))
                                continue;
                            break;
                        case InterModel.INVERT:
                            break;
                        case InterModel.TOTAL:
                            if (SessionsTotal.Contains(ctx))
                                continue;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(model), model, null);
                    }

                    int ctxMinLength = int.MaxValue;

                    switch (ctx.CurrentWinner)
                    {
                        case Winner.BOT:
                            _curX++;
                            if (model != InterModel.TOTAL)
                                return (null, false);
                            return SessionsTotal.Contains(ctx) ? (null, false) : (ctx, false);
                        case Winner.USER:
                            _curX++;
                            return model == InterModel.INVERT ? (ctx, false) : (null, false);
                        case Winner.STANDOFF:
                            _curX++;
                            if (model != InterModel.STANDOFF)
                                return (null, false);
                            return SessionsTotal.Contains(ctx) ? (null, false) : (ctx, false);
                        case Winner.NOBODY:
                            if (model == InterModel.INVERT || SessionsTotal.Contains(ctx))
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