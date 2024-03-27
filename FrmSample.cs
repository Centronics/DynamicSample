using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using DynamicMosaic;
using DynamicParser;
using DynamicProcessor;

using Processor = DynamicParser.Processor;

namespace DynamicSample
{
    public partial class FrmSample : Form
    {
        public static SignValue UserHit => SignValue.MaxValue;

        public static SignValue BotHit => SignValue.MinValue;

        public static SignValue EmptySpace => SignValue.MaxValue.Average(SignValue.MinValue);

        readonly bool[,] _lastBotHit = new bool[3, 3];

        public enum Winner
        {
            USER,
            BOT,
            NOBODY
        }

        /// <summary>
        ///     Искомое и распознаваемое изображения не должны быть прозрачными, т.к. платформа не позволяет установить параметр
        ///     прозрачности.
        ///     Этот параметр необходим для проверки значения прозрачности.
        /// </summary>
        public static byte DefaultOpacity => 0xFF;

        /// <summary>
        ///     Недопустимые символы, которые не должен содержать путь.
        /// </summary>
        internal static HashSet<char> InvalidCharSet { get; }

        RecognizeProcessorStorage _recognizeProcessorStorages;

        Containers _containers;

        SignValue[,] _mainProcessor;

        readonly List<Processor> _mainStream = new List<Processor>();

        readonly List<List<Processor>> _streamCollection = new List<List<Processor>>();

        /// <summary>
        ///     Необходим для инициализации коллекции недопустимых символов пути к файлу или папке.
        /// </summary>
        static FrmSample()
        {
            InvalidCharSet = new HashSet<char>(Path.GetInvalidFileNameChars());

            foreach (char c in Path.GetInvalidPathChars())
                InvalidCharSet.Add(c);
        }

        public FrmSample()
        {
            InitializeComponent();
        }

        /// <summary>
        ///     Расширение изображений, которые интерпретируются как карты <see cref="Processor" />.
        /// </summary>
        static string ExtImg => "bmp";

        /// <summary>
        ///     Название папки, содержащей изображения, интерпретируемые как карты <see cref="Processor" />, которые необходимо
        ///     найти на распознаваемой(ых) карте(ах).
        /// </summary>
        static string ImagesFolder => "Images";

        /// <summary>
        ///     Название папки, содержащей изображения, интерпретируемые как карты <see cref="Processor" />, на которых необходимо
        ///     выполнять поисковые запросы.
        /// </summary>
        static string RecognizeFolder => "Recognize";

        /// <summary>
        ///     Рабочий каталог приложения (<see cref="Application.StartupPath" />).
        ///     Содержит хранилища (<see cref="SearchImagesPath" />, <see cref="RecognizeImagesPath" />)
        ///     и лог программы <see cref="LogPath" />.
        /// </summary>
        /// <seealso cref="SearchImagesPath" />
        /// <seealso cref="RecognizeImagesPath" />
        /// <seealso cref="Application.StartupPath" />
        static string WorkingDirectory { get; } = Application.StartupPath;

        /// <summary>
        ///     Путь, по которому ищутся изображения, которые интерпретируются как карты <see cref="Processor" />, которые
        ///     необходимо найти на распознаваемой(ых) карте(ах).
        /// </summary>
        internal static string SearchImagesPath { get; } = Path.Combine(WorkingDirectory, ImagesFolder);

        /// <summary>
        ///     Путь, по которому ищутся изображения, которые интерпретируются как карты <see cref="Processor" />, на которых
        ///     необходимо выполнять поисковые запросы.
        /// </summary>
        internal static string RecognizeImagesPath { get; } = Path.Combine(WorkingDirectory, RecognizeFolder);

        readonly HashSet<Size> _setSizes = new HashSet<Size>();

