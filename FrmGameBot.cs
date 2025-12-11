using DynamicParser;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Serialization;
using BitImages = DynamicSample.FrmGameBot.SettingsProfilesArray.HitSettings.BitImages;
using Device = SharpDX.Direct3D11.Device;
using Format = SharpDX.DXGI.Format;
using HitCounter = DynamicSample.FrmGameBot.SettingsProfilesArray.HitSettings.HitCounter;
using MapFlags = SharpDX.Direct3D11.MapFlags;
using Processor = DynamicParser.Processor;
using Resource = SharpDX.DXGI.Resource;
using ThreadState = System.Threading.ThreadState;

namespace DynamicSample
{
    public partial class FrmGameBot : Form
    {
        [Serializable]
        public sealed class SettingsProfilesArray
        {
            [Serializable]
            public sealed class HitSettings
            {
                [Serializable]
                public sealed class HitCounter
                {
                    public int Counter { get; set; }

                    public void Inc()
                    {
                        ++Counter;

                        if (Counter < 0)
                            Counter = 0;

                        int c = Counter % 9;

                        if (Counter > 8)
                            Counter = c;
                    }
                }

                [Serializable]
                public sealed class BitImages
                {
                    public BitImages()
                    {
                        // Empty (for serialization)
                    }

                    public BitImages(BitImages bi)
                    {
                        if (bi is null)
                            return;

                        FieldSize = bi.FieldSize;
                        Name = bi.Name;
                        Coords = bi.Coords;
                        Data = new List<int>(bi.Data);
                    }

                    public Size FieldSize { get; set; }

                    public char Name { get; set; }

                    public Point Coords { get; set; }

                    public List<int> Data { get; set; } = new List<int>();

                    public int HitX => Coords.X + FieldSize.Width / 2;

                    public int HitY => Coords.Y + FieldSize.Height / 2;

                    public Processor AsProcessor => new Processor(AsBitmap, Name.ToString());

                    public Rectangle AsRectangle => new Rectangle(Coords, FieldSize);

                    public Bitmap AsBitmap
                    {
                        get
                        {
                            int mx = FieldSize.Width, my = FieldSize.Height;
                            List<int> data = Data;

                            Bitmap bmp = new Bitmap(mx, my);

                            for (int y = 0; y < my; y++)
                                for (int x = 0; x < mx; x++)
                                    bmp.SetPixel(x, y, Color.FromArgb(data[mx * y + x]));

                            return bmp;
                        }
                    }

                    public override string ToString()
                    {
                        StringBuilder sb = new StringBuilder();

                        sb.AppendLine($@"{nameof(FieldSize)} = ({FieldSize.Width}) x ({FieldSize.Height})");
                        sb.AppendLine($@"{nameof(Name)} = {Name}");
                        sb.AppendLine($@"{nameof(Coords)} = ({Coords.X}, {Coords.Y})");
                        sb.AppendLine($@"{nameof(Data)}.Length = {Data.Count}");

                        return sb.ToString();
                    }
                }

                public List<BitImages> Spaces { get; set; } = new List<BitImages>();

                public List<BitImages> EventClicks { get; set; } = new List<BitImages>();

                public string ProfileName { get; set; } = string.Empty;

                public HitCounter StartHitCounter { get; set; } = new HitCounter();
            }

            public List<HitSettings> Profiles { get; set; } = new List<HitSettings>();

            [XmlIgnore]
            static string SettingsFilePath => $@"{Application.StartupPath}\{Application.ProductName}_{nameof(FrmGameBot)}Settings.xml";

            [XmlIgnore]
            public static SettingsProfilesArray CurrentSettings
            {
                get
                {
                    try
                    {
                        XmlSerializer ser = new XmlSerializer(typeof(SettingsProfilesArray));
                        using (FileStream fs = new FileStream(SettingsFilePath, FileMode.Open))
                            return (SettingsProfilesArray)ser.Deserialize(fs);
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLog(() => $@"{nameof(SettingsProfilesArray)}(get): {ex.Message}", Logger.LogLevel.ERROR);
                        return new SettingsProfilesArray();
                    }
                }

                set
                {
                    try
                    {
                        XmlSerializer ser = new XmlSerializer(typeof(SettingsProfilesArray));
                        using (FileStream fs = new FileStream(SettingsFilePath, FileMode.Create))
                            ser.Serialize(fs, value);
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLog(() => $@"{nameof(SettingsProfilesArray)}(set): {ex.Message}", Logger.LogLevel.ERROR);
                        throw;
                    }
                }
            }
        }

        /// <summary>
        ///     Обеспечивает потокобезопасность членов этого класса.
        /// </summary>
        readonly object _commonLocker = new object();

        string _btnStartCaption;

        /// <summary>
        ///     Поток, отвечающий за выполнение текущего поискового запроса.
        /// </summary>
        /// <remarks>
        ///     Хранит значение свойства <see cref="GameBotThread" />.
        /// </remarks>
        /// <seealso cref="GameBotThread" />
        Thread _recognizerThread;

        Thread _escapeThread;

        bool _escapeThreadStopFlag;

        bool _needSaveProfile;

        /// <summary>
        ///     Поток, который останавливает процесс выполнения поискового запроса.
        /// </summary>
        /// <remarks>
        ///     Хранит значение свойства <see cref="StopperThread" />.
        /// </remarks>
        /// <seealso cref="StopperThread" />
        Thread _stoppingThread;

        /// <summary>
        ///     Поток, выполняющий текущий поисковый запрос.
        /// </summary>
        /// <remarks>
        ///     Если никакой запрос не выполняется, значение свойства будет равно <see langword="null" />.
        ///     Свойство потокобезопасно как на чтение, так и на запись.
        ///     Потокобезопасность обеспечивает поле <see cref="_commonLocker" />.
        ///     Значение свойства содержит поле <see cref="_recognizerThread" />.
        /// </remarks>
        /// <seealso cref="_commonLocker" />
        /// <seealso cref="_recognizerThread" />
        Thread GameBotThread
        {
            get
            {
                lock (_commonLocker)
                {
                    return _recognizerThread;
                }
            }

            set
            {
                lock (_commonLocker)
                {
                    _recognizerThread = value;
                }
            }
        }

        bool ProgramStopFlag
        {
            get
            {
                lock (_commonLocker)
                {
                    return _escapeThreadStopFlag;
                }
            }

            set
            {
                lock (_commonLocker)
                {
                    _escapeThreadStopFlag = value;
                }
            }
        }

        public FrmGameBot()
        {
            InitializeComponent();
        }

        readonly SettingsProfilesArray _settingProfiles = SettingsProfilesArray.CurrentSettings;

        SettingsProfilesArray.HitSettings _selectedProfileSettings = new SettingsProfilesArray.HitSettings();

