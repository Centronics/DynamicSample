using System;
using System.Collections.Generic;
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

        public FrmGameBot()
        {
            InitializeComponent();
        }

        readonly SettingsProfilesArray _currentSettings = SettingsProfilesArray.CurrentSettings;

        SettingsProfilesArray.HitSettings _currentHitSettings = new SettingsProfilesArray.HitSettings();

        static Bitmap TakeScreenshot()
        {
            Factory1 factory = new Factory1();
            Adapter1 adapter = factory.GetAdapter1(0);
            Console.WriteLine(adapter.Description1.Description);
            Device device = new Device(adapter);
            Output output = adapter.GetOutput(0);
            Console.WriteLine(output.Description.DeviceName);
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
                    catch (SharpDXException ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                    finally
                    {
                        screenResource?.Dispose();
                    }

                    return bmp;
                }
            }
        }

        Bitmap CopyGameFieldFromScreen()
        {
            return CopyGameFieldFromScreen(out Bitmap _);
        }

        Bitmap CopyGameFieldFromScreen(out Bitmap origin)
        {
            Rectangle rect = GameFieldRect;
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
            TransparencyKey = Color.Red; // по умолчанию ЧЕРНЫЙ
            AllowTransparency = true;

            cbxProfiles.Items.AddRange(_currentSettings.Profiles.Select(s => (object)s.ProfileName).ToArray());
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

        void BtnSavePosition_Click(object sender, EventArgs e)
        {
            Rectangle gfr = GameFieldRect;

            Bitmap b;

            if (radNeedClick.Checked)
            {
                BitImages bi = new BitImages
                {
                    Data = GetBitmapAsInts(CopyGameFieldFromScreen()),
                    Coords = gfr.Location,
                    FieldSize = gfr.Size,
                    Name = 'Z'
                };

                _currentHitSettings.EventClicks.Add(bi);
                radEmptySpace.Checked = true;

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

                _currentHitSettings.Spaces.Add(bi);
                GetClick();

                radField_X.Checked = true;
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

                _currentHitSettings.Spaces.Add(bi);
                GetClick();

                radField_O.Checked = true;
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

                _currentHitSettings.Spaces.Add(bi);
                GetClick();
            }

            return;

            void GetClick()
            {
                List<BitImages> eventClicks =
                    new List<BitImages>();

                foreach (BitImages bi3 in _currentHitSettings.EventClicks)
                {
                    BitImages bi2 =
                        new BitImages
                        {
                            Data = GetBitmapAsInts(GetBitmapPiece(new Rectangle(bi3.Coords, bi3.FieldSize), b)),
                            Coords = bi3.Coords,
                            FieldSize = bi3.FieldSize,
                            Name = 'E'
                        };

                    eventClicks.Add(bi3);
                    eventClicks.Add(bi2);
                }

                _currentHitSettings.EventClicks = eventClicks;
            }
        }

        void BtnClearPosition_Click(object sender, EventArgs e)
        {
            if (radNeedClick.Checked)
                return;

            if (radEmptySpace.Checked || radField_X.Checked || radField_O.Checked)
                _currentHitSettings.Spaces.Clear();
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

            while (!IsPlayingStopped())
            {
                if (timeout.HasValue)
                {
                    Thread.Sleep(timeout.Value);
                    continue;
                }

                if (!IsFrameChanged())
                    return lastFullFrame;

                if (timeout.HasValue)
                    continue;

                SafeExecute(() =>
                {
                    timeout = int.TryParse(textBox1.Text, out int t) && t > 0 && t < 10000
                        ? t
                        : 300;
                }, true);
            }

            return null;

            bool IsFrameChanged()
            {
                Bitmap now = CopyGameFieldFromScreen(out lastFullFrame);

                if (lastFrame is null)
                {
                    lastFrame = now;
                    return true;
                }

                if (BitmapCompare(now, lastFrame))
                    return true;

                lastFrame = now;
                return false;
            }
        }

        bool IsPlayingStopped()
        {
            if (_inGame)
                return false;

            SafeExecute(() => btnGameStart.Enabled = true, true);
            return true;
        }

        static Bitmap GetBitmapPiece(Rectangle rect, Bitmap where)
        {
            Bitmap result = new Bitmap(rect.Width, rect.Height);

            for (int y = 0, ly = rect.Y; y < rect.Height; y++, ly++)
                for (int x = 0, lx = rect.X; x < rect.Width; x++, lx++)
                    result.SetPixel(x, y, where.GetPixel(lx, ly));

            return result;
        }

        static Point? GetUserHitPoint(int[,] map1, GameSession map2)
        {
            Point? result = null;

            for (int y = 0; y < 3; y++)
                for (int x = 0; x < 3; x++)
                {
                    if (map1[x, y] == map2[x, y])
                        continue;

                    if (result.HasValue)
                        throw new InvalidOperationException();

                    result = new Point(x, y);
                }

            if (!result.HasValue)
                return null;

            int v1 = map1[result.Value.X, result.Value.Y];
            int v2 = map2[result.Value.X, result.Value.Y];

            if (v1 != GameSession.EmptyHit)
                throw new InvalidOperationException();

            if (v2 != GameSession.UserHit)
                throw new InvalidOperationException();

            return result;
        }

        static int GetMyHero(int[,] map, int x, int y)
        {
            int v = map[x, y];

            if (v == GameSession.BotHit)
                return GameSession.UserHit;
            if (v == GameSession.UserHit)
                return GameSession.BotHit;

            throw new UnauthorizedAccessException();
        }

        void GameThreadFunction()
        {
            GameSession gameSession = new GameSession();
            int[,] sessionCopy = new int[3, 3];
            int amIxo = GameSession.EmptyHit;

            try
            {
                (BitImages, ProcessorContainer)[] pcs = GetProcessorHandlers();
                ProcessorContainer req = new ProcessorContainer(_currentHitSettings.Spaces.Select(bi => new Processor(bi.AsBitmap, bi.Name.ToString())).ToArray());

                while (true)
                {
                    try
                    {
                        if (IsPlayingStopped())
                            break;

                        Bitmap fullFrameNow = DoClickOperations(pcs);

                        if (IsPlayingStopped() || fullFrameNow is null)
                            break;

                        IEnumerable<Processor> pq1 = _currentHitSettings.Spaces.Select(bi => new Processor(GetBitmapPiece(new Rectangle(bi.Coords, bi.FieldSize), fullFrameNow), @"Z"));

                        if (IsPlayingStopped())
                            break;

                        List<SearchResults> results = new List<SearchResults>(pq1.Select(p => p.GetEqual(req)));

                        if (IsPlayingStopped())
                            break;

                        if (results.Count != 9)
                            throw new InvalidOperationException($@"{nameof(results)} не равно 9: {results.Count}");

                        for (int k = 0; k < 9; k++)
                        {
                            if (IsPlayingStopped())
                                return;

                            ProcPerc pp = results[k][0, 0];

                            if (pp.Procs.Length != 1)
                                throw new ArgumentException($@"Неоднозначность ({pp.Procs.Length}).");

                            int x = k % 3;
                            int y = k / 3;

                            switch (pp.Procs[0].Tag[0])
                            {
                                case 'X':
                                    if (sessionCopy[x, y] == GameSession.EmptyHit)
                                        sessionCopy[x, y] = GameSession.UserHit;
                                    break;

                                case 'O':
                                    if (sessionCopy[x, y] == GameSession.EmptyHit)
                                        sessionCopy[x, y] = GameSession.BotHit;
                                    break;

                                case 'E':
                                    if (sessionCopy[x, y] != GameSession.EmptyHit)
                                        throw new Exception($@"Непонятное значение в поле ({sessionCopy[x, y]}).");
                                    break;

                                default:
                                    throw new Exception();
                            }
                        }

                        if (IsPlayingStopped())
                            break;

                        Point? userHit = GetUserHitPoint(sessionCopy, gameSession);

                        if (!userHit.HasValue)
                            continue;

                        if (amIxo == GameSession.EmptyHit)
                            amIxo = GetMyHero(sessionCopy, userHit.Value.X, userHit.Value.Y);

                        if (!gameSession.MakeUserHit(userHit.Value.X, userHit.Value.Y))
                            throw new InvalidOperationException(@"Ударить не получилось.");

                        if (gameSession.CurrentWinner == GameSession.Winner.NOBODY)
                        {
                            Point hit = gameSession.MakeBotHit();

                            if (sessionCopy[hit.X, hit.Y] != GameSession.EmptyHit)
                                throw new InvalidOperationException($@"Пытаюсь пойти не туда ({hit.X}, {hit.Y})");

                            sessionCopy[hit.X, hit.Y] = amIxo;
                            BitImages bi = _currentHitSettings.Spaces[hit.Y * 3 + hit.X];
                            int px = bi.Coords.X + bi.FieldSize.Width / 2;
                            int py = bi.Coords.Y + bi.FieldSize.Height / 2;

                            MouseClickMethods.Click(new Point(px, py));
                            GetFullFrameNow();
                        }

                        if (IsPlayingStopped())
                            break;

                        switch (gameSession.CurrentWinner)
                        {
                            case GameSession.Winner.NOBODY:
                                break;
                            case GameSession.Winner.STANDOFF:
                            case GameSession.Winner.USER:
                            case GameSession.Winner.BOT:
                                DoClickOperations(pcs);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                    catch (Exception ex)
                    {
                        SafeExecute(() => MessageBox.Show(ex.Message, @"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error), true);
                        break;
                    }
                }
            }
            finally
            {
                SafeExecute(() =>
                {
                    radNeedClick.Enabled = true;
                    radEmptySpace.Enabled = true;
                    radField_X.Enabled = true;
                    radField_O.Enabled = true;
                    btnClearPosition.Enabled = true;
                    btnSavePosition.Enabled = true;
                }, true);
            }

            return;

            Bitmap DoClickOperations((BitImages, ProcessorContainer)[] pcs)
            {
                Bitmap fullFrameNow = GetFullFrameNow();

                if (IsPlayingStopped() || fullFrameNow is null)
                    return null;

                foreach ((BitImages bi, ProcessorContainer pc) in pcs)
                {
                    if (IsPlayingStopped())
                        return null;

                    Processor pq = new Processor(GetBitmapPiece(new Rectangle(bi.Coords, bi.FieldSize), fullFrameNow), @"Z");
                    SearchResults sr = pq.GetEqual(pc);

                    if (sr[0, 0].Procs.All(p => p.Tag[0] != 'Z'))
                        continue;

                    MouseClickMethods.Click(new Point(bi.HitX, bi.HitY));
                    fullFrameNow = GetFullFrameNow();
                }

                return fullFrameNow;
            }

            (BitImages, ProcessorContainer)[] GetProcessorHandlers()
            {
                List<(BitImages, ProcessorHandler)> result = new List<(BitImages, ProcessorHandler)>();
                Dictionary<Rectangle, ProcessorHandler> phs = new Dictionary<Rectangle, ProcessorHandler>();

                foreach (BitImages ec in _currentHitSettings.EventClicks)
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

        volatile bool _inGame;

        void PbScreenField_MouseLeave(object sender, EventArgs e)
        {
            _inGame = false;
        }

        void PbScreenField_MouseEnter(object sender, EventArgs e)
        {
            _inGame = true;
        }

        void BtnGameStart_Click(object sender, EventArgs e)
        {
            //_currentHitSettings.MainWindowRect = GameFieldRect;

            if (!_currentHitSettings.EventClicks.Any())
            {
                MessageBox.Show(@"Не указан последний клик!");
                return;
            }

            if (_currentHitSettings.Spaces.All(m => m.Name != 'X'))
            {
                MessageBox.Show(@"Не указаны символы крестиков.");
                return;
            }

            if (_currentHitSettings.Spaces.All(m => m.Name != 'O'))
            {
                MessageBox.Show(@"Не указаны символы ноликов.");
                return;
            }

            if (_currentHitSettings.Spaces.All(m => m.Name != 'E'))
            {
                MessageBox.Show(@"Не все клетки обозначены.");
                return;
            }

            if (_currentHitSettings.Spaces.Count != 9)
            {
                MessageBox.Show(@"Указанное количество символов не равно 9.");
                return;
            }

            string profileName = string.Empty;

            using (FrmName fn = new FrmName())
                if (fn.ShowDialog(this) == DialogResult.OK)
                    profileName = fn.MyTxtName;

            if (string.IsNullOrEmpty(profileName))
                profileName = cbxProfiles.Items.Count.ToString();

            _currentHitSettings.ProfileName = profileName;
            _currentSettings.Profiles.Insert(0, _currentHitSettings);

            cbxProfiles.Items.Insert(0, _currentHitSettings.ProfileName);
            _currentHitSettings = new SettingsProfilesArray.HitSettings();

            radNeedClick.Enabled = false;
            radEmptySpace.Enabled = false;
            radField_X.Enabled = false;
            radField_O.Enabled = false;
            btnClearPosition.Enabled = false;
            btnSavePosition.Enabled = false;

            new Thread(GameThreadFunction)
            {
                IsBackground = true
            }.Start();//научитья работать с потоками
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
                SettingsProfilesArray.CurrentSettings = _currentSettings;
            }
            catch (Exception ex)
            {
                if (MessageBox.Show(this, $@"Ошибка при сохранении настроек: {ex.Message}{Environment.NewLine}Всё равно выйти?", @"Ошибка", MessageBoxButtons.YesNo) == DialogResult.No)
                    e.Cancel = true;
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
                if (!_currentSettings.Profiles.Any())
                    return;

                _currentHitSettings = _currentSettings.Profiles[cbxProfiles.SelectedIndex];
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, @"Ошибка");
            }
        }
    }
}
