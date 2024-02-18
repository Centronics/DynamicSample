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
using DynamicMosaic;
using DynamicParser;
using DynamicProcessor;

using Processor = DynamicParser.Processor;

namespace DynamicSample
{
    public partial class FrmSample : Form
    {
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

        List<ImageProcessorStorage> _containers;

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
            public Containers(int width, int height)
            {
                Processors = new ImageProcessorStorage(SearchImagesPath, ExtImg, width, height);
            }

            public ImageProcessorStorage Processors { get; }

            public Containers SubContainers { get; }

            public Processor GetNegative()
            {
                return null;///////////////
            }

            public static string GetSizeStr(int width, int height)
            {
                return $@"{width}*{height}";
            }

            public override string ToString()
            {
                return GetSizeStr(Processors.Width, Processors.Height);
            }
        }

        void BtnRefresh_Click(object sender, EventArgs e)
        {
            try
            {
                _recognizeProcessorStorages.Clear();

                // ПЕРЕБРАТЬ ПАПКИ, размеры не могут конфликтовать, если конфликт - ошибка; система может быть только ОДНА

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

                _containers = GetContainers(containersMap.Values.Select(c => c.Processors));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        void FrmSample_Shown(object sender, EventArgs e)
        {
            _recognizeProcessorStorages = new RecognizeProcessorStorage(RecognizeImagesPath, MinimumSize.Width, MaximumSize.Width, MinimumSize.Height, MaximumSize.Height, ExtImg);
            BtnRefresh_Click(btnRefresh, EventArgs.Empty);
        }

        static List<ImageProcessorStorage> GetContainers(IEnumerable<ImageProcessorStorage> imageProcessorStorages)
        {
            if (imageProcessorStorages == null)
                return new List<ImageProcessorStorage>();

            List<ImageProcessorStorage> r = imageProcessorStorages.ToList();

            if (r.Count < 2)
                return r;

            for (int k = 0; k < r.Count; k++)
            {
                ImageProcessorStorage ips = r[k];

                for (int d = k + 1; d < r.Count; d++)
                {
                    ImageProcessorStorage ip = r[d];

                    if (ip.Width == ips.Width || ip.Height == ips.Height)
                        throw new Exception($@"Размеры систем ({ip.Width}, {ip.Height}) не могут совпадать.");

                    bool b1 = ip.Width > ips.Width && ip.Height < ips.Height;
                    bool b2 = ip.Width < ips.Width && ip.Height > ips.Height;

                    if (b1 || b2)
                        throw new Exception($@"Обнаружены несовместимые размеры: {ips.Width}, {ips.Height} и {ip.Width}, {ip.Height}.");

                    if (ip.Width <= ips.Width || ip.Height <= ips.Height)
                        continue;

                    r[k] = r[d];
                    r[d] = ips;
                }
            }

            return r;
        }
    }
}
