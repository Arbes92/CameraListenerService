using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CameraListenerService.Configuration;

namespace CameraListenerService.Utils
{
    public enum NotificationChannel
    {
        Email = 0,
        SMS = 1,
        Telegram = 2
    }
    public static class Notify
    {
        internal static void Error(ServiceConfigurationSection config, string header, string body, List<NotificationChannel> channels = null)
        {
            if (channels == null || config == null || config.Recipients == null || config.Recipients.Count == 0)
                return;

            var sender = GetSender(config);

            foreach (RecipientElement rec in config.Recipients)
            {
                if (!rec.Enabled)
                    continue;

                if (channels.Contains(NotificationChannel.Email) && rec.Value.Contains("@"))
                    Email.SendSMTP(rec.Value, header, body, sender.Address, sender.Port, sender.Account, sender.Password, sender.Sender);
                else if (channels.Contains(NotificationChannel.Telegram) && string.IsNullOrEmpty(rec.Value) && !string.IsNullOrEmpty(rec.TelegramBotId) && !string.IsNullOrEmpty(rec.TelegramChatId))
                    Telegram.Send(rec.TelegramChatId, rec.TelegramBotId, header, body);
                else if (channels.Contains(NotificationChannel.SMS) && !string.IsNullOrEmpty(rec.Value) && !rec.Value.Contains("@"))
                    SMS.Send(rec.Value, header, string.Format("{0}\r\n{1}", header, body));
            }
        }

        private static EmailSenderElement GetSender(ServiceConfigurationSection config)
        {
            EmailSenderElement sender = null;
            if (config.EmailSenders != null && config.EmailSenders.Count != 0)
            {
                var senders = config.EmailSenders.Cast<EmailSenderElement>().Where(s => s.Enabled).ToList();
                sender = senders.Count > 0 ? senders[0] : null;
            }
            return sender;
        }
    }
}
