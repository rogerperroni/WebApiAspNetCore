public static class ExternalApiResiliencePolicy
    {
        private static HttpClient _client;
        private static string authUserName = UrlSanitizer.Sanitize($"{Environment.GetEnvironmentVariable("AUTH_USERNAME")}");
        private static string authPassword = UrlSanitizer.Sanitize($"{Environment.GetEnvironmentVariable("AUTH_PASSWORD")}");
        private static string autenticationUrl = $"{Environment.GetEnvironmentVariable("AUTH_URL")}";
        private static string token = string.Empty;
        public static async Task<HttpResponseMessage> ExecuteExternalApiGetCallPolicy<T>(string endPoint, HttpClient client)
        {
            _client = client;
            HttpStatusCode[] httpStatusCodesWorthRetrying = {
            HttpStatusCode.RequestTimeout,
            HttpStatusCode.InternalServerError,
            HttpStatusCode.BadGateway,
            HttpStatusCode.ServiceUnavailable,
            HttpStatusCode.GatewayTimeout
		};
            var circuitBreakerTimeOutPolicy = Policy.Handle<TimeoutException>()
               .Or<Exception>()
              .CircuitBreakerAsync(3, TimeSpan.FromSeconds(2));

            var unauthorizedPolicy = Policy.HandleResult<HttpResponseMessage>(t => t.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                .RetryAsync(3, async (exception, retryCount) =>
                {
                    await GetTokenAsync(new AuthenticationDto { user_name = authUserName, password = authPassword });
                })
                .WrapAsync(circuitBreakerTimeOutPolicy);


            var httpRequestExceptionPolicy = Policy.Handle<HttpRequestException>()
                 .OrResult<HttpResponseMessage>(r => httpStatusCodesWorthRetrying.Contains(r.StatusCode))
                 .RetryAsync(3)
                 .WrapAsync(circuitBreakerTimeOutPolicy);          

            
            return await unauthorizedPolicy.ExecuteAsync(() => GetApiResponse(endPoint));
        }

        private static async Task<HttpResponseMessage> GetApiResponse(string endpoint)
        {
            SetHeadersAsync();
            return await _client.GetAsync(endpoint);

        }
        private static void SetHeadersAsync()
        {
            if (!_client.DefaultRequestHeaders.Contains("Authorization"))
            {
                _client.DefaultRequestHeaders.Remove("Authorization");
                _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            }
        }
        private static void RemoveHeadersAsync()
        {
            if (_client.DefaultRequestHeaders.Contains("Authorization"))
            {
                _client.DefaultRequestHeaders.Remove("Authorization");               
            }
        }
        private static async Task<string> GetTokenAsync(AuthenticationDto autentication)
        {
            RemoveHeadersAsync();
            var response = await _client.PostJsonAsync(autenticationUrl, autentication);
            token = await response.Content.ReadJsonAttributeAsync<string>("access_token");
            return token;

        }
    }
