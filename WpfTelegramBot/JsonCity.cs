using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfTelegramBot
{
    class JsonCity
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public string State { get; set; }

        public string Country { get; set; }

        public Coordinates Coord { get; set; }

    }

    class Coordinates
    {
        public float Lon { get; set; }
        public float Lat { get; set; }
    }
}
