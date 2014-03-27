using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RzdTickets
{
    public class Train
    {
        const string ERR_DATE_PARSE = "Не удалось распарсить дату {0} {1}";

        public string Number { get; set; }
        public string Date0 { get; set; }
        public string Time0 { get; set; }
        public string Date1 { get; set; }
        public string Time1 { get; set; }
        public IList<Wagon> Cars { get; set; }

        public DateTime DateFrom
        {
            get
            {
                DateTime date;
                if (!DateTime.TryParse(string.Format("{0} {1}", Date0, Time0), out date))
                    throw new Exception(string.Format(ERR_DATE_PARSE, Date0, Time0));
                return date;
            }
        }
        public DateTime DateTo
        {
            get
            {
                DateTime date;
                if (!DateTime.TryParse(string.Format("{0} {1}", Date1, Time1), out date))
                    throw new Exception(string.Format(ERR_DATE_PARSE, Date1, Time1));
                return date;
            }
        }
    }

    public class Wagon
    {
        public string TypeLoc { get; set; }
        public string Type { get; set; }
        public float Tariff { get; set; }
        public int FreeSeats { get; set; }
    }
}
