using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Wpf.Ui.Controls;
using System.Security.Authentication;

namespace CrowdControl.Pages
{
    /// <summary>
    /// Interaction logic for PartnerProgram.xaml
    /// </summary>
    public partial class PartnerProgram : UiPage
    {
        public PartnerProgram()
        {
            InitializeComponent();
        }
        public static string GetToken()
        {
            HttpListener listener = new();
            // Add the prefixes.
            listener.Prefixes.Add("http://localhost:3245/");
            listener.Start();
            // Note: The GetContext method blocks while waiting for a request.
            HttpListenerContext context = listener.GetContext();
            HttpListenerRequest request = context.Request;
            // Obtain a response object.
            HttpListenerResponse response = context.Response;
            // Construct a response.
            string responseString = "<script>window.close(); window.location = \"about:blank\";</script>";
            byte[] buffer = Encoding.UTF8.GetBytes(responseString);
            // Get a response stream and write the response to it.
            response.ContentLength64 = buffer.Length;
            Stream output = response.OutputStream;
            output.Write(buffer, 0, buffer.Length);
            // You must close the output stream.
            output.Close();
            Task.Run(() =>
            {
                Task.Delay(500).Wait();
                listener.Stop();
            });
            if (request.QueryString.Count == 1)
                if (request.QueryString.Keys[0] == "app_token")
                    return request.QueryString.Get(0) ?? string.Empty;
            return string.Empty;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = "https://steam.scrapmechanic.run/paypal/api?get_app_token"
            });

            string app_token = GetToken();
            string cost = "";/*Cost.Text;*/
            var url = "about:blank";
            
            HttpClientHandler handler = new()
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
                ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, cetChain, policyErrors) => true
            };
            using (HttpClient client = new(handler))
            {
                
                var tsk = client.GetAsync($"https://steam.scrapmechanic.run/paypal/api?app_token={app_token}&steamid=76561198299556567&amount=" + cost);
                url = tsk.GetAwaiter().GetResult().Content.ReadAsStringAsync().GetAwaiter().GetResult();
            }
            Process.Start(new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = url
            });
        }
    }
}