        /// <summary>
        /// Изображение карты.
        /// </summary>
        Bitmap _currentCanvas;
        /// <summary>
        /// Поверхность для рисования.
        /// </summary>
        Graphics _currentgrFront;
        /// <summary>
        /// Служит для отображения объектов карты.
        /// </summary>
        Pen _workPen = new Pen(Color.Black);
        /// <summary>
        ///     Определяет, разрешён вывод создаваемой пользователем линии на экран или нет.
        ///     Значение <see langword="true" /> - вывод разрешён, в противном случае - <see langword="false" />.
        /// </summary>
        bool _drawAllowed;

        /// <summary>
        ///     Выполняет проверку значения <see cref="Color.A" /> указанного цвета.
        /// </summary>
        /// <param name="c">Проверяемый цвет.</param>
        /// <returns>Возвращает параметр <paramref name="c" />.</returns>
        /// <remarks>
        ///     В случае несоответствия значению <see cref="DefaultOpacity" />, будет выброшено исключение
        ///     <see cref="InvalidOperationException" />.
        ///     Метод потокобезопасен.
        /// </remarks>
        /// <exception cref="InvalidOperationException" />
        /// <seealso cref="Color.A" />
        /// <seealso cref="DefaultOpacity" />
        internal static Color CheckAlphaColor(Color c)
        {
            return c.A == DefaultOpacity
                ? c
                : throw new InvalidOperationException(
                    $@"Значение прозрачности не может быть задано 0x{c.A:X2}. Должно быть задано как 0x{DefaultOpacity:X2}.");
        }

        /// <summary>
        ///     Получает значение, отражающее, является ли указанный путь приемлемым в качестве пути к карте.
        /// </summary>
        /// <param name="path">Проверяемый путь.</param>
        /// <returns>
        ///     Если указанный путь является приемлемым в качестве пути к карте, возвращает значение <see langword="true" />.
        ///     Если входная строка пустая (<see langword="null" /> или <see cref="string.Empty" />), возвращает значение
        ///     <see langword="false" />.
        /// </returns>
        /// <remarks>
        ///     Метод проверяет только расширение.
        ///     Путь обязательно должен содержать хотя бы один разделитель
        ///     (<see cref="Path.DirectorySeparatorChar" /> или <see cref="Path.AltDirectorySeparatorChar" />).
        ///     Расширение должно быть <see cref="ExtImg" />.
        ///     Регистр символов расширения не имеет значения.
        ///     К файловой системе не обращается.
        ///     Метод потокобезопасный.
        /// </remarks>
        /// <seealso cref="ExtImg" />
        /// <seealso cref="Path.DirectorySeparatorChar" />
        /// <seealso cref="Path.AltDirectorySeparatorChar" />
        public bool IsProcessorFile(string path)
        {
            return !string.IsNullOrEmpty(path) &&
                   string.Compare(Path.GetExtension(path), $".{ExtImg}", StringComparison.OrdinalIgnoreCase) == 0;
        }

        /// <summary>
        ///     Получает перечисляемую коллекцию путей к изображениям, интерпретируемым как карты, в указанной папке и всех
        ///     подпапках.
        ///     Это файлы с расширением <see cref="ExtImg" />.
        /// </summary>
        /// <param name="path">Путь, по которому требуется получить список файлов изображений карт.</param>
        /// <returns>Возвращает запрос списка файлов.</returns>
        /// <remarks>
        ///     Поскольку этот запрос является длительным, на него влияет флаг <see cref="LongOperationsAllowed" />.
        ///     Чтобы выполнить этот запрос, необходимо вызвать соответствующий метод для того, чтобы получить коллекцию.
        ///     Метод является потокобезопасным.
        /// </remarks>
        IEnumerable<string> QueryProcessorFiles(string path)
        {
            return Directory.EnumerateFiles(path, $"*.{ExtImg}", SearchOption.AllDirectories)
                .Where(IsProcessorFile);
        }

        class Containers
        {
            public Containers(ImageProcessorStorage processors = null)
            {
                Processors = processors;
            }

