using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
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

        public enum InterModel
        {
            NULL,
            STANDOFF,
            INVERT,
            TOTAL
        }

        public enum FieldState
        {
            EMPTY,
            WAITHIT,
            FULL
        }

        readonly int[,] _gameField;

        int _curX, _curY;

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
                public int[] GameField { get; set; }

                public StopSessionsKeeper()
                {
                    Logger.WriteLog(() => $@"{nameof(StopSessionsKeeper)}: Сериализация...", Logger.LogLevel.DEBUG);
                }

                public StopSessionsKeeper(GameSession gameSession)
                {
                    Logger.WriteLog(() => $@"{nameof(StopSessionsKeeper)}: Вызван конструктор.", Logger.LogLevel.DEBUG);

                    if (gameSession is null)
                    {
                        Logger.WriteLog(() => $@"{nameof(StopSessionsKeeper)}: {nameof(gameSession)} is null.", Logger.LogLevel.ERROR);
                        throw new ArgumentNullException();
                    }

                    Logger.WriteLog(() => $@"{nameof(StopSessionsKeeper)}: {nameof(gameSession)} =>{Environment.NewLine}{gameSession}.", Logger.LogLevel.DEBUG);

                    int[,] gf = gameSession._gameField;

                    GameField = new int[gf.Length];

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

                        int[,] gf = new int[3, 3];

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

            static string SettingsFilePath => $@"{Launcher.BaseFilePath}_{nameof(GameSession)}_{nameof(StopSessions)}.xml";

            [XmlIgnore]
            public static StopSessions StopSessionsFromFile
            {
                get
                {
                    try
                    {
                        XmlSerializer ser = new XmlSerializer(typeof(StopSessionsKeeperArrays));
                        using (FileStream fs = new FileStream(SettingsFilePath, FileMode.Open))
                        {
                            Logger.WriteLog(() => $@"{nameof(StopSessionsFromFile)}(get): {nameof(SettingsFilePath)} = {SettingsFilePath}.", Logger.LogLevel.DEBUG);
                            return new StopSessions((StopSessionsKeeperArrays)ser.Deserialize(fs));
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
                        using (FileStream fs = new FileStream(SettingsFilePath, FileMode.Create))
                        {
                            Logger.WriteLog(() => $@"{nameof(StopSessionsFromFile)}(set): {nameof(SettingsFilePath)} = {SettingsFilePath}.", Logger.LogLevel.DEBUG);
                            ser.Serialize(fs, new StopSessionsKeeperArrays(value));
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

        public const int EmptyHit = 0;

        public const int UserHit = int.MaxValue;

        public const int BotHit = int.MinValue;

        public GameSession()
        {
            Logger.WriteLog(() => $@"{nameof(GameSession)}: Конструктор по умолчанию.", Logger.LogLevel.DEBUG, true);

            _gameField = new int[3, 3];

            for (int y = 0, mY = _gameField.GetLength(1); y < mY; y++)
                for (int x = 0, mX = _gameField.GetLength(0); x < mX; x++)
                    _gameField[x, y] = EmptyHit;
        }

        public GameSession(int[,] map)
        {
            Logger.WriteLog(() => $@"{nameof(GameSession)} ({nameof(Int32)}[,]) {nameof(map)} =>{Environment.NewLine}{ArrayVisualize(map)}.", Logger.LogLevel.DEBUG, true);

            if (map == null)
                throw new ArgumentNullException(nameof(map));

            _gameField = GameFieldCopy(map);
        }

        GameSession(int[,] map, InterModel model)
        {
            Logger.WriteLog(() => $@"{nameof(GameSession)} ({nameof(Int32)}[,] {nameof(map)}, {nameof(InterModel)} {nameof(model)}): ({nameof(map)} =>{Environment.NewLine}{ArrayVisualize(map)}{nameof(model)} = {model}).", Logger.LogLevel.DEBUG, true);

            if (map == null)
                throw new ArgumentNullException(nameof(map));

            _gameField = GameFieldCopy(map);
            CurrentModel = model;
        }

        public int HitX { get; private set; } = -1;

        public int HitY { get; private set; } = -1;

        public InterModel CurrentModel { get; }

        public int this[int x, int y] => _gameField[x, y];

        public static void LoadStopSessionsFromFile() => _currentStopSessions = StopSessions.StopSessionsFromFile;

        public static void SaveStopSessionsToFile() => StopSessions.StopSessionsFromFile = _currentStopSessions;

        public static FieldState GetCurrentState(int[,] gameField)
        {
            Logger.WriteLog(() => $@"{nameof(GetCurrentState)}: {ArrayVisualize(gameField)}.", Logger.LogLevel.DEBUG, true);

            if (gameField is null)
                throw new ArgumentNullException();

            FieldState fs = GetFs();
            Logger.WriteLog(() => $@"{nameof(GetCurrentState)}: возвращено {fs}.", Logger.LogLevel.DEBUG, true);
            return fs;

            FieldState GetFs()
            {
                if (GetFieldHero(gameField, EmptyHit) == gameField.Length)
                    return FieldState.EMPTY;

                if (GetCurrentWinner(gameField) != Winner.NOBODY)
                    return FieldState.FULL;

                int uCount = GetFieldHero(gameField, UserHit);
                int bCount = GetFieldHero(gameField, BotHit);

                if (uCount == bCount)
                    return FieldState.WAITHIT;

                return Math.Abs(uCount - bCount) != 1 ? FieldState.FULL : FieldState.WAITHIT;
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

        public Winner CurrentWinner => GetCurrentWinner(_gameField);

        public static Winner GetCurrentWinner(int[,] gameField)
        {
            Logger.WriteLog(() => $@"{nameof(GetCurrentWinner)}: {ArrayVisualize(gameField)}.", Logger.LogLevel.DEBUG, true);

            Winner result = GetCw();
            Logger.WriteLog(() => $@"{nameof(GetCurrentWinner)}: возвращено {result}.", Logger.LogLevel.DEBUG, true);
            return result;

            Winner GetCw()
            {
                bool bh = IsLine(BotHit);

                switch (IsLine(UserHit))
                {
                    case true when bh:
                        return Winner.STANDOFF;
                    case true:
                        return Winner.USER;
                    case false when bh:
                        return Winner.BOT;
                }

                for (int y = 0, mY = gameField.GetLength(1); y < mY; y++)
                    for (int x = 0, mX = gameField.GetLength(0); x < mX; x++)
                        if (gameField[x, y] == EmptyHit)
                            return Winner.NOBODY;

                return Winner.STANDOFF;
            }

            bool IsLine(int sv)
            {
                if (gameField[0, 0] == sv && gameField[1, 0] == sv &&
                    gameField[2, 0] == sv)
                    return true;

                if (gameField[0, 1] == sv && gameField[1, 1] == sv &&
                    gameField[2, 1] == sv)
                    return true;

                if (gameField[0, 2] == sv && gameField[1, 2] == sv &&
                    gameField[2, 2] == sv)
                    return true;

                if (gameField[0, 0] == sv && gameField[0, 1] == sv &&
                    gameField[0, 2] == sv)
                    return true;

                if (gameField[1, 0] == sv && gameField[1, 1] == sv &&
                    gameField[1, 2] == sv)
                    return true;

                if (gameField[2, 0] == sv && gameField[2, 1] == sv &&
                    gameField[2, 2] == sv)
                    return true;

                if (gameField[0, 0] == sv && gameField[1, 1] == sv &&
                    gameField[2, 2] == sv)
                    return true;

                return gameField[0, 2] == sv && gameField[1, 1] == sv &&
                       gameField[2, 0] == sv;
            }
        }

        static int GetFieldHero(int[,] gameField, int hero)
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

            Logger.WriteLog(() => $@"{nameof(FixGameStep)}: {nameof(CurrentWinner)} = {fcw}", Logger.LogLevel.DEBUG, true);

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
                Logger.WriteLog(() => $@"{nameof(FixGameStep)}: {nameof(_lastBotHit)} = null", Logger.LogLevel.DEBUG, true);
                return;
            }

            Point? wp = null;

            for (int y = 0, mY = _gameField.GetLength(1); y < mY; y++)
                for (int x = 0, mX = _gameField.GetLength(0); x < mX; x++)
                    if (_gameField[x, y] == EmptyHit)
                    {
                        _gameField[x, y] = UserHit;
                        Winner cw = CurrentWinner;
                        _gameField[x, y] = EmptyHit;

                        if (cw == Winner.STANDOFF && wp is null)
                        {
                            wp = new Point(x, y);
                            continue;
                        }

                        if (cw != Winner.USER)
                            continue;

                        if (!MakeUserHit(x, y))
                            throw new InvalidOperationException();

                        Logger.WriteLog(() => $@"{nameof(FixGameStep)}: {nameof(MakeUserHit)} = <<<Соперник выиграл>>> ({x}, {y})", Logger.LogLevel.DEBUG, true);

                        return;
                    }

            if (wp is null)
            {
                Logger.WriteLog(() => $@"{nameof(FixGameStep)}: {nameof(MakeUserHit)} = <<<Соперник не выигрывал>>>", Logger.LogLevel.DEBUG, true);
                return;
            }

            if (!MakeUserHit(wp.Value.X, wp.Value.Y))
                throw new InvalidOperationException();

            Logger.WriteLog(() => $@"{nameof(FixGameStep)}: {nameof(MakeUserHit)} = <<<Была ничья>>> ({wp.Value.X}, {wp.Value.Y})", Logger.LogLevel.DEBUG, true);
        }

        public bool MakeUserHit(int x, int y) => MakeHit(x, y, UserHit);

        public bool MakeBotHit(int x, int y) => MakeHit(x, y, BotHit);

        bool MakeHit(int x, int y, int hit)
        {
            Logger.WriteLog(() => $@"{nameof(MakeHit)}({x}, {y}, {hit}): Попытка нанесения удара в указанную точку...", Logger.LogLevel.DEBUG, true);

            int v = _gameField[x, y];

            if (v != EmptyHit)
            {
                Logger.WriteLog(() => $@"{nameof(MakeHit)}({x}, {y}, {hit}): Попытка нанести удар по занятому месту ({v}).", Logger.LogLevel.ERROR);
                return false;
            }

            _gameField[x, y] = hit;

            if (hit == BotHit)
            {
                HitX = x;
                HitY = y;
            }

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
                            if (cw != Winner.STANDOFF)
                            {
                                _currentStopSessions.SessionsStandoff.Add(_lastBotHit);
                                Logger.WriteLog(() => $@"{nameof(HitFeedBack)}: Игра завершена, я (бот) проиграл, это поведение не приводит к ""ничьей""...{Environment.NewLine}Добавляю в запрещённые (""ничья""):{Environment.NewLine}{_lastBotHit}.", Logger.LogLevel.DEBUG);
                                break;
                            }

                            Logger.WriteLog(() => $@"{nameof(HitFeedBack)}: Это поведение привело в ""ничью""... так и ожидалось!", Logger.LogLevel.DEBUG);
                            break;
                        case InterModel.TOTAL:
                            _currentStopSessions.SessionsTotal.Add(_lastBotHit);
                            if (cw == Winner.USER)
                            {
                                _currentStopSessions.SessionsStandoff.Add(_lastBotHit);
                                Logger.WriteLog(() => $@"{nameof(HitFeedBack)}: Я (бот) проиграл, поэтому подобное поведение неприемлимо ни с какой точки зрения!{Environment.NewLine}Добавляю в запрещённые:{Environment.NewLine}{_lastBotHit}.", Logger.LogLevel.DEBUG);
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

            int[,] gf = GameFieldCopy(_gameField);
            gf[result.HitX, result.HitY] = BotHit;

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
                default:
                    throw new ArgumentOutOfRangeException();
            }

            Logger.WriteLog(() => $@"{nameof(HowChangeFrame)}: Итоговый результат:{Environment.NewLine}{result}.", Logger.LogLevel.DEBUG, true);

            return result;
        }

        (GameSession frame, bool end) NextFrame(bool isBot, int[,] map, ref int ctxLength, InterModel model)
        {
            int cl = ctxLength;
            int[,] mp = map;
            Logger.WriteLog(() => $@"{nameof(NextFrame)}: {nameof(isBot)} = {isBot}, {nameof(ctxLength)} = {cl}, {nameof(model)} = {model}, {nameof(map)} ->{Environment.NewLine}{ArrayVisualize(mp)}.", Logger.LogLevel.DEBUG, true);

            int ctl = ++ctxLength;

            if (map == null)
            {
                map = GameFieldCopy(_gameField);
                ctl = ctxLength = 0;
                int ctxln = ctxLength;
                Logger.WriteLog(() => $@"{nameof(NextFrame)}: Процедура поиска решения запущена: {nameof(ctxLength)} = {ctxln}, {nameof(ctl)} = {ctl}, {nameof(map)} ->{Environment.NewLine}{ArrayVisualize(map)}.", Logger.LogLevel.DEBUG, true);
            }

            for (int mMainY = map.GetLength(1); _curY < mMainY; _curY++)
            {
                for (int mMainX = map.GetLength(0); _curX < mMainX; _curX++)
                {
                    int v = map[_curX, _curY];

                    Logger.WriteLog(() => $@"{nameof(NextFrame)}: Место предполагаемого удара -> ({nameof(_curX)} = {_curX}, {nameof(_curY)} = {_curY}) => {GetNumberDescription(v, @"<<Empty>>")}.", Logger.LogLevel.DEBUG, true);

                    if (v != EmptyHit)
                        continue;

                    int hit = isBot ? model == InterModel.INVERT ? UserHit : BotHit :
                        model == InterModel.INVERT ? BotHit : UserHit;

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

        static int[,] GameFieldCopy(int[,] map)
        {
            Logger.WriteLog(() => $@"{nameof(GameFieldCopy)}:{Environment.NewLine}{ArrayVisualize(map)}", Logger.LogLevel.DEBUG, true);

            if (map == null)
                throw new ArgumentNullException(nameof(map));

            int sX = map.GetLength(0), sY = map.GetLength(1);

            if (sX != 3)
                throw new ArgumentException();

            if (sY != 3)
                throw new ArgumentException();

            int[,] result = new int[sX, sY];

            for (int y = 0; y < sY; y++)
                for (int x = 0; x < sX; x++)
                    result[x, y] = map[x, y];

            return result;
        }

        public static bool IsGameCompetitorsInverted { get; set; }

        public static string ArrayVisualize(int[,] array)
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

        static string GetNumberDescription(int v, string emptyText)
        {
            switch (v)
            {
                case BotHit:
                    return @"O";
                case EmptyHit:
                    return emptyText;
                case UserHit:
                    return @"X";
            }

            return $@"Неизвестное значение: {v}";
        }
    }
}