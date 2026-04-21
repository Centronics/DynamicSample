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
using System.Xml;
using System.Xml.Serialization;
using BitImages = DynamicSample.FrmGameBot.SettingsProfilesArray.HitSettings.BitImages;
using Device = SharpDX.Direct3D11.Device;
using Format = SharpDX.DXGI.Format;
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
                public sealed class BitImages
                {
                    public BitImages() => Logger.WriteLog(() => $@"{nameof(BitImages)}: Сериализация...", Logger.LogLevel.DEBUG);

                    public BitImages(BitImages bi)
                    {
                        if (bi is null)
                        {
                            Logger.WriteLog(() => $@"{nameof(BitImages)}: Конструктор копирования (по умолчанию).", Logger.LogLevel.DEBUG);
                            return;
                        }

                        Logger.WriteLog(() =>
                        {
                            StringBuilder sb = new StringBuilder();

                            sb.AppendLine(@"(Конструктор копирования)");
                            sb.AppendLine($@"{nameof(FieldSize)} = ({bi.FieldSize.Width}) x ({bi.FieldSize.Height})");
                            sb.AppendLine($@"{nameof(Name)} = {bi.Name}");
                            sb.AppendLine($@"{nameof(Coords)} = ({bi.Coords.X}, {bi.Coords.Y})");
                            sb.AppendLine(bi.Data is null
                                ? $@"{nameof(Data.Count)} = <null>"
                                : $@"{nameof(Data.Count)} = {bi.Data.Count}");

                            return $@"{nameof(BitImages)}: {sb}.";
                        }, Logger.LogLevel.DEBUG);

                        if (bi.Data is null)
                            throw new ArgumentNullException(nameof(bi), $@"{nameof(BitImages)}: Данные изображения должны быть заданы.");

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

                public override string ToString() => string.IsNullOrWhiteSpace(ProfileName) ? @"<<NONAME>>" : ProfileName;
            }

            public List<HitSettings> Profiles { get; set; } = new List<HitSettings>();

            public int SelectedProfileIndex { get; set; }

            [XmlIgnore]
            static string SettingsFilePath => $@"{Launcher.BaseFilePath}_{nameof(FrmGameBot)}Settings.xml";

            [XmlIgnore]
            public static SettingsProfilesArray CurrentProfileSettings
            {
                get
                {
                    try
                    {
                        XmlSerializer ser = new XmlSerializer(typeof(SettingsProfilesArray));
                        using (FileStream fs = new FileStream(SettingsFilePath, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete))
                        {
                            Logger.WriteLog(() => $@"{nameof(SettingsProfilesArray)}(get): {nameof(SettingsFilePath)} = {SettingsFilePath}.", Logger.LogLevel.DEBUG);

                            using (XmlReader xr = XmlReader.Create(fs))
                                return (SettingsProfilesArray)ser.Deserialize(xr);
                        }
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
                        using (FileStream fs = new FileStream(SettingsFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete))
                        {
                            Logger.WriteLog(() => $@"{nameof(SettingsProfilesArray)}(set): {nameof(SettingsFilePath)} = {SettingsFilePath}.", Logger.LogLevel.DEBUG);

                            using (XmlWriter xw = new XmlTextWriter(fs, Encoding.UTF8))
                                ser.Serialize(xw, value);
                        }
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
        Thread _gameBotThread;

        Thread _escapeThread;

        bool _escapeThreadStopFlag;

        bool _needSaveProfile;

        static volatile int _isBotActivated = 1;

        static bool IsBotActivated
        {
            get => _isBotActivated != 0;

            set
            {
                if (value)
                {
                    Interlocked.CompareExchange(ref _isBotActivated, 1, 0);
                    return;
                }

                Interlocked.CompareExchange(ref _isBotActivated, 0, 1);
            }
        }

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
        ///     Значение свойства содержит поле <see cref="_gameBotThread" />.
        /// </remarks>
        /// <seealso cref="_commonLocker" />
        /// <seealso cref="_gameBotThread" />
        Thread GameBotThread
        {
            get
            {
                lock (_commonLocker)
                {
                    return _gameBotThread;
                }
            }

            set
            {
                lock (_commonLocker)
                {
                    _gameBotThread = value;
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

        static SettingsProfilesArray _profileSettings;

        SettingsProfilesArray.HitSettings _selectedProfileSettings = new SettingsProfilesArray.HitSettings();

        public static void LoadProfilesFromFile() => _profileSettings = SettingsProfilesArray.CurrentProfileSettings;

        public static void SaveProfilesToFile() => SettingsProfilesArray.CurrentProfileSettings = _profileSettings;

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
            Logger.WriteLog(() => $@"{nameof(EscapeThreadFunc)}: Запущен поток обработки горячих клавиш.", Logger.LogLevel.DEBUG);

            try
            {
                while (!ProgramStopFlag)
                {
                    while (!ProgramStopFlag && NativeMethods.GetAsyncKeyState(Keys.Escape) == 0)
                        Thread.Sleep(50);

                    while (!ProgramStopFlag && NativeMethods.GetAsyncKeyState(Keys.Escape) != 0)
                        Thread.Sleep(10);

                    if (ProgramStopFlag)
                    {
                        Logger.WriteLog(() => $@"{nameof(EscapeThreadFunc)}: Закрываю поток ({Thread.CurrentThread.Name})...", Logger.LogLevel.DEBUG);
                        continue;
                    }

                    if (!(StopperThread is null))
                    {
                        Logger.WriteLog(() => $@"{nameof(EscapeThreadFunc)}: Выход из игры (бота) уже в процессе...", Logger.LogLevel.DEBUG);
                        continue;
                    }

                    Logger.WriteLog(() => $@"{nameof(EscapeThreadFunc)}: Запускаю выход из игры (бота)...", Logger.LogLevel.DEBUG);

                    Thread t = new Thread(() =>
                    {
                        try
                        {
                            if (StopGameBotThread())
                            {
                                Logger.WriteLog(() => $@"{nameof(EscapeThreadFunc)}: Игра (бот) остановлена клавишей ESC.");
                                return;
                            }

                            Logger.WriteLog(() => $@"{nameof(EscapeThreadFunc)}: Выхожу из игры по клавише ESC ({Keys.Escape})...", Logger.LogLevel.DEBUG);

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
                            Logger.WriteLog(() => $@"{nameof(EscapeThreadFunc)}: Вышел из игры по клавише ESC ({Keys.Escape}).", Logger.LogLevel.DEBUG);
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

            Logger.WriteLog(() => $@"{nameof(EscapeThreadFunc)}: Поток обработки горячих клавиш остановлен.", Logger.LogLevel.DEBUG);
        }

        void AddProfile()
        {
            Logger.WriteLog(() => $@"{nameof(AddProfile)}: Добавляю новый профиль настроек: ""{_selectedProfileSettings}"". Всего профилей {_profileSettings.Profiles.Count}.");

            for (int k = 0; k < _profileSettings.Profiles.Count; k++)
            {
                SettingsProfilesArray.HitSettings profile = _profileSettings.Profiles[k];

                if (string.Compare(_selectedProfileSettings.ToString(), profile.ToString(), StringComparison.OrdinalIgnoreCase) != 0)
                {
                    int k1 = k;
                    Logger.WriteLog(() => $@"{nameof(AddProfile)}: Профили настроек различаются (""{_selectedProfileSettings}"" и ""{profile}""), номер {k1}. Продолжаю поиск по имени...", Logger.LogLevel.DEBUG);
                    continue;
                }

                Logger.WriteLog(() => $@"{nameof(AddProfile)}: Профили настроек называются одинаково, без учёта регистра (""{_selectedProfileSettings}"" и ""{profile}""), номер {k}. Перезаписываю найденный профиль...", Logger.LogLevel.DEBUG);

                _profileSettings.Profiles.RemoveAt(k);
                _profileSettings.Profiles.Insert(k, _selectedProfileSettings);
                _profileSettings.SelectedProfileIndex = k;
                cbxProfiles.SelectedIndex = k + 1;
                _needSaveProfile = false;

                Logger.WriteLog(() => $@"{nameof(AddProfile)}: Профиль (""{_selectedProfileSettings}"") успешно перезаписан.");

                return;
            }

            Logger.WriteLog(() => $@"{nameof(AddProfile)}: Добавляю новый профиль настроек: ""{_selectedProfileSettings}"".", Logger.LogLevel.DEBUG);

            _profileSettings.Profiles.Insert(0, _selectedProfileSettings);
            _profileSettings.SelectedProfileIndex = 0;
            cbxProfiles.Items.Insert(1, _selectedProfileSettings);
            cbxProfiles.SelectedIndex = 1;
            _needSaveProfile = false;

            Logger.WriteLog(() => $@"{nameof(AddProfile)}: Добавлен новый профиль настроек: ""{_selectedProfileSettings}"".");
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

        void FrmGameBot_Shown(object sender, EventArgs e)
        {
            Logger.WriteLog(() => $@"{nameof(FrmGameBot_Shown)}: Начало работы в режиме ""бот"".");

            try
            {
                TransparencyKey = pbScreenField.BackColor = Color.Red;

                _escapeThread = new Thread(EscapeThreadFunc)
                {
                    IsBackground = true,
                    Name = @"EscapeThread"
                };
                _escapeThread.Start();

                Logger.WriteLog(() => $@"{nameof(FrmGameBot_Shown)}: Запущен поток {_escapeThread.Name}.", Logger.LogLevel.DEBUG);

                _btnStartCaption = btnGameStart.Text;

                cbxProfiles.Items.AddRange(GetProfileStrings().ToArray());

                if (!_profileSettings.Profiles.Any())
                {
                    Logger.WriteLog(() => $@"{nameof(FrmGameBot_Shown)}: Пользовательский интерфейс готов к работе. Профилей настроек нет.");

                    _profileSettings.SelectedProfileIndex = -1;
                    cbxProfiles.SelectedIndex = 0;
                    return;
                }

                Logger.WriteLog(() => $@"{nameof(FrmGameBot_Shown)}: Пользовательский интерфейс готов к работе. Найдено профилей настроек: {_profileSettings.Profiles.Count}.");

                if (_profileSettings.SelectedProfileIndex > -1 &&
                    _profileSettings.SelectedProfileIndex < _profileSettings.Profiles.Count)
                {
                    Logger.WriteLog(() => $@"{nameof(FrmGameBot_Shown)}: Выбран профиль #{_profileSettings.SelectedProfileIndex}.", Logger.LogLevel.DEBUG);

                    _selectedProfileSettings = _profileSettings.Profiles[_profileSettings.SelectedProfileIndex];
                    cbxProfiles.SelectedIndex = _profileSettings.SelectedProfileIndex + 1;

                    Logger.WriteLog(() => $@"{nameof(FrmGameBot_Shown)}: Выбран профиль ""{_selectedProfileSettings}"".");
                }
                else
                {
                    if (_profileSettings.SelectedProfileIndex < 0)
                    {
                        Logger.WriteLog(() => $@"{nameof(FrmGameBot_Shown)}: Выбрано создание нового профиля #{_profileSettings.SelectedProfileIndex}.", Logger.LogLevel.DEBUG);

                        _selectedProfileSettings = new SettingsProfilesArray.HitSettings();
                        _profileSettings.SelectedProfileIndex = -1;
                        cbxProfiles.SelectedIndex = 0;

                        Logger.WriteLog(() => $@"{nameof(FrmGameBot_Shown)}: Новый профиль создан ({_selectedProfileSettings}).");
                    }
                    else
                    {
                        Logger.WriteLog(() => $@"{nameof(FrmGameBot_Shown)}: Выбран профиль (по умолчанию) #{_profileSettings.SelectedProfileIndex}.", Logger.LogLevel.ERROR);

                        _selectedProfileSettings = _profileSettings.Profiles[0];
                        _profileSettings.SelectedProfileIndex = 0;
                        cbxProfiles.SelectedIndex = 1;

                        Logger.WriteLog(() => $@"{nameof(FrmGameBot_Shown)}: Выбран профиль (по умолчанию) ""{_selectedProfileSettings}"".", Logger.LogLevel.ERROR);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(() => $@"{nameof(FrmGameBot_Shown)}: Исключение ""{ex.Message}""", Logger.LogLevel.ERROR);
                throw;
            }

            Logger.WriteLog(() => $@"{nameof(FrmGameBot_Shown)}: Пользовательский интерфейс готов к работе.", Logger.LogLevel.DEBUG);

            return;

            IEnumerable<object> GetProfileStrings()
            {
                foreach (SettingsProfilesArray.HitSettings pf in _profileSettings.Profiles)
                {
                    Logger.WriteLog(() => $@"{nameof(FrmGameBot_Shown)}: Загружен профиль {pf}.", Logger.LogLevel.DEBUG);
                    yield return pf;
                }
            }
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

                Logger.WriteLog(() => $@"{nameof(UpdateProfileStatus)}: {message}{Environment.NewLine}Не показывать сообщение пользователю = {silent}.");

                if (silent)
                    return false;

                MessageBox.Show(this, message);
                return false;
            }

            if (_selectedProfileSettings.Spaces.All(m => m.Name != 'O'))
            {
                btnGameStart.Enabled = false;

                const string message = @"Отсутствуют обозначения ноликов (O). Создайте новый профиль.";

                Logger.WriteLog(() => $@"{nameof(UpdateProfileStatus)}: {message}{Environment.NewLine}Не показывать сообщение пользователю = {silent}.");

                if (silent)
                    return false;

                MessageBox.Show(this, message);
                return false;
            }

            if (_selectedProfileSettings.Spaces.All(m => m.Name != 'E'))
            {
                btnGameStart.Enabled = false;

                const string message = @"Отсутствуют обозначения пустых мест. Создайте новый профиль.";

                Logger.WriteLog(() => $@"{nameof(UpdateProfileStatus)}: {message}{Environment.NewLine}Не показывать сообщение пользователю = {silent}.");

                if (silent)
                    return false;

                MessageBox.Show(this, message);
                return false;
            }

            int spacesCount = _selectedProfileSettings.Spaces.Count;

            if (spacesCount < 9)
            {
                btnGameStart.Enabled = false;

                string message = $@"Недостаточно обозначений мест ударов. Сейчас их {spacesCount}, а должно быть 9.";

                Logger.WriteLog(() => $@"{nameof(UpdateProfileStatus)}: {message}{Environment.NewLine}Не показывать сообщение пользователю = {silent}.");

                if (silent)
                    return false;

                MessageBox.Show(this, message);
                return false;
            }

            if (spacesCount > 9)
            {
                btnGameStart.Enabled = false;

                string message = $@"Мест ударов меньше, чем создано. Сейчас их {spacesCount}, а должно быть 9. Создайте профиль заново.";

                Logger.WriteLog(() => $@"{nameof(UpdateProfileStatus)}: {message}{Environment.NewLine}Не показывать сообщение пользователю = {silent}.");

                MessageBox.Show(this, message);
                return false;
            }

            try
            {
                if (BuildField() is null)
                {
                    btnGameStart.Enabled = false;

                    const string message = @"Процесс компиляции завершился сбоем.";

                    Logger.WriteLog(() => $@"{nameof(UpdateProfileStatus)}: {message}{Environment.NewLine}Не показывать сообщение пользователю = {silent}.");

                    MessageBox.Show(this, message);
                    return false;
                }
            }
            catch (Exception ex)
            {
                btnGameStart.Enabled = false;

                string message = $@"Процесс компиляции сборки завершился сбоем.{Environment.NewLine}Текст ошибки: {ex.Message}.";

                Logger.WriteLog(() => $@"{nameof(UpdateProfileStatus)}: {message}{Environment.NewLine}Не показывать сообщение пользователю = {silent}.");

                MessageBox.Show(this, message);
                return false;
            }

            Logger.WriteLog(() => $@"{nameof(UpdateProfileStatus)}: Успех. Не показывать сообщение пользователю = {silent}.");

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
                    if (_selectedProfileSettings.Spaces.Count >= 9)
                    {
                        _selectedProfileSettings.Spaces.Clear();
                        _needSaveProfile = true;
                    }

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
                    if (_selectedProfileSettings.Spaces.Count >= 9)
                    {
                        _selectedProfileSettings.Spaces.Clear();
                        _needSaveProfile = true;
                    }

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
                {
                    const string m = @"Неизвестно, какой параметр выбран.";
                    Logger.WriteLog(() => $@"{nameof(BtnSavePosition_Click)}: {m}", Logger.LogLevel.ERROR);
                    throw new Exception(m);
                }

                if (_selectedProfileSettings.Spaces.Count >= 9)
                {
                    _selectedProfileSettings.Spaces.Clear();
                    _needSaveProfile = true;
                }

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

        void GameHandler(GameSession.GameFieldHit[,] sessionCopy, GameSession gameSession, Dictionary<Size, (BitImages, ProcessorHandler)> pcs)
        {
            #region Block1

            Point hp = new Point();
            int diffCount = 0;
            bool noMyHit = false;

            for (int y = 0; y < 3; y++)
                for (int x = 0; x < 3; x++)
                {
                    GameSession.GameFieldHit m1 = sessionCopy[x, y];
                    GameSession.GameFieldHit m2 = gameSession[x, y];

                    #region Block1

                    if (m1 == m2)
                        continue;

                    #endregion

                    #region Block2

                    if (m1 == GameSession.GameFieldHit.EMPTY && m2 != GameSession.GameFieldHit.EMPTY)
                    {
                        #region Block1

                        if (x == gameSession.HitX && y == gameSession.HitY)
                        {
                            Logger.WriteLog(() => $@"{nameof(GameHandler)}: Игра продолжается... Ждём отображения моего хода на экране ({gameSession.HitX}, {gameSession.HitY}).", Logger.LogLevel.DEBUG);
                            noMyHit = true;
                            continue;
                        }

                        #endregion

                        #region Block2

                        GameSession.FieldState cfs = gameSession.CurrentFieldState;

                        if (cfs == GameSession.FieldState.FULL || cfs == GameSession.FieldState.ERROR)
                        {
                            if (!DoClickActions(pcs))
                                throw new Exception($@"{nameof(GameHandler)}(1): Не удалось перезапустить игровую сессию...");

                            gameSession.Reset();
                            GameSession.ResetGameData();
                            return;
                        }

                        #endregion

                        #region Block3

                        Logger.WriteLog(() => $@"{nameof(GameHandler)}: Странная ситуация - найден какое-то поле на экране ({x}, {y}), которое должно быть занято, но оно пусто ({m2}).", Logger.LogLevel.DEBUG);

                        gameSession.FixGameStep();
                        gameSession.Reset();
                        GameSession.ResetGameData();

                        #endregion

                        #region Block4

                        if (!DoClickActions(pcs))
                            throw new Exception($@"{nameof(GameHandler)}(2): Не удалось перезапустить игровую сессию...");

                        Logger.WriteLog(() => $@"{nameof(GameHandler)}: Действие (клик) совершено.", Logger.LogLevel.DEBUG);
                        return;

                        #endregion
                    }

                    #endregion

                    #region Block3

                    if (m1 != GameSession.GameFieldHit.EMPTY && m2 != GameSession.GameFieldHit.EMPTY)
                    {
                        #region Block1

                        Logger.WriteLog(() =>
                        {
                            string xy = $@"({nameof(x)} = {x}, {nameof(y)} = {y}) => ({m1}, {m2})";
                            return $@"{nameof(GameHandler)}: Необходимо уточнить содержимое игрового поля {xy}.";
                        }, Logger.LogLevel.DEBUG);

                        GameSession.FieldState cfs = gameSession.CurrentFieldState;

                        Logger.WriteLog(() => $@"{nameof(GameHandler)}: {nameof(GameSession.FieldState)} = {cfs}.", Logger.LogLevel.DEBUG);

                        if (cfs == GameSession.FieldState.FULL || cfs == GameSession.FieldState.ERROR)
                        {
                            if (!DoClickActions(pcs))
                                throw new Exception($@"{nameof(GameHandler)}(3): Не удалось перезапустить игровую сессию...");

                            gameSession.Reset();
                            GameSession.ResetGameData();
                            return;
                        }

                        #endregion

                        #region Block2

                        if (x == gameSession.HitX && y == gameSession.HitY)
                        {
                            if (!GameSession.IsGameCompetitorsInverted)
                            {
                                Logger.WriteLog(() => $@"{nameof(GameHandler)}: Выполняю коррекцию своей стороны.", Logger.LogLevel.DEBUG);
                                GameSession.IsGameCompetitorsInverted = true;
                                return;
                            }

                            Logger.WriteLog(() => $@"{nameof(GameHandler)}: Игра продолжается... Инвертировал интерпретацию со своей стороны, но это не помогло.", Logger.LogLevel.ERROR);
                        }
                        else
                            Logger.WriteLog(() =>
                            {
                                string xy = $@"({nameof(x)} = {x}, {nameof(y)} = {y}) => ({m1}, {m2})";
                                return $@"{nameof(GameHandler)}: При поиске изменений на игровом поле, была найдена несостыковка {xy}.";
                            }, Logger.LogLevel.ERROR);

                        #endregion

                        #region Block3

                        Logger.WriteLog(() =>
                        {
                            string st0 = $@"{nameof(GameHandler)}: ";
                            const string st1 = @"Игра продолжается... Заметил разницу в картах, применяю карту с экрана.";
                            const string st2 = @"Поступившая карта ->";
                            string mx = GameSession.ArrayVisualize(sessionCopy);
                            const string st3 = @"Моя карта ->";

                            return $@"{st0}{st1}{Environment.NewLine}{st2}{Environment.NewLine}{mx}{Environment.NewLine}{st3}{Environment.NewLine}{gameSession}.";
                        }, Logger.LogLevel.DEBUG);

                        gameSession.FixGameStep();
                        gameSession.Reset();
                        GameSession.ResetGameData();

                        #endregion

                        #region Block4

                        if (!DoClickActions(pcs))
                            throw new Exception($@"{nameof(GameHandler)}: Игра останавливается по причине неизвестной ошибки! Сброс игры не удался! Прекращаю игру!");

                        Logger.WriteLog(() => $@"{nameof(GameHandler)}: Игра продолжается... Надо было закрыть всплывающее окно...", Logger.LogLevel.DEBUG);
                        return;

                        #endregion
                    }

                    #endregion

                    #region Block4

                    hp = new Point(x, y);
                    ++diffCount;

                    Point hp1 = hp;
                    int dc = diffCount;
                    Logger.WriteLog(() => $@"{nameof(GameHandler)}: Найдена точка удара соперника ({hp1.X}, {hp1.Y}). Количество найденных изменений {dc}.", Logger.LogLevel.DEBUG);

                    #endregion
                }

            #endregion

            #region Block2

            if (diffCount < 1)
            {
                Logger.WriteLog(() => $@"{nameof(GameHandler)}: Количество отличий должно быть равно одному, а не ({diffCount}).", Logger.LogLevel.DEBUG);

                GameSession.FieldState cfs = gameSession.CurrentFieldState;

                Logger.WriteLog(() => $@"{nameof(GameHandler)}: {nameof(cfs)} = {cfs}.", Logger.LogLevel.DEBUG);

                switch (cfs)
                {
                    #region Block1

                    case GameSession.FieldState.EMPTY:
                        Logger.WriteLog(() => $@"{nameof(GameHandler)}: Игра начинается... Вижу, что поле пустое, и наношу первый удар.", Logger.LogLevel.DEBUG);

                        GameSession.ResetGameData();

                        (int gx, int gy) = gameSession.MakeHitDecision();

                        Logger.WriteLog(() => $@"{nameof(GameHandler)}: Планируется нанести первый удар ({gx}, {gy}).", Logger.LogLevel.DEBUG);

                        if (!gameSession.MakeBotHit(gx, gy))
                            throw new Exception($@"{nameof(GameHandler)}: Странное решение (1) ({gx}, {gy}).");

                        DoPhysicalHit(gx, gy);

                        Logger.WriteLog(() => $@"{nameof(GameHandler)}: Первый удар нанесён ({gx}, {gy}).", Logger.LogLevel.DEBUG);

                        return;

                    #endregion

                    #region Block2

                    case GameSession.FieldState.WAITHIT:
                        throw new Exception();

                    case GameSession.FieldState.WAITUSERHIT:
                    case GameSession.FieldState.WAITBOTHIT:
                        Logger.WriteLog(() => $@"{nameof(GameHandler)}: Игра продолжается... Статус игры ({cfs}).", Logger.LogLevel.DEBUG);

                        if (noMyHit)
                        {
                            if (cfs != GameSession.FieldState.WAITUSERHIT)
                                throw new Exception();

                            Logger.WriteLog(() => $@"{nameof(GameHandler)}: Не вижу свой удар, попробую сделать его снова.", Logger.LogLevel.DEBUG);
                            DoPhysicalHit(gameSession.HitX, gameSession.HitY);
                            break;
                        }

                        if (cfs != GameSession.FieldState.WAITBOTHIT)
                        {
                            Logger.WriteLog(() => $@"{nameof(GameHandler)}: Игра в состоянии, когда она не ждёт удара бота, поэтому делать ничего не буду.", Logger.LogLevel.DEBUG);
                            break;
                        }

                        Logger.WriteLog(() => $@"{nameof(GameHandler)}: Игра продолжается... Отменяю своё решение сделать ход ({gameSession.HitX}, {gameSession.HitY}).", Logger.LogLevel.DEBUG);

                        gameSession.CancelHit();

                        (int dx, int dy) = gameSession.MakeHitDecision();

                        Logger.WriteLog(() => $@"{nameof(GameHandler)}: Сформировано решение по ответному удару ({dx}, {dy}).", Logger.LogLevel.DEBUG);

                        if (!gameSession.MakeBotHit(dx, dy))
                            throw new Exception($@"{nameof(GameHandler)}: Странное решение (2) ({dx}, {dy}).");

                        DoPhysicalHit(dx, dy);

                        GameSession.FieldState cfs1 = gameSession.CurrentFieldState;

                        if (cfs1 != GameSession.FieldState.FULL && cfs1 != GameSession.FieldState.ERROR)
                        {
                            Logger.WriteLog(() => $@"{nameof(GameHandler)}: Игра продолжается... Я нанёс удар ({dx}, {dy}). Статус игры ({cfs1}).", Logger.LogLevel.DEBUG);
                            return;
                        }

                        if (!DoClickActions(pcs))
                            throw new Exception($@"{nameof(GameHandler)}(4): Не удалось перезапустить игровую сессию...");

                        gameSession.Reset();
                        GameSession.ResetGameData();

                        Logger.WriteLog(() => $@"{nameof(GameHandler)}: Возникла неизвестная ошибка. Начинаю игру заново. Статус игры ({cfs1}).", Logger.LogLevel.ERROR);

                        break;

                    #endregion

                    #region Block3

                    case GameSession.FieldState.FULL:
                        Logger.WriteLog(() => $@"{nameof(GameHandler)}: Игра завершается... Статус игры ({cfs}).", Logger.LogLevel.DEBUG);

                        if (noMyHit && GameSession.GetCurrentFieldStateEx(sessionCopy) != GameSession.FieldState.FULL)
                        {
                            DoPhysicalHit(gameSession.HitX, gameSession.HitY);
                            return;
                        }

                        if (!DoClickActions(pcs))
                            throw new Exception($@"{nameof(GameHandler)}(5): Не удалось перезапустить игровую сессию...");

                        gameSession.Reset();
                        GameSession.ResetGameData();
                        return;

                    case GameSession.FieldState.ERROR:
                        throw new Exception($@"{nameof(GameHandler)}: Игра завершена с ошибкой! Значение ({cfs}).");

                    #endregion

                    #region Block4

                    default:
                        Logger.WriteLog(() => $@"{nameof(GameHandler)}: Игра продолжается... Неизвестное значение ({cfs}).", Logger.LogLevel.ERROR);
                        throw new ArgumentOutOfRangeException(nameof(cfs), $@"Неизвестное значение ({cfs}).");

                        #endregion
                }

                return;
            }

            #endregion

            #region Block3

            if (diffCount == 1)
            {
                #region Block1

                if (noMyHit)
                {
                    Logger.WriteLog(() => $@"{nameof(GameHandler)}: Вижу ход соперника, но не вижу свой удар ({gameSession.HitX}, {gameSession.HitY}), поэтому отменяю решение о нём.{Environment.NewLine}Карта на экране:{Environment.NewLine}{GameSession.ArrayVisualize(sessionCopy)}{Environment.NewLine}Моя карта:{Environment.NewLine}{gameSession}.", Logger.LogLevel.DEBUG);
                    gameSession.CancelHit();
                }

                Logger.WriteLog(() => $@"{nameof(GameHandler)}: Ход соперника определён ({hp.X}, {hp.Y}). Наношу удар.", Logger.LogLevel.DEBUG);

                if (!gameSession.MakeUserHit(hp.X, hp.Y))
                    throw new Exception($@"{nameof(GameHandler)}: Что-то пошло не так ({hp.X}, {hp.Y}).");

                #endregion

                #region Block2

                GameSession.FieldState cfs = gameSession.CurrentFieldState;

                if (cfs == GameSession.FieldState.FULL || cfs == GameSession.FieldState.ERROR)
                {
                    if (cfs == GameSession.FieldState.FULL)
                        Logger.WriteLog(() => $@"{nameof(GameHandler)}: Соперник нанёс удар ({hp.X}, {hp.Y}) ({cfs}). Партия завершена.", Logger.LogLevel.DEBUG);
                    else
                        Logger.WriteLog(() => $@"{nameof(GameHandler)}: Ход соперника ({hp.X}, {hp.Y}) вызвал ошибку ({cfs}), попробую перезапустить игру...", Logger.LogLevel.ERROR);

                    if (!DoClickActions(pcs))
                        throw new Exception($@"{nameof(GameHandler)}(6): Не удалось перезапустить игровую сессию...");

                    gameSession.Reset();
                    GameSession.ResetGameData();
                    return;
                }

                #endregion

                #region Block3

                (int dx, int dy) = gameSession.MakeHitDecision();

                Logger.WriteLog(() => $@"{nameof(GameHandler)}: Принято решение нанести ответный удар ({dx}, {dy}).", Logger.LogLevel.DEBUG);

                if (!gameSession.MakeBotHit(dx, dy))
                    throw new Exception($@"{nameof(GameHandler)}: Странное решение (3) ({dx}, {dy}).");

                DoPhysicalHit(dx, dy);

                #endregion

                #region Block4

                cfs = gameSession.CurrentFieldState;

                if (cfs != GameSession.FieldState.FULL && cfs != GameSession.FieldState.ERROR)
                {
                    Logger.WriteLog(() => $@"{nameof(GameHandler)}: Ход сделан {cfs}: ({dx}, {dy}), игра продолжается.", Logger.LogLevel.DEBUG);
                    return;
                }

                Logger.WriteLog(() => $@"{nameof(GameHandler)}: Перезапускаю игровую сессию ({cfs})...", Logger.LogLevel.ERROR);

                if (!DoClickActions(pcs))
                    throw new Exception($@"{nameof(GameHandler)}(7): Не удалось перезапустить игровую сессию...");

                gameSession.Reset();
                GameSession.ResetGameData();

                Logger.WriteLog(() => $@"{nameof(GameHandler)}: Игровая сессия была перезапущена из-за ошибки.", Logger.LogLevel.ERROR);

                return;

                #endregion
            }

            #endregion

            #region Block4

            Logger.WriteLog(() => $@"{nameof(GameHandler)}: Не удалось определить ход соперника. Действие (клик) не требуется. Количество отличий ({diffCount}).", Logger.LogLevel.DEBUG);

            if (noMyHit)
            {
                Logger.WriteLog(() => $@"{nameof(GameHandler)}: Отменяю свой планируемый удар ({gameSession.HitX}, {gameSession.HitY}).", Logger.LogLevel.DEBUG);
                gameSession.CancelHit();
            }

            gameSession.FixGameStep();
            gameSession.Reset();
            GameSession.ResetGameData();

            if (!DoClickActions(pcs))
                throw new Exception($@"{nameof(GameHandler)}(8): Не удалось перезапустить игровую сессию...");

            Logger.WriteLog(() => $@"{nameof(GameHandler)}: Не удалось определить ход соперника. Действие (клик) совершено. Количество отличий ({diffCount}).", Logger.LogLevel.DEBUG);

            #endregion
        }

        ProcessorContainer ProcessorContainerFromSettings => new ProcessorContainer(_selectedProfileSettings.Spaces.Select((bi, bx) =>
        {
            Processor p = new Processor(bi.AsBitmap, $@"{bi.Name}{bx}");

            Logger.WriteLog(() => $@"{nameof(ProcessorContainerFromSettings)}: Искомый элемент игрового поля создан ({p.Tag}: {p.Width}, {p.Height}).", Logger.LogLevel.DEBUG);

            return p;
        }).ToArray());

        GameSession.GameFieldHit[,] BuildField(ProcessorContainer req = null)
        {
            #region Block1

            Bitmap fullFrameNow = TakeScreenshot();

            if (fullFrameNow is null)
            {
                Logger.WriteLog(() => $@"{nameof(BuildField)}: Ошибка при построении игрового поля.", Logger.LogLevel.ERROR);
                return null;
            }

            #endregion

            #region Block2

            IEnumerable<Processor> pFromScreen = _selectedProfileSettings.Spaces.Select(bi =>
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

            List<SearchResults> results = new List<SearchResults>(pFromScreen.Select(p => p.GetEqual(req)));

            if (results.Count != 9)
                throw new InvalidOperationException($@"{nameof(BuildField)}: {nameof(results)} не равно 9: {results.Count}.");

            GameSession.GameFieldHit[,] sessionCopy = new GameSession.GameFieldHit[3, 3];

            #endregion

            for (int k = 0; k < 9; k++)
            {
                #region Block3

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

                #endregion

                #region Block4

                int x = k % 3;
                int y = k / 3;

                switch (rTag)
                {
                    case 'X':
                        sessionCopy[x, y] = GameSession.IsGameCompetitorsInverted ? GameSession.GameFieldHit.BOT : GameSession.GameFieldHit.USER;
                        break;

                    case 'O':
                        sessionCopy[x, y] = GameSession.IsGameCompetitorsInverted ? GameSession.GameFieldHit.USER : GameSession.GameFieldHit.BOT;
                        break;

                    case 'E':
                        sessionCopy[x, y] = GameSession.GameFieldHit.EMPTY;
                        break;

                    default:
                        throw new Exception($@"{nameof(BuildField)}: Неизвестная карта ({rTag}).");
                }

                #endregion
            }

            Logger.WriteLog(() => $@"{nameof(BuildField)}: Игровое поле собрано ->{Environment.NewLine}{GameSession.ArrayVisualize(sessionCopy)}.", Logger.LogLevel.DEBUG);

            return sessionCopy;
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
                Logger.WriteLog(() => $@"{nameof(GameThreadFunction)}: Запуск бота...", Logger.LogLevel.DEBUG);

                Dictionary<Size, (BitImages, ProcessorHandler)> pcs = GetEventClickHandlers();
                ProcessorContainer req = ProcessorContainerFromSettings;
                GameSession gameSession = new GameSession();
                GameSession.ResetGameData();

                Logger.WriteLog(() => $@"{nameof(GameThreadFunction)}: Бот запущен.");

                if (DoClickActions(pcs))
                    Logger.WriteLog(() => $@"{nameof(GameThreadFunction)}: Нажимаю сброс перед началом игры.", Logger.LogLevel.DEBUG);
                else
                    Logger.WriteLog(() => $@"{nameof(GameThreadFunction)}: Нажимать сброс перед началом игры не требуется.", Logger.LogLevel.DEBUG);

                while (IsBotActivated)
                {
                    #region Block1

                    GameSession.GameFieldHit[,] sessionCopy = BuildField(req);

                    #endregion

                    #region Block2

                    if (!(sessionCopy is null))
                    {
                        GameHandler(sessionCopy, gameSession, pcs);
                        continue;
                    }

                    #endregion

                    #region Block3

                    if (DoClickActions(pcs))
                        continue;

                    #endregion

                    #region Block4

                    Logger.WriteLog(() => $@"{nameof(GameThreadFunction)}: Не могу обработать ситуацию.", Logger.LogLevel.ERROR);
                    throw new Exception(@"Не могу обработать ситуацию.");

                    #endregion
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(() => $@"{nameof(GameThreadFunction)}: {ex.Message}", Logger.LogLevel.ERROR);
                SafeExecute(() => MessageBox.Show(this, $@"Возникла ошибка. Бот будет остановлен.{Environment.NewLine}См. лог.", @"Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error), true);
            }
            finally
            {
                try
                {
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
            #region Block1

            if (ps is null)
            {
                Logger.WriteLog(() => $@"{nameof(DoClickActions)}: Контейнер равен null.", Logger.LogLevel.ERROR);
                return false;
            }

            if (!ps.Any())
            {
                Logger.WriteLog(() => $@"{nameof(DoClickActions)}: Контейнер пустой.", Logger.LogLevel.DEBUG);
                return false;
            }

            #endregion

            #region Block2

            Bitmap fullFrameNow = TakeScreenshot();

            #endregion

            #region Block3

            if (fullFrameNow is null)
            {
                Logger.WriteLog(() => $@"{nameof(DoClickActions)}: Не могу получить снимок экрана.", Logger.LogLevel.ERROR);
                return false;
            }

            #endregion

            #region Block4

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

            #endregion
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

            IsBotActivated = false;

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

                    IsBotActivated = true;

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
            Logger.WriteLog(() => $@"{nameof(FrmGameBot_FormClosing)}: Завершение работы программы в режиме ""бот""...", Logger.LogLevel.DEBUG);

            if (!Save(SaveProfilesToFile))
            {
                Logger.WriteLog(() => $@"{nameof(FrmGameBot_FormClosing)}: Произошла ошибка при сохранении профилей настроек, и пользователь отменил выход из программы.", Logger.LogLevel.DEBUG);
                return;
            }

            if (!Save(GameSession.SaveStopSessionsToFile))
            {
                Logger.WriteLog(() => $@"{nameof(FrmGameBot_FormClosing)}: Произошла ошибка при сохранении наработанного опыта, и пользователь отменил выход из программы.", Logger.LogLevel.DEBUG);
                return;
            }

            try
            {
                ProgramStopFlag = true;

                if (StopGameBotThread())
                    Logger.WriteLog(() => $@"{nameof(FrmGameBot_FormClosing)}: Бот успешно остановлен.", Logger.LogLevel.DEBUG);
                else
                    Logger.WriteLog(() => $@"{nameof(FrmGameBot_FormClosing)}: Бот не был активен.", Logger.LogLevel.DEBUG);

                if (_escapeThread is null)
                    Logger.WriteLog(() => $@"{nameof(FrmGameBot_FormClosing)}: Внутренняя ошибка, т.к. поток, отвечающий за горячие клавиши, должен быть всегда активен.", Logger.LogLevel.ERROR);
                else
                {
                    if (_escapeThread.Join(30000))
                        Logger.WriteLog(() => $@"{nameof(FrmGameBot_FormClosing)}: Поток, отвечающий за горячие клавиши, успешно завершён.", Logger.LogLevel.DEBUG);
                    else
                        Logger.WriteLog(() => $@"{nameof(FrmGameBot_FormClosing)}: Произошла ошибка при завершении потока, отвечающего за горячие клавиши.", Logger.LogLevel.ERROR);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLog(() => $@"{nameof(FrmGameBot_FormClosing)}: Ошибка: {ex.Message}.", Logger.LogLevel.ERROR);
            }

            Logger.WriteLog(() => $@"{nameof(FrmGameBot_FormClosing)}: Работа в режиме ""бот"" завершена.");

            return;

            bool Save(Action act)
            {
                try
                {
                    act();
                }
                catch (Exception ex)
                {
                    Logger.WriteLog(() => $@"{nameof(FrmGameBot_FormClosing)}.{nameof(Save)}: Ошибка: {ex.Message}.", Logger.LogLevel.ERROR);
                    e.Cancel = MessageBox.Show(this,
                        $@"Ошибка при сохранении настроек: ""{ex.Message}""{Environment.NewLine}Всё равно выйти?",
                        @"Ошибка", MessageBoxButtons.YesNo) != DialogResult.Yes;

                    return !e.Cancel;
                }

                return true;
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
                    Logger.WriteLog(() => $@"{nameof(CbxProfiles_SelectedIndexChanged)}: Выбрана опция создания нового профиля.");

                    _selectedProfileSettings = new SettingsProfilesArray.HitSettings();
                    _profileSettings.SelectedProfileIndex = -1;
                    _needSaveProfile = false;
                    return;
                }

                if (!_profileSettings.Profiles.Any())
                {
                    Logger.WriteLog(() => $@"{nameof(CbxProfiles_SelectedIndexChanged)}: Выбрана позиция {cbxProfiles.SelectedIndex}, при этом, профилей в хранилище нет.", Logger.LogLevel.ERROR);
                    return;
                }

                Logger.WriteLog(() => $@"{nameof(CbxProfiles_SelectedIndexChanged)}: Выбран профиль номер {cbxProfiles.SelectedIndex}.");

                int si = cbxProfiles.SelectedIndex - 1;
                _selectedProfileSettings = _profileSettings.Profiles[si];
                _profileSettings.SelectedProfileIndex = si;
                _needSaveProfile = false;
                btnGameStart.Enabled = true;
            }
            catch (Exception ex)
            {
                Logger.WriteLog(() => $@"{nameof(CbxProfiles_SelectedIndexChanged)}: Исключение ""{ex.Message}""", Logger.LogLevel.ERROR);
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
