using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
public class LlamaModelController : MonoBehaviour
{
    private string apiKey = "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"; // Reemplaza esto con tu API Key de Groq
    private string apiUrl = "https://api.groq.com/v1/chat/completions";
    public async Task<string> GenerateContentAsync(string prompt, string model)
    {
        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            var requestBody = new
            {
                messages = new[]
                {
                    new {
                        role = "user",
                        content = prompt
                    }
                },
                model = model
            };

            string jsonBody = JsonUtility.ToJson(requestBody);
            HttpContent content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync(apiUrl, content);

            if (response.IsSuccessStatusCode)
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                return responseBody;
            }
            else
            {
                Debug.LogError($"Error en la solicitud: {response.StatusCode}");
                return null;
            }
        }
    }
}
