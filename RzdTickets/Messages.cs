using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RzdTickets
{
    public class Messages
    {
        public static string GenerateHtmlMail(List<Train> trains, RequestConfig request)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("<h3 style='color:red;'>{0} - {1} {2} {3}. Поезда: {4}, вагоны: {5}, цена до: {6}, мест: {7}</h3>", request.From, request.To, request.Date.ToShortDateString(), request.Time, request.Trains, request.WagonTypes, request.Price, request.Seats);

            sb.AppendFormat("Найдено: {0} поездов<br />", trains.Count);
            foreach(var train in trains)
            {
                sb.AppendFormat("<h4><span style='color:blue'>{0}</span> {1} <span style='color:blue'>{2}</span>-{3} <span style='color:blue'>{4}</span></h4>", 
                    train.Number, 
                    train.DateFrom.ToString(Misc.FORMAT_DATE),
                    train.DateFrom.ToString(Misc.FORMAT_TIME),
                    train.DateTo.ToString(Misc.FORMAT_DATE),
                    train.DateTo.ToString(Misc.FORMAT_TIME));
                foreach(var wagon in train.Cars)
                {
                    sb.AppendFormat("<b>{0}</b> {1} руб {2} мест.<br />", wagon.TypeLoc,wagon.Tariff, wagon.FreeSeats);
                }
            }

            return sb.ToString();
        }
    }
}
