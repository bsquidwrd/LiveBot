namespace LiveBot.Core.HealthCheck
{
    public class Program
    {
        private static readonly HttpClient client = new();
        private const string serverUrl = "http://localhost/";

        private static async Task Main(string[] args)
        {
            try
            {
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Add("User-Agent", "LiveBot Health Check");

                var response = await client.GetAsync(serverUrl);
                response.EnsureSuccessStatusCode();

                Console.WriteLine("Request returned success status");
                Environment.Exit(0);
            }
            catch
            {
                Console.WriteLine("Request did not return success status");
                Environment.Exit(1);
            }
        }
    }
}