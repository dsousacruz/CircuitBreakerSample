using Polly;
using Polly.CircuitBreaker;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CircuitBreakerSample
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var circuitBreaker = Policy
                                    .Handle<Exception>()
                                    .CircuitBreakerAsync(
                                        exceptionsAllowedBeforeBreaking: 3,
                                        durationOfBreak: TimeSpan.FromSeconds(2),
                                        onBreak: (ex, timeSpan) =>
                                        {
                                            Console.WriteLine($"\n\n------- Circuit Open -------\n\nError => {ex.Message}\n\n");
                                        },
                                        onReset: () =>
                                        {
                                            Console.WriteLine("\n\n------- Circuit Closed -------\n\n");
                                        }
                                    );

            string result = "";

            Func<CancellationToken, Task> cacheFunction = async (CancellationToken cancellationToken) => {
                result = await GetCache(cancellationToken);
            };

            var policy = Policy
                            .Handle<Exception>()
                            .FallbackAsync(cacheFunction)
                            .WrapAsync(circuitBreaker);

            for (int i = 0; i <= 1000; i++)
            {
                Thread.Sleep(TimeSpan.FromSeconds(1));
                Console.WriteLine($"\nEstado do circuito => {circuitBreaker.CircuitState}");

                policy.ExecuteAsync(async () =>
                {
                    result = await Get();
                });

                Console.WriteLine(result);
            }
        }

        private static Task<string> Get()
        {
            var requestUri = $"http://demo6093061.mockable.io/polly";

            using (var client = new HttpClient())
            {
                HttpResponseMessage response = client.GetAsync(requestUri).Result;

                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                    throw new HttpRequestException("Erro ao chamar WS");

                //Console.WriteLine(response.Content.ReadAsStringAsync().Result);

                return response.Content.ReadAsStringAsync();
            }
        }

        private static Task<string> GetCache(CancellationToken cancellationToken)
        {
            //Console.WriteLine("GET CACHE");
            return Task.FromResult("GET CACHE");
        }
    }
}
