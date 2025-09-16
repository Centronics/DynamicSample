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
                    catch
                    {
                        return new SettingsProfilesArray();
                    }
                }

                set
                {
                    XmlSerializer ser = new XmlSerializer(typeof(SettingsProfilesArray));
                    using (FileStream fs = new FileStream(SettingsFilePath, FileMode.Create))
                        ser.Serialize(fs, value);
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
                                return bmp;

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

                        return bmp;
                    }
                }
            }
            catch
            {
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
                            if (!StopGameBotThread())
                                SafeExecute(Application.Exit, true);
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
            catch
            {
                // ignored
            }
        }

        void SaveProfile()
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

                if (silent)
                    return false;

                MessageBox.Show(this, @"Отсутствуют обозначения крестиков (X). Создайте новый профиль.");
                return false;
            }

            if (_selectedProfileSettings.Spaces.All(m => m.Name != 'O'))
            {
                btnGameStart.Enabled = false;

                if (silent)
                    return false;

                MessageBox.Show(this, @"Отсутствуют обозначения ноликов (O). Создайте новый профиль.");
                return false;
            }

            if (_selectedProfileSettings.Spaces.All(m => m.Name != 'E'))
            {
                btnGameStart.Enabled = false;

                if (silent)
                    return false;

                MessageBox.Show(this, @"Отсутствуют обозначения пустых мест. Создайте новый профиль.");
                return false;
            }

            if (_selectedProfileSettings.Spaces.Count < 9)
            {
                btnGameStart.Enabled = false;

                if (silent)
                    return false;

                MessageBox.Show(this, $@"Недостаточно обозначений мест ударов. Сейчас их {_selectedProfileSettings.Spaces.Count}, а должно быть 9.");
                return false;
            }

            if (_selectedProfileSettings.Spaces.Count > 9)
            {
                btnGameStart.Enabled = false;
                MessageBox.Show(this, $@"Мест ударов меньше, чем создано. Сейчас их {_selectedProfileSettings.Spaces.Count}, а должно быть 9. Создайте профиль заново.");
                return false;
            }

            try
            {
                if (BuildField() is null)
                {
                    btnGameStart.Enabled = false;
                    MessageBox.Show(this, @"Процесс компиляции сборки завершился сбоем.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                btnGameStart.Enabled = false;
                MessageBox.Show(this, $@"Процесс компиляции сборки завершился сбоем.{Environment.NewLine}Текст ошибки: {ex.Message}.");
                return false;
            }

            btnGameStart.Enabled = true;

            return true;
        }

        void BtnSavePosition_Click(object sender, EventArgs e)
        {
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
            }
            finally
            {
                UpdateProfileStatus(true);
            }
        }

        static Bitmap GetBitmapPiece(Rectangle rect, Bitmap where)
        {
            Bitmap result = new Bitmap(rect.Width, rect.Height);

            for (int y = 0, ly = rect.Y; y < rect.Height; y++, ly++)
                for (int x = 0, lx = rect.X; x < rect.Width; x++, lx++)
                    result.SetPixel(x, y, where.GetPixel(lx, ly));

            return result;
        }

        void GameHandler(int[,] sessionCopy, ref GameSession gameSession, Dictionary<Size, (BitImages, ProcessorHandler)> pcs, HitCounter hc)
        {
            if (sessionCopy is null)
                return;

            if (gameSession is null)
                gameSession = new GameSession();

            GameSession.FieldState fs = GameSession.GetCurrentState(sessionCopy);

            if (gameSession.HitX < 0 || gameSession.HitY < 0)
            {
                switch (fs)
                {
                    case GameSession.FieldState.EMPTY:
                        gameSession = new GameSession(sessionCopy);
                        DoFirstHit(gameSession);
                        return;
                    case GameSession.FieldState.FULL:
                        DoClickOperations(pcs);
                        return;
                    case GameSession.FieldState.WAITHIT:
                        {
                            (int hit, Point hitPoint) = GetAloneHit(sessionCopy);

                            switch (hit)
                            {
                                case GameSession.EmptyHit:
                                    DoUnknownHit(gameSession);
                                    return;
                                case GameSession.UserHit:
                                    if (GameSession.IsGameCompetitorsInverted)
                                        GameSession.IsGameCompetitorsInverted = false;
                                    else
                                        DoGameHit(gameSession, hitPoint);
                                    return;
                                case GameSession.BotHit:
                                    if (GameSession.IsGameCompetitorsInverted)
                                        DoGameHit(gameSession, hitPoint);
                                    else
                                        GameSession.IsGameCompetitorsInverted = true;
                                    return;
                                default:
                                    throw new Exception($@"Что-то пошло не так, удар ({hit}).");
                            }
                        }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            if (fs == GameSession.FieldState.EMPTY)
            {
                if (DoClickOperations(pcs))
                    return;

                gameSession.FixGameStep();
                gameSession = new GameSession(sessionCopy);
                DoFirstHit(gameSession);

                return;
            }

            switch (sessionCopy[gameSession.HitX, gameSession.HitY])
            {
                case GameSession.UserHit:
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
                        gameSession = new GameSession(sessionCopy);
                        DoClickOperations(pcs);
                        return;
                    }

                    if (m1 == GameSession.EmptyHit && m2 != GameSession.EmptyHit)
                    {
                        if (x == gameSession.HitX && y == gameSession.HitY)
                            continue;

                        if (DoClickOperations(pcs))
                            return;

                        gameSession = new GameSession(sessionCopy);
                        DoUnknownHit(gameSession);
                        return;
                    }

                    hp = new Point(x, y);
                    ++diffCount;
                }

            if (diffCount < 1)
            {
                DoClickOperations(pcs);
                return;
            }

            if (diffCount == 1)
            {
                DoGameHit(gameSession, hp);
                return;
            }

            if (DoClickOperations(pcs))
                return;

            gameSession = new GameSession(sessionCopy);
            DoUnknownHit(gameSession);

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
                    throw new Exception($@"Странное решение ({x}, {y}).");

                DoPhysicalHit(x, y);
            }

            void DoFirstHit(GameSession gs)
            {
                hc.Inc();
                gs.MakeBotHit(hc.Counter % 3, hc.Counter / 3);
                DoPhysicalHit(gs.HitX, gs.HitY);
            }

            void DoGameHit(GameSession gs, Point hitPoint)
            {
                if (!gs.MakeUserHit(hitPoint.X, hitPoint.Y))
                    throw new Exception(
                        $@"Что-то пошло не так ({hitPoint.X}, {hitPoint.Y}).");

                if (gs.CurrentWinner != GameSession.Winner.NOBODY)
                    return;

                (int x, int y) = gs.MakeHitDecision();

                if (!gs.MakeBotHit(x, y))
                    throw new Exception($@"Странное решение ({x}, {y}).");

                DoPhysicalHit(x, y);
            }
        }

        ProcessorContainer ProcessorContainerFromSettings => new ProcessorContainer(_selectedProfileSettings.Spaces.Select((bi, bx) => new Processor(bi.AsBitmap, $@"{bi.Name}{bx}")).ToArray());

        int[,] BuildField(ProcessorContainer req = null)
        {
            Bitmap fullFrameNow = TakeScreenshot();

            if (fullFrameNow is null)
                return null;

            while (true)
            {
                IEnumerable<Processor> pq1 = _selectedProfileSettings.Spaces.Select(bi =>
                    new Processor(GetBitmapPiece(new Rectangle(bi.Coords, bi.FieldSize), fullFrameNow), @"Z"));

                if (req is null)
                    req = ProcessorContainerFromSettings;

                List<SearchResults> results = new List<SearchResults>(pq1.Select(p => p.GetEqual(req)));

                if (results.Count != 9)
                    throw new InvalidOperationException($@"{nameof(results)} не равно 9: {results.Count}");

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
                                    $@"Неоднозначность ({pps.Length}) => ({pps[0].Tag} <==> {pps[kp].Tag}), клетка номер {k} (с нуля).");
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
                            throw new Exception();
                    }
                }

                return sessionCopy;
            }
        }

        void DoPhysicalHit(int x, int y)
        {
            BitImages bi = _selectedProfileSettings.Spaces[y * 3 + x];
            int px = bi.Coords.X + bi.FieldSize.Width / 2;
            int py = bi.Coords.Y + bi.FieldSize.Height / 2;

            MouseClickMethods.Click(GetPhysicalCoords(px, py));
        }

        static Point GetPhysicalCoords(int x, int y)
        {
            Screen screen = Screen.FromPoint(new Point(x, y));

            int cX = screen.Bounds.Width;
            int cY = screen.Bounds.Height;

            int pX = GetAbsoluteCoordinate(x, cX);
            int pY = GetAbsoluteCoordinate(y, cY);

            return new Point(screen.Bounds.Left + pX, screen.Bounds.Top + pY);

            int GetAbsoluteCoordinate(int pixelCoordinate, int screenResolution) => pixelCoordinate * 65536 / screenResolution + 1;
        }

        void GameThreadFunction()
        {
            try
            {
                Dictionary<Size, (BitImages, ProcessorHandler)> pcs = EventClickHandlers;
                ProcessorContainer req = ProcessorContainerFromSettings;
                HitCounter hc = _selectedProfileSettings.StartHitCounter;

                DoClickOperations(pcs);

                GameSession gameSession = null;

                while (true)
                {
                    int[,] sessionCopy = BuildField(req);

                    GameHandler(sessionCopy, ref gameSession, pcs, hc);

                    if (sessionCopy is null && !DoClickOperations(pcs))
                        break;
                }
            }
            catch (ThreadAbortException)
            {
                ResetAbort();
            }
            catch (Exception ex)
            {
                SafeExecute(() => MessageBox.Show(this, ex.Message, @"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error), true);
            }
            finally
            {
                GameBotThread = null;

                if (!ProgramStopFlag)
                {
                    BeginInvoke(new Action(() => SafeExecute(() =>
                    {
                        if (ProgramStopFlag)
                            return;

                        radNeedClick.Enabled = true;
                        radEmptySpace.Enabled = true;
                        radField_X.Enabled = true;
                        radField_O.Enabled = true;
                        btnSavePosition.Enabled = true;
                        btnGameStart.Text = _btnStartCaption;
                    })));
                }
            }
        }

        bool DoClickOperations(IDictionary<Size, (BitImages, ProcessorHandler)> ps)
        {
            if (ps is null)
                return false;

            Bitmap fullFrameNow = TakeScreenshot();

            if (fullFrameNow is null)
                return false;

            foreach (BitImages bi in _selectedProfileSettings.EventClicks)
            {
                Processor pq = new Processor(GetBitmapPiece(new Rectangle(bi.Coords, bi.FieldSize), fullFrameNow), @"Z");
                SearchResults sr = pq.GetEqual(new ProcessorContainer(ps[bi.FieldSize].Item2.Processors.ToArray()));

                if (sr[0, 0].Procs.All(p => p.Tag[0] != 'Z'))
                    continue;

                MouseClickMethods.Click(GetPhysicalCoords(bi.HitX, bi.HitY));

                return true;
            }

            return false;
        }

        Dictionary<Size, (BitImages, ProcessorHandler)> EventClickHandlers
        {
            get
            {
                Dictionary<Size, (BitImages, ProcessorHandler)> phs = new Dictionary<Size, (BitImages, ProcessorHandler)>();

                foreach (BitImages ec in _selectedProfileSettings.EventClicks)
                {
                    if (phs.TryGetValue(ec.FieldSize, out (BitImages, ProcessorHandler) phh))
                    {
                        phh.Item2.Add(ec.AsProcessor);
                        continue;
                    }

                    ProcessorHandler ph = new ProcessorHandler();
                    ph.Add(ec.AsProcessor);
                    phs.Add(ec.FieldSize, (ec, ph));
                }

                return phs;
            }
        }

        bool StopGameBotThread()
        {
            Thread rt = GameBotThread;

            if (rt is null)
                return false;

            rt.Abort();
            rt.Join();

            GameBotThread = null;

            if (ProgramStopFlag)
                return true;

            SafeExecute(() =>
            {
                radNeedClick.Enabled = true;
                radEmptySpace.Enabled = true;
                radField_X.Enabled = true;
                radField_O.Enabled = true;
                btnSavePosition.Enabled = true;
                btnGameStart.Text = _btnStartCaption;
            }, true);

            return true;
        }

        void BtnGameStart_Click(object sender, EventArgs e)
        {
            SafeExecute(() =>
            {
                if (StopGameBotThread())
                    return;

                if (!UpdateProfileStatus(false))
                    return;

                if (_needSaveProfile)
                {
                    string profileName = _selectedProfileSettings.ProfileName;

                    using (FrmName fn = new FrmName())
                    {
                        fn.MyTxtName = profileName;
                        if (fn.ShowDialog(this) == DialogResult.OK)
                            profileName = fn.MyTxtName;
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

                    SaveProfile();
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
