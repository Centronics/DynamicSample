using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Serialization;
using DynamicParser;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;
using Format = SharpDX.DXGI.Format;
using MapFlags = SharpDX.Direct3D11.MapFlags;
using Resource = SharpDX.DXGI.Resource;
using Processor = DynamicParser.Processor;
using BitImages = DynamicSample.FrmGameBot.SettingsProfilesArray.HitSettings.BitImages;
using HitCounter = DynamicSample.FrmGameBot.SettingsProfilesArray.HitSettings.HitCounter;
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
                    public Size FieldSize { get; set; }

                    public char Name { get; set; }

                    public Point Coords { get; set; }

                    public List<int> Data { get; set; } = new List<int>();

                    public int HitX => Coords.X + FieldSize.Width / 2;

                    public int HitY => Coords.Y + FieldSize.Height / 2;

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
        ///     Хранит значение свойства <see cref="RecognizerThread" />.
        /// </remarks>
        /// <seealso cref="RecognizerThread" />
        Thread _recognizerThread;

        Thread _escapeThread;

        bool _escapeThreadStopFlag;

        bool _needSaveProfile;

        int _firstHitX = -1, _firstHitY = -1;

        int _iAmXo = GameSession.EmptyHit;

        bool _isActived;

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
        Thread RecognizerThread
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

        bool EscapeThreadStopFlag
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

        public bool IsActived
        {
            get
            {
                lock (_commonLocker)
                {
                    return _isActived;
                }
            }

            set
            {
                lock (_commonLocker)
                {
                    _isActived = value;
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
                    Thread.Sleep(20); // захватчику экрана надо время проинициализироваться
                    Bitmap bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                    Resource screenResource = null;
                    try
                    {
                        if (duplicatedOutput.TryAcquireNextFrame(10, out OutputDuplicateFrameInformation _, out screenResource) != Result.Ok)
                            return bmp;

                        using (Texture2D screenTexture2D = screenResource.QueryInterface<Texture2D>())
                            device.ImmediateContext.CopyResource(screenTexture2D, screenTexture);

                        DataBox mapSource = device.ImmediateContext.MapSubresource(screenTexture, 0, MapMode.Read, MapFlags.None);
                        BitmapData bmpData = bmp.LockBits(new Rectangle(Point.Empty, bmp.Size), ImageLockMode.WriteOnly, bmp.PixelFormat);
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

        void EscapeThreadFunc()
        {
            try
            {
                while (!EscapeThreadStopFlag)
                {
                    while (!EscapeThreadStopFlag && NativeMethods.GetAsyncKeyState(Keys.Escape) == 0)
                        Thread.Sleep(50);

                    while (!EscapeThreadStopFlag && NativeMethods.GetAsyncKeyState(Keys.Escape) != 0)
                        Thread.Sleep(10);

                    if (EscapeThreadStopFlag || !(StopperThread is null))
                        continue;

                    Thread t = new Thread(() =>
                    {
                        try
                        {
                            if (!StopGameThread() && IsActived)
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

        void StartNewSession()
        {
            _firstHitX = -1;
            _firstHitY = -1;
            _iAmXo = GameSession.EmptyHit;
        }

        Bitmap CopyGameFieldFromScreen(out Bitmap origin)
        {
            Rectangle rect = new Rectangle();
            SafeExecute(() => rect = GameFieldRect, true);
            origin = TakeScreenshot();
            Bitmap b = new Bitmap(rect.Width, rect.Height);

            for (int x = rect.X, xto = 0; x < rect.Right; x++, xto++)
                for (int y = rect.Y, yto = 0; y < rect.Bottom; y++, yto++)
                    b.SetPixel(xto, yto, origin.GetPixel(x, y));

            return b;
        }

        void FrmGameSettings_Shown(object sender, EventArgs e)
        {
            pbScreenField.BackColor = Color.Red;
            //pbScreenField.ForeColor = Color.Red;
            TransparencyKey = Color.Red; // по умолчанию ЧЕРНЫЙ
            AllowTransparency = true;

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
            Bitmap b;

            try
            {
                Rectangle gfr = GameFieldRect;

                if (radNeedClick.Checked)
                {
                    BitImages bi = new BitImages
                    {
                        Data = GetBitmapAsInts(CopyGameFieldFromScreen(out b)),
                        Coords = gfr.Location,
                        FieldSize = gfr.Size,
                        Name = 'Z'
                    };

                    UpdateClicks(bi);
                    return;
                }

                if (radEmptySpace.Checked)
                {
                    BitImages bi = new BitImages
                    {
                        Data = GetBitmapAsInts(CopyGameFieldFromScreen(out b)),
                        Coords = gfr.Location,
                        FieldSize = gfr.Size,
                        Name = 'E'
                    };

                    _selectedProfileSettings.Spaces.Add(bi);
                    UpdateClicks();
                    return;
                }

                if (radField_X.Checked)
                {
                    BitImages bi = new BitImages
                    {
                        Data = GetBitmapAsInts(CopyGameFieldFromScreen(out b)),
                        Coords = gfr.Location,
                        FieldSize = gfr.Size,
                        Name = 'X'
                    };

                    _selectedProfileSettings.Spaces.Add(bi);
                    UpdateClicks();
                    return;
                }

                if (radField_O.Checked)
                {
                    BitImages bi = new BitImages
                    {
                        Data = GetBitmapAsInts(CopyGameFieldFromScreen(out b)),
                        Coords = gfr.Location,
                        FieldSize = gfr.Size,
                        Name = 'O'
                    };

                    _selectedProfileSettings.Spaces.Add(bi);
                    UpdateClicks();
                }
            }
            finally
            {
                UpdateProfileStatus(true);
            }

            return;

            void UpdateClicks(BitImages biAdd = null)
            {
                _needSaveProfile = true;

                List<BitImages> eventClicks = new List<BitImages>();
                ProcessorHandler ph = new ProcessorHandler();

                if (!(biAdd is null))
                {
                    Processor prAdd = new Processor(biAdd.AsBitmap, biAdd.Name.ToString());

                    int tc = ph.Processors.Count();

                    ph.Add(prAdd);

                    if (tc > ph.Processors.Count())
                        eventClicks.Add(biAdd);

                    return;
                }

                int tagsCount = ph.Processors.Count();

                foreach (BitImages biClicks in _selectedProfileSettings.EventClicks)
                {
                    Bitmap bt = GetBitmapPiece(new Rectangle(biClicks.Coords, biClicks.FieldSize), b);
                    const char pTag = 'E';

                    ph.Add(new Processor(bt, pTag.ToString()));

                    int tc = ph.Processors.Count();

                    if (tc <= tagsCount)
                        continue;

                    tagsCount = tc;

                    eventClicks.Add(new BitImages
                    {
                        Data = GetBitmapAsInts(bt),
                        Coords = biClicks.Coords,
                        FieldSize = biClicks.FieldSize,
                        Name = pTag
                    });
                }

                _selectedProfileSettings.EventClicks = eventClicks;
            }
        }

        static bool BitmapCompare(Bitmap btm1, Bitmap btm2)
        {
            if (btm1 is null || btm2 is null)
                return true;

            if (btm1.Width != btm2.Width)
                return false;
            if (btm1.Height != btm2.Height)
                return false;

            for (int y = 0; y < btm1.Height; y++)
                for (int x = 0; x < btm1.Width; x++)
                    if (btm1.GetPixel(x, y) != btm2.GetPixel(x, y))
                        return false;

            return true;
        }

        Bitmap GetFullFrameNow()
        {
            Bitmap lastFrame = null, lastFullFrame;

            int? timeout = null;

            while (true)
            {
                if (timeout.HasValue)
                    Thread.Sleep(timeout.Value);

                if (!IsFrameChanged())
                    return lastFullFrame;

                if (timeout.HasValue)
                    continue;

                SafeExecute(() =>
                {
                    timeout = int.TryParse(textBox1.Text, out int t) && t > 0 && t < 10000 ? t : 700;
                }, true);
            }

            bool IsFrameChanged()
            {
                Bitmap now = CopyGameFieldFromScreen(out lastFullFrame);

                if (lastFrame is null)
                {
                    lastFrame = now;
                    return true;
                }

                if (BitmapCompare(now, lastFrame))
                    return false;

                lastFrame = now;
                return true;
            }
        }

        //bool WaitWhilePlayingPaused()
        //{
        //    bool wait = false;

        //    while (!InGame)
        //    {
        //        wait = true;
        //        Thread.Sleep(1000);
        //    }

        //    return wait;
        //}

        static Bitmap GetBitmapPiece(Rectangle rect, Bitmap where)
        {
            Bitmap result = new Bitmap(rect.Width, rect.Height);

            for (int y = 0, ly = rect.Y; y < rect.Height; y++, ly++)
                for (int x = 0, lx = rect.X; x < rect.Width; x++, lx++)
                    result.SetPixel(x, y, where.GetPixel(lx, ly));

            return result;
        }

        (Point? userHitPoint, bool isEmpty) GetUserHitPoint(int[,] map1, ref GameSession map2)
        {
            if (_firstHitX < 0 || _firstHitY < 0)
            {
                if (map2.HitX > -1 && map2.HitY > -1)
                {
                    int iAmxo = map1[map2.HitX, map2.HitY];

                    if (iAmxo != GameSession.BotHit && iAmxo != GameSession.UserHit)
                        return (null, false);

                    _iAmXo = iAmxo;
                    _firstHitX = map2.HitX;
                    _firstHitY = map2.HitY;
                }
                else
                {
                    (int x, int y)? r = GameSession.GetAloneHit(map1);

                    if (r.HasValue)
                    {
                        map2 = new GameSession();
                        return (new Point(r.Value.x, r.Value.y), false);
                    }

                    map2 = new GameSession(map1);
                    return (null, true);
                }
            }
            else
            {
                int iAmxo = map1[_firstHitX, _firstHitY];

                if (iAmxo != GameSession.BotHit && iAmxo != GameSession.UserHit)
                    return (null, false);

                if (iAmxo != _iAmXo)
                    return (null, false);
            }

            bool mapIsEmpty = true;
            Point result = new Point();
            int diffCount = 0;

            for (int y = 0; y < 3; y++)
                for (int x = 0; x < 3; x++)
                {
                    int m1 = map1[x, y];
                    int m2 = map2[x, y];

                    if (m1 != GameSession.EmptyHit)
                        mapIsEmpty = false;

                    if (m1 == m2)
                        continue;

                    if (m1 == GameSession.EmptyHit)
                        return (null, false);

                    if (m1 != GameSession.EmptyHit && m2 != GameSession.EmptyHit)
                        return (null, false);

                    result = new Point(x, y);
                    ++diffCount;
                }

            if (diffCount < 1)
                return (null, mapIsEmpty);

            if (diffCount > 1)
                return (null, false);

            return (result, mapIsEmpty);
        }

        ProcessorContainer ProcessorContainerFromSettings => new ProcessorContainer(_selectedProfileSettings.Spaces.Select((bi, bx) => new Processor(bi.AsBitmap, $@"{bi.Name}{bx}")).ToArray());

        int[,] BuildField(ProcessorContainer req = null)
        {
            Bitmap fullFrameNow = GetFullFrameNow();

            if (fullFrameNow is null)
                return null;

            IEnumerable<Processor> pq1 = _selectedProfileSettings.Spaces.Select(bi => new Processor(GetBitmapPiece(new Rectangle(bi.Coords, bi.FieldSize), fullFrameNow), @"Z"));

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
                            throw new ArgumentException($@"Неоднозначность ({pps.Length}) => ({pps[0].Tag} <==> {pps[kp].Tag}), клетка номер {k} (с нуля).");
                        continue;
                    }

                    rTag = pps[kp].Tag[0];
                }

                int x = k % 3;
                int y = k / 3;

                switch (rTag)
                {
                    case 'X':
                        sessionCopy[x, y] = GameSession.UserHit;
                        break;

                    case 'O':
                        sessionCopy[x, y] = GameSession.BotHit;
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
            StartNewSession();
            GameSession gameSession = new GameSession();

            (BitImages, ProcessorContainer)[] pcs = GetProcessorHandlers();
            ProcessorContainer req = ProcessorContainerFromSettings;
            HitCounter hc = _selectedProfileSettings.StartHitCounter;
            Stopwatch timer = new Stopwatch();

            while (true)
            {
                try
                {
                    int[,] sessionCopy = BuildField(req);

                    if (sessionCopy is null)
                        break;

                    if (DoClickOperations(pcs))
                    {
                        GameSession.FixGameStep();
                        continue;
                    }

                    (Point? userHitPoint, bool isEmpty) = GetUserHitPoint(sessionCopy, ref gameSession);

                    if (isEmpty)
                    {
                        int myHit, myHitStart = hc.Counter;
                        bool hOk = false;

                        do
                        {
                            myHit = hc.Counter;
                            hc.Inc();

                            if (!gameSession.MakeTargetHit(myHit % 3, myHit / 3))
                                continue;

                            hOk = true;
                            break;
                        } while (myHit != myHitStart);

                        if (!hOk)
                        {
                            StartNewSession();
                            gameSession = new GameSession();
                            continue;
                        }

                        DoPhysicalHit(gameSession.HitX, gameSession.HitY);

                        int[,] bf = BuildField(req);

                        if (bf is null)
                            break;

                        GameSession.IsGameCompetitorsInverted = bf[gameSession.HitX, gameSession.HitY] == int.MaxValue;
                        gameSession.ActualizeLastHitValue();

                        switch (gameSession.CurrentWinner)
                        {
                            case GameSession.Winner.NOBODY:
                                break;
                            case GameSession.Winner.STANDOFF:
                            case GameSession.Winner.USER:
                            case GameSession.Winner.BOT:
                                DoClickOperations(pcs);
                                StartNewSession();
                                gameSession = new GameSession();
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }

                        continue;
                    }

                    if (!userHitPoint.HasValue)
                    {
                        switch (timer.IsRunning)
                        {
                            case true when timer.Elapsed.Seconds > 10:
                                timer.Stop();
                                DoClickOperations(pcs);
                                StartNewSession();
                                gameSession = new GameSession();
                                break;
                            case false:
                                timer.Restart();
                                break;
                        }

                        continue;
                    }

                    timer.Stop();

                    if (!gameSession.MakeCompetitorHit(userHitPoint.Value.X, userHitPoint.Value.Y))
                        throw new Exception($@"Что-то пошло не так ({userHitPoint.Value.X}, {userHitPoint.Value.Y}).");

                    if (gameSession.CurrentWinner == GameSession.Winner.NOBODY)
                    {
                        gameSession.MakeHitDecision();
                        DoPhysicalHit(gameSession.HitX, gameSession.HitY);

                        if (_iAmXo == GameSession.EmptyHit)
                        {
                            int[,] bf = BuildField(req);

                            if (bf is null)
                                break;

                            GameSession.IsGameCompetitorsInverted = bf[gameSession.HitX, gameSession.HitY] == int.MaxValue;
                            gameSession.ActualizeLastHitValue();
                        }
                    }

                    switch (gameSession.CurrentWinner)
                    {
                        case GameSession.Winner.NOBODY:
                            break;
                        case GameSession.Winner.STANDOFF:
                        case GameSession.Winner.USER:
                        case GameSession.Winner.BOT:
                            DoClickOperations(pcs);
                            StartNewSession();
                            gameSession = new GameSession();
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                catch (ThreadAbortException)
                {
                    // ignored
                }
                catch (Exception ex)
                {
                    SafeExecute(() => MessageBox.Show(this, ex.Message, @"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error), true);
                    break;
                }
            }

            return;

            bool DoClickOperations((BitImages, ProcessorContainer)[] ps)
            {
                Bitmap fullFrameNow = GetFullFrameNow();

                if (fullFrameNow is null)
                    return false;

                bool result = false;

                foreach ((BitImages bi, ProcessorContainer pc) in ps)
                {
                    Processor pq = new Processor(GetBitmapPiece(new Rectangle(bi.Coords, bi.FieldSize), fullFrameNow), @"Z");
                    SearchResults sr = pq.GetEqual(pc);

                    if (sr[0, 0].Procs.All(p => p.Tag[0] != 'Z'))
                        continue;

                    result = true;
                    MouseClickMethods.Click(new Point(bi.HitX, bi.HitY));
                    Thread.Sleep(1000);
                    fullFrameNow = GetFullFrameNow();
                }

                return result;
            }

            (BitImages, ProcessorContainer)[] GetProcessorHandlers()
            {
                List<(BitImages, ProcessorHandler)> result = new List<(BitImages, ProcessorHandler)>();
                Dictionary<Rectangle, ProcessorHandler> phs = new Dictionary<Rectangle, ProcessorHandler>();

                foreach (BitImages ec in _selectedProfileSettings.EventClicks)
                {
                    if (phs.TryGetValue(new Rectangle(ec.Coords, ec.FieldSize), out ProcessorHandler ph))
                    {
                        ph.Add(new Processor(ec.AsBitmap, ec.Name.ToString()));
                        continue;
                    }

                    ProcessorHandler ph1 = new ProcessorHandler();
                    ph1.Add(new Processor(ec.AsBitmap, ec.Name.ToString()));

                    phs.Add(new Rectangle(ec.Coords, ec.FieldSize), ph1);
                    result.Add((ec, ph1));
                }

                return result.Select(bp => (bp.Item1, new ProcessorContainer(bp.Item2.Processors.ToArray()))).ToArray();
            }
        }

        bool StopGameThread()
        {
            Thread rt = RecognizerThread;

            if (rt is null)
                return false;

            rt.Abort();
            rt.Join();

            if (EscapeThreadStopFlag)
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

            RecognizerThread = null;

            return true;
        }

        void BtnGameStart_Click(object sender, EventArgs e)
        {
            SafeExecute(() =>
            {
                if (StopGameThread())
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
                    IsBackground = true
                };

                t.Start();

                RecognizerThread = t;
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
                EscapeThreadStopFlag = true;
                StopGameThread();
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

        void TextBox1_TextChanged(object sender, EventArgs e) => _needSaveProfile = true;

        void FrmGameBot_Activated(object sender, EventArgs e) => IsActived = true;

        void FrmGameBot_Deactivate(object sender, EventArgs e) => IsActived = false;

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
