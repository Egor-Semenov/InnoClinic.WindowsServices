using Serilog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.ServiceProcess;
using System.Timers;

namespace InnoClinic.WindowsServices
{
    public sealed partial class EmailService : ServiceBase
    {
        private const int INTERVAL = 24 * 60 * 60 * 1000;
        private readonly Timer _scheduler;
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _smtpEmailAddress;
        private readonly string _googleAppPassword;
        private readonly string _sqlConnection;

        public EmailService()
        {
            _scheduler = new Timer();
            _scheduler.Elapsed += new ElapsedEventHandler(SendEmail);
            _scheduler.Enabled = true;
            _scheduler.AutoReset = true;

            Log.Logger = new LoggerConfiguration()
                .WriteTo.File("D:\\InnoClinicWindowsServices\\Logs.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            _smtpHost = ConfigurationManager.AppSettings["SmtpHost"];
            _smtpPort = int.Parse(ConfigurationManager.AppSettings["SmtpPort"]);
            _smtpEmailAddress = ConfigurationManager.AppSettings["SmtpEmailAddress"];
            _googleAppPassword = ConfigurationManager.AppSettings["GoogleAppPassword"];
            _sqlConnection = ConfigurationManager.AppSettings["SqlConnectionString"];

            Log.Information("EmailService initialized.");

            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            _scheduler.Start();
            _scheduler.Interval = INTERVAL;
            Log.Information("Timer start.");
        }

        protected override void OnStop()
        {
            _scheduler.Stop();
            Log.Information("Timer stop.");
        }

        private void SendEmail(object source, ElapsedEventArgs e)
        {
            var emails = GetEmails();
            if (emails is null || !emails.Any())
            {
                return;
            }

            try
            {
                using (var smtpClient = new SmtpClient(_smtpHost, _smtpPort))
                {
                    smtpClient.UseDefaultCredentials = false;
                    smtpClient.Credentials = new NetworkCredential(_smtpEmailAddress, _googleAppPassword);
                    smtpClient.EnableSsl = true;

                    using (var mailMessage = new MailMessage())
                    {
                        mailMessage.From = new MailAddress(_smtpEmailAddress, "InnoClinic");

                        foreach (var email in emails)
                        {
                            mailMessage.To.Add(email);
                        }

                        mailMessage.Subject = "Birthday Greetings";
                        mailMessage.Body = "Happy Birthday! This is your special day.";

                        smtpClient.Send(mailMessage);
                        Log.Information("Email Sent.");
                    }
                }           
            }
            catch(Exception ex)
            {
                Log.Error(ex, ex.Message);
            }
        }

        private List<string> GetEmails()
        {
            try
            {
                Log.Information("Fetching emails from the database");
                var emails = new List<string>();

                using (var connection = new SqlConnection(_sqlConnection))
                {
                    connection.Open();

                    var getQuery = "SELECT Email FROM Doctors WHERE DAY(BirthDate) = DAY(GETDATE()) AND MONTH(BirthDate) = MONTH(GETDATE());";
                    var query = new SqlCommand(getQuery, connection);

                    var reader = query.ExecuteReader();

                    while (reader.Read())
                    {
                        emails.Add(reader.GetString(0));
                    }

                    Log.Information("Emails fetched successfully.");
                }

                return emails;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error fetching emails from the database.");
                return new List<string>();
            }
        }
    }
}
