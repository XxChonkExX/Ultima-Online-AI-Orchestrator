using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Server;

namespace Server.AIOrchestrator
{
    public static class LLMClient
    {
        private static readonly HttpClient HttpClient = new HttpClient { Timeout = TimeSpan.FromMilliseconds(120000) };
        private static readonly SemaphoreSlim RequestGate = new SemaphoreSlim(2, 2);

        /// <summary>
        /// Unified chat completion across all supported backends.
        /// Dispatches based on AIConfig.LLMBackend.
        /// </summary>
        public static async Task<string> ChatAsync(string systemPrompt, string userPrompt, string model = null)
        {
            if (!AIConfig.Enabled)
                return null;

            model = model ?? AIConfig.ModelDialogue;

            if (!await RequestGate.WaitAsync(TimeSpan.FromSeconds(30)).ConfigureAwait(false))
            {
                Console.WriteLine("[AIOrchestrator] LLM gate timeout — all slots busy");
                return null;
            }

            try
            {
                switch (AIConfig.LLMBackend)
                {
                    case LLMBackend.Ollama:
                        return await ChatOllama(systemPrompt, userPrompt, model).ConfigureAwait(false);
                    case LLMBackend.VLLM:
                    case LLMBackend.OpenAI:
                    case LLMBackend.LMStudio:
                    case LLMBackend.KoboldCpp:
                    case LLMBackend.TGI:
                        return await ChatOpenAICompatible(systemPrompt, userPrompt, model, AIConfig.LLMBackend).ConfigureAwait(false);
                    default:
                        Console.WriteLine($"[AIOrchestrator] Unsupported backend: {AIConfig.LLMBackend}");
                        return null;
                }
            }
            finally
            {
                RequestGate.Release();
            }
        }

        // ===================== OLLAMA =====================
        private static async Task<string> ChatOllama(string systemPrompt, string userPrompt, string model)
        {
            var url = AIConfig.OllamaBaseUrl.TrimEnd('/') + "/api/chat";

            // Use /api/generate with raw=true for Gemma4 to bypass chat template overhead
            // For other models, use /api/chat
            var isGemma = model?.IndexOf("gemma", StringComparison.OrdinalIgnoreCase) >= 0;
            if (isGemma)
            {
                var rawReq = new
                {
                    model = model,
                    prompt = (systemPrompt + "\n\n" + userPrompt).Trim(),
                    stream = false,
                    options = new
                    {
                        num_predict = 30,
                        num_ctx = 1024,
                        temperature = 0.7f,
                        top_p = 0.9f,
                        repeat_penalty = 1.1f
                    }
                };
                var json = JsonSerializer.Serialize(rawReq);
                using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
                {
                    var resp = await HttpClient.PostAsync(AIConfig.OllamaBaseUrl.TrimEnd('/') + "/api/generate", content).ConfigureAwait(false);
                    if (!resp.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"[AIOrchestrator] Ollama generate HTTP {(int)resp.StatusCode}");
                        return null;
                    }
                    var text = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                    try
                    {
                        using (var doc = JsonDocument.Parse(text))
                        {
                            return doc.RootElement.GetProperty("response").GetString()?.Trim() ?? "";
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[AIOrchestrator] Ollama parse error: {ex.Message}");
                        return null;
                    }
                }
            }
            else
            {
                var req = new
                {
                    model = model,
                    stream = false,
                    messages = new[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = userPrompt }
                    },
                    options = new
                    {
                        num_predict = 30,
                        num_ctx = 1024,
                        temperature = 0.7f,
                        top_p = 0.9f,
                        repeat_penalty = 1.1f
                    }
                };
                var json = JsonSerializer.Serialize(req);
                using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
                {
                    var resp = await HttpClient.PostAsync(url, content).ConfigureAwait(false);
                    if (!resp.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"[AIOrchestrator] Ollama chat HTTP {(int)resp.StatusCode}");
                        return null;
                    }
                    var text = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                    try
                    {
                        using (var doc = JsonDocument.Parse(text))
                        {
                            return doc.RootElement.GetProperty("message").GetProperty("content").GetString()?.Trim() ?? "";
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[AIOrchestrator] Ollama parse error: {ex.Message}");
                        return null;
                    }
                }
            }
        }

        // ===================== OPENAI-COMPATIBLE (vLLM, LM Studio, KoboldCpp, TGI, OpenAI) =====================
        private static async Task<string> ChatOpenAICompatible(string systemPrompt, string userPrompt, string model, LLMBackend backend)
        {
            string baseUrl;
            string apiKey;

            if (backend == LLMBackend.OpenAI)
            {
                baseUrl = "https://api.openai.com";
                apiKey = AIConfig.OpenAIApiKey;
            }
            else
            {
                baseUrl = AIConfig.OpenAIBaseUrl.TrimEnd('/');
                apiKey = AIConfig.OpenAIApiKey;
            }

            var url = baseUrl + "/v1/chat/completions";

            var req = new
            {
                model = model,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = userPrompt }
                },
                max_tokens = 30,
                temperature = 0.7f,
                top_p = 0.9f,
                frequency_penalty = 0.0f,
                presence_penalty = 0.0f,
                stream = false
            };

            var json = JsonSerializer.Serialize(req);
            using (var httpReq = new HttpRequestMessage(HttpMethod.Post, url))
            {
                httpReq.Content = new StringContent(json, Encoding.UTF8, "application/json");
                if (!string.IsNullOrEmpty(apiKey))
                    httpReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                var resp = await HttpClient.SendAsync(httpReq).ConfigureAwait(false);
                if (!resp.IsSuccessStatusCode)
                {
                    var err = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                    Console.WriteLine($"[AIOrchestrator] {backend} HTTP {(int)resp.StatusCode}: {err}");
                    return null;
                }

                var text = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                try
                {
                    using (var doc = JsonDocument.Parse(text))
                    {
                        return doc.RootElement
                            .GetProperty("choices")[0]
                            .GetProperty("message")
                            .GetProperty("content")
                            .GetString()?.Trim() ?? "";
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AIOrchestrator] {backend} parse error: {ex.Message}");
                    return null;
                }
            }
        }
    }
}