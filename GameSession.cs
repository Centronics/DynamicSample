using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace DynamicSample
{
    public sealed class GameSession
    {
        public enum Winner
        {
            USER,
            BOT,
            STANDOFF,
            NOBODY
        }

        enum InterModel
        {
            NULL,
            STANDOFF,
            INVERT,
            TOTAL
        }

        public enum FieldState
        {
            EMPTY,
            WAITBOTHIT,
            WAITUSERHIT,
            WAITHIT,
            FULL,
            ERROR
        }

        enum GameStatus
        {
            UNKNOWN,
            WAITBOTHIT,
            WAITUSERHIT
        }

        readonly GameFieldHit[,] _gameField = new GameFieldHit[3, 3];

        int _curX, _curY;

        GameStatus CurrentGameStatus { get; set; } = GameStatus.UNKNOWN;

        static GameSession _lastBotHit;

        [Serializable]
        public sealed class StopSessions
        {
            public StopSessions()
            {
                Logger.WriteLog(() => $@"{nameof(StopSessions)}: Конструктор по умолчанию.", Logger.LogLevel.DEBUG);
                SessionsStandoff = new HashSet<GameSession>();
                SessionsTotal = new HashSet<GameSession>();
            }

            public StopSessions(StopSessionsKeeperArrays stopSessionsKeeperStorage)
            {
                Logger.WriteLog(() => $@"{nameof(StopSessions)}: Вызван конструктор.", Logger.LogLevel.DEBUG);

                if (stopSessionsKeeperStorage is null)
                {
                    Logger.WriteLog(() => $@"{nameof(StopSessions)}: {nameof(stopSessionsKeeperStorage)} is null.", Logger.LogLevel.ERROR);
                    throw new ArgumentNullException(nameof(stopSessionsKeeperStorage));
                }

                int countStOff = stopSessionsKeeperStorage.SessionsStandoff.Count;
                int countTotal = stopSessionsKeeperStorage.SessionsTotal.Count;

                Logger.WriteLog(() => $@"{nameof(StopSessions)}: {nameof(countStOff)} = {countStOff}.", Logger.LogLevel.DEBUG);
                Logger.WriteLog(() => $@"{nameof(StopSessions)}: {nameof(countTotal)} = {countTotal}.", Logger.LogLevel.DEBUG);

                SessionsStandoff = new HashSet<GameSession>(countStOff);
                SessionsTotal = new HashSet<GameSession>(countTotal);

                foreach (GameSession gs in stopSessionsKeeperStorage.SessionsStandoff.Select(sk => sk.AsGameSession))
                {
                    if (gs is null)
                    {
                        Logger.WriteLog(() => $@"{nameof(gs)} is null.", Logger.LogLevel.ERROR);
                        throw new ArgumentNullException(nameof(gs));
                    }

                    Logger.WriteLog(() => $@"{nameof(StopSessions)}: {nameof(StopSessionsKeeperArrays.SessionsStandoff)} =>{Environment.NewLine}{gs}.", Logger.LogLevel.DEBUG);
                    SessionsStandoff.Add(gs);
                }

                foreach (GameSession gs in stopSessionsKeeperStorage.SessionsTotal.Select(sk => sk.AsGameSession))
                {
                    if (gs is null)
                    {
                        Logger.WriteLog(() => $@"{nameof(gs)} is null.", Logger.LogLevel.ERROR);
                        throw new ArgumentNullException(nameof(gs));
                    }

                    Logger.WriteLog(() => $@"{nameof(StopSessions)}: {nameof(StopSessionsKeeperArrays.SessionsTotal)} =>{Environment.NewLine}{gs}.", Logger.LogLevel.DEBUG);
                    SessionsTotal.Add(gs);
                }

                Logger.WriteLog(() => $@"{nameof(StopSessions)}: Все карты успешно загружены.", Logger.LogLevel.DEBUG);
            }

            [Serializable]
            public sealed class StopSessionsKeeper
            {
                public GameFieldHit[] GameField { get; set; }

                public StopSessionsKeeper() => Logger.WriteLog(() => $@"{nameof(StopSessionsKeeper)}: Сериализация...", Logger.LogLevel.DEBUG);

                public StopSessionsKeeper(GameSession gameSession)
                {
                    Logger.WriteLog(() => $@"{nameof(StopSessionsKeeper)}: Вызван конструктор.", Logger.LogLevel.DEBUG);

                    if (gameSession is null)
                    {
                        Logger.WriteLog(() => $@"{nameof(StopSessionsKeeper)}: {nameof(gameSession)} is null.", Logger.LogLevel.ERROR);
                        throw new ArgumentNullException();
                    }

                    Logger.WriteLog(() => $@"{nameof(StopSessionsKeeper)}: {nameof(gameSession)} =>{Environment.NewLine}{gameSession}.", Logger.LogLevel.DEBUG);

                    GameFieldHit[,] gf = gameSession._gameField;

                    GameField = new GameFieldHit[gf.Length];

                    for (int y = 0, my = gf.GetLength(1), mIndex = 0; y < my; y++)
                        for (int x = 0, mx = gf.GetLength(0); x < mx; x++)
                            GameField[mIndex++] = gf[x, y];

                    Logger.WriteLog(() => $@"{nameof(StopSessionsKeeper)}: Карта успешно загружена.", Logger.LogLevel.DEBUG);
                }

                public GameSession AsGameSession
                {
                    get
                    {
                        if (GameField is null)
                            throw new NullReferenceException();

                        if (GameField.Length != 9)
                            throw new ArgumentException();

                        GameFieldHit[,] gf = new GameFieldHit[3, 3];

                        for (int k = 0; k < 9; k++)
                            gf[k % 3, k / 3] = GameField[k];

                        return new GameSession(gf);
                    }
                }
            }

            [Serializable]
            public sealed class StopSessionsKeeperArrays
            {
                public HashSet<StopSessionsKeeper> SessionsStandoff { get; set; }

                public HashSet<StopSessionsKeeper> SessionsTotal { get; set; }

                public StopSessionsKeeperArrays()
                {
                    Logger.WriteLog(() => $@"{nameof(StopSessionsKeeperArrays)}: Конструктор по умолчанию.", Logger.LogLevel.DEBUG);

                    SessionsStandoff = new HashSet<StopSessionsKeeper>();
                    SessionsTotal = new HashSet<StopSessionsKeeper>();
                }

                public StopSessionsKeeperArrays(StopSessions stp)
                {
                    Logger.WriteLog(() => $@"{nameof(StopSessionsKeeperArrays)}: Вызван конструктор.", Logger.LogLevel.DEBUG);

                    if (stp is null)
                    {
                        Logger.WriteLog(() => $@"{nameof(StopSessionsKeeperArrays)}: {nameof(stp)} is null.", Logger.LogLevel.ERROR);
                        throw new ArgumentNullException(nameof(stp));
                    }

                    int countStOff = stp.SessionsStandoff.Count;
                    int countTotal = stp.SessionsTotal.Count;

                    SessionsStandoff = new HashSet<StopSessionsKeeper>(countStOff);
                    SessionsTotal = new HashSet<StopSessionsKeeper>(countTotal);

                    Logger.WriteLog(() => $@"{nameof(StopSessionsKeeperArrays)}: {nameof(countStOff)} = {countStOff}.", Logger.LogLevel.DEBUG);
                    Logger.WriteLog(() => $@"{nameof(StopSessionsKeeperArrays)}: {nameof(countTotal)} = {countTotal}.", Logger.LogLevel.DEBUG);

                    foreach (GameSession gs in stp.SessionsStandoff)
                    {
                        if (gs is null)
                        {
                            Logger.WriteLog(() => $@"{nameof(gs)} is null.", Logger.LogLevel.ERROR);
                            throw new ArgumentNullException(nameof(gs));
                        }

                        Logger.WriteLog(() => $@"{nameof(StopSessionsKeeperArrays)}: {nameof(StopSessions.SessionsStandoff)} =>{Environment.NewLine}{gs}.", Logger.LogLevel.DEBUG);
                        SessionsStandoff.Add(new StopSessionsKeeper(gs));
                    }

                    foreach (GameSession gs in stp.SessionsTotal)
                    {
                        if (gs is null)
                        {
                            Logger.WriteLog(() => $@"{nameof(gs)} is null.", Logger.LogLevel.ERROR);
                            throw new ArgumentNullException(nameof(gs));
                        }

                        Logger.WriteLog(() => $@"{nameof(StopSessionsKeeperArrays)}: {nameof(StopSessions.SessionsTotal)} =>{Environment.NewLine}{gs}.", Logger.LogLevel.DEBUG);
                        SessionsTotal.Add(new StopSessionsKeeper(gs));
                    }

                    Logger.WriteLog(() => $@"{nameof(StopSessionsKeeperArrays)}: Все карты успешно загружены.", Logger.LogLevel.DEBUG);
                }
            }

            public readonly HashSet<GameSession> SessionsStandoff;

            public readonly HashSet<GameSession> SessionsTotal;

            static string StopSessionsFilePath => $@"{Launcher.BaseFilePath}_{nameof(GameSession)}_{nameof(StopSessions)}.xml";

            [XmlIgnore]
            public static StopSessions StopSessionsFromFile
            {
                get
                {
                    try
                    {
                        XmlSerializer ser = new XmlSerializer(typeof(StopSessionsKeeperArrays));
                        using (FileStream fs = new FileStream(StopSessionsFilePath, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete))
                        {
                            Logger.WriteLog(() => $@"{nameof(StopSessionsFromFile)}(get): {nameof(StopSessionsFilePath)} = {StopSessionsFilePath}.", Logger.LogLevel.DEBUG);

                            using (XmlReader xr = XmlReader.Create(fs))
                                return new StopSessions((StopSessionsKeeperArrays)ser.Deserialize(xr));
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLog(() => $@"{nameof(StopSessionsFromFile)}(get): {ex.Message}", Logger.LogLevel.ERROR);
                        return new StopSessions();
                    }
                }

                set
                {
                    try
                    {
                        XmlSerializer ser = new XmlSerializer(typeof(StopSessionsKeeperArrays));
                        using (FileStream fs = new FileStream(StopSessionsFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete))
                        {
                            Logger.WriteLog(() => $@"{nameof(StopSessionsFromFile)}(set): {nameof(StopSessionsFilePath)} = {StopSessionsFilePath}.", Logger.LogLevel.DEBUG);

                            using (XmlWriter xw = new XmlTextWriter(fs, Encoding.UTF8))
                                ser.Serialize(xw, new StopSessionsKeeperArrays(value));
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLog(() => $@"{nameof(StopSessionsFromFile)}(set): {ex.Message}", Logger.LogLevel.ERROR);
                        throw;
                    }
                }
            }
        }

        static StopSessions _currentStopSessions;

        public enum GameFieldHit
        {
            [XmlEnum("Empty_HIT")]
            EMPTY,

            [XmlEnum("Bot_HIT")]
            BOT,

            [XmlEnum("User_HIT")]
            USER
        }

        public GameSession()
        {
            Logger.WriteLog(() => $@"{nameof(GameSession)}: Конструктор по умолчанию.", Logger.LogLevel.DEBUG, true);
            Reset();
        }

        public GameSession(GameFieldHit[,] map) => Reset(map);

        GameSession(GameFieldHit[,] map, InterModel model)
        {
            Logger.WriteLog(() => $@"{nameof(GameSession)} ({nameof(Int32)}[,] {nameof(map)}, {nameof(InterModel)} {nameof(model)}): ({nameof(map)} =>{Environment.NewLine}{ArrayVisualize(map)}{nameof(model)} = {model}).", Logger.LogLevel.DEBUG, true);

            if (map == null)
                throw new ArgumentNullException(nameof(map));

            GameFieldCopy(_gameField, map);
            CurrentModel = model;
        }

        public int HitX { get; private set; } = -1;

        public int HitY { get; private set; } = -1;

        InterModel CurrentModel { get; }

        public GameFieldHit this[int x, int y] => _gameField[x, y];

        public static void LoadStopSessionsFromFile() => _currentStopSessions = StopSessions.StopSessionsFromFile;

        public static void SaveStopSessionsToFile() => StopSessions.StopSessionsFromFile = _currentStopSessions;

        public void CancelHit()
        {
            HitX = -1;
            HitY = -1;

            CurrentGameStatus = GameStatus.UNKNOWN;
        }

        public void Reset()
        {
            Logger.WriteLog(() => $@"{nameof(Reset)}: Сброс игровой сессии (по умолчанию).", Logger.LogLevel.DEBUG, true);

            for (int y = 0, mY = _gameField.GetLength(1); y < mY; y++)
                for (int x = 0, mX = _gameField.GetLength(0); x < mX; x++)
                    _gameField[x, y] = GameFieldHit.EMPTY;

            _curX = 0;
            _curY = 0;

            HitX = -1;
            HitY = -1;

            CurrentGameStatus = GameStatus.UNKNOWN;
        }

        public void Reset(GameFieldHit[,] map)
        {
            Logger.WriteLog(() => $@"{nameof(Reset)}({nameof(Int32)}[,] {nameof(map)}) =>{Environment.NewLine}{ArrayVisualize(map)}.", Logger.LogLevel.DEBUG, true);

            if (map == null)
                throw new ArgumentNullException(nameof(map));

            GameFieldCopy(_gameField, map);

            _curX = 0;
            _curY = 0;

            HitX = -1;
            HitY = -1;

            CurrentGameStatus = GameStatus.UNKNOWN;

            if (CurrentFieldState == FieldState.ERROR)
                throw new InvalidOperationException();
        }

        public static void ResetGameData()
        {
            if (_lastBotHit is null)
                Logger.WriteLog(() => $@"{nameof(ResetGameData)}: Отсутствует последний ход, сделанный мной.", Logger.LogLevel.DEBUG);
            else
                Logger.WriteLog(() => $@"{nameof(ResetGameData)}: Сбрасываю последний ход, сделанный мной:{Environment.NewLine}{_lastBotHit}.", Logger.LogLevel.DEBUG);

            _lastBotHit = null;

            Logger.WriteLog(() => $@"{nameof(ResetGameData)}: Сбрасываю флаг {nameof(IsGameCompetitorsInverted)} = {IsGameCompetitorsInverted}.", Logger.LogLevel.DEBUG);

            IsGameCompetitorsInverted = false;
        }

        public static FieldState GetCurrentFieldStateEx(GameFieldHit[,] gameField, GameSession gs = null)
        {
            Logger.WriteLog(() => $@"{nameof(GetCurrentFieldStateEx)}: {ArrayVisualize(gameField)}.", Logger.LogLevel.DEBUG);

            if (gameField is null)
                throw new ArgumentNullException();

            FieldState fs = GetFs();
            Logger.WriteLog(() => $@"{nameof(GetCurrentFieldStateEx)}: возвращено {fs}.", Logger.LogLevel.DEBUG);
            return fs;

            FieldState GetFs()
            {
                if (GetFieldHero(gameField, GameFieldHit.EMPTY) == gameField.Length)
                    return FieldState.EMPTY;

                if (GetCurrentWinner(gameField).curWinner != Winner.NOBODY)
                    return FieldState.FULL;

                int uCount = GetFieldHero(gameField, GameFieldHit.USER);
                int bCount = GetFieldHero(gameField, GameFieldHit.BOT);

                if (uCount == bCount)
                {
                    if (gs is null)
                        return FieldState.WAITHIT;

                    switch (gs.CurrentGameStatus)
                    {
                        case GameStatus.WAITBOTHIT:
                            return FieldState.WAITBOTHIT;
                        case GameStatus.WAITUSERHIT:
                            return FieldState.WAITUSERHIT;
                        case GameStatus.UNKNOWN:
                        default:
                            return FieldState.WAITHIT;
                    }
                }

                int tfs = uCount - bCount;

                Logger.WriteLog(() => $@"{nameof(GetCurrentFieldStateEx)}: Разница между ударами пользователя и бота: {tfs}.", Logger.LogLevel.DEBUG);

                switch (tfs)
                {
                    case -1:
                        return FieldState.WAITUSERHIT;
                    case 1:
                        return FieldState.WAITBOTHIT;
                }

                return FieldState.ERROR;
            }
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;

            if (!(obj is GameSession gs))
                return false;

            int mx = gs._gameField.GetLength(0);
            int my = gs._gameField.GetLength(1);

            if (_gameField.GetLength(0) != mx || _gameField.GetLength(1) != my)
                return false;

            for (int y = 0; y < my; y++)
                for (int x = 0; x < mx; x++)
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
                        yield return Convert.ToInt32(_gameField[x, y]);
            }
        }

        public static bool operator ==(GameSession a, GameSession b)
        {
            if (ReferenceEquals(a, b))
                return true;

            return a?.Equals(b) == true;
        }

        public static bool operator !=(GameSession a, GameSession b) => !(a == b);

        public Winner CurrentWinner => GetCurrentWinner(_gameField).curWinner;

        public (Winner curWinner, Point[] winPts) CurrentWinnerEx => GetCurrentWinner(_gameField);

        public FieldState CurrentFieldState => GetCurrentFieldStateEx(_gameField, this);

        public static (Winner curWinner, Point[] winPts) GetCurrentWinner(GameFieldHit[,] gameField)
        {
            Logger.WriteLog(() => $@"{nameof(GetCurrentWinner)}: {ArrayVisualize(gameField)}.", Logger.LogLevel.DEBUG, true);

            Point[] wPts = new Point[3];
            Winner result = GetCw();

            Logger.WriteLog(() => $@"{nameof(GetCurrentWinner)}: Возвращено ({result}, {{({wPts[0].X}, {wPts[0].Y}), ({wPts[1].X}, {wPts[1].Y}), ({wPts[2].X}, {wPts[2].Y})}}).", Logger.LogLevel.DEBUG, true);

            return (result, wPts);

            Winner GetCw()
            {
                bool bh = IsLine(GameFieldHit.BOT);

                switch (IsLine(GameFieldHit.USER))
                {
                    case true when bh:
                        {
                            string s = $@"Нестандартная ситуация {nameof(Winner.STANDOFF)}.";
                            Logger.WriteLog(() => $@"{nameof(GetCurrentWinner)}: {s}", Logger.LogLevel.ERROR, true);
                            throw new InvalidOperationException(s);
                        }
                    case true:
                        return Winner.USER;
                    case false when bh:
                        return Winner.BOT;
                }

                for (int y = 0, mY = gameField.GetLength(1); y < mY; y++)
                    for (int x = 0, mX = gameField.GetLength(0); x < mX; x++)
                        if (gameField[x, y] == GameFieldHit.EMPTY)
                            return Winner.NOBODY;

                return Winner.STANDOFF;
            }

            bool IsLine(GameFieldHit sv)
            {
                if (gameField[0, 0] == sv && gameField[1, 0] == sv && // '
                    gameField[2, 0] == sv)
                {
                    wPts[0] = new Point(0, 0);
                    wPts[1] = new Point(1, 0);
                    wPts[2] = new Point(2, 0);
                    return true;
                }

                if (gameField[0, 1] == sv && gameField[1, 1] == sv && // -
                    gameField[2, 1] == sv)
                {
                    wPts[0] = new Point(0, 1);
                    wPts[1] = new Point(1, 1);
                    wPts[2] = new Point(2, 1);
                    return true;
                }

                if (gameField[0, 2] == sv && gameField[1, 2] == sv && // _
                    gameField[2, 2] == sv)
                {
                    wPts[0] = new Point(0, 2);
                    wPts[1] = new Point(1, 2);
                    wPts[2] = new Point(2, 2);
                    return true;
                }

                if (gameField[0, 0] == sv && gameField[0, 1] == sv && // |
                    gameField[0, 2] == sv)
                {
                    wPts[0] = new Point(0, 0);
                    wPts[1] = new Point(0, 1);
                    wPts[2] = new Point(0, 2);
                    return true;
                }

                if (gameField[1, 0] == sv && gameField[1, 1] == sv && //  |
                    gameField[1, 2] == sv)
                {
                    wPts[0] = new Point(1, 0);
                    wPts[1] = new Point(1, 1);
                    wPts[2] = new Point(1, 2);
                    return true;
                }

                if (gameField[2, 0] == sv && gameField[2, 1] == sv && //   |
                    gameField[2, 2] == sv)
                {
                    wPts[0] = new Point(2, 0);
                    wPts[1] = new Point(2, 1);
                    wPts[2] = new Point(2, 2);
                    return true;
                }

                if (gameField[0, 0] == sv && gameField[1, 1] == sv && // \
                    gameField[2, 2] == sv)
                {
                    wPts[0] = new Point(0, 0);
                    wPts[1] = new Point(1, 1);
                    wPts[2] = new Point(2, 2);
                    return true;
                }

                if (gameField[0, 2] != sv || gameField[1, 1] != sv || // /
                    gameField[2, 0] != sv)
                    return false;

                wPts[0] = new Point(0, 2);
                wPts[1] = new Point(1, 1);
                wPts[2] = new Point(2, 0);
                return true;
            }
        }

        static int GetFieldHero(GameFieldHit[,] gameField, GameFieldHit hero)
        {
            Logger.WriteLog(() => $@"{nameof(GetFieldHero)}: {nameof(gameField)} = {ArrayVisualize(gameField)}, {nameof(hero)} = {hero}.", Logger.LogLevel.DEBUG, true);

            int hCount = 0;

            for (int y = 0, mY = gameField.GetLength(1); y < mY; y++)
                for (int x = 0, mX = gameField.GetLength(0); x < mX; x++)
                    if (gameField[x, y] == hero)
                        ++hCount;

            Logger.WriteLog(() => $@"{nameof(GetFieldHero)}: возвращено {hCount}", Logger.LogLevel.DEBUG, true);

            return hCount;
        }

        public void FixGameStep()
        {
            Winner fcw = CurrentWinner;

            Logger.WriteLog(() => $@"{nameof(FixGameStep)}: {nameof(CurrentWinner)} = {fcw}.", Logger.LogLevel.DEBUG, true);

            switch (fcw)
            {
                case Winner.NOBODY:
                    break;
                case Winner.BOT:
                case Winner.USER:
                case Winner.STANDOFF:
                    return;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (_lastBotHit is null)
            {
                Logger.WriteLog(() => $@"{nameof(FixGameStep)}: {nameof(_lastBotHit)} = null.", Logger.LogLevel.DEBUG, true);
                return;
            }

            Point? wp = null;

            for (int y = 0, mY = _gameField.GetLength(1); y < mY; y++)
                for (int x = 0, mX = _gameField.GetLength(0); x < mX; x++)
                    if (_gameField[x, y] == GameFieldHit.EMPTY)
                    {
                        _gameField[x, y] = GameFieldHit.USER;
                        Winner cw = CurrentWinner;
                        _gameField[x, y] = GameFieldHit.EMPTY;

                        if (cw == Winner.STANDOFF && wp is null)
                        {
                            wp = new Point(x, y);
                            continue;
                        }

                        if (cw != Winner.USER)
                            continue;

                        if (!MakeUserHit(x, y))
                            throw new InvalidOperationException();

                        Logger.WriteLog(() => $@"{nameof(FixGameStep)}: {nameof(MakeUserHit)} = <<<Соперник выиграл>>> ({x}, {y}).", Logger.LogLevel.DEBUG, true);

                        return;
                    }

            if (wp is null)
            {
                Logger.WriteLog(() => $@"{nameof(FixGameStep)}: {nameof(MakeUserHit)} = <<<Соперник не выигрывал>>>.", Logger.LogLevel.DEBUG, true);
                return;
            }

            if (!MakeUserHit(wp.Value.X, wp.Value.Y))
                throw new InvalidOperationException();

            Logger.WriteLog(() => $@"{nameof(FixGameStep)}: {nameof(MakeUserHit)} = <<<Была ничья>>> ({wp.Value.X}, {wp.Value.Y}).", Logger.LogLevel.DEBUG, true);
        }

        public bool MakeUserHit(int x, int y) => MakeHit(x, y, GameFieldHit.USER);

        public bool MakeBotHit(int x, int y) => MakeHit(x, y, GameFieldHit.BOT);

        bool MakeHit(int x, int y, GameFieldHit hit)
        {
            Logger.WriteLog(() => $@"{nameof(MakeHit)}({x}, {y}, {hit}): Попытка нанесения удара в указанную точку...", Logger.LogLevel.DEBUG, true);

            GameFieldHit v = _gameField[x, y];

            if (v != GameFieldHit.EMPTY)
            {
                Logger.WriteLog(() => $@"{nameof(MakeHit)}({x}, {y}, {hit}): Попытка нанести удар по занятому месту ({v}).", Logger.LogLevel.ERROR);
                return false;
            }

            _gameField[x, y] = hit;

            if (hit == GameFieldHit.BOT)
            {
                HitX = x;
                HitY = y;
                CurrentGameStatus = GameStatus.WAITUSERHIT;
            }
            else
                CurrentGameStatus = GameStatus.WAITBOTHIT;

            HitFeedBack();

            Logger.WriteLog(() => $@"{nameof(MakeHit)}({x}, {y}, {hit}): Удар нанесён.", Logger.LogLevel.DEBUG);

            return true;
        }

        void HitFeedBack()
        {
            if (_lastBotHit is null)
            {
                Logger.WriteLog(() => $@"{nameof(HitFeedBack)}: Обратной связи (обучения) нет.", Logger.LogLevel.DEBUG);
                return;
            }

            Winner cw = CurrentWinner;

            switch (cw)
            {
                case Winner.NOBODY:
                    Logger.WriteLog(() => $@"{nameof(HitFeedBack)}: Игра продолжается...", Logger.LogLevel.DEBUG);
                    break;
                case Winner.BOT:
                    Logger.WriteLog(() => $@"{nameof(HitFeedBack)}: Игра завершена, я (бот) выиграл, ничего у себя не меняю.", Logger.LogLevel.DEBUG);
                    _lastBotHit = null;
                    break;
                case Winner.USER:
                case Winner.STANDOFF:

                    switch (cw)
                    {
                        case Winner.USER:
                            Logger.WriteLog(() => $@"{nameof(HitFeedBack)}: Игра завершена: я (бот) проиграл, надо сделать выводы...", Logger.LogLevel.DEBUG);
                            break;
                        case Winner.STANDOFF:
                            Logger.WriteLog(() => $@"{nameof(HitFeedBack)}: Игра завершена: ""ничья"", это нежелательно, надо сделать выводы...", Logger.LogLevel.DEBUG);
                            break;
                    }

                    InterModel model = _lastBotHit.CurrentModel;

                    Logger.WriteLog(() => $@"{nameof(HitFeedBack)}: Модель поведения, которая была выбрана: {model}.", Logger.LogLevel.DEBUG);

                    switch (model)
                    {
                        case InterModel.STANDOFF:
                            _currentStopSessions.SessionsTotal.Add(_lastBotHit);
                            if (cw == Winner.USER)
                            {
                                Logger.WriteLog(() => $@"{nameof(HitFeedBack)}: Игра завершена, я (бот) проиграл, это поведение не приводит к ""ничьей""...{Environment.NewLine}Добавляю в запрещённые (""ничья""):{Environment.NewLine}{_lastBotHit}.", Logger.LogLevel.DEBUG);
                                _currentStopSessions.SessionsStandoff.Add(_lastBotHit);
                                break;
                            }

                            Logger.WriteLog(() => $@"{nameof(HitFeedBack)}: Это поведение привело в ""ничью""... так и ожидалось!", Logger.LogLevel.DEBUG);
                            break;
                        case InterModel.TOTAL:
                            _currentStopSessions.SessionsTotal.Add(_lastBotHit);
                            if (cw == Winner.USER)
                            {
                                Logger.WriteLog(() => $@"{nameof(HitFeedBack)}: Я (бот) проиграл, поэтому подобное поведение неприемлимо ни с какой точки зрения!{Environment.NewLine}Добавляю в запрещённые:{Environment.NewLine}{_lastBotHit}.", Logger.LogLevel.DEBUG);
                                _currentStopSessions.SessionsStandoff.Add(_lastBotHit);
                                break;
                            }

                            Logger.WriteLog(() => $@"{nameof(HitFeedBack)}: Ничья! Это поведение явно не может считаться выигрышным...{Environment.NewLine}Вот эта карта:{Environment.NewLine}{_lastBotHit}.", Logger.LogLevel.DEBUG);
                            break;
                        case InterModel.NULL:
                        case InterModel.INVERT:
                            Logger.WriteLog(() =>
                                $@"{nameof(HitFeedBack)}: В этом случае какие-либо действия не предпрнимаюся, т.к. эти модели поведения невозможно как-либо изменить ({model}).", Logger.LogLevel.DEBUG);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    _lastBotHit = null;

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public (int x, int y) MakeHitDecision()
        {
            GameSession p = HowChangeFrame() ?? throw new InvalidDataException();

            int x = p.HitX, y = p.HitY;

            Logger.WriteLog(() => $@"{nameof(MakeHitDecision)}: Принято решение ударить в точку ({x}, {y}).", Logger.LogLevel.DEBUG);

            return (x, y);
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
                    (GameSession frame, bool end) = NextFrame(true, null, ref ctxLength, k);

                    InterModel k1 = k;
                    Logger.WriteLog(() =>
                    {
                        string sv = end ? @"Больше вариантов нет." : @"Варианты ещё есть...";
                        return $@"{nameof(HowChangeFrame)}: Модель [{k1}], сложность операции {ctxLength}, {sv}, решение:{Environment.NewLine}{frame}.";
                    }, Logger.LogLevel.DEBUG, true);

                    if (end)
                    {
                        if (!(frame is null))
                        {
                            Logger.WriteLog(() => $@"{nameof(HowChangeFrame)}: Решение принято (больше вариантов нет):{Environment.NewLine}{frame}.",
                                Logger.LogLevel.DEBUG, true);
                            result = frame;
                        }

                        Logger.WriteLog(() => $@"{nameof(HowChangeFrame)}: Просмотр решений завершён.",
                            Logger.LogLevel.DEBUG, true);

                        break;
                    }

                    if (frame is null)
                    {
                        Logger.WriteLog(() => $@"{nameof(HowChangeFrame)}: В заданном контексте решений нет, посмотрим далее...",
                            Logger.LogLevel.DEBUG, true);
                        continue;
                    }

                    if (ctxLength > resultLength)
                    {
                        int length = resultLength;
                        Logger.WriteLog(() => $@"{nameof(HowChangeFrame)}: Найденное решение сложнее предыдущего ({ctxLength} > {length}), посмотрим далее...",
                            Logger.LogLevel.DEBUG, true);
                        continue;
                    }

                    if (k == pk && ctxLength == resultLength)
                    {
                        int length = resultLength;
                        InterModel k2 = k;
                        InterModel pk1 = pk;
                        Logger.WriteLog(() => $@"{nameof(HowChangeFrame)}: Найденное решение аналогично предыдущему ({k2} == {pk1} && {ctxLength} = {length}), посмотрим далее...",
                            Logger.LogLevel.DEBUG, true);
                        continue;
                    }

                    pk = k;
                    resultLength = ctxLength;
                    result = frame;

                    InterModel pk2 = pk;
                    int length1 = resultLength;
                    GameSession result1 = result;
                    Logger.WriteLog(() => $@"{nameof(HowChangeFrame)}: Последнее выработанное решение: модель {pk2}, сложность {length1}, решение:{Environment.NewLine}{result1}.",
                        Logger.LogLevel.DEBUG, true);
                }

                _curY = _curX = 0;

                InterModel k3 = k;
                Logger.WriteLog(() => $@"{nameof(HowChangeFrame)}: Переход к следующей модели (текущая {k3}).", Logger.LogLevel.DEBUG, true);
            }

            if (result is null)
            {
                Logger.WriteLog(() => $@"{nameof(HowChangeFrame)}: {nameof(result)} is null.", Logger.LogLevel.ERROR, true);
                throw new InvalidOperationException($@"{nameof(result)} почему-то null...");
            }

            InterModel cm = result.CurrentModel;
            if (cm == InterModel.NULL || cm == InterModel.INVERT)
            {
                Logger.WriteLog(() => $@"{nameof(HowChangeFrame)}: Модель {cm} не позволяет себя изменять (обучать), возвращаю результат:{Environment.NewLine}{result}.",
                    Logger.LogLevel.DEBUG, true);
                return result;
            }

            GameFieldHit[,] gf = new GameFieldHit[3, 3];
            GameFieldCopy(gf, _gameField);
            gf[result.HitX, result.HitY] = GameFieldHit.BOT;

            GameSession gs = new GameSession(gf, cm)
            {
                HitX = result.HitX,
                HitY = result.HitY
            };

            Logger.WriteLog(() => $@"{nameof(HowChangeFrame)}: Сформировано решение: точка удара ({gs.HitX}, {gs.HitY}), изобразим его реализацию на практике. Модель {gs.CurrentModel}{Environment.NewLine}{gs}.",
                Logger.LogLevel.DEBUG, true);

            switch (cm)
            {
                case InterModel.STANDOFF:
                    if (!_currentStopSessions.SessionsStandoff.Contains(gs))
                    {
                        _lastBotHit = gs;
                        Logger.WriteLog(() => $@"{nameof(HowChangeFrame)} ({cm}): Эта карта отсутствует в списке проигрышных, поэтому сохраним текущую карту как последний ход.{Environment.NewLine}{_lastBotHit}.", Logger.LogLevel.DEBUG, true);
                        break;
                    }
                    Logger.WriteLog(() => $@"{nameof(HowChangeFrame)} ({cm}): В силу того, что эта карта уже сохранена как нерабочая, оставим последний удар как есть, чтобы не допустить эту же ситуацию в следующий раз.{Environment.NewLine}{_lastBotHit}.", Logger.LogLevel.DEBUG, true);
                    break;
                case InterModel.TOTAL:
                    if (!_currentStopSessions.SessionsTotal.Contains(gs))
                    {
                        _lastBotHit = gs;
                        Logger.WriteLog(() => $@"{nameof(HowChangeFrame)} ({cm}): Эта карта отсутствует в списке проигрышных, поэтому сохраним её как последний ход.{Environment.NewLine}{_lastBotHit}.", Logger.LogLevel.DEBUG, true);
                        break;
                    }
                    Logger.WriteLog(() => $@"{nameof(HowChangeFrame)} ({cm}): В силу того, что эта карта уже сохранена как нерабочая, оставим последний удар как есть, чтобы не допустить эту же ситуацию в следующий раз.{Environment.NewLine}{_lastBotHit}.", Logger.LogLevel.DEBUG, true);
                    break;
                case InterModel.NULL:
                case InterModel.INVERT:
                default:
                    throw new ArgumentOutOfRangeException();
            }

            Logger.WriteLog(() => $@"{nameof(HowChangeFrame)}: Итоговый результат:{Environment.NewLine}{result}.", Logger.LogLevel.DEBUG, true);

            return result;
        }

        (GameSession frame, bool end) NextFrame(bool isBot, GameFieldHit[,] map, ref int ctxLength, InterModel model)
        {
            int cl = ctxLength;
            GameFieldHit[,] mp = map;
            Logger.WriteLog(() => $@"{nameof(NextFrame)}: {nameof(isBot)} = {isBot}, {nameof(ctxLength)} = {cl}, {nameof(model)} = {model}, {nameof(map)} ->{Environment.NewLine}{ArrayVisualize(mp)}.", Logger.LogLevel.DEBUG, true);

            int ctl = ++ctxLength;

            if (map == null)
            {
                map = new GameFieldHit[3, 3];
                GameFieldCopy(map, _gameField);
                ctl = ctxLength = 0;
                int ctxln = ctxLength;
                Logger.WriteLog(() => $@"{nameof(NextFrame)}: Процедура поиска решения запущена: {nameof(ctxLength)} = {ctxln}, {nameof(ctl)} = {ctl}, {nameof(map)} ->{Environment.NewLine}{ArrayVisualize(map)}.", Logger.LogLevel.DEBUG, true);
            }

            for (int mMainY = map.GetLength(1); _curY < mMainY; _curY++)
            {
                for (int mMainX = map.GetLength(0); _curX < mMainX; _curX++)
                {
                    GameFieldHit v = map[_curX, _curY];

                    Logger.WriteLog(() => $@"{nameof(NextFrame)}: Место предполагаемого удара -> ({nameof(_curX)} = {_curX}, {nameof(_curY)} = {_curY}) => {GetNumberDescription(v, @"<<Empty>>")}.", Logger.LogLevel.DEBUG, true);

                    if (v != GameFieldHit.EMPTY)
                        continue;

                    GameFieldHit hit = isBot ? model == InterModel.INVERT ? GameFieldHit.USER : GameFieldHit.BOT :
                        model == InterModel.INVERT ? GameFieldHit.BOT : GameFieldHit.USER;

                    Logger.WriteLog(() => $@"{nameof(NextFrame)}: Предполагаемый удар {nameof(hit)} = {hit}.", Logger.LogLevel.DEBUG, true);

                    GameSession ctx = new GameSession(map, model)
                    {
                        _gameField =
                        {
                            [_curX, _curY] = hit
                        },
                        HitX = _curX,
                        HitY = _curY
                    };

                    switch (model)
                    {
                        case InterModel.NULL:
                            Logger.WriteLog(() => $@"{nameof(NextFrame)}: Модель {model} не предполагает каких-либо действий. Возвращаю результат (true) ->{Environment.NewLine}{ctx}.", Logger.LogLevel.DEBUG, true);
                            return (ctx, true);
                        case InterModel.STANDOFF:
                            if (_currentStopSessions.SessionsStandoff.Contains(ctx))
                            {
                                Logger.WriteLog(() => $@"{nameof(NextFrame)}: Модель {model} - это решение не будет работать, т.е. ""ничья"" не будет достигнута. Поиск решения будет продолжен. Текущее решение ->{Environment.NewLine}{ctx}.", Logger.LogLevel.DEBUG, true);
                                continue;
                            }

                            Logger.WriteLog(() => $@"{nameof(NextFrame)}: Модель {model} - это решение возможно проработать ->{Environment.NewLine}{ctx}.", Logger.LogLevel.DEBUG, true);
                            break;
                        case InterModel.INVERT:
                            Logger.WriteLog(() => $@"{nameof(NextFrame)}: Модель {model} - никаких действий не требуется. Продолжаю исследовать возможное решение ->{Environment.NewLine}{ctx}.", Logger.LogLevel.DEBUG, true);
                            break;
                        case InterModel.TOTAL:
                            if (_currentStopSessions.SessionsTotal.Contains(ctx))
                            {
                                Logger.WriteLog(() => $@"{nameof(NextFrame)}: Модель {model} - это решение не будет работать, т.е. победа не будет достигнута. Поиск решения будет продолжен. Текущее решение ->{Environment.NewLine}{ctx}.", Logger.LogLevel.DEBUG, true);
                                continue;
                            }

                            Logger.WriteLog(() => $@"{nameof(NextFrame)}: Модель {model} - это решение возможно проработать ->{Environment.NewLine}{ctx}.", Logger.LogLevel.DEBUG, true);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(model), model, null);
                    }

                    int ctxMinLength = int.MaxValue;
                    Winner wr = ctx.CurrentWinner;

                    switch (wr)
                    {
                        case Winner.BOT:
                            _curX++;

                            if (model != InterModel.TOTAL)
                            {
                                Logger.WriteLog(() => $@"{nameof(NextFrame)}: Победитель {wr}. Бот (я) подедил... Модель {model} должна быть какой-нибудь другой. Возвращаю отсутствие результата (null, false) ->{Environment.NewLine}{ctx}.", Logger.LogLevel.DEBUG, true);
                                return (null, false);
                            }

                            if (_currentStopSessions.SessionsTotal.Contains(ctx))
                            {
                                Logger.WriteLog(() => $@"{nameof(NextFrame)}: Победитель {wr}. Бот (я) подедил... Модель {model}. Этот сценарий не будет работать, т.к. он отмечен как проигрышный (возвращаю отсутствие результата (null, false) ->{Environment.NewLine}{ctx}.", Logger.LogLevel.DEBUG, true);
                                return (null, false);
                            }

                            (GameSession frame, bool end) r = (ctx, false);

                            Logger.WriteLog(() => $@"{nameof(NextFrame)}: Победитель {wr}. Бот (я) победил... Модель {model}. Есть вероятность, что этот сценарий сработает. Возвращаю результат ({r.end}) ->{Environment.NewLine}{r.frame}.", Logger.LogLevel.DEBUG, true);

                            return r;
                        case Winner.USER:
                            _curX++;

                            if (model == InterModel.INVERT)
                            {
                                (GameSession frame, bool end) r1 = (ctx, false);

                                Logger.WriteLog(() => $@"{nameof(NextFrame)}: Победитель {wr}. То есть я. Модель {model}. Возвращаю результат ({r1.end}) ->{Environment.NewLine}{r1.frame}.", Logger.LogLevel.DEBUG, true);

                                return r1;
                            }

                            Logger.WriteLog(() => $@"{nameof(NextFrame)}: Победитель {wr}. То есть пользователь. Модель {model}. Этот вариант заведомо проигрышный, поэтому возвращаю отсутствие результата (null, false) ->{Environment.NewLine}{ctx}.", Logger.LogLevel.DEBUG, true);

                            return (null, false);
                        case Winner.STANDOFF:
                            _curX++;

                            if (model != InterModel.STANDOFF)
                            {
                                Logger.WriteLog(() => $@"{nameof(NextFrame)}: Победитель {wr}. ""Ничья"". Этот результат приемлим только при рассмотрении модели {nameof(InterModel.STANDOFF)}. Модель {model}. Возвращаю отсутствие результата (null, false) ->{Environment.NewLine}{ctx}.", Logger.LogLevel.DEBUG, true);
                                return (null, false);
                            }

                            if (_currentStopSessions.SessionsTotal.Contains(ctx))
                            {
                                Logger.WriteLog(() => $@"{nameof(NextFrame)}: Победитель {wr}. Модель {model}. Этот сценарий не будет работать, т.к. он отмечен как проигрышный, поэтому возвращаю отсутствие результата (null, false) ->{Environment.NewLine}{ctx}.", Logger.LogLevel.DEBUG, true);
                                return (null, false);
                            }

                            (GameSession frame, bool end) r2 = (ctx, false);

                            Logger.WriteLog(() => $@"{nameof(NextFrame)}: Победитель {wr}. Модель {model}. Этот результат приемлим, т.к. рассматривается модель {nameof(InterModel.STANDOFF)}. Возвращаю результат ({r2.end}) ->{Environment.NewLine}{r2.frame}.", Logger.LogLevel.DEBUG, true);

                            return r2;
                        case Winner.NOBODY:
                            if (model == InterModel.INVERT)
                            {
                                Logger.WriteLog(() => $@"{nameof(NextFrame)}: Победитель {wr}. Модель {model}. Для этой модели не существует продолжения игры более, чем на один шаг. Этот ход ни к чему не приводит. Поиск решения будет продолжен. Текущее решение ->{Environment.NewLine}{ctx}.", Logger.LogLevel.DEBUG, true);
                                continue;
                            }

                            if (_currentStopSessions.SessionsTotal.Contains(ctx))
                            {
                                Logger.WriteLog(() => $@"{nameof(NextFrame)}: Победитель {wr}. Модель {model}. Этот ход не подходит, т.к. помечен как проигрышный. Поиск решения будет продолжен. Текущее решение ->{Environment.NewLine}{ctx}.", Logger.LogLevel.DEBUG, true);
                                continue;
                            }

                            while (true)
                            {
                                (GameSession frame, bool end) = ctx.NextFrame(!isBot, ctx._gameField, ref ctxLength, model);

                                if (end)
                                {
                                    if (frame is null)
                                    {
                                        int ic = ctxLength;
                                        Logger.WriteLog(
                                            () =>
                                                $@"{nameof(NextFrame)}: Победитель {wr}. Модель {model}. {nameof(ctxLength)} = {ic}, {nameof(ctl)} = {ctl}. Решений больше нет. Попробую поискать что-нибудь ещё. Текущее решение ->{Environment.NewLine}{ctx}.",
                                            Logger.LogLevel.DEBUG, true);
                                    }
                                    else
                                    {
                                        int ic = ctxLength;
                                        Logger.WriteLog(
                                            () =>
                                                $@"{nameof(NextFrame)}: Победитель {wr}. Модель {model}. {nameof(ctxLength)} = {ic}, {nameof(ctl)} = {ctl}. Решений больше нет, но одно из них всё же было найдено. Текущее решение ->{Environment.NewLine}{frame}.",
                                            Logger.LogLevel.DEBUG, true);
                                    }

                                    ctxLength = ctl;
                                    break;
                                }

                                if (ctxMinLength > ctxLength)
                                {
                                    if (frame is null)
                                    {
                                        int ic1 = ctxLength;
                                        int mLength1 = ctxMinLength;
                                        Logger.WriteLog(() => $@"{nameof(NextFrame)}: Победитель {wr}. Модель {model}. {nameof(ctxMinLength)} = {mLength1}, {nameof(ctxLength)} = {ic1}, {nameof(ctl)} = {ctl}. Решений лучше я не нешёл, попробую поискать ещё. Текущее решение ->{Environment.NewLine}{ctx}.", Logger.LogLevel.DEBUG, true);

                                        ctxLength = ctl;
                                        continue;
                                    }

                                    int ic = ctxLength;
                                    int mLength = ctxMinLength;
                                    Logger.WriteLog(() => $@"{nameof(NextFrame)}: Победитель {wr}. Модель {model}. {nameof(ctxMinLength)} = {mLength}, {nameof(ctxLength)} = {ic}, {nameof(ctl)} = {ctl}. Найденное решение лучше предыдущего, но, тем не менее, попробую поискать ещё. Текущее решение ->{Environment.NewLine}{frame}.", Logger.LogLevel.DEBUG, true);

                                    ctxMinLength = ctxLength;
                                }
                                else if (frame is null)
                                {
                                    int ic = ctxLength;
                                    int mLength = ctxMinLength;
                                    Logger.WriteLog(() => $@"{nameof(NextFrame)}: Победитель {wr}. Модель {model}. {nameof(ctxMinLength)} = {mLength}, {nameof(ctxLength)} = {ic}, {nameof(ctl)} = {ctl}. При заданных условиях решение отсутствует. Попробую поискать другое решение. Текущее решение ->{Environment.NewLine}{ctx}.", Logger.LogLevel.DEBUG, true);
                                }
                                else
                                {
                                    int ic = ctxLength;
                                    int mLength = ctxMinLength;
                                    Logger.WriteLog(() => $@"{nameof(NextFrame)}: Победитель {wr}. Модель {model}. {nameof(ctxMinLength)} = {mLength}, {nameof(ctxLength)} = {ic}, {nameof(ctl)} = {ctl}. Найденное решение хуже предыдущего. Попробую поискать более оптимальное решение. Найденное решение ->{Environment.NewLine}{frame}.", Logger.LogLevel.DEBUG, true);
                                }

                                ctxLength = ctl;
                            }

                            if (ctxMinLength == int.MaxValue)
                            {
                                int ic = ctxLength;
                                Logger.WriteLog(() => $@"{nameof(NextFrame)}: Победитель {wr}. Модель {model}. {nameof(ctxMinLength)} = {ctxMinLength}, {nameof(ctxLength)} = {ic}, {nameof(ctl)} = {ctl}. Ни одного решения найдено не было. Попробую поискать далее. Текущее решение ->{Environment.NewLine}{ctx}.", Logger.LogLevel.DEBUG, true);
                                continue;
                            }

                            int ic2 = ctxLength;
                            Logger.WriteLog(() => $@"{nameof(NextFrame)}: Победитель {wr}. Модель {model}. {nameof(ctxMinLength)} = {ctxMinLength}, {nameof(ctxLength)} = {ic2}, {nameof(ctl)} = {ctl}. В данном контексте решений больше нет. Возвращаю текущее найденное решение ->{Environment.NewLine}{ctx}.", Logger.LogLevel.DEBUG, true);

                            _curX++;
                            ctxLength = ctxMinLength;

                            return (ctx, false);
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                Logger.WriteLog(() => $@"{nameof(NextFrame)}: Строка (с нуля) #{_curY} пройдена.", Logger.LogLevel.DEBUG, true);

                _curX = 0;
            }

            int ctxLen = ctxLength;
            Logger.WriteLog(() => $@"{nameof(NextFrame)}: Поиск решений завершён: {nameof(ctxLength)} = {ctxLen}, {nameof(ctl)} = {ctl}. Возвращаю отсутствие результата (null, true) ->{Environment.NewLine}{ArrayVisualize(mp)}.", Logger.LogLevel.DEBUG, true);

            ctxLength = ctl;
            return (null, true);
        }

        public override string ToString() => ArrayVisualize(_gameField);

        static void GameFieldCopy(GameFieldHit[,] to, GameFieldHit[,] from, bool invert = false)
        {
            Logger.WriteLog(() => $@"{nameof(GameFieldCopy)}({nameof(invert)} = {invert}):{Environment.NewLine}{ArrayVisualize(from)}", Logger.LogLevel.DEBUG, true);

            if (to == null)
                throw new ArgumentNullException(nameof(to));

            if (from == null)
                throw new ArgumentNullException(nameof(from));

            int sX = to.GetLength(0), sY = to.GetLength(1);

            if (sX != 3)
                throw new ArgumentException(nameof(sX));

            if (sY != 3)
                throw new ArgumentException(nameof(sY));

            sX = from.GetLength(0);
            sY = from.GetLength(1);

            if (sX != 3)
                throw new ArgumentException(nameof(sX));

            if (sY != 3)
                throw new ArgumentException(nameof(sY));

            for (int y = 0; y < sY; y++)
                for (int x = 0; x < sX; x++)
                    if (!invert)
                        to[x, y] = from[x, y];
                    else
                    {
                        switch (from[x, y])
                        {
                            case GameFieldHit.BOT:
                                to[x, y] = GameFieldHit.USER;
                                break;
                            case GameFieldHit.USER:
                                to[x, y] = GameFieldHit.BOT;
                                break;
                            case GameFieldHit.EMPTY:
                                to[x, y] = GameFieldHit.EMPTY;
                                break;
                            default:
                                throw new Exception();
                        }
                    }
        }

        public static bool IsGameCompetitorsInverted { get; set; }

        public static string ArrayVisualize(GameFieldHit[,] array)
        {
            if (array == null)
                return @"<null>";

            if (array.Length == 0)
                return @"<empty>";

            StringBuilder sb = new StringBuilder();

            int mx = array.GetLength(0);
            int my = array.GetLength(1);

            sb.AppendLine($@"[{mx}, {my}]");

            for (int y = 0; y < my; y++)
            {
                for (int x = 0; x < mx; x++)
                {
                    if (x != 0)
                        sb.Append(' ');

                    sb.Append(GetNumberDescription(array[x, y], @" "));
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }

        static string GetNumberDescription(GameFieldHit v, string emptyText)
        {
            switch (v)
            {
                case GameFieldHit.BOT:
                    return @"O";
                case GameFieldHit.EMPTY:
                    return emptyText;
                case GameFieldHit.USER:
                    return @"X";
            }

            return $@"Неизвестное значение: {v}";
        }
    }
}