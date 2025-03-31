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
using DynamicParser;
using Device = SharpDX.Direct3D11.Device;
using Format = SharpDX.DXGI.Format;
using MapFlags = SharpDX.Direct3D11.MapFlags;
using Resource = SharpDX.DXGI.Resource;
using Processor = DynamicParser.Processor;

namespace DynamicSample
{
    public partial class FrmGameBot : Form
    {
        public sealed class GameSettings
        {
            public sealed class BitImages
            {
                public Size FieldSize { get; set; }
                public char Name { get; set; }
                public Point Coords { get; set; }
                public List<int> Data { get; set; }

                public Bitmap GetBitmap()
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

            public Rectangle MainWindowRect { get; set; }

            public Point? FirstClick { get; set; }

            public Point? LastClick { get; set; }

            public List<BitImages> Spaces { get; set; } // работать с помощью DynamicParser
        }

        public FrmGameBot()
        {
            InitializeComponent();
        }

        Point? _firstClickBuf, _lastClickBuf;

        public GameSettings CurrentSettings { get; } = new GameSettings();

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
            Rectangle rect = GameFieldRect;
            Bitmap from = TakeScreenshot();
            Bitmap b = new Bitmap(rect.Width, rect.Height);

            for (int x = rect.X, xto = 0; x < rect.Right; x++, xto++)
                for (int y = rect.Y, yto = 0; y < rect.Bottom; y++, yto++)
                    b.SetPixel(xto, yto, from.GetPixel(x, y));

            return b;
        }

        void FrmGameSettings_Shown(object sender, EventArgs e)
        {
            pbScreenField.BackColor = Color.Red;
            TransparencyKey = Color.Red; // по умолчанию ЧЕРНЫЙ
            AllowTransparency = true;
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

        void pictureBox1_MouseClick(object sender, MouseEventArgs e)
        {
            if (radFirstClick.Checked)
            {
                _firstClickBuf = e.Location;
                return;
            }

            if (radClickAfter.Checked)
                _lastClickBuf = e.Location;
        }

        void btnSavePosition_Click(object sender, EventArgs e)
        {
            if (radFirstClick.Checked)
            {
                if (_firstClickBuf.HasValue)
                {
                    CurrentSettings.FirstClick = _firstClickBuf;
                    _firstClickBuf = null;
                }

                radClickAfter.Checked = true;
                return;
            }

            if (radClickAfter.Checked)
            {
                if (_lastClickBuf.HasValue)
                {
                    CurrentSettings.LastClick = _lastClickBuf;
                    _lastClickBuf = null;
                }

                radEmptySpace.Checked = true;
                return;
            }

            if (radEmptySpace.Checked)
            {
                GameSettings.BitImages bi = new GameSettings.BitImages
                {
                    Data = GetBitmapAsInts(CopyGameFieldFromScreen()),
                    Coords = GameFieldRect.Location,
                    FieldSize = GameFieldRect.Size,
                    Name = 'E'
                };

                CurrentSettings.Spaces.Add(bi);

                radField_X.Checked = true;
                return;
            }

            if (radField_X.Checked)
            {
                GameSettings.BitImages bi = new GameSettings.BitImages
                {
                    Data = GetBitmapAsInts(CopyGameFieldFromScreen()),
                    Coords = GameFieldRect.Location,
                    FieldSize = GameFieldRect.Size,
                    Name = 'X'
                };

                CurrentSettings.Spaces.Add(bi);

                radField_O.Checked = true;
                return;
            }

            if (!radField_O.Checked)
                return;

            GameSettings.BitImages bi1 = new GameSettings.BitImages
            {
                Data = GetBitmapAsInts(CopyGameFieldFromScreen()),
                Coords = GameFieldRect.Location,
                FieldSize = GameFieldRect.Size,
                Name = 'O'
            };

            CurrentSettings.Spaces.Add(bi1);
        }

        void btnClearPosition_Click(object sender, EventArgs e)
        {
            if (radFirstClick.Checked)
            {
                _firstClickBuf = null;
                return;
            }

            if (radClickAfter.Checked)
            {
                _lastClickBuf = null;
                return;
            }

            if (radEmptySpace.Checked || radField_X.Checked || radField_O.Checked)
                CurrentSettings.Spaces.Clear();
        }


        int[,] _gameField;

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

        Bitmap GetFrameNow()
        {
            Bitmap lastFrame = null;

            int timeout = 300;

            SafeExecute(() =>
            {
                if (!int.TryParse(textBox1.Text, out timeout) || timeout < 0 || timeout > 10000)
                    timeout = 300;
            }, true);

            while (!IsPlayingStopped())
            {
                if (!IsFrameChanged())
                    return lastFrame;

                Thread.Sleep(timeout);
            }

            return null;

            bool IsFrameChanged()
            {
                Bitmap now = CopyGameFieldFromScreen(); // сравнить с _lastFrame - на каждый кадр надо делать реакцию!

                if (lastFrame is null)
                {
                    lastFrame = now;
                    return true;
                }

                if (BitmapCompare(now, lastFrame))
                    return
                        true; // ДО начала игры все выбранные точки надо отображать; а хранить их надо в виде дистанции от краёв главной формы, а, перед игрой, проверять, не выходим ли мы (точки) за них

                lastFrame = now;
                return false;

                // НЕ надо держать игру только в одном экране - ФОРМА (во вреям игры) служит ТОЛЬКО для понимания того, чтобы остановить или проолжить игру - в зависимости от того, находится ли указатель внутри нее
                // она НЕ должна мешать игре
            }
        }

        //bool IsGameReStarted(Bitmap frame, out bool? iamX)
        //{
        // если заполненных клеточек было больше раньше
        //}

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

        static Point? GetUserHitPoint(int[,] map1, int[,] map2)
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

        static int? GetFirstHit(int[,] map)
        {
            int? result = null;

            for (int y = 0; y < 3; y++)
                for (int x = 0; x < 3; x++)
                {
                    if (map[x, y] == GameSession.EmptyHit)
                        continue;

                    if (result.HasValue)
                        return null;

                    result = map[x, y];
                }

            return result;
        }

        void GameThreadFunction()
        {
            GameSession gameSession = new GameSession();

            try
            {
                ProcessorContainer req = new ProcessorContainer(CurrentSettings.Spaces.Select(bi => new Processor(bi.GetBitmap(), bi.Name.ToString())).ToArray());

                while (true)
                {
                    try
                    {
                        if (IsPlayingStopped())
                            break;

                        Bitmap b = GetFrameNow();

                        if (b is null)
                            break;

                        IEnumerable<Processor> pq = CurrentSettings.Spaces.Select(bi => new Processor(GetBitmapPiece(new Rectangle(bi.Coords, bi.FieldSize), b), @"Z"));
                        List<SearchResults> results = new List<SearchResults>(pq.Select(p => p.GetEqual(req)));

                        // поля получил и запросы написал, теперь надо отследить изменения - НАДО сформировать поле и дисгностировать его на предмет статуса игры

                        if (IsPlayingStopped())
                            break;



                        // считается, что массив карт упорядочен слева направо, сверху вниз, т.о. получается, что определять место удара по координатам не надо
                        if (!gameSession.MakeUserHit(e.X / 161, e.Y / 161)) // НЕ делать фиксу, а делить на три
                            return;

                        RefreshGameField();

                        if (gameSession.CurrentWinner == GameSession.Winner.NOBODY)
                        {
                            if (!gameSession.MakeBotHit())
                            {
                                MessageBox.Show(@"Ничья, никто не сможет выиграть!");
                                RefreshGameField(true);
                                return;
                            }

                            RefreshGameField();
                        }

                        switch (gameSession.CurrentWinner)
                        {
                            case GameSession.Winner.NOBODY:
                                return;
                            case GameSession.Winner.STANDOFF:
                                MessageBox.Show(@"Ничья!");
                                RefreshGameField(true);
                                return;
                            case GameSession.Winner.USER:
                                MessageBox.Show(@"Ты выиграл!");
                                RefreshGameField(true);
                                return;
                            case GameSession.Winner.BOT:
                                MessageBox.Show(@"Компьютер выиграл!");
                                RefreshGameField(true);
                                return;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, @"Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        //Application.Exit();
                    }
                }
            }
            finally
            {
                //INVOKE
                radFirstClick.Enabled = true;
                radClickAfter.Enabled = true;
                radEmptySpace.Enabled = true;
                radField_X.Enabled = true;
                radField_O.Enabled = true;
                btnClearPosition.Enabled = true;
                btnSavePosition.Enabled = true;
            }
        }

        bool _inGame;

        private void pbScreenField_MouseLeave(object sender, EventArgs e)
        {
            _inGame = false;
        }

        private void pbScreenField_MouseEnter(object sender, EventArgs e)
        {
            _inGame = true;
        }

        void btnGameStart_Click(object sender, EventArgs e)
        {
            CurrentSettings.MainWindowRect = GameFieldRect;

            if (!CurrentSettings.LastClick.HasValue)
            {
                MessageBox.Show(@"Не указан последний клик!");
                return;
            }

            if (CurrentSettings.Spaces.All(m => m.Name != 'X'))
            {
                MessageBox.Show(@"Не указаны символы крестиков.");
                return;
            }

            if (CurrentSettings.Spaces.All(m => m.Name != 'O'))
            {
                MessageBox.Show(@"Не указаны символы ноликов.");
                return;
            }

            if (CurrentSettings.Spaces.All(m => m.Name != 'E'))
            {
                MessageBox.Show(@"Не все клетки обозначены.");
                return;
            }

            if (CurrentSettings.Spaces.Count != 9)
            {
                MessageBox.Show(@"Указанное количество символов не равно 9.");
                return;
            }

            radFirstClick.Enabled = false;
            radClickAfter.Enabled = false;
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
    }
}
