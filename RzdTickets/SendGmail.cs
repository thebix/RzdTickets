using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace RzdTickets
{
    public class SendGmail : ISendNotification
    {
        private const string _host = "smtp.gmail.com";
        private readonly MailAddress _from;
        private readonly MailAddress _to;
        private readonly string _subject;
        private readonly string _password;

        public SendGmail()
        {
            _from = new MailAddress(ConfigurationSettings.AppSettings["GMailFrom"]);
            _to = new MailAddress(ConfigurationSettings.AppSettings["GMailTo"]);
            _password = ConfigurationSettings.AppSettings["GMailPassword"];
            _subject = ConfigurationSettings.AppSettings["GMailSubject"];

        }

        //TODO: try-catch
        public void SendInfo(string body)
        {
            var smtp = new SmtpClient
            {
                Host = _host,
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(_from.Address, _password)
            };
            using (var message = new MailMessage(_from, _to)
            {
                Subject = _subject,
                Body = body
            })
            {
                message.IsBodyHtml = true;
                smtp.Send(message);
            }
        }
    }
}
