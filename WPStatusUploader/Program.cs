using Serilog;
using System.Device.Gpio;
using Microsoft.Extensions.Configuration;

namespace WPStatusUploader
{
    internal class Program
    {
        const int Pin = 21;
        const string Heizen = "HEIZEN";
        const string Kühlen = "KÜHLEN";
        public static string lastState = "X";



        static async Task Main(string[] args)
        {
            IConfiguration Configuration = new ConfigurationBuilder()
                                .Build();


            using var controller = new GpioController();
            controller.OpenPin(Pin, PinMode.InputPullUp);

            var logger = new LoggerConfiguration()
               .WriteTo.File(AppContext.BaseDirectory + "log/serilog-wpupload_.txt", rollingInterval: RollingInterval.Month)
               .CreateLogger();
            Serilog.Log.Logger = logger;
            Console.WriteLine($"{DateTime.Now}: WärmepumpenStatusUpload " + Constants.appVersion + " STARTUP");
            Serilog.Log.Information("WärmepumpenStatusUpload " + Constants.appVersion + " STARTUP");
           var currentState = controller.Read(Pin) == PinValue.Low ? Kühlen : Heizen;
            Console.WriteLine($"{DateTime.Now}: Aktueller Status: " + currentState);
            Serilog.Log.Information("WärmepumpenStatusUpload " + Constants.appVersion + " Aktueller Status: " + currentState);
            sendResultToServer(currentState).GetAwaiter().GetResult();

        }
        
        static void checkForStateChange(GpioController controller)
        {
            Console.WriteLine($"{DateTime.Now}: Checking state...");
            var currentState = controller.Read(Pin) == PinValue.Low ? Heizen : Kühlen;
            if (currentState != lastState)
            {
                Console.WriteLine($"{DateTime.Now}: PIN Status changed to: " + currentState);
                lastState = currentState;
                //sendResultToServer(currentState).GetAwaiter().GetResult();
            }
        }

        static async Task sendResultToServer(string currentState)
        {
            HttpClientWithCertificate httpClientWithCertificateOWFS = new HttpClientWithCertificate();
            Console.WriteLine($"{DateTime.Now}: Sending Status to Server...");
            await httpClientWithCertificateOWFS.MakeRequestWithClientCertificate("https://upload.xyz.de/StoreWPStatus?currentState=" + currentState);
            Serilog.Log.Information($"Upload Status done.");
            Console.WriteLine($"{DateTime.Now}: Status to Server has been sent...");
            Serilog.Log.Information($"Upload: " + currentState);
            await Task.Delay(1000);
        }
    }
}
