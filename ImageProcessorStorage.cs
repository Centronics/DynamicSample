﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using DynamicParser;

namespace DynamicSample
{
    /// <summary>
    ///     Реализация потокобезопасного хранилища карт <see cref="ConcurrentProcessorStorage" />.
    /// </summary>
    /// <remarks>
    ///     Учитывает особенности обработки карт, данные из которых требуется найти.
    /// </remarks>
    public sealed class ImageProcessorStorage : ConcurrentProcessorStorage
    {
        /// <summary>
        ///     Инициализирует текущий экземпляр, указывая расширение файлов с изображениями, с которым будет работать хранилище.
        /// </summary>
        /// <param name="path">Путь к рабочему каталогу хранилища.</param>
        /// <param name="extImg">Параметр <see cref="ConcurrentProcessorStorage.ExtImg" />.</param>
        public ImageProcessorStorage(string path, string extImg, int width, int height) : base(extImg)
        {
            WorkingDirectory = path;
            Width = width;
            Height =height;
        }

        public int Height { get; }

        public int Width { get; }

        /// <summary>
        ///     Рабочий каталог хранилища <see cref="ImageProcessorStorage" />.
        /// </summary>
        /// <remarks>
        ///     Подробнее см. <see cref="ConcurrentProcessorStorage.WorkingDirectory" />.
        ///     Содержится в <see cref="FrmSample.SearchImagesPath" />.
        /// </remarks>
        public override string WorkingDirectory { get; }

        /// <summary>
        ///     Тип хранилища <see cref="ImageProcessorStorage" />.
        /// </summary>
        /// <remarks>
        ///     Подробнее см. <see cref="ConcurrentProcessorStorage.StorageType" />.
        ///     Является <see cref="ConcurrentProcessorStorage.ProcessorStorageType.IMAGE" />.
        /// </remarks>
        public override ProcessorStorageType StorageType => ProcessorStorageType.IMAGE;

        /// <summary>
        ///     Получает значение, отражающее наличие текущей выбранной карты в хранилище <see cref="ImageProcessorStorage" />.
        /// </summary>
        /// <remarks>
        ///     Является потокобезопасным.
        ///     Проверяет, содержится ли последняя выбранная карта (<see cref="ConcurrentProcessorStorage.SelectedPath" />) в
        ///     коллекции на данный момент.
        ///     Является оптимизированной копией свойства <see cref="ConcurrentProcessorStorage.SelectedIndex" /> в том смысле, что
        ///     он не ищет индекс, а определяет наличие карты в коллекции, используя поиск в хеш-таблице вместо линейного поиска.
        ///     Если возможно, читает значение из внутренней переменной <see cref="ConcurrentProcessorStorage.IntSelectedIndex" />.
        ///     В этом случае результат возвращается мгновенно.
        /// </remarks>
        /// <seealso cref="ConcurrentProcessorStorage.SelectedPath" />
        /// <seealso cref="ConcurrentProcessorStorage.SelectedIndex" />
        public override bool IsSelectedOne
        {
            get
            {
                lock (SyncObject)
                {
                    if (IntSelectedIndex > -1)
                        return true;

                    string selectedPath = SelectedPath;

                    return !string.IsNullOrEmpty(selectedPath) &&
                           DictionaryByKey.ContainsKey(GetStringKey(selectedPath));
                }
            }
        }

