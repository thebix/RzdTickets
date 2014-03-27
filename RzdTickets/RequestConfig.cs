using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RzdTickets
{
    public class RequestConfig
    {
        #region Свойства из конфигурации
        public string From { get; set; }
        public string To { get; set; }
        public DateTime Date { get; set; }

        private string _time; //Если предпочитаемое время не передано, задаем интервал 0-24, т.е. полный день
        public string Time
        {
            get
            {
                if (string.IsNullOrEmpty(_time))
                    _time = "0-24";

                return _time;
            }
            set
            {
                _time = value;
            }
        }
        public string Trains { get; set; }
        public string WagonTypes { get; set; } //(itype: Плац=1, Люкс=6, Купе=4, Сид=3, Мягкий=5,)
        public int? Price { get; set; }
        public int? Seats { get; set; }
        #endregion

        public List<int> TrainsList
        {
            get
            {
                if (string.IsNullOrEmpty(Trains))
                    return new List<int>();

                return Trains.Split(',').Select( x=>
                    Convert.ToInt32(Regex.Replace(x, "[^0-9]","")))
                    .ToList();
            }
        }

        public List<string> WagonTypesList
        {
            get
            {
                if (string.IsNullOrEmpty(WagonTypes))
                    return new List<string>();

                return WagonTypes.Split(',').Select(x => x.Trim()).ToList();
            }
        }

    }
}