            public Containers(IEnumerable<ImageProcessorStorage> cps)
            {
                if (cps == null)
                    throw new ArgumentNullException(nameof(cps));

                ImageProcessorStorage[] cpsArray = cps.ToArray();

                if (cpsArray.Length == 0)
                    throw new ArgumentException();

                Processors = cpsArray[0];

                Containers containers = this;

                for (int k = 1; k < cpsArray.Length; k++)
                    containers = containers.SubContainers = new Containers(cpsArray[k]);
            }

            public Containers(int width, int height)
            {
                Processors = new ImageProcessorStorage(SearchImagesPath, ExtImg, width, height);
            }

            public ImageProcessorStorage Processors { get; }

            public Containers SubContainers { get; private set; }

            //public Processor GetNegative()
            //{
            //    if (Processors == null)
            //        return null;

            //    Processor[] psVar = Processors.Elements.ToArray();

            //    double[,] matrix = new double[Processors.Width, Processors.Height];

            //    foreach (Processor p in psVar)
            //    {
            //        for (int y = 0; y < p.Height; y++)
            //            for (int x = 0; x < p.Width; x++)
            //                matrix[x, y] += p[x, y].Value;
            //    }

            //    SignValue[,] svv = new SignValue[Processors.Width, Processors.Height];

            //    for (int y = 0; y < Processors.Height; y++)
            //        for (int x = 0; x < Processors.Width; x++)
            //            svv[x, y] = new SignValue(Convert.ToInt32(Math.Round(matrix[x, y] / psVar.Length)));

            //    return new Processor(svv, @"NEGATIVE");
            //}

            public static string GetSizeStr(int width, int height)
            {
                return $@"{width}*{height}";
            }

            public override string ToString()
            {
                return GetSizeStr(Processors.Width, Processors.Height);
            }
        }

        bool RenewContainers()
        {
            try
            {
                _recognizeProcessorStorages.Clear();

                _recognizeProcessorStorages.AddProcessor(RecognizeImagesPath);

                Dictionary<string, Containers> containersMap = new Dictionary<string, Containers>();

                foreach (string path in QueryProcessorFiles(SearchImagesPath))
                {
                    Bitmap btm = ConcurrentProcessorStorage.ReadBitmap(path);

                    string strSize = Containers.GetSizeStr(btm.Width, btm.Height);

                    if (containersMap.TryGetValue(strSize, out Containers value))
                        value.Processors.AddProcessor(path);
                    else
                        containersMap.Add(strSize, new Containers(btm.Width, btm.Height));
                }

                _containers = GetContainers(containersMap.Values);

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, @"123", MessageBoxButtons.OK, MessageBoxIcon.Error);

                return false;
            }
        }

        void SaveContainers()
        {
            //сделать сохранение уникальных карт (отследить, если нет такой карты, то сохранить)
        }

