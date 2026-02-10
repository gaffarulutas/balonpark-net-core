using System.Net;
using System.Net.Mail;
using System.Text;
using BalonPark.Models;

namespace BalonPark.Services;

public class EmailService(IConfiguration configuration, ILogger<EmailService> logger) : IEmailService
{

    public async Task<bool> SendContactEmailAsync(ContactFormModel contactForm)
    {
        try
        {
            var subject = $"İletişim Formu - {contactForm.Subject}";
            var htmlBody = GenerateContactEmailHtml(contactForm);
            
            return await SendEmailAsync(contactForm.Email, subject, htmlBody, true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending contact email");
            return false;
        }
    }

    public async Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true)
    {
        try
        {
            var emailSettings = configuration.GetSection("EmailSettings");
            var smtpServer = emailSettings["SmtpServer"];
            var smtpPort = int.Parse(emailSettings["SmtpPort"] ?? "465");
            var smtpUsername = emailSettings["SmtpUsername"];
            var smtpPassword = emailSettings["SmtpPassword"];
            var fromEmail = emailSettings["FromEmail"];
            var fromName = emailSettings["FromName"];
            var toEmail = emailSettings["ToEmail"];
            var enableSsl = bool.Parse(emailSettings["EnableSsl"] ?? "true");

            using var client = new SmtpClient(smtpServer, smtpPort);
            client.EnableSsl = enableSsl;
            client.Credentials = new NetworkCredential(smtpUsername, smtpPassword);

            using var message = new MailMessage();
            message.From = new MailAddress(fromEmail ?? "info@balonpark.com", fromName ?? "Balon Park");
            message.To.Add(toEmail ?? "info@balonpark.com"); // Always send to info@balonpark.com
            message.ReplyToList.Add(to); // Set reply-to as the sender's email
            message.Subject = subject;
            message.Body = body;
            message.IsBodyHtml = isHtml;
            message.BodyEncoding = Encoding.UTF8;
            message.SubjectEncoding = Encoding.UTF8;

            await client.SendMailAsync(message);
            logger.LogInformation("Email sent successfully to {ToEmail}", toEmail);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending email to {ToEmail}", to);
            return false;
        }
    }

    private string GenerateContactEmailHtml(ContactFormModel contactForm)
    {
        var siteUrl = configuration["siteUrl"];
        
        return $@"
<!DOCTYPE html>
<html lang=""tr"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>İletişim Formu - Balon Park</title>
    <style>
        body {{
            font-family: Arial, sans-serif;
            line-height: 1.6;
            color: #333;
            max-width: 100%;
            width: 100%;
            margin: 0;
            padding: 0;
            background-color: #f4f4f4;
            box-sizing: border-box;
        }}
        .container {{
            background-color: #ffffff;
            padding: 30px;
            border-radius: 10px;
            box-shadow: 0 0 10px rgba(0,0,0,0.1);
            max-width: 100%;
            width: 100%;
            box-sizing: border-box;
        }}
        .header {{
            text-align: center;
            border-bottom: 3px solid #007bff;
            padding-bottom: 20px;
            margin-bottom: 30px;
        }}
        .header h1 {{
            color: #007bff;
            margin: 0;
            font-size: 24px;
        }}
        .header p {{
            color: #666;
            margin: 10px 0 0 0;
        }}
        .form-details {{
            background-color: #f8f9fa;
            padding: 20px;
            border-radius: 5px;
            margin-bottom: 20px;
            max-width: 100%;
            width: 100%;
            box-sizing: border-box;
        }}
        .form-details h2 {{
            color: #007bff;
            margin-top: 0;
            font-size: 18px;
        }}
        .detail-row {{
            margin-bottom: 15px;
            padding-bottom: 10px;
            border-bottom: 1px solid #eee;
            max-width: 100%;
            width: 100%;
            box-sizing: border-box;
            word-wrap: break-word;
            overflow-wrap: break-word;
        }}
        .detail-row:last-child {{
            border-bottom: none;
            margin-bottom: 0;
        }}
        .label {{
            font-weight: bold;
            color: #555;
            display: inline-block;
            min-width: 100px;
            max-width: 150px;
            width: auto;
        }}
        .value {{
            color: #333;
            word-wrap: break-word;
            overflow-wrap: break-word;
            max-width: 100%;
        }}
        .message-content {{
            background-color: #fff;
            padding: 20px;
            border-left: 4px solid #007bff;
            margin-top: 20px;
            max-width: 100%;
            width: 100%;
            box-sizing: border-box;
        }}
        .message-content h3 {{
            color: #007bff;
            margin-top: 0;
        }}
        .message-content p {{
            max-width: 100%;
            width: 100%;
            word-wrap: break-word;
            overflow-wrap: break-word;
            box-sizing: border-box;
        }}
        .footer {{
            text-align: center;
            margin-top: 30px;
            padding-top: 20px;
            border-top: 1px solid #eee;
            color: #666;
            font-size: 14px;
            max-width: 100%;
            width: 100%;
            box-sizing: border-box;
        }}
        .footer p {{
            max-width: 100%;
            width: 100%;
            word-wrap: break-word;
            overflow-wrap: break-word;
            box-sizing: border-box;
        }}
        .footer a {{
            color: #007bff;
            text-decoration: none;
            word-wrap: break-word;
            overflow-wrap: break-word;
        }}
        .footer a:hover {{
            text-decoration: underline;
        }}
        /* Ensure all elements are responsive */
        * {{
            max-width: 100%;
            box-sizing: border-box;
        }}
        /* Mobile responsiveness */
        @media (max-width: 600px) {{
            .container {{
                padding: 15px;
                margin: 10px;
            }}
            .form-details {{
                padding: 15px;
            }}
            .message-content {{
                padding: 15px;
            }}
            .label {{
                display: block;
                width: 100%;
                margin-bottom: 5px;
            }}
        }}
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""header"">
            <h1>Balon Park Şişme Oyun Grupları</h1>
            <p>İletişim Formu Bildirimi</p>
        </div>

        <div class=""form-details"">
            <h2>Gönderen Bilgileri</h2>
            <div class=""detail-row"">
                <span class=""label"">Ad Soyad:</span>
                <span class=""value"">{contactForm.Name}</span>
            </div>
            <div class=""detail-row"">
                <span class=""label"">E-posta:</span>
                <span class=""value"">{contactForm.Email}</span>
            </div>
            <div class=""detail-row"">
                <span class=""label"">Telefon:</span>
                <span class=""value"">{contactForm.Phone}</span>
            </div>
            <div class=""detail-row"">
                <span class=""label"">Konu:</span>
                <span class=""value"">{contactForm.Subject}</span>
            </div>
            <div class=""detail-row"">
                <span class=""label"">Gönderim Tarihi:</span>
                <span class=""value"">{contactForm.SubmittedAt:dd.MM.yyyy HH:mm}</span>
            </div>
        </div>

        <div class=""message-content"">
            <h3>Mesaj İçeriği</h3>
            <p>{contactForm.Message.Replace("\n", "<br>")}</p>
        </div>

        <div class=""footer"">
            <p>Bu e-posta <a href=""{siteUrl}"">Balon Park</a> web sitesindeki iletişim formu aracılığıyla gönderilmiştir.</p>
            <p>Yanıtlamak için bu e-postayı yanıtlayabilir veya {contactForm.Email} adresine doğrudan e-posta gönderebilirsiniz.</p>
        </div>
    </div>
</body>
</html>";
    }

}
