using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Net.Http;
using System.Threading.Tasks;
using System.Configuration;
using System.Diagnostics;
using System.Net;
using System.Net.Configuration;
using System.Net.Mail;
using System.Text;


public class Program
{
    #region Fields

    private static readonly HttpClient httpClient = new HttpClient();

    private static string connectionString = null /*ConfigurationManager.ConnectionStrings["YOUR CONFIGURARION NAME"].ConnectionString*/;

    #endregion

    #region Main

    public static async Task Main()
    {
        List<string> urls = new List<string>();

        Console.WriteLine("Getting the list of URLs in 404 Status ...");

        Console.WriteLine("Choose the option : \n 1. Test the URL in console \n 2. Test using Stored Procedure(DB)" );

        Console.WriteLine("Enter Option: ");
        string option = Console.ReadLine();

        switch(option)
        {
            case "1":
                Console.WriteLine("Enter the URl you want test : ");
                urls.Add(Console.ReadLine());
                break;
            case "2":
                Console.WriteLine("Enter the Stored Procedure name : ");
                var spName = Console.ReadLine();
                urls = (GetRequestURLsFromSp(spName));
                break;
            default:
                Console.WriteLine("To Exit, Press 0");
                break;
        }

        Dictionary<string, string> NotFoundUrls = new Dictionary<string, string>();

        foreach (string url in urls)
        {
            try
            {
                HttpResponseMessage response = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Head, url));

                //response.EnsureSuccessStatusCode();

                if (!response.IsSuccessStatusCode)
                {
                    NotFoundUrls.Add(url, Convert.ToString(response.StatusCode));

                     Console.WriteLine("404 - Not found");
                    
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accessing URL: {url} | {ex.Message}");
            }
        }

       ////Test URLs
       // NotFoundUrls.Add("YOUR TEST URL FOR SMTP TEST", "NotFound");

        if (NotFoundUrls.Count < 0)
        {
            Console.WriteLine("200 - Success!");

        }
        

        Console.WriteLine("\nTotal ULRs returns 404 : " + NotFoundUrls.Count + "\n");

        //Mail Service
        await SendEmail(NotFoundUrls);

        Console.ReadLine();
    }
    #endregion

    #region Methods

    //DataBind
    private static List<string> GetRequestURLsFromSp(string spName)
    {
        List<string> urls = new List<string>();

        try
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand(spName, connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string url = reader.GetString(0);

                            urls.Add(url);
                        }
                    }
                }
                connection.Close();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }

        return urls;
    }

    //SMTP
    private static async Task SendEmail(Dictionary<string, string> Urls)
    {

        var recipientMail = "YOUR RECIPIENT MAIL";

        //Body
        string emailBody = "<style>body,table{font-family: arial, sans-serif;border-collapse: collapse;width: 100%;}td,th {border: 1px solid #dddddd;text-align: left;padding: 5px;}tr:nth-child(even){background-color: #dddddd;}</style><h2>URL TEST</h2><table border = \"1\"><tr><th> URLs </th><th> Status </th></tr>";

        foreach (KeyValuePair<string, string> url in Urls)
        {
            emailBody += $"<tr><td><a href={url.Key}>{url.Key}</a></td><td>{url.Value}</td></tr>";
        }

        emailBody += $"</table><h3>Total Count of URLs returns 404 : {Urls.Count}</h3>";

        // Create a new MailMessage object
        MailMessage message = new MailMessage();

        message.From = new MailAddress("noreply-YOUR SENDER MAIL");
        message.To.Add(new MailAddress(recipientMail));
        message.CC.Add(new MailAddress("IF REQUIRED"));
        message.Subject = "YOUR SUBJECT";
        message.Body = emailBody;
        message.IsBodyHtml = true;

        SmtpSection smtpConfiguration = (SmtpSection)ConfigurationManager.GetSection("system.net/mailSettings/smtp");

        using (SmtpClient smtpClient = new SmtpClient(smtpConfiguration.Network.Host, smtpConfiguration.Network.Port))
        {
            smtpClient.Credentials = new NetworkCredential(smtpConfiguration.Network.UserName, smtpConfiguration.Network.Password);
            smtpClient.EnableSsl = smtpConfiguration.Network.EnableSsl;

            try
            {
                await smtpClient.SendMailAsync(message);

                Console.WriteLine("The list of URLs that returns 404 have been to sent to the mail " + recipientMail);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to send email: " + ex);
            }
        }
    }
    #endregion
}
