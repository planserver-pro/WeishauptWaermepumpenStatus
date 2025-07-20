using Limilabs.Mail.Headers;
using Limilabs.Mail;
using Limilabs.Client.SMTP;

namespace geothermieUploadServer
{
    public class clsCommunication
    {
        public void sendMail(string subject, string body, string toAddress, string BCCAdresses, IConfiguration config)
        {
            //string fileName = Limilabs.Mail.Licensing.LicenseHelper.GetLicensePath();
            //LicenseStatus status = Limilabs.Mail.Licensing.LicenseHelper.GetLicenseStatus();
            MailBuilder builder = new MailBuilder();
            builder.From.Add(new MailBox(config["SMTP_senderaddress"], config["SMTP_senderName"]));
            builder.To.Add(new MailBox(toAddress));
            foreach (var address in BCCAdresses.Split(','))
            {
                if (!string.IsNullOrWhiteSpace(address))
                {
                    builder.Bcc.Add(new MailBox(address.Trim()));
                }
            }
            builder.Subject = subject;
            builder.Text = body;
            IMail email = builder.Create();
            using (Smtp smtp = new Smtp())
            {
                smtp.Connect(config["SMTP_server"]);    // or ConnectSSL for SSL
                smtp.UseBestLogin(config["SMTP_username"], config["SMTP_password"]); // remove if authentication is not needed

                ISendMessageResult result = smtp.SendMessage(email);
                if (result.Status == SendMessageStatus.Success)
                {
                    // Message was sent.
                }

                smtp.Close();
            }
        }
    }
}