        static Bitmap TakeScreenshot()
        {
            try
            {
                Factory1 factory = new Factory1();
                Adapter1 adapter = factory.GetAdapter1(0);
                Device device = new Device(adapter);
                Output output = adapter.GetOutput(0);
                Output1 output1 = output.QueryInterface<Output1>();

                int width = output.Description.DesktopBounds.Right;
                int height = output.Description.DesktopBounds.Bottom;

                Texture2DDescription textureDesc = new Texture2DDescription
                {
                    CpuAccessFlags = CpuAccessFlags.Read,
                    BindFlags = BindFlags.None,
                    Format = Format.B8G8R8A8_UNorm,
                    Width = width,
                    Height = height,
                    OptionFlags = ResourceOptionFlags.None,
                    MipLevels = 1,
                    ArraySize = 1,
                    SampleDescription = { Count = 1, Quality = 0 },
                    Usage = ResourceUsage.Staging
                };

                using (Texture2D screenTexture = new Texture2D(device, textureDesc))
                {
                    using (OutputDuplication duplicatedOutput = output1.DuplicateOutput(device))
                    {
                        Thread.Sleep(100); // Захватчику экрана надо время проинициализироваться.
                        Bitmap bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                        Resource screenResource = null;
                        try
                        {
                            if (duplicatedOutput.TryAcquireNextFrame(10, out OutputDuplicateFrameInformation _,
                                    out screenResource) != Result.Ok)
                            {
                                Logger.WriteLog(() => $@"{nameof(TakeScreenshot)}1: Возвращаю изображение ({bmp.Width}, {bmp.Height}).", Logger.LogLevel.DEBUG, true);
                                return bmp;
                            }

                            using (Texture2D screenTexture2D = screenResource.QueryInterface<Texture2D>())
                                device.ImmediateContext.CopyResource(screenTexture2D, screenTexture);

                            DataBox mapSource =
                                device.ImmediateContext.MapSubresource(screenTexture, 0, MapMode.Read, MapFlags.None);
                            BitmapData bmpData = bmp.LockBits(new Rectangle(Point.Empty, bmp.Size),
                                ImageLockMode.WriteOnly, bmp.PixelFormat);
                            IntPtr sourcePtr = mapSource.DataPointer;
                            IntPtr destPtr = bmpData.Scan0;
                            Utilities.CopyMemory(destPtr, sourcePtr, mapSource.RowPitch * height);
                            bmp.UnlockBits(bmpData);
                            device.ImmediateContext.UnmapSubresource(screenTexture, 0);
                            duplicatedOutput.ReleaseFrame();
                        }
                        finally
                        {
                            screenResource?.Dispose();
                        }

                        Logger.WriteLog(() => $@"{nameof(TakeScreenshot)}2: Возвращаю изображение ({bmp.Width}, {bmp.Height}).", Logger.LogLevel.DEBUG, true);

                        return bmp;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(() => $@"{nameof(TakeScreenshot)}3: {ex.Message}", Logger.LogLevel.ERROR);
                return null;
            }
        }

        void EscapeThreadFunc()
        {
            try
            {
                while (!ProgramStopFlag)
                {
                    while (!ProgramStopFlag && NativeMethods.GetAsyncKeyState(Keys.Escape) == 0)
                        Thread.Sleep(50);

                    while (!ProgramStopFlag && NativeMethods.GetAsyncKeyState(Keys.Escape) != 0)
                        Thread.Sleep(10);

                    if (ProgramStopFlag || !(StopperThread is null))
                        continue;

                    Thread t = new Thread(() =>
                    {
                        try
                        {
                            if (StopGameBotThread())
                            {
                                Logger.WriteLog(() => $@"{nameof(EscapeThreadFunc)}: Игра (бот) остановлена клавишей ESC.", Logger.LogLevel.ERROR);
                                return;
                            }

                            SafeExecute(() =>
                            {
                                try
                                {
                                    Application.Exit();
                                }
                                catch (Exception ex)
                                {
                                    Logger.WriteLog(() => $@"{nameof(EscapeThreadFunc)}: {nameof(Application.Exit)}: {ex.Message}", Logger.LogLevel.ERROR);
                                }
                            }, true);
                            Logger.WriteLog(() => $@"{nameof(EscapeThreadFunc)}: Exited by ESC ({Keys.Escape}).", Logger.LogLevel.DEBUG);
                        }
                        catch (Exception ex)
                        {
                            Logger.WriteLog(() => $@"{nameof(EscapeThreadFunc)}: {ex.Message}", Logger.LogLevel.ERROR);
                        }
                        finally
                        {
                            StopperThread = null;
                        }
                    })
                    {
                        IsBackground = true,
                        Name = @"Stopper"
                    };

                    StopperThread = t;

                    t.Start();
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(() => $@"{nameof(EscapeThreadFunc)}: {ex.Message}", Logger.LogLevel.ERROR);
            }
        }

        void AddProfile()
        {
            for (int k = 0; k < _settingProfiles.Profiles.Count; k++)
            {
                if (_selectedProfileSettings.ProfileName != _settingProfiles.Profiles[k].ProfileName)
                    continue;

                _settingProfiles.Profiles.RemoveAt(k);
                _settingProfiles.Profiles.Insert(k, _selectedProfileSettings);
                _needSaveProfile = false;
                return;
            }

            _settingProfiles.Profiles.Insert(0, _selectedProfileSettings);
            cbxProfiles.Items.Insert(1, _selectedProfileSettings.ProfileName);
            _needSaveProfile = false;
        }

        Bitmap CopyGameFieldFromScreen()
        {
            Rectangle rect = new Rectangle();
            SafeExecute(() => rect = GameFieldRect, true);
            Bitmap b = new Bitmap(rect.Width, rect.Height);

            Bitmap origin = TakeScreenshot();

            if (origin is null)
                return b;

            for (int x = rect.X, xto = 0; x < rect.Right; x++, xto++)
                for (int y = rect.Y, yto = 0; y < rect.Bottom; y++, yto++)
                    b.SetPixel(xto, yto, origin.GetPixel(x, y));

            return b;
        }

        void FrmGameSettings_Shown(object sender, EventArgs e)
        {
            TransparencyKey = pbScreenField.BackColor = Color.Red;

            _escapeThread = new Thread(EscapeThreadFunc)
            {
                IsBackground = true,
                Name = @"EscapeThread"
            };
            _escapeThread.Start();

            _btnStartCaption = btnGameStart.Text;

            foreach (SettingsProfilesArray.HitSettings pf in _settingProfiles.Profiles)
                cbxProfiles.Items.Insert(1, pf.ProfileName);

            if (!_settingProfiles.Profiles.Any())
            {
                cbxProfiles.SelectedIndex = 0;
                return;
            }

            _selectedProfileSettings = _settingProfiles.Profiles[0];
            cbxProfiles.SelectedIndex = 1;
        }

        Rectangle GameFieldRect => new Rectangle(pbScreenField.PointToScreen(new Point()), pbScreenField.Size);

        static List<int> GetBitmapAsInts(Bitmap btm)
        {
            List<int> result = new List<int>();

            for (int y = 0; y < btm.Height; y++)
                for (int x = 0; x < btm.Width; x++)
                    result.Add(btm.GetPixel(x, y).ToArgb());

            return result;
        }

        bool UpdateProfileStatus(bool silent)
        {
            if (_selectedProfileSettings.Spaces.All(m => m.Name != 'X'))
            {
                btnGameStart.Enabled = false;

                const string message = @"Отсутствуют обозначения крестиков (X). Создайте новый профиль.";

                Logger.WriteLog(() => $@"{nameof(UpdateProfileStatus)}: {message} silent = {silent}.");

                if (silent)
                    return false;

                MessageBox.Show(this, message);
                return false;
            }

            if (_selectedProfileSettings.Spaces.All(m => m.Name != 'O'))
            {
                btnGameStart.Enabled = false;

                const string message = @"Отсутствуют обозначения ноликов (O). Создайте новый профиль.";

                Logger.WriteLog(() => $@"{nameof(UpdateProfileStatus)}: {message} silent = {silent}.");

                if (silent)
                    return false;

                MessageBox.Show(this, message);
                return false;
            }

            if (_selectedProfileSettings.Spaces.All(m => m.Name != 'E'))
            {
                btnGameStart.Enabled = false;

                const string message = @"Отсутствуют обозначения пустых мест. Создайте новый профиль.";

                Logger.WriteLog(() => $@"{nameof(UpdateProfileStatus)}: {message} silent = {silent}.");

                if (silent)
                    return false;

                MessageBox.Show(this, message);
                return false;
            }

            if (_selectedProfileSettings.Spaces.Count < 9)
            {
                btnGameStart.Enabled = false;

                string message = $@"Недостаточно обозначений мест ударов. Сейчас их {_selectedProfileSettings.Spaces.Count}, а должно быть 9.";

                Logger.WriteLog(() => $@"{nameof(UpdateProfileStatus)}: {message} silent = {silent}.");

                if (silent)
                    return false;

                MessageBox.Show(this, message);
                return false;
            }

            if (_selectedProfileSettings.Spaces.Count > 9)
            {
                btnGameStart.Enabled = false;

                string message = $@"Мест ударов меньше, чем создано. Сейчас их {_selectedProfileSettings.Spaces.Count}, а должно быть 9. Создайте профиль заново.";

                Logger.WriteLog(() => $@"{nameof(UpdateProfileStatus)}: {message} silent = {silent}.");

                MessageBox.Show(this, message);
                return false;
            }

            try
            {
                if (BuildField() is null)
                {
                    btnGameStart.Enabled = false;

                    const string message = @"Процесс компиляции завершился сбоем.";

                    Logger.WriteLog(() => $@"{nameof(UpdateProfileStatus)}: {message} silent = {silent}.");

                    MessageBox.Show(this, message);
                    return false;
                }
            }
            catch (Exception ex)
            {
                btnGameStart.Enabled = false;

                string message = $@"Процесс компиляции сборки завершился сбоем.{Environment.NewLine}Текст ошибки: {ex.Message}.";

                Logger.WriteLog(() => $@"{nameof(UpdateProfileStatus)}: {message} silent = {silent}.");

                MessageBox.Show(this, message);
                return false;
            }

            Logger.WriteLog(() => $@"{nameof(UpdateProfileStatus)}: Успех. silent = {silent}.");

            btnGameStart.Enabled = true;

            return true;
        }

        void BtnSavePosition_Click(object sender, EventArgs e)
        {
            Logger.WriteLog(() => $@"{nameof(BtnSavePosition_Click)}: Сохранение элемента карты...");

            Rectangle gfr = GameFieldRect;

            if (radNeedClick.Checked)
            {
                BitImages bi = new BitImages
                {
                    Data = GetBitmapAsInts(CopyGameFieldFromScreen()),
                    Coords = gfr.Location,
                    FieldSize = gfr.Size,
                    Name = 'Z'
                };

                _selectedProfileSettings.EventClicks.Add(bi);
                _needSaveProfile = true;

                Logger.WriteLog(() => $@"{nameof(BtnSavePosition_Click)}: Действие добавлено:{Environment.NewLine}{bi}.");

                return;
            }

            try
            {
                if (radEmptySpace.Checked)
                {
                    BitImages bi = new BitImages
                    {
                        Data = GetBitmapAsInts(CopyGameFieldFromScreen()),
                        Coords = gfr.Location,
                        FieldSize = gfr.Size,
                        Name = 'E'
                    };

                    _selectedProfileSettings.Spaces.Add(bi);
                    _needSaveProfile = true;

                    Logger.WriteLog(() => $@"{nameof(BtnSavePosition_Click)}: Объект свободного поля добавлен:{Environment.NewLine}{bi}.");

                    return;
                }

                if (radField_X.Checked)
                {
                    BitImages bi = new BitImages
                    {
                        Data = GetBitmapAsInts(CopyGameFieldFromScreen()),
                        Coords = gfr.Location,
                        FieldSize = gfr.Size,
                        Name = 'X'
                    };

                    _selectedProfileSettings.Spaces.Add(bi);
                    _needSaveProfile = true;

                    Logger.WriteLog(() => $@"{nameof(BtnSavePosition_Click)}: Объект X добавлен:{Environment.NewLine}{bi}.");

                    return;
                }

                if (!radField_O.Checked)
                    return;

                BitImages bi1 = new BitImages
                {
                    Data = GetBitmapAsInts(CopyGameFieldFromScreen()),
                    Coords = gfr.Location,
                    FieldSize = gfr.Size,
                    Name = 'O'
                };

                _selectedProfileSettings.Spaces.Add(bi1);
                _needSaveProfile = true;

                Logger.WriteLog(() => $@"{nameof(BtnSavePosition_Click)}: Объект O добавлен:{Environment.NewLine}{bi1}.");
            }
            finally
            {
                UpdateProfileStatus(true);
            }
        }

        static Bitmap GetBitmapPiece(Rectangle rect, Bitmap where)
        {
            Logger.WriteLog(() => $@"{nameof(GetBitmapPiece)}: {nameof(rect.X)} = {rect.X}, {nameof(rect.Y)} = {rect.Y}; {nameof(rect.Width)} = {rect.Width}, {nameof(rect.Height)} = {rect.Height}{Environment.NewLine}{nameof(where)} = ({where.Width}, {where.Height}).", Logger.LogLevel.DEBUG);

            Bitmap result = new Bitmap(rect.Width, rect.Height);

            for (int y = 0, ly = rect.Y; y < rect.Height; y++, ly++)
                for (int x = 0, lx = rect.X; x < rect.Width; x++, lx++)
                    result.SetPixel(x, y, where.GetPixel(lx, ly));

            return result;
        }

        void GameHandler(int[,] sessionCopy, ref GameSession gameSession, Dictionary<Size, (BitImages, ProcessorHandler)> pcs, HitCounter hc)
        {
            if (sessionCopy is null)
            {
                Logger.WriteLog(() => $@"{nameof(GameHandler)}: {nameof(sessionCopy)} is null.", Logger.LogLevel.ERROR);
                return;
            }

            if (gameSession is null)
            {
                gameSession = new GameSession();
                GameSession.IsGameCompetitorsInverted = false;

                Logger.WriteLog(() => $@"{nameof(GameHandler)}: {nameof(gameSession)} is null.", Logger.LogLevel.DEBUG);
            }

            GameSession.FieldState fs = GameSession.GetCurrentState(sessionCopy);

            Logger.WriteLog(() => $@"{nameof(GameHandler)}: {nameof(fs)} = {fs}.", Logger.LogLevel.DEBUG);

            if (gameSession.HitX < 0 || gameSession.HitY < 0)
            {
                switch (fs)
                {
                    case GameSession.FieldState.EMPTY:
                        Logger.WriteLog(() => $@"{nameof(GameHandler)}: Игра начинается с пустого поля.", Logger.LogLevel.DEBUG);
                        gameSession = new GameSession(sessionCopy);
                        DoFirstHit(gameSession);
                        GameSession.IsGameCompetitorsInverted = false;
                        return;
                    case GameSession.FieldState.FULL:
                        Logger.WriteLog(() => $@"{nameof(GameHandler)}: Игра завершилась полностью заполненным полем.", Logger.LogLevel.DEBUG);
                        DoClickActions(pcs);
                        GameSession.IsGameCompetitorsInverted = false;
                        return;
                    case GameSession.FieldState.WAITHIT:
                        {
                            Logger.WriteLog(() => $@"{nameof(GameHandler)}: Ожидается мой удар...", Logger.LogLevel.DEBUG);
                            (int hit, Point hitPoint) = GetAloneHit(sessionCopy);

                            Logger.WriteLog(() => $@"{nameof(GameHandler)}: {nameof(hit)} = {hit}; {nameof(hitPoint)} = ({hitPoint.X}, {hitPoint.Y}).", Logger.LogLevel.DEBUG);

                            switch (hit)
                            {
                                case GameSession.EmptyHit:
                                    Logger.WriteLog(() => $@"{nameof(GameHandler)}: Игра начинается... Неизвестно, какими я играю... Бью на удачу!", Logger.LogLevel.DEBUG);
                                    DoUnknownHit(gameSession);
                                    GameSession.IsGameCompetitorsInverted = false;
                                    return;
                                case GameSession.UserHit:
                                    Logger.WriteLog(() => $@"{nameof(GameHandler)}: Игра начинается... Вижу свой удар, хожу крестиками (X).", Logger.LogLevel.DEBUG);
                                    DoGameHit(gameSession, hitPoint);
                                    GameSession.IsGameCompetitorsInverted = false;
                                    return;
                                case GameSession.BotHit:
                                    Logger.WriteLog(() => $@"{nameof(GameHandler)}: Игра начинается... Вижу удар пользователя, хожу ноликами (O).", Logger.LogLevel.DEBUG);
                                    GameSession.IsGameCompetitorsInverted = true;
                                    DoGameHit(gameSession, hitPoint);
                                    return;
                                default:
                                    throw new Exception($@"Игра начинается... Что-то пошло не так, удар ({hit}).");
                            }
                        }
                    default:
                        Logger.WriteLog(() => $@"{nameof(GameHandler)}: {nameof(GameSession.FieldState)} is unknown.", Logger.LogLevel.ERROR);
                        throw new ArgumentOutOfRangeException();
                }
            }

            if (fs == GameSession.FieldState.EMPTY)
            {
                if (DoClickActions(pcs))
                {
                    Logger.WriteLog(() => $@"{nameof(GameHandler)}: Игра продолжается... Оказалось, что надо было сделать действие.", Logger.LogLevel.DEBUG);
                    GameSession.IsGameCompetitorsInverted = false;
                    return;
                }

                Logger.WriteLog(() => $@"{nameof(GameHandler)}: Игра продолжается... Вижу, что поле пустое, и наношу первый удар.", Logger.LogLevel.DEBUG);

                gameSession.FixGameStep();
                gameSession = new GameSession(sessionCopy);
                DoFirstHit(gameSession);
                GameSession.IsGameCompetitorsInverted = false;

                return;
            }

            switch (sessionCopy[gameSession.HitX, gameSession.HitY])
            {
                case GameSession.UserHit:
                    Logger.WriteLog(() => $@"{nameof(GameHandler)}: Игра продолжается... Вижу, что я играю крестиками (X).", Logger.LogLevel.DEBUG);
                    GameSession.IsGameCompetitorsInverted = true;
                    return;
            }

            Point hp = new Point();
            int diffCount = 0;

            for (int y = 0; y < 3; y++)
                for (int x = 0; x < 3; x++)
                {
                    int m1 = sessionCopy[x, y];
                    int m2 = gameSession[x, y];

                    if (m1 == m2)
                        continue;

                    if (m1 != GameSession.EmptyHit && m2 != GameSession.EmptyHit)
                    {
                        Logger.WriteLog(() => $@"{nameof(GameHandler)}: При поиске изменений на игровом поле, была найдена ошибка ({m1}, {m2}).", Logger.LogLevel.DEBUG);
                        gameSession = new GameSession(sessionCopy);
                        DoClickActions(pcs);
                        GameSession.IsGameCompetitorsInverted = false;
                        return;
                    }

                    if (m1 == GameSession.EmptyHit && m2 != GameSession.EmptyHit)
                    {
                        if (x == gameSession.HitX && y == gameSession.HitY)
                        {
                            int x1 = x;
                            int y1 = y;
                            Logger.WriteLog(() => $@"{nameof(GameHandler)}: Вижу свой последний удар ({x1}, {y1}), который, видимо, не успел отобразиться...", Logger.LogLevel.DEBUG);
                            continue;
                        }

                        Logger.WriteLog(() => $@"{nameof(GameHandler)}: Странная ситуация - найден какое-то поле, которое должно быть пустое, но это не так ({m2}).", Logger.LogLevel.DEBUG);

                        if (DoClickActions(pcs))
                        {
                            Logger.WriteLog(() => $@"{nameof(GameHandler)}: Действие (клик) совершено.", Logger.LogLevel.DEBUG);
                            GameSession.IsGameCompetitorsInverted = false;
                            return;
                        }

                        Logger.WriteLog(() => $@"{nameof(GameHandler)}: Действие (клик) не требуется, попробую сходить ""на удачу"".", Logger.LogLevel.DEBUG);

                        gameSession = new GameSession(sessionCopy);
                        DoUnknownHit(gameSession);
                        GameSession.IsGameCompetitorsInverted = false;
                        return;
                    }

                    hp = new Point(x, y);
                    ++diffCount;

                    Point hp1 = hp;
                    int dc = diffCount;
                    Logger.WriteLog(() => $@"{nameof(GameHandler)}: Найдена точка удара соперника ({hp1.X}, {hp1.Y}). Количество найденных изменений {dc}.", Logger.LogLevel.DEBUG);
                }

            if (diffCount < 1)
            {
                Logger.WriteLog(() => $@"{nameof(GameHandler)}: Количество отличий должно быть равно одному, а не ({diffCount}).", Logger.LogLevel.DEBUG);

                if (DoClickActions(pcs))
                {
                    Logger.WriteLog(() => $@"{nameof(GameHandler)}: Действие (клик) совершено.", Logger.LogLevel.DEBUG);
                    GameSession.IsGameCompetitorsInverted = false;
                    return;
                }

                Logger.WriteLog(() => $@"{nameof(GameHandler)}: Действие (клик) не требуется.", Logger.LogLevel.DEBUG);

                return;
            }

            if (diffCount == 1)
            {
                Logger.WriteLog(() => $@"{nameof(GameHandler)}: Ход соперника определён ({hp.X}, {hp.Y}). Наношу удар.", Logger.LogLevel.DEBUG);

                DoGameHit(gameSession, hp);
                return;
            }

            if (DoClickActions(pcs))
            {
                Logger.WriteLog(() => $@"{nameof(GameHandler)}: Не удалось определить ход соперника. Действие (клик) совершено. Количество отличий ({diffCount}).", Logger.LogLevel.DEBUG);
                GameSession.IsGameCompetitorsInverted = false;
                return;
            }

            Logger.WriteLog(() => $@"{nameof(GameHandler)}: Не удалось определить ход соперника. Действие (клик) не требуется. Количество отличий ({diffCount}).", Logger.LogLevel.DEBUG);

            gameSession = new GameSession(sessionCopy);
            DoUnknownHit(gameSession);
            GameSession.IsGameCompetitorsInverted = false;

            return;

            (int hit, Point hitPoint) GetAloneHit(int[,] mp)
            {
                if (mp is null)
                    throw new ArgumentNullException();

                (int hit, Point hitPoint) result = new ValueTuple<int, Point>();

                for (int y = 0, mY = mp.GetLength(1); y < mY; y++)
                    for (int x = 0, mX = mp.GetLength(0); x < mX; x++)
                    {
                        int v = mp[x, y];

                        if (v == GameSession.EmptyHit)
                            continue;

                        if (result.hit != GameSession.EmptyHit)
                            return (GameSession.EmptyHit, new Point());

                        result = (v, new Point(x, y));
                    }

                return result;
            }

            void DoUnknownHit(GameSession gs)
            {
                (int x, int y) = gs.MakeHitDecision();

                if (!gs.MakeBotHit(x, y))
                    throw new Exception($@"{nameof(DoUnknownHit)}: Странное решение ({x}, {y}).");

                DoPhysicalHit(x, y);
            }

            void DoFirstHit(GameSession gs)
            {
                hc.Inc();
                int x = hc.Counter % 3, y = hc.Counter / 3;
                if (!gs.MakeBotHit(x, y))
                    throw new Exception($@"{nameof(DoFirstHit)}: Странное решение ({x}, {y}).");
                DoPhysicalHit(gs.HitX, gs.HitY);
            }

            void DoGameHit(GameSession gs, Point hitPoint)
            {
                if (!gs.MakeUserHit(hitPoint.X, hitPoint.Y))
                    throw new Exception(
                        $@"{nameof(DoGameHit)}: Что-то пошло не так ({hitPoint.X}, {hitPoint.Y}).");

                if (gs.CurrentWinner != GameSession.Winner.NOBODY)
                    return;

                (int x, int y) = gs.MakeHitDecision();

                if (!gs.MakeBotHit(x, y))
                    throw new Exception($@"{nameof(DoGameHit)}: Странное решение ({x}, {y}).");

                DoPhysicalHit(x, y);
            }
        }

        ProcessorContainer ProcessorContainerFromSettings => new ProcessorContainer(_selectedProfileSettings.Spaces.Select((bi, bx) =>
        {
            Processor p = new Processor(bi.AsBitmap, $@"{bi.Name}{bx}");

            Logger.WriteLog(() => $@"{nameof(ProcessorContainerFromSettings)}: Искомый элемент игрового поля создан ({p.Tag}: {p.Width}, {p.Height}).", Logger.LogLevel.DEBUG);

            return p;
        }).ToArray());

        int[,] BuildField(ProcessorContainer req = null)
        {
            Bitmap fullFrameNow = TakeScreenshot();

            if (fullFrameNow is null)
            {
                Logger.WriteLog(() => $@"{nameof(BuildField)}: Ошибка при построении игрового поля.", Logger.LogLevel.ERROR);
                return null;
            }

            while (true)
            {
                IEnumerable<Processor> pq1 = _selectedProfileSettings.Spaces.Select(bi =>
                {
                    Logger.WriteLog(() => $@"{nameof(BuildField)}: Область поиска игровой сетки:{Environment.NewLine}1) Общий размер поля: {fullFrameNow.Width} x {fullFrameNow.Height}{Environment.NewLine}2) Необходимо извлечь фрагмент: X: {bi.Coords.X}, Y: {bi.Coords.Y}; W: {bi.FieldSize.Width}, H: {bi.FieldSize.Height}.", Logger.LogLevel.DEBUG, true);

                    Processor p = new Processor(GetBitmapPiece(new Rectangle(bi.Coords, bi.FieldSize), fullFrameNow), @"Z");

                    Logger.WriteLog(() => $@"{nameof(BuildField)}: Создана исследуемая карта ({p.Tag}: {p.Width}, {p.Height}).", Logger.LogLevel.DEBUG, true);

                    return p;
                });

                if (req is null)
                    req = ProcessorContainerFromSettings;

                Logger.WriteLog(() =>
                {
                    StringBuilder r = new StringBuilder($@"{nameof(BuildField)}: Поисковый запрос выглядит следующим образом:");

                    for (int k = 0; k < req.Count; k++)
                    {
                        r.AppendLine();
                        Processor p = req[k];
                        r.Append($@"{k + 1}) {p.Tag} ({p.Width}, {p.Height})");
                    }

                    r.Append('.');

                    return r.ToString();
                }, Logger.LogLevel.DEBUG, true);

                List<SearchResults> results = new List<SearchResults>(pq1.Select(p => p.GetEqual(req)));

                if (results.Count != 9)
                    throw new InvalidOperationException($@"{nameof(BuildField)}: {nameof(results)} не равно 9: {results.Count}");

                int[,] sessionCopy = new int[3, 3];

                for (int k = 0; k < 9; k++)
                {
                    ProcPerc pp = results[k][0, 0];

                    Processor[] pps = pp.Procs;
                    char rTag = pps[0].Tag[0];

                    for (int kp = 1; kp < pps.Length; kp++)
                    {
                        if (rTag != 'E')
                        {
                            char c = pps[kp].Tag[0];
                            if (rTag != c && c != 'E')
                                throw new ArgumentException(
                                    $@"{nameof(BuildField)}: Неоднозначность ({pps.Length}) => ({pps[0].Tag} <==> {pps[kp].Tag}), клетка номер {k} (с нуля).");
                            continue;
                        }

                        rTag = pps[kp].Tag[0];
                    }

                    int x = k % 3;
                    int y = k / 3;

                    switch (rTag)
                    {
                        case 'X':
                            sessionCopy[x, y] = GameSession.IsGameCompetitorsInverted ? GameSession.BotHit : GameSession.UserHit;
                            break;

                        case 'O':
                            sessionCopy[x, y] = GameSession.IsGameCompetitorsInverted ? GameSession.UserHit : GameSession.BotHit;
                            break;

                        case 'E':
                            sessionCopy[x, y] = GameSession.EmptyHit;
                            break;

                        default:
                            throw new Exception($@"{nameof(BuildField)}: ");
                    }
                }

                Logger.WriteLog(() => $@"{nameof(BuildField)}: Игровое поле собрано ->{Environment.NewLine}{GameSession.ArrayVisualize(sessionCopy)}.", Logger.LogLevel.DEBUG);

                return sessionCopy;
            }
        }

        void DoPhysicalHit(int x, int y)
        {
            Logger.WriteLog(() => $@"{nameof(DoPhysicalHit)}: {nameof(x)} = {x}, {nameof(y)} = {y}.", Logger.LogLevel.DEBUG);

            BitImages bi = _selectedProfileSettings.Spaces[y * 3 + x];
            int px = bi.Coords.X + bi.FieldSize.Width / 2;
            int py = bi.Coords.Y + bi.FieldSize.Height / 2;

            MouseClickMethods.Click(GetPhysicalCoords(px, py));
        }

        static Point GetPhysicalCoords(int x, int y)
        {
            Logger.WriteLog(() => $@"{nameof(GetPhysicalCoords)}: {nameof(x)} = {x}, {nameof(y)} = {y}.", Logger.LogLevel.DEBUG);

            Screen screen = Screen.FromPoint(new Point(x, y));

            int cX = screen.Bounds.Width;
            int cY = screen.Bounds.Height;

            Logger.WriteLog(() => $@"{nameof(GetPhysicalCoords)}: {nameof(cX)} = {cX}, {nameof(cY)} = {cY}.", Logger.LogLevel.DEBUG);

            int pX = GetAbsoluteCoordinate(x, cX);
            int pY = GetAbsoluteCoordinate(y, cY);

            Logger.WriteLog(() => $@"{nameof(GetPhysicalCoords)}: {nameof(pX)} = {pX}, {nameof(pY)} = {pY}.", Logger.LogLevel.DEBUG);

            int sbX = screen.Bounds.Left;
            int sbY = screen.Bounds.Top;

            Logger.WriteLog(() => $@"{nameof(GetPhysicalCoords)}: {nameof(sbX)} = {sbX}, {nameof(sbY)} = {sbY}.", Logger.LogLevel.DEBUG);

            Point p = new Point(sbX + pX, sbY + pY);

            Logger.WriteLog(() => $@"{nameof(GetPhysicalCoords)}: Возвращаю значение ({p.X}, {p.Y}).", Logger.LogLevel.DEBUG);

            return p;

            int GetAbsoluteCoordinate(int pixelCoordinate, int screenResolution) => pixelCoordinate * 65536 / screenResolution + 1;
        }

        void GameThreadFunction()
        {
            try
            {
                Logger.WriteLog(() => $@"{nameof(GameThreadFunction)} Запуск бота...");

                Dictionary<Size, (BitImages, ProcessorHandler)> pcs = GetEventClickHandlers();
                ProcessorContainer req = ProcessorContainerFromSettings;
                HitCounter hc = _selectedProfileSettings.StartHitCounter;

                DoClickActions(pcs);

                GameSession gameSession = null;

                Logger.WriteLog(() => $@"{nameof(GameThreadFunction)} Бот запущен.");

                while (true)
                {
                    int[,] sessionCopy = BuildField(req);

                    GameHandler(sessionCopy, ref gameSession, pcs, hc);

                    if (!(sessionCopy is null) || DoClickActions(pcs))
                        continue;

                    Logger.WriteLog(() => $@"{nameof(GameThreadFunction)} Не могу обработать ситуацию.", Logger.LogLevel.ERROR);
                    break;
                }
            }
            catch (ThreadAbortException ex)
            {
                ResetAbort();
                Logger.WriteLog(() => $@"{nameof(GameThreadFunction)}: Бот останавливается. ({ex.Message})");
            }
            catch (Exception ex)
            {
                Logger.WriteLog(() => $@"{nameof(GameThreadFunction)}: {ex.Message}", Logger.LogLevel.ERROR);
                SafeExecute(() => MessageBox.Show(this, ex.Message, @"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error), true);
            }
            finally
            {
                try
                {
                    GameBotThread = null;

                    if (!ProgramStopFlag)
                    {
                        BeginInvoke(new Action(() => SafeExecute(() =>
                        {
                            try
                            {
                                Logger.WriteLog(() => $@"{nameof(GameThreadFunction)}: ProgramStopFlag = {ProgramStopFlag}",
                                    Logger.LogLevel.DEBUG);

                                if (ProgramStopFlag)
                                    return;

                                radNeedClick.Enabled = true;
                                radEmptySpace.Enabled = true;
                                radField_X.Enabled = true;
                                radField_O.Enabled = true;
                                btnSavePosition.Enabled = true;
                                btnGameStart.Text = _btnStartCaption;
                            }
                            catch (Exception ex)
                            {
                                Logger.WriteLog(() => $@"{nameof(GameThreadFunction)}: {ex.Message}", Logger.LogLevel.ERROR);
                                throw;
                            }
                        })));
                    }
                }
                catch (Exception ex)
                {
                    Logger.WriteLog(() => $@"{nameof(GameThreadFunction)}: {ex.Message}", Logger.LogLevel.ERROR);
                    throw;
                }
                finally
                {
                    Logger.WriteLog(() => $@"{nameof(GameThreadFunction)}: Бот остановлен.");
                }
            }
        }

        bool DoClickActions(IDictionary<Size, (BitImages, ProcessorHandler)> ps)
        {
            if (ps is null)
            {
                Logger.WriteLog(() => $@"{nameof(DoClickActions)}: Контейнер равен null.", Logger.LogLevel.ERROR);
                return false;
            }

            Bitmap fullFrameNow = TakeScreenshot();

            if (fullFrameNow is null)
            {
                Logger.WriteLog(() => $@"{nameof(DoClickActions)}: Не могу получить снимок экрана.", Logger.LogLevel.ERROR);
                return false;
            }

            foreach (BitImages bi in _selectedProfileSettings.EventClicks)
            {
                Point pcs = bi.Coords;
                Size psz = bi.FieldSize;

                Logger.WriteLog(() => $@"{nameof(DoClickActions)}: Проверяю необходимость совершения действия ({pcs.X}, {pcs.Y}; {psz.Width}, {psz.Height}).", Logger.LogLevel.DEBUG);

                Processor pq = new Processor(GetBitmapPiece(new Rectangle(pcs, psz), fullFrameNow), @"Z");

                Logger.WriteLog(() => $@"{nameof(DoClickActions)}: Изображение преобразовано в карту ({pq.Tag}: {pq.Width}, {pq.Height}).", Logger.LogLevel.DEBUG);

                SearchResults sr = pq.GetEqual(new ProcessorContainer(ps[psz].Item2.Processors.ToArray()));

                if (sr[0, 0].Procs.All(p => p.Tag[0] != 'Z'))
                {
                    Logger.WriteLog(() => $@"{nameof(DoClickActions)}: Карта Z отсутствует. Продолжаю поиск требуемого действия.", Logger.LogLevel.DEBUG);
                    continue;
                }

                Logger.WriteLog(() => $@"{nameof(DoClickActions)}: Попытка совершить клик, координаты на экране: {bi.HitX}, {bi.HitY}.", Logger.LogLevel.DEBUG);
                MouseClickMethods.Click(GetPhysicalCoords(bi.HitX, bi.HitY));
                Logger.WriteLog(() => $@"{nameof(DoClickActions)}: Действие совершено.", Logger.LogLevel.DEBUG);

                return true;
            }

            Logger.WriteLog(() => $@"{nameof(DoClickActions)}: Какие-либо действия выполнять не требуется.", Logger.LogLevel.DEBUG);
            return false;
        }

        Dictionary<Size, (BitImages, ProcessorHandler)> GetEventClickHandlers()
        {
            try
            {
                Logger.WriteLog(() => $@"{nameof(GetEventClickHandlers)}: Настройка фильтров для определения необходимости выполнения действий (кликов).", Logger.LogLevel.DEBUG);

                Dictionary<Size, (BitImages, ProcessorHandler)> phs =
                    new Dictionary<Size, (BitImages, ProcessorHandler)>();

                foreach (BitImages ec in _selectedProfileSettings.EventClicks)
                {
                    Size sz = ec.FieldSize;
                    if (phs.TryGetValue(sz, out (BitImages, ProcessorHandler) phh))
                    {
                        Logger.WriteLog(() => $@"{nameof(GetEventClickHandlers)}: Фильтр этого размера ({sz.Width}; {sz.Height}) отсутствует в списке, добавляю...", Logger.LogLevel.DEBUG);
                        Processor p = ec.AsProcessor;
                        phh.Item2.Add(p);
                        Logger.WriteLog(() => $@"{nameof(GetEventClickHandlers)}: Фильтр ({sz.Width}; {sz.Height}) -> {p.Tag}: ({p.Width}; {p.Height}) добавлен.", Logger.LogLevel.DEBUG);
                        continue;
                    }

                    Logger.WriteLog(() => $@"{nameof(GetEventClickHandlers)}: Фильтр этого размера ({sz.Width}; {sz.Height}) присутствует в списке, добавляю ему пару...", Logger.LogLevel.DEBUG);

                    ProcessorHandler ph = new ProcessorHandler();
                    Processor p1 = ec.AsProcessor;
                    ph.Add(p1);
                    phs.Add(sz, (ec, ph));

                    Logger.WriteLog(() => $@"{nameof(GetEventClickHandlers)}: Фильтр ({sz.Width}; {sz.Height}) -> {p1.Tag}: ({p1.Width}; {p1.Height}) добавлен.", Logger.LogLevel.DEBUG);
                }

                Logger.WriteLog(() => $@"{nameof(GetEventClickHandlers)}: Фильтры мониторинга необходимых действий настроены (количество {phs.Count}).", Logger.LogLevel.DEBUG);

                return phs;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(() => $@"{nameof(GetEventClickHandlers)}: Неизвестная ошибка: {ex.Message}.", Logger.LogLevel.ERROR);
                throw;
            }
        }

        bool StopGameBotThread()
        {
            Thread rt = GameBotThread;

            if (rt is null)
            {
                Logger.WriteLog(() => $@"{nameof(StopGameBotThread)}: Бот неактивен.", Logger.LogLevel.DEBUG);
                return false;
            }

            rt.Abort();
            rt.Join();

            Logger.WriteLog(() => $@"{nameof(StopGameBotThread)}: Рабочий поток бота завершён.", Logger.LogLevel.DEBUG);

            GameBotThread = null;

            if (ProgramStopFlag)
            {
                Logger.WriteLog(() => $@"{nameof(StopGameBotThread)}: Программа в процессе завершения.", Logger.LogLevel.DEBUG);
                return true;
            }

            SafeExecute(() =>
            {
                try
                {
                    radNeedClick.Enabled = true;
                    radEmptySpace.Enabled = true;
                    radField_X.Enabled = true;
                    radField_O.Enabled = true;
                    btnSavePosition.Enabled = true;
                    btnGameStart.Text = _btnStartCaption;
                }
                catch (Exception ex)
                {
                    Logger.WriteLog(() => $@"{nameof(StopGameBotThread)}: Ошибка интерфейса пользователя: {ex.Message}.", Logger.LogLevel.ERROR);
                    throw;
                }
            }, true);

            Logger.WriteLog(() => $@"{nameof(StopGameBotThread)}: Работа бота завершена.", Logger.LogLevel.DEBUG);
            return true;
        }

        void BtnGameStart_Click(object sender, EventArgs e)
        {
            SafeExecute(() =>
            {
                try
                {
                    if (StopGameBotThread())
                    {
                        Logger.WriteLog(() => $@"{nameof(BtnGameStart_Click)}: Работа бота прервана.", Logger.LogLevel.DEBUG);
                        return;
                    }

                    if (!UpdateProfileStatus(false))
                    {
                        Logger.WriteLog(() => $@"{nameof(BtnGameStart_Click)}: Ошибка при проверке профиля настроек бота.",
                            Logger.LogLevel.DEBUG);
                        return;
                    }

                    if (_needSaveProfile)
                    {
                        Logger.WriteLog(() => $@"{nameof(BtnGameStart_Click)}: Создание рабочей сборки для бота...",
                            Logger.LogLevel.DEBUG);

                        string profileName = _selectedProfileSettings.ProfileName;

                        using (FrmName fn = new FrmName())
                        {
                            fn.MyTxtName = profileName;
                            if (fn.ShowDialog(this) == DialogResult.OK)
                            {
                                profileName = fn.MyTxtName;
                                string pn = profileName;
                                Logger.WriteLog(() =>
                                    $@"{nameof(BtnGameStart_Click)}: Профиль в процессе добавления: {pn}.");
                            }
                        }

                        if (string.IsNullOrEmpty(profileName))
                            profileName = cbxProfiles.Items.Count.ToString();

                        _selectedProfileSettings.ProfileName = profileName;
                        Bitmap screenshot = TakeScreenshot();

                        if (!(screenshot is null))
                        {
                            _selectedProfileSettings.EventClicks.AddRange(
                                _selectedProfileSettings.EventClicks.Select(ec =>
                                    (ec, GetBitmapPiece(ec.AsRectangle, screenshot))).Select(bt => new BitImages(bt.ec)
                                    {
                                        Data = GetBitmapAsInts(bt.Item2),
                                        Name = 'E'
                                    }).ToArray());
                        }

                        AddProfile();
                        Logger.WriteLog(() => $@"{nameof(BtnGameStart_Click)}: Профиль добавлен: {profileName}.");
                    }

                    radNeedClick.Enabled = false;
                    radEmptySpace.Enabled = false;
                    radField_X.Enabled = false;
                    radField_O.Enabled = false;
                    btnSavePosition.Enabled = false;
                    btnGameStart.Text = @"Стоп";

                    Thread t = new Thread(GameThreadFunction)
                    {
                        IsBackground = true,
                        Name = @"GameThread"
                    };

                    t.Start();

                    GameBotThread = t;

                    Logger.WriteLog(() => $@"{nameof(BtnGameStart_Click)}: Бот запущен.");
                }
                catch (Exception ex)
                {
                    Logger.WriteLog(() => $@"{nameof(BtnGameStart_Click)}: Ошибка: {ex.Message}.", Logger.LogLevel.ERROR);
                    throw;
                }
            });
        }

        /// <summary>
        ///     Представляет обёртку для выполнения функций с применением блоков <see langword="try" />-<see langword="catch" />,
        ///     а также выдачей сообщений обо всех
        ///     ошибках.
        /// </summary>
        /// <param name="funcAction">Функция, которая должна быть выполнена.</param>
        /// <param name="needInvoke">Значение <see langword="true" /> в случае необходимости выполнить функцию в основном потоке.</param>
        void SafeExecute(Action funcAction, bool needInvoke = false)
        {
            if (funcAction == null)
            {
                string s = $@"{nameof(SafeExecute)}(1): Выполняемая функция отсутствует.";

                throw new ArgumentNullException(nameof(funcAction), s);
            }

            try
            {
                void Act()
                {
                    try
                    {
                        funcAction.Invoke();
                    }
                    catch (Exception ex)
                    {
                        try
                        {
                            ResetAbort();

                            ErrorMessageInOtherThread(ex.Message);
                        }
                        catch
                        {
                            throw ex;
                        }
                    }
                }

                if (needInvoke && InvokeRequired)
                    Invoke((Action)Act);
                else
                    Act();
            }
            catch (Exception ex)
            {
                ResetAbort();

                ErrorMessageInOtherThread(ex.Message);
            }
        }

        /// <summary>
        ///     Отменяет <see cref="Thread.Abort()" />, вызванный для текущего потока, если он находится в состоянии
        ///     <see cref="System.Threading.ThreadState.AbortRequested" />.
        ///     Использует метод <see cref="Thread.ResetAbort()" />.
        /// </summary>
        /// <seealso cref="Thread.Abort()" />
        /// <seealso cref="System.Threading.ThreadState.AbortRequested" />
        /// <seealso cref="Thread.ResetAbort()" />
        static void ResetAbort()
        {
            if ((Thread.CurrentThread.ThreadState & ThreadState.AbortRequested) != 0)
                Thread.ResetAbort();
        }

        void FrmGameBot_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                SettingsProfilesArray.CurrentSettings = _settingProfiles;
            }
            catch (Exception ex)
            {
                if (MessageBox.Show(this,
                        $@"Ошибка при сохранении настроек: ""{ex.Message}""{Environment.NewLine}Всё равно выйти?",
                        @"Ошибка", MessageBoxButtons.YesNo) != DialogResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }
            }

            try
            {
                ProgramStopFlag = true;
                StopGameBotThread();
                _escapeThread?.Join();
            }
            catch
            {
                // ignored
            }
        }

        /// <summary>
        ///     Отображает сообщение с указанным текстом, в другом потоке.
        /// </summary>
        /// <param name="message">Текст отображаемого сообщения.</param>
        void ErrorMessageInOtherThread(string message)
        {
            new Thread(() =>
                SafeExecute(
                    () => MessageBox.Show(this, message, @"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Exclamation),
                    true))
            {
                IsBackground = true,
                Name = @"Message"
            }.Start();
        }

        void CbxProfiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (cbxProfiles.SelectedIndex < 1)
                {
                    _selectedProfileSettings = new SettingsProfilesArray.HitSettings();
                    _needSaveProfile = false;
                    return;
                }

                if (!_settingProfiles.Profiles.Any())
                    return;

                _selectedProfileSettings = _settingProfiles.Profiles[cbxProfiles.SelectedIndex - 1];
                _needSaveProfile = false;
                btnGameStart.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, @"Ошибка");
            }
        }

        /// <summary>
        ///     Получает или задаёт поток, который останавливает процесс выполнения поискового запроса.
        /// </summary>
        /// <remarks>
        ///     В случае, если поток не активен, в этом свойстве содержится значение <see langword="null" />.
        ///     Свойство потокобезопасно как на чтение, так и на запись.
        ///     Потокобезопасность обеспечивает поле <see cref="_commonLocker" />.
        ///     Значение этого свойства содержит поле <see cref="_stoppingThread" />.
        /// </remarks>
        /// <seealso cref="_commonLocker" />
        /// <seealso cref="_stoppingThread" />
        Thread StopperThread
        {
            get
            {
                lock (_commonLocker)
                {
                    return _stoppingThread;
                }
            }

            set
            {
                lock (_commonLocker)
                {
                    _stoppingThread = value;
                }
            }
        }
    }
}
