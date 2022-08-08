using CycloProxyCore;
using System.Net;

namespace CycloProxy
{
    public partial class Form1 : Form
    {
        private int port = 8888;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            ProxyServer proxy = new ProxyServer();
            proxy.CustomDNS.Add("www.bing.com", "127.0.0.1");
            proxy.Start(IPAddress.Any, port);
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;

            CertMaker cm = new CertMaker();
            cm.Install();

            //HttpClient client = new HttpClient(new HttpClientHandler
            //{
            //    Proxy = new WebProxy
            //    {
            //        Address = new Uri($"http://localhost:" + port),
            //    }
            //});
            //try
            //{
            //    await client.GetAsync("https://example.com");
            //}
            //catch (Exception ex)
            //{
            //    Console.WriteLine(ex);
            //}
            //client.Dispose();

            button1.Enabled = true;
        }
    }
}