using NLog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RzdTickets
{
    public static class Misc
    {
        #region Константы
        public const string FORMAT_DATE = "dd.MM.yyyy";
        public const string FORMAT_DATE_TIME = "dd.MM.yyyy HH:mm";
        public const string FORMAT_TIME = "HH:mm";

        const string KEY_IS_DEBUG = "IsDebug";

        static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        #endregion

        #region Свойства
        public static bool IsDebug
        {
            get
            {
                return bool.Parse(ConfigurationSettings.AppSettings[KEY_IS_DEBUG]);
            }
        }
        #endregion

        public static void WriteLog(Exception ex)
        {
            if (ex != null)
            {
                WriteLog(ex.Message, true);
                _logger.Error(ex);
            }
        }
        public static void WriteLog(string text, bool? isError = false)
        {
            Console.WriteLine("{0}: {1}", isError.HasValue && isError.Value ? "ERROR" : "INFO", text);
            if (isError.HasValue)
            {
                if (isError.Value)
                    _logger.Error(text);
                else
                    _logger.Info(text);
            }
        }

        /// <summary>
        /// Пишет только если в АппСеттингс включен дебаг
        /// </summary>
        /// <param name="text"></param>
        public static void WriteDebug(string text)
        {
            if (!Misc.IsDebug)
                return;
            Console.WriteLine(string.Format("DEBUG: {0}", text));
            _logger.Debug(text);
        }
    }
}
