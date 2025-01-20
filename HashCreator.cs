using System;
using System.Collections.Generic;
using System.Linq;

namespace DynamicSample
{
    /// <summary>
    ///     Генерирует хеш-код указанной карты <see cref="Processor" />.
    /// </summary>
    internal static class HashCreator
    {
        /// <summary>
        ///     Таблица значений, необходимых для генерации хеш-кода.
        /// </summary>
        static readonly int[] Table = new int[256];

        /// <summary>
        ///     Инициализирует таблицу значений, необходимых для генерации хеш-кода.
        /// </summary>
        static HashCreator()
        {
            for (int k = 0; k < Table.Length; k++)
            {
                int num = k;

                for (int i = 0; i < 8; i++)
                    if ((uint)(num & 128) > 0U)
                        num = (num << 1) ^ 49;
                    else
                        num <<= 1;

                Table[k] = num;
            }
        }

        /// <summary>
        ///     Получает хеш-код указанной карты, без учёта значения свойства <see cref="Processor.Tag" />.
        /// </summary>
        /// <param name="p">Карта, для которой необходимо вычислить хеш.</param>
        /// <returns>Возвращает хеш-код указанной карты.</returns>
        /// <remarks>
        ///     Карта не может быть равна <see langword="null" />, иначе будет выброшено исключение <see cref="ArgumentNullException" />.
        /// </remarks>
        /// <exception cref="ArgumentNullException" />
        public static int GetHash(IEnumerable<int> p)
        {
            return GetProcessorBytes(p).Aggregate(255,
                (currentValue, currentByte) => Table[unchecked((byte)(currentValue ^ currentByte))]);
        }

        /// <summary>
        ///     Представляет содержимое указанной карты в виде последовательности байт.
        /// </summary>
        /// <param name="p">Карта, содержимое которой необходимо получить.</param>
        /// <returns>Возвращает содержимое карты в виде последовательности байт.</returns>
        /// <remarks>
        ///     Поле <see cref="Processor.Tag" /> не учитывается.
        ///     Перечисление строк карты происходит последовательно: от меньшего индекса к большему.
        /// </remarks>
        /// <exception cref="ArgumentNullException" />
        static IEnumerable<byte> GetProcessorBytes(IEnumerable<int> p)
        {
            //if (p == null)
            //  throw new ArgumentNullException(nameof(p), $@"{nameof(GetProcessorBytes)}: Карта равна значению null.");

            //for (int y = 0, my = p.GetLength(1); y < my; y++)
            //  for (int x = 0, mx = p.GetLength(0); x < mx; x++)
            foreach (int i in p)
                foreach (byte r in BitConverter.GetBytes(i)) //p[x, y]))
                    yield return r;
        }
    }
}
