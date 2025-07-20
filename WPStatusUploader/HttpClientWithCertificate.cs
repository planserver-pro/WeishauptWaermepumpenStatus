
using System.Security.Cryptography.X509Certificates;


//https://webscraping.ai/faq/httpclient-c/how-do-i-use-httpclient-c-with-a-client-side-certificate

namespace WPStatusUploader
{
    public class HttpClientWithCertificate
    {
        public string LastResult = "";
        public async Task MakeRequestWithClientCertificate(string urlWithQueryString)
        {
            //var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

            try
            {
                // Load the certificate from a file (alternatively, it can be from a store or byte array)
                // Ensure the file contains the private key if the server requires client authentication
                var certificate = new X509Certificate2(AppContext.BaseDirectory+"upload-client-cert.pfx", "xyz");

                // Create an HttpClientHandler and add the certificate
                var handler = new HttpClientHandler();
                //Um Fehler in Bezug auf Certificate zu vermeiden:
                //https://stackoverflow.com/questions/38138952/bypass-invalid-ssl-certificate-in-net-core
                handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                handler.ClientCertificates.Add(certificate);

                // Create an HttpClient with the handler
                using (var client = new HttpClient(handler))
                {
                    // Make the HTTP request
                    var response = await client.GetAsync (urlWithQueryString);


                    // Ensure we got a successful response
                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"Error: {response.StatusCode}");
                        Serilog.Log.Information($"Error: {response.StatusCode}");
                        LastResult = "ERR";
                        return;
                    }
                    else
                    {
                        LastResult = "OK";
                    }
                    // Read the response content (if any)
                    var contentResponse = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("Response: " + contentResponse);
                    Serilog.Log.Information("Response: " + contentResponse);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR:" + e.ToString());
                Serilog.Log.Information("ERROR:" + e.ToString());
            }


           
        }
    }
}
