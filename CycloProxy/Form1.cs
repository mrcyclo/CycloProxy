using CycloProxyCore;
using System.Net;

namespace CycloProxy
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            ProxyServer proxy = new ProxyServer();
            proxy.Start(IPAddress.Any, 8888);
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;

            HttpClient client = new HttpClient(new HttpClientHandler
            {
                Proxy = new WebProxy
                {
                    Address = new Uri($"http://localhost:8888"),
                }
            });
            try
            {
                await client.GetAsync("http://example.com");
            }
            catch (Exception)
            {

            }
            client.Dispose();

            button1.Enabled = true;
        }
    }
}