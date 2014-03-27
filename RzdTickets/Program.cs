using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NLog;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RzdTickets
{
    class Program
    {
        #region Константы
        const string TEXT_START = "Начало работы";
        const string TEXT_END = "Окончание работы";
        const string TEXT_PRESS_KEY_TO_END = "Для завершения работы нажать любую клавишу";

        const string ERR_CONFIG_FILE_NOT_EXISTS = "Файл с конфигурацией запросов по билетам не найден";
        const string ERR_CONFIG_FILE_EMPTY = "Файл с конфигурацией пуст";
        const string ERR_CONFIG_EMPTY = "Не удалось правильно прочитать конфигурацию из файла";

        const string TEXT_HAS_CONDITION_TRAINS = "Подходящие поезда найдены";
        const string TEXT_CONFIG_DONE = "Поезда для всех запросов найдены - больше проверок не будет. Для завершения работы приложения нажмите любую клавишу";

        const string KEY_CHECK_INTERVAL = "CheckInterval";

        const string FILE_REQUESTS_CONFIG = "RequestsConfig";

        static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        #endregion

        static List<RequestConfig> _requestsConfig;
        static int _checkInterval;
        static Timer timer;

        static void Main(string[] args)
        {
            Misc.WriteLog(TEXT_START);
            try
            {
                _checkInterval = int.Parse(System.Configuration.ConfigurationSettings.AppSettings[KEY_CHECK_INTERVAL]) * 1000 * 60;
                _requestsConfig = ReadRequestsConfig();
                timer = new Timer(CheckTickets, null, 0, _checkInterval);
            }
            catch (Exception ex)
            {
                Misc.WriteLog(ex);
            }
            finally
            {
                Misc.WriteLog(TEXT_PRESS_KEY_TO_END, null);
                Console.ReadKey();
                Misc.WriteLog(TEXT_END);
            }
        }

        /// <summary>
        /// Чтение файла запросов на поиск
        /// </summary>
        /// <returns></returns>
        public static List<RequestConfig> ReadRequestsConfig()
        {
            var configFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FILE_REQUESTS_CONFIG);
            if (!File.Exists(configFile))
                throw new Exception(ERR_CONFIG_FILE_NOT_EXISTS);
            string configText = File.ReadAllText(configFile);
            if (string.IsNullOrEmpty(configText))
                throw new Exception(ERR_CONFIG_FILE_EMPTY);
            var config = JsonConvert.DeserializeObject<List<RequestConfig>>(configText, new IsoDateTimeConverter { DateTimeFormat = Misc.FORMAT_DATE });
            if (config == null || config.Count == 0)
                throw new Exception(ERR_CONFIG_EMPTY);

            return config;
        }

        /// <summary>
        /// Последовательное выполнение запросов из конфигурации. По таймеру.
        /// </summary>
        /// <param name="o"></param>
        private static void CheckTickets(Object o)
        {
            Dictionary<RequestConfig, List<Train>> result = new Dictionary<RequestConfig,List<Train>>();

            Misc.WriteLog("======================================================================");
            Misc.WriteLog("======================================================================");
            foreach (var item in _requestsConfig)
            {
                var journey = new Journey(item);
                var trains = journey.CheckTickets();
                //var trains = new Journey(item).CheckTickets();
                if (trains.Count > 0)
                {
                    Misc.WriteLog(TEXT_HAS_CONDITION_TRAINS);
                    result.Add(item,trains);
                }
                Misc.WriteLog("---------------------------------------------------------------------");
            }

            if (result.Count == 0)
                return;

            StringBuilder sb = new StringBuilder();
            foreach(var key in result.Keys)
            {
                sb.Append(Messages.GenerateHtmlMail(result[key], key));
                _requestsConfig.Remove(key);
            }

            //отправка данных
            new SendGmail().SendInfo(sb.ToString());


            if (_requestsConfig.Count == 0)
            {
                Misc.WriteLog(TEXT_CONFIG_DONE);
                timer.Dispose();
            }
        }
    }
}
