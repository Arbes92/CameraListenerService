using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace CameraListenerService.Utils
{
    public static class Email
    {
        public static void Send(string aRecipient, string aSubject, string aBodyText)
        {
            try
            {
                if (string.IsNullOrEmpty(aRecipient))
                    return;

                var myMessage = new MailMessage("fleetwise@l-track.com", aRecipient, aSubject, aBodyText) { IsBodyHtml = false };
                myMessage.Body += Environment.NewLine + Environment.NewLine + "Date: " + DateTime.Now.ToString("dd-MM-yyyy HH:mm");
                var smtpClient = new SmtpClient("mail.zen.co.uk");
                smtpClient.Send(myMessage);
            }
            catch (Exception ex)
            {
                Logger.GetInstance().Exception("Failed to send email.", ex, string.Empty);
            }
        }
        /// <summary>
        /// Sends the SMTP email.
        /// </summary>
        /// <param name="aRecipient">A recipient.</param>
        /// <param name="aSubject">A subject.</param>
        /// <param name="aBodyText">A body text.</param>
        /// <param name="cfgSmtpAddress">The SMTP address (read from the config file).</param>
        /// <param name="cfgSmtpPort">The SMTP port (read from the config file).</param>
        /// <param name="cfgAccount">The SMTP account (read from the config file).</param>
        /// <param name="cfgPassword">The SMTP account password (read from the config file).</param>
        /// <param name="cfgSender">The SMTP sender mailbox (read from the config file).</param>
        public static void SendSMTP(string aRecipient, string aSubject, string aBodyText, string cfgSmtpAddress, int cfgSmtpPort, string cfgAccount, string cfgPassword, string cfgSender)
        {
            try
            {
                var client = new SmtpClient(cfgSmtpAddress, cfgSmtpPort)
                {
                    EnableSsl = true,
                    Credentials = new System.Net.NetworkCredential(cfgAccount, cfgPassword)
                };
                var from = new MailAddress(cfgSender, String.Empty, Encoding.UTF8);
                var to = new MailAddress(aRecipient);
                var message = new MailMessage(from, to)
                {
                    Body = aBodyText,
                    BodyEncoding = Encoding.UTF8,
                    Subject = aSubject,
                    SubjectEncoding = Encoding.UTF8,
                    IsBodyHtml = true
                };

                client.Send(message);
            }
            catch (Exception ex)
            {
                Logger.GetInstance().Exception("Failed to send SMTP email.", ex, string.Empty);
            }
        }
    }
}
