using DynamicProcessor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace DynamicSample
{
    [Serializable]
    internal class TosterMap
    {
        [Serializable]
        public class Toster
        {
            public int X { get; set; }
            public int Y { get; set; }
            public uint QueryNumber { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
            public List<char> Query { get; set; }
        }

        public List<Toster> Tosters { get; set; }

        /// <summary>
        /// Записывает карту в указанный поток, в формате XML.
        /// </summary>
        /// <param name="st">Поток, в который необходимо сохранить текущую карту.</param>
        public void ToStream(Stream st)
        {
            XmlSerializer formatter = new XmlSerializer(typeof(List<Toster>));
            formatter.Serialize(st, Tosters);
        }
    }
}