        /// <summary>
        ///     Считывает карту по указанному пути (не добавляя её в коллекцию), выполняя все необходимые проверки, характерные для
        ///     хранилища <see cref="ImageProcessorStorage" />.
        /// </summary>
        /// <param name="fullPath">Абсолютный путь к карте.</param>
        /// <returns>Возвращает считанную карту.</returns>
        /// <remarks>
        ///     Метод потокобезопасен.
        ///     При указании относительного пути возможны различные коллизии, поэтому рекомендуется всегда указывать только абсолютный путь.
        ///     Метод выполняет проверку значения <see cref="Color.A" /> в считанном изображении, с помощью метода
        ///     <see cref="ConcurrentProcessorStorage.CheckBitmapByAlphaColor(Bitmap)" />, который, в случае неудачной проверки,
        ///     выбрасывает исключение <see cref="InvalidOperationException" />.
        ///     Изображение по ширине (<see cref="Image.Width" />) должно соответствовать <see cref="FrmSample.ImageWidth" /> и
        ///     высоте (<see cref="Image.Height" />) должно соответствовать <see cref="FrmSample.ImageHeight" />, иначе будет
        ///     выброшено исключение <see cref="ArgumentException" />.
        ///     Метод выбрасывает исключения только в виде <see cref="Exception" />.
        ///     При обработке исключений необходимо проверять свойство <see cref="Exception.InnerException" />, т.к. в нём
        ///     находится первоначальное исключение.
        ///     Имя получаемой карты представляет собой имя файла без расширения, получаемое с помощью метода
        ///     <see cref="Path.GetFileNameWithoutExtension(string)" />.
        ///     <paramref name="fullPath" /> не должен содержать недопустимые символы (<see cref="Path.GetInvalidPathChars()" />),
        ///     в том числе, быть пустым (<see langword="null" />, <see cref="string.Empty" /> или состоять из пробелов), иначе
        ///     метод выбросит исключение <see cref="ArgumentException" />.
        /// </remarks>
        /// <exception cref="Exception" />
        /// <exception cref="FileNotFoundException" />
        /// <exception cref="InvalidOperationException" />
        /// <exception cref="ArgumentException" />
        /// <exception cref="ArgumentNullException" />
        /// <seealso cref="ConcurrentProcessorStorage.ReadBitmap(string)" />
        /// <seealso cref="GetProcessorTag(string)" />
        /// <seealso cref="FrmSample.DefaultOpacity" />
        /// <seealso cref="FrmSample.CheckAlphaColor(Color)" />
        /// <seealso cref="ConcurrentProcessorStorage.CheckBitmapByAlphaColor(Bitmap)" />
        /// <seealso cref="Path.GetFileNameWithoutExtension(string)" />
        /// <seealso cref="Path.GetInvalidPathChars()" />
        /// <seealso cref="FrmSample.ImageWidth" />
        /// <seealso cref="FrmSample.ImageHeight" />
        /// <seealso cref="Image.Width" />
        /// <seealso cref="Image.Height" />
        protected override Processor GetAddingProcessor(string fullPath)
        {
            try
            {
                return ImageRect.GetProcessor(ReadBitmap(fullPath), GetProcessorTag(fullPath), Height, Width);
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $@"{nameof(GetAddingProcessor)} ({StorageType}): {ex.Message}{Environment.NewLine}Путь: {fullPath}.", ex);
            }
        }

        /// <summary>
        ///     Извлекает имя (<see cref="Processor.Tag" />) искомой карты из указанного пути.
        /// </summary>
        /// <param name="fullPath">Путь, из которого требуется извлечь значение свойства <see cref="Processor.Tag" />.</param>
        /// <returns>Возвращает значение свойства <see cref="Processor.Tag" /> искомой карты.</returns>
        /// <remarks>
        ///     Метод потокобезопасен.
        ///     Имя карты представляет собой имя файла без расширения, получаемое с помощью метода
        ///     <see cref="Path.GetFileNameWithoutExtension(string)" />.
        ///     <paramref name="fullPath" /> не должен содержать недопустимые символы (<see cref="Path.GetInvalidPathChars()" />),
        ///     в том числе, быть пустым (<see langword="null" />, <see cref="string.Empty" /> или состоять из пробелов), иначе
        ///     метод выбросит исключение <see cref="ArgumentException" />.
        ///     Путь может быть как абсолютным, так и относительным.
        /// </remarks>
        /// <exception cref="ArgumentException" />
        /// <seealso cref="Path.GetFileNameWithoutExtension(string)" />
        /// <seealso cref="Path.GetInvalidPathChars()" />
        public static string GetProcessorTag(string fullPath)
        {
            if (string.IsNullOrWhiteSpace(fullPath))
                throw new ArgumentException(
                    $@"{nameof(GetProcessorTag)}: Поле {nameof(Processor.Tag)} карты не может быть пустым или белым полем.",
                    nameof(fullPath));

            try
            {
                return Path.GetFileNameWithoutExtension(fullPath);
            }
            catch (Exception ex)
            {
                throw new Exception($@"{nameof(GetProcessorTag)}: {ex.Message}{Environment.NewLine}Путь: {fullPath}.",
                    ex);
            }
        }