        void BtnRefresh_Click(object sender, EventArgs e)
        {
            try
            {
                if (!RenewContainers())
                    return;

                for (Containers containers = _containers; containers != null; containers = _containers.SubContainers)
                {

                }

                SaveContainers();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, @"1234", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // необходимо реализовать "разные действительности" т.е. те, что включают карты с предыдущих запросов, для обучения - именно те карты "останутся" - он их "притянет" - те, что позволят ему распознать требуемую карту - предлагаю отметить карты, которые были на различных этапах обучения - и искать именно их, чтобы находить только карты с одной действительности, а "действительность" - это все карты из запроса, где общие карты, найденные на всех картах одновременно (одинаковые по содерж) - и есть то, что надо
        // таким образом, возникает правило, что "одна буква - одна карта"
        // получается так, что карты, которые без конфликтов нашлись в разных реальностях - короче, это когда находятся карты ТОЛЬКО из одной действительности, и они соответствуют запросу ()
        // НАДО выбирать карты ТОЛЬКО соответствуюзие зпросу, отбирая по максимальному проценту соответствия (исходя из названий - в случае конфликта) - в данном случае речь идет только о том, что карта пересекается сама с собой - а что делать, если она пересекается с другой?
        // и что делать, когда пересекаются несколько областей - мб для них прдумать ограничения? - ДА, например, когда карты должны СТРОГО соответствовать друг другу по каким-либо размерам

        /// <summary>
        ///     Цвет, который считается изначальным. Определяет изначальный цвет, отображаемый на поверхности для рисования.
        ///     Используется для стирания изображения.
        /// </summary>
        public static Color DefaultColor = CheckAlphaColor(Color.White);

        /// <summary>
        ///     Задаёт цвет и ширину для рисования в окне создания распознаваемого изображения.
        /// </summary>
        static readonly Pen BlackPen = new Pen(CheckAlphaColor(Color.Black), 2.0f);

        /// <summary>
        ///     Задаёт цвет и ширину для стирания в окне создания распознаваемого изображения.
        /// </summary>
        static readonly Pen WhitePen = new Pen(DefaultColor, 2.0f);

        /// <summary>
        ///     Рисует точку в указанном месте на <see cref="pbRecognizeImageDraw" /> с помощью
        ///     <see cref="_grRecognizeImageGraphics" />.
        /// </summary>
        /// <param name="x">Координата Х.</param>
        /// <param name="y">Координата Y.</param>
        /// <param name="button">Данные о нажатой кнопке мыши.</param>
        void DrawPoint(int x, int y, MouseButtons button)
        {
            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (button)
            {
                case MouseButtons.Left:
                    _currentgrFront.DrawRectangle(BlackPen, new Rectangle(x, y, 1, 1));
                    break;
                case MouseButtons.Right:
                    _currentgrFront.DrawRectangle(WhitePen, new Rectangle(x, y, 1, 1));
                    break;
            }

            pbDraw.Refresh();
        }

        void FrmSample_Shown(object sender, EventArgs e)
        {
            _currentCanvas = new Bitmap(pbDraw.Width, pbDraw.Height);
            _currentgrFront = Graphics.FromImage(_currentCanvas);
            pbDraw.Image = _currentCanvas;

            btnNewGame_Click(btnNewGame, EventArgs.Empty);

            //_recognizeProcessorStorages = new RecognizeProcessorStorage(RecognizeImagesPath, 1, 3840, 1, 2160, ExtImg);

            //if (!RenewContainers())
            //  Application.Exit();
        }

        static Containers GetContainers(IEnumerable<Containers> imageProcessorStorages)
        {
            if (imageProcessorStorages == null)
                return new Containers();

            List<Containers> r = imageProcessorStorages.ToList();

            if (r.Count < 1)
                throw new Exception($@"Коллекций искомых карт не может быть меньше одной ({r.Count}).");

            for (int k = 0; k < r.Count; k++)
            {
                Containers ips = r[k];

                for (int d = k + 1; d < r.Count; d++)
                {
                    Containers ip = r[d];

                    if (ip.Processors.Width == ips.Processors.Width || ip.Processors.Height == ips.Processors.Height)
                        throw new Exception($@"Размеры систем ({ip.Processors.Width}, {ip.Processors.Height}) не могут совпадать.");

                    bool b1 = ip.Processors.Width > ips.Processors.Width && ip.Processors.Height < ips.Processors.Height;
                    bool b2 = ip.Processors.Width < ips.Processors.Width && ip.Processors.Height > ips.Processors.Height;

                    if (b1 || b2)
                        throw new Exception($@"Обнаружены несовместимые размеры: {ips.Processors.Width}, {ips.Processors.Height} и {ip.Processors.Width}, {ip.Processors.Height}.");

                    if (ip.Processors.Width <= ips.Processors.Width || ip.Processors.Height <= ips.Processors.Height)
                        continue;

                    r[k] = r[d];
                    r[d] = ips;
                }
            }

            ImageProcessorStorage mainStorage = r[r.Count - 1].Processors;

            bool b3 = false, b4 = false;

            for (int k = 0; k < r.Count - 1; k++)
            {
                if (!b3)
                    b3 = r[k].Processors.Width == mainStorage.Width;

                if (!b4)
                    b4 = r[k].Processors.Height == mainStorage.Height;

                if (b3 && b4)
                    break;
            }

            if (!b3)
                throw new Exception($@"Должна быть хотя бы одна карта, ширина которой равна ширине карты минимального размера ({mainStorage.Width}).");

            if (!b4)
                throw new Exception($@"Должна быть хотя бы одна карта, высота которой равна высоте карты минимального размера ({mainStorage.Height}).");

            return new Containers(r.Select(t => t.Processors));
        }

        public Winner CurrentWinner
        {
            get
            {
                if (IsLine(UserHit))
                    return Winner.USER;

                return IsLine(BotHit) ? Winner.BOT : Winner.NOBODY;

                bool IsLine(SignValue sv)
                {
                    if (_mainProcessor[0, 0] == sv && _mainProcessor[1, 0] == sv &&
                        _mainProcessor[2, 0] == sv)
                        return true;

                    if (_mainProcessor[0, 1] == sv && _mainProcessor[1, 1] == sv &&
                        _mainProcessor[2, 1] == sv)
                        return true;

                    if (_mainProcessor[0, 2] == sv && _mainProcessor[1, 2] == sv &&
                        _mainProcessor[2, 2] == sv)
                        return true;

                    if (_mainProcessor[0, 0] == sv && _mainProcessor[0, 1] == sv &&
                        _mainProcessor[0, 2] == sv)
                        return true;

                    if (_mainProcessor[1, 0] == sv && _mainProcessor[1, 1] == sv &&
                        _mainProcessor[1, 2] == sv)
                        return true;

                    if (_mainProcessor[2, 0] == sv && _mainProcessor[2, 1] == sv &&
                        _mainProcessor[2, 2] == sv)
                        return true;

                    if (_mainProcessor[0, 0] == sv && _mainProcessor[1, 1] == sv &&
                        _mainProcessor[2, 2] == sv)
                        return true;

                    return _mainProcessor[0, 2] == sv && _mainProcessor[1, 1] == sv &&
                           _mainProcessor[2, 0] == sv;
                }
            }
        }

        void Repaint()
        {
            _currentgrFront.Clear(Color.LightGray);

            _currentgrFront.DrawRectangle(BlackPen, 161, 0, 2, pbDraw.Height);
            _currentgrFront.DrawRectangle(BlackPen, 322, 0, 2, pbDraw.Height);

            _currentgrFront.DrawRectangle(BlackPen, 0, 161, pbDraw.Width, 2);
            _currentgrFront.DrawRectangle(BlackPen, 0, 322, pbDraw.Width, 2);

            for (int y = 0; y < 3; y++)
                for (int x = 0; x < 3; x++)
                {
                    if (_mainProcessor[x, y] == UserHit)
                        DrawX(x * 161, y * 161);
                    if (_mainProcessor[x, y] == BotHit)
                        DrawZero(x * 161, y * 161, _lastBotHit[x, y]);
                }

            pbDraw.Refresh();

            return;

            void DrawX(int x, int y)
            {
                _currentgrFront.DrawString(@"X",
                    new Font(FontFamily.GenericMonospace, 224.0F, FontStyle.Italic, GraphicsUnit.Pixel),
                    new SolidBrush(Color.DodgerBlue), x - 40, y - 50);
            }

            void DrawZero(int x, int y, bool lastHit)
            {
                _currentgrFront.DrawString(@"O",
                    new Font(FontFamily.GenericMonospace, 224.0F, FontStyle.Italic, GraphicsUnit.Pixel),
                    new SolidBrush(lastHit ? Color.Green : Color.Red), x - 35, y - 43);
            }
        }

        private void pbDraw_MouseDown(object sender, MouseEventArgs e)
        {
            _drawAllowed = true;
            DrawPoint(e.X, e.Y, e.Button);
        }

        private void pbDraw_MouseLeave(object sender, EventArgs e)
        {
            _drawAllowed = false;
        }

        bool IsHitPossible(string str)
        {
            if (string.IsNullOrEmpty(str))
                throw new Exception(@"IsHitPossible");

            if (str.Length < 3)
                throw new Exception(@"IsHitPossible<3");

            try
            {
                int x = Convert.ToInt32(str[1]);
                int y = Convert.ToInt32(str[2]);

                return _mainProcessor[x, y] == EmptySpace;
            }
            catch (Exception e)
            {
                throw new Exception($@"IsHitPossible2 {e.Message}", e);
            }
        }

        ProcessorContainer CommonStreamContainer()
        {
            ProcessorContainer mainResult = null;

            uint n = 0;

            foreach (Processor p in _streamCollection.Select(GetProc).Where(p => p != null))
            {
                if (mainResult == null)
                {
                    mainResult = new ProcessorContainer(p);
                    continue;
                }

                mainResult.Add(p);
            }

            return mainResult;

            Processor GetProc(IReadOnlyList<Processor> ps)
            {
                if (ps.Count < _mainStream.Count)
                    return null;

                SignValue[,] result = new SignValue[_mainStream.Count * 3, 3];

                for (int y = 0; y < result.GetLength(1); y++)
                    for (int x = 0; x < result.GetLength(0); x++)
                        result[x, y] = ps[x / 3][x % 3, y];

                Processor p = new Processor(result, $@"p{n}");

                ++n;

                return p;
            }
        }

        ProcessorContainer SessionsMapCreator()
        {
            ProcessorContainer pc = null;

            int mapCounter = 0;

            foreach (List<Processor> sp in _streamCollection)
            {
                if (sp.Count < _mainStream.Count)
                {
                    mapCounter++;
                    continue;
                }

                SignValue[,] result = new SignValue[_mainStream.Count * 3, 3];

                for (int y = 0; y < result.GetLength(1); y++)
                    for (int x = 0; x < result.GetLength(0); x++)
                        result[x, y] = sp[x / 3][x % 3, y];

                if (pc == null)
                    pc = new ProcessorContainer(new Processor(result, mapCounter.ToString()));
                else
                    pc.Add(new Processor(result, mapCounter.ToString()));

                mapCounter++;
            }

            return pc;
        }

        Processor CurrentSessionMapCreator()
        {
            SignValue[,] result = new SignValue[_mainStream.Count * 3, 3];

            for (int y = 0; y < result.GetLength(1); y++)
                for (int x = 0; x < result.GetLength(0); x++)
                    result[x, y] = _mainStream[x / 3][x % 3, y];

            return new Processor(result, @"p");
        }

        private void pbDraw_MouseUp(object sender, MouseEventArgs e)
        {
            //_drawAllowed = false;

            int cx = e.X / 161;
            int cy = e.Y / 161;

            if (_mainProcessor[cx, cy] != EmptySpace)
                return;

            _mainProcessor[cx, cy] = UserHit;

            // Ход бота

            for (int y = 0; y < _lastBotHit.GetLength(1); y++)
                for (int x = 0; x < _lastBotHit.GetLength(0); x++)
                    _lastBotHit[x, y] = false;

            // БОТ должен сходить здесь

            // удар бота _lastBotHit[x, y] = true;

            //

            Repaint();

            Winner winner = CurrentWinner;

            if (winner == Winner.USER)
            {
                MessageBox.Show(@"Вы выиграли!");
                btnNewGame_Click(btnNewGame, EventArgs.Empty);
                return;
            }

            if (winner != Winner.BOT)
                return;

            MessageBox.Show(@"Компьютер выиграл! Этот опыт запомнится как положительный, и будет использован в будущем."); // Всё написано правильно, продолжаем в том же духе

            // зафиксировать результат бота, если он есть

            btnNewGame_Click(btnNewGame, EventArgs.Empty);
        }

        private void btnNewGame_Click(object sender, EventArgs e)
        {
            if (_mainProcessor == null)
                _mainProcessor = new SignValue[3, 3];

            for (int y = 0; y < _mainProcessor.GetLength(1); y++)
                for (int x = 0; x < _mainProcessor.GetLength(0); x++)
                    _mainProcessor[x, y] = EmptySpace;

            Repaint();
        }
    }
}
