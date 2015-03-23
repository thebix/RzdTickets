using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace RzdTickets
{
    public class Journey
    {
        #region Константы
        //0 - {from}
        //1 - {code_from}
        //2 - {date}
        //3 - {time} //21-24
        //4 - {to}
        //5 - {code_to}
        //6 - {date}
        const string URL_PATTERN = "http://pass.rzd.ru/timetable/public/ru?STRUCTURE_ID=735&layer_id=5371&dir=0&tfl=3&checkSeats=1&st0={0}&code0={1}&dt0={2}&ti0={3}&st1={4}&code1={5}&dt1={6}";
        const string URL_PATTERN_SESSION = "&rid={0}&SESSION_ID={1}";

        //Тексты
        const string TEXT_REQUEST_SUCCESS = "Запрос вернул таблицу поездов";
        const string TEXT_NO_CONDITION_TRAINS = "Подходящие поезда не найдены";
        //0 - {from}
        //1 - {to}
        //2 - {date}
        //3 - {time}
        //4 - {trains}
        //5 - {wagons}
        //6 - {price}
        //7 - {seats}
        const string TEXT_REQUEST_INFO = "Запрос: {0} - {1} {2} {3}. Поезда: {4}, вагоны: {5}, цена до: {6}, мест: {7}";
        const string TEXT_TRAIN_INFO = "{0} {1} {2} - {3} {4}";
        //Ошибки
        const string ERR_NO_DATA = "Первый или второй запрос не вернул данных";
        const string ERR_DESERIALIZE_EMPTY = "При десереализации данных из json результат null";
        const string ERR_SERVER_RETURN_ERROR = "Сервер вернул ошибку";
        const string ERR_TABLE_NO_DATA = "Не удалось получить таблицу данных";
        const string ERR_LIST_NO_DATA = "Не удалось получить список поездов";
        const string ERR_WAGON_NO_DATA = "Не удалось получить список вагонов поезда";
        const string ERR_RID_DATA_AGAIN = "Сервер повторно вернул данные о сессии. Повтор запроса";

        public const string KEY_IN_REQUEST_INTERVAL = "InRequestInterval";
        const string VALUE_RESULT_RID = "RID";
        const string VALUE_RESULT_OK = "OK"; 
        #endregion

        #region Глобальные перменные
        CookieAwareWebClient _client;
        RequestConfig _request;

        bool _checkTrains;     //Поиск по конкретным номерам поезда
        bool _checkWagonTypes;
        bool _checkPrice;
        bool _checkSeats;
        string _url;
        static int _inRequestInterval;
        #endregion

        #region Конструкторы
        public Journey(RequestConfig request)
        {
            _client = new CookieAwareWebClient();
            _client.Encoding = System.Text.Encoding.UTF8;
            _request = request;

            _checkTrains = _request.TrainsList.Count > 0;
            _checkWagonTypes = _request.WagonTypesList.Count > 0;
            _checkPrice = _request.Price.HasValue && _request.Price.Value > 0;
            _checkSeats = _request.Seats.HasValue && _request.Seats.Value > 0;

            _inRequestInterval = int.Parse(System.Configuration.ConfigurationSettings.AppSettings[KEY_IN_REQUEST_INTERVAL]) * 1000;
            _url = string.Format(URL_PATTERN,
                    _request.From, _request.FromId, _request.Date.ToString(Misc.FORMAT_DATE), _request.Time, _request.To, _request.ToId, DateTime.Now.ToString(Misc.FORMAT_DATE));
        }
        #endregion

        #region Методы
        public List<Train> CheckTickets()
        {
            List<Train> trains = new List<Train>();
            Misc.WriteLog(string.Format(TEXT_REQUEST_INFO,
                _request.From, _request.To, _request.Date.ToShortDateString(), _request.Time, _request.Trains, _request.WagonTypes, _request.Price, _request.Seats));

            try
            {
                //Первый запрос - получение сессии
                var res = GetResponse(_url);

                dynamic data = JsonConvert.DeserializeObject(res);
                if (data == null)
                    throw new Exception(ERR_DESERIALIZE_EMPTY);

                Misc.WriteDebug(string.Format("SESSION_ID: {0}, rid: {1}", data.SESSION_ID, data.rid));

                //Второй запрос -- получение расписания и мест
                trains = RequestTrains(data);
            }
            catch (Exception ex)
            {
                Misc.WriteLog(ex);
            }
            finally
            {
                _client.Dispose();
            }
            return trains;
        }
        #endregion

        #region Вспомогательные методы
        /// <summary>
        /// Второй запрос, получение данных по поездке
        /// </summary>
        private List<Train> RequestTrains(dynamic sessionData)
        {
            Thread.Sleep(_inRequestInterval);
            //Второй запрос -- получение расписания и мест
            var url = string.Format("{0}{1}", _url, string.Format(URL_PATTERN_SESSION, sessionData.rid, sessionData.SESSION_ID));
            var res = GetResponse(url);

            dynamic data = JsonConvert.DeserializeObject(res);
            if (data == null)
                throw new Exception(ERR_DESERIALIZE_EMPTY);

            //Иногда сервер повторно возвращает данные с сессией => запрос надо повторить
            if (data.result == VALUE_RESULT_RID)
            {
                Misc.WriteLog(ERR_RID_DATA_AGAIN);
                return RequestTrains(data);
            }
            if (data.result != VALUE_RESULT_OK)
                throw new Exception(ERR_SERVER_RETURN_ERROR);

            dynamic table = data.tp;
            if (table == null)
                throw new Exception(ERR_TABLE_NO_DATA);

            dynamic list = table[0].list;
            if (list == null)
                throw new Exception(ERR_LIST_NO_DATA);

            var trains = JsonConvert.DeserializeObject<IList<Train>>(list.ToString(), new IsoDateTimeConverter { DateTimeFormat = Misc.FORMAT_DATE });

            Misc.WriteDebug(TEXT_REQUEST_SUCCESS);

            //получение удовлетворяющих условиям поездов
            trains = GetTrainsByConditions(trains);

            return trains;
        }
        /// <summary>
        /// Получение ответа от сервера по url
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private string GetResponse(string url)
        {
            var res = _client.DownloadString(url);
            if (string.IsNullOrEmpty(res))
                throw new Exception(ERR_NO_DATA);
            return res;
        }

        private List<Train> GetTrainsByConditions(IList<Train> list)
        {
            List<Train> resTrains = new List<Train>();

            foreach (var train in list)
            {
                int trainNum = Convert.ToInt32(Regex.Replace(train.Number, "[^0-9]", ""));

                //условие на номера поездов
                if (_checkTrains && !_request.TrainsList.Contains(trainNum))
                    continue;

                if (train.Cars.Count == 0)
                    continue;

                if (train.Cars == null || train.Cars.Count == 0)
                {
                    Misc.WriteLog(string.Format("{0}. {1}", ERR_WAGON_NO_DATA, GetTrainInfo(train)), true);
                    continue;
                }

                List<Wagon> resWagons = new List<Wagon>();
                foreach (var wagon in train.Cars)
                {
                    if (_checkWagonTypes && !_request.WagonTypesList.Contains(wagon.Type))
                        continue;

                    if (_checkPrice && _request.Price <= wagon.Tariff)
                        continue;

                    if (_checkSeats && _request.Seats > wagon.FreeSeats)
                        continue;

                    //вагон подходит по критериям поиска, откладываем его
                    resWagons.Add(wagon);
                }

                //найдены удовлетворяющие условиям поиска вагоны
                if (resWagons.Count > 0)
                {
                    train.Cars = resWagons;
                    resTrains.Add(train);
                    Misc.WriteDebug(string.Format("Найден подходящий поезд {0} {1}-{2}. Типов мест: {3}", train.Number, train.DateFrom, train.DateTo, train.Cars.Count));
                }
            }

            return resTrains;
        }

        /// <summary>
        /// Краткая информация о поезде в строке
        /// </summary>
        /// <param name="train"></param>
        /// <returns></returns>
        private string GetTrainInfo(dynamic train)
        {
            return string.Format(TEXT_TRAIN_INFO, train.number, train.date0, train.time0, train.date1, train.time1);
        }
        #endregion

        #region Вспомогательные классы
        //Расширение WebClient для работы с куками
        class CookieAwareWebClient : WebClient
        {
            public CookieContainer CookieContainer { get; set; }

            public CookieAwareWebClient()
                : base()
            {
                CookieContainer = new CookieContainer();
            }

            protected override WebRequest GetWebRequest(Uri address)
            {
                WebRequest request = base.GetWebRequest(address);

                HttpWebRequest webRequest = request as HttpWebRequest;
                if (webRequest != null)
                {
                    webRequest.CookieContainer = CookieContainer;
                }

                return request;
            }
        }
        #endregion
    }
}