        /// <summary>
        ///     Сохраняет указанную карту в рабочий каталог <see cref="WorkingDirectory" />, с расширением
        ///     <see cref="ConcurrentProcessorStorage.ExtImg" />, в формате <see cref="ImageFormat.Bmp" />.
        ///     Сохраняет под уникальным названием, в случае необходимости, маскирует название карты.
        /// </summary>
        /// <param name="processor">Карта, которую требуется сохранить.</param>
        /// <remarks>
        ///     Метод потокобезопасен.
        ///     В случае отсутствия рабочего каталога (<see cref="WorkingDirectory" />), создаёт его с помощью метода (
        ///     <see cref="ConcurrentProcessorStorage.CreateWorkingDirectory()" />).
        ///     Свойство <see cref="ConcurrentProcessorStorage.SelectedPath" /> будет содержать путь, по которому карта была
        ///     сохранена.
        /// </remarks>
        /// <seealso cref="ConcurrentProcessorStorage.CreateWorkingDirectory()" />
        /// <seealso cref="ConcurrentProcessorStorage.WorkingDirectory" />
        /// <seealso cref="ConcurrentProcessorStorage.ExtImg" />
        /// <seealso cref="ConcurrentProcessorStorage.SelectedPath" />
        /// <seealso cref="WorkingDirectory" />
        /// <seealso cref="ImageFormat.Bmp" />
        public void SaveToFile(Processor processor)
        {
            if (processor == null)
                throw new ArgumentNullException(nameof(processor),
                    $@"{nameof(SaveToFile)}: Необходимо указать карту, которую требуется сохранить.");

            lock (SyncObject)
            {
                CreateWorkingDirectory();

                string path = GetUniquePath(processor.Tag);
                SaveToFile(ImageRect.GetBitmap(processor), path);
                SelectedPath = path;
            }
        }

        /// <summary>
        ///     Сохраняет группу карт в указанную папку рабочего каталога <see cref="WorkingDirectory" />.
        /// </summary>
        /// <param name="folderName">Папка для сохранения группы карт.</param>
        /// <param name="processors">Группа карт, которую требуется сохранить.</param>
        /// <remarks>
        ///     Является потокобезопасным.
        ///     В случае отсутствия указанной папки, метод создаёт её.
        ///     Путь к папке может содержать несколько уровней.
        ///     Выполняет демаскировку названия карты, если это необходимо.
        ///     Это может потребоваться в случае, если название карты уже было замаскировано или его значение совпало по форме
        ///     маскировки.
        ///     Для этого используется метод <see cref="ConcurrentProcessorStorage.ParseName(string)" />.
        /// </remarks>
        /// <seealso cref="WorkingDirectory" />
        /// <seealso cref="ConcurrentProcessorStorage.ParseName(string)" />
        public void SaveToFile(string folderName, IEnumerable<Processor> processors)
        {
            if (processors == null)
                throw new ArgumentNullException(nameof(processors),
                    $@"{nameof(SaveToFile)}: Необходимо указать карты, которые требуется сохранить.");

            string folder = CombinePaths(folderName);

            folder = AddEndingSlash(folder);

            CreateFolder(folder);

            List<Processor> lstProcs = processors.ToList();

            lock (SyncObject)
            {
                foreach ((Processor p, string path) in GetUniqueProcessor(lstProcs.Select(proc =>
                             ((Processor)null, ((ulong?)null,
                                     ParseName(proc.Tag).name),
                                 folder))).Select((pp, index) => (lstProcs[index], pp.path)))
                    SaveToFile(ImageRect.GetBitmap(p), path);
            }
        }
    }
}