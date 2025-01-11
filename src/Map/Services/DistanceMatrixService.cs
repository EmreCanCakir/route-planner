using Map.Core.Cache;
using System.Globalization;

namespace Map.Services
{
    public class DistanceMatrixService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ICacheService _cacheService;
        public DistanceMatrixService(HttpClient httpClient, IConfiguration configuration, ICacheService cacheService)
        {
            _httpClient = httpClient;
            _apiKey = configuration["GoogleApiKey"]!;
            _cacheService = cacheService;
        }

        public async Task<DistanceMatrixResult> GetDistanceMatrixAsync(List<Coordinate> coordinates)
        {
            const int maxBatchSize = 10; // Google API'nin limitleri max 10*10 data alabiliyor
            int totalAddresses = coordinates.Count;

            // Matris oluşturma
            var distances = new int[totalAddresses, totalAddresses];
            var durations = new int[totalAddresses, totalAddresses];
            for (int originIndex = 0; originIndex < totalAddresses; originIndex += maxBatchSize)
            {
                for (int destinationIndex = 0; destinationIndex < totalAddresses; destinationIndex += maxBatchSize)
                {
                    // Grupları oluştur
                    var originBatch = coordinates.Skip(originIndex).Take(maxBatchSize).ToList();
                    var destinationBatch = coordinates.Skip(destinationIndex).Take(maxBatchSize).ToList();

                    // Google API'ye istek gönder
                    var result = await GetMatrixBatchAsync(originBatch, destinationBatch);

                    // Sonuçları ana matrise yerleştir
                    for (int i = 0; i < originBatch.Count; i++)
                    {
                        for (int j = 0; j < destinationBatch.Count; j++)
                        {
                            distances[originIndex + i, destinationIndex + j] = result.Distances[i][j];
                            durations[originIndex + i, destinationIndex + j] = result.Durations[i][j];
                        }
                    }
                }
            }

            return new DistanceMatrixResult
            {
                Distances = ConvertToNestedList(distances),
                Durations = ConvertToNestedList(durations)
            };
        }

        private async Task<DistanceMatrixResult> GetMatrixBatchAsync(List<Coordinate> origins, List<Coordinate> destinations)
        {
            // 1. Koordinatları API formatına dönüştür
            string originString = string.Join("|", origins.Select(c => $"{c.Latitude.ToString(CultureInfo.InvariantCulture)},{c.Longitude.ToString(CultureInfo.InvariantCulture)}"));
            string destinationString = string.Join("|", destinations.Select(c => $"{c.Latitude.ToString(CultureInfo.InvariantCulture)},{c.Longitude.ToString(CultureInfo.InvariantCulture)}"));

            await _cacheService.SetAsync("test", "Test Data");
            var test = await _cacheService.GetAsync<string>("test");
            // 2. Google Distance Matrix API URL
            string url = $"https://maps.googleapis.com/maps/api/distancematrix/json?" +
                         $"origins={originString}&destinations={destinationString}&key={_apiKey}";

            // 3. API'ye çağrı yap
            var response = await FetchDistanceMatrixDataAsync(url, _apiKey);

            // 4. Sonuçları matris formatına dönüştür
            var distances = new List<List<int>>();
            var durations = new List<List<int>>();

            foreach (var row in response.Rows)
            {
                distances.Add(row.Elements.Select(e => e.Distance?.Value ?? 0).ToList());
                durations.Add(row.Elements.Select(e => e.Duration?.Value ?? 0).ToList());
            }

            return new DistanceMatrixResult
            {
                Distances = distances,
                Durations = durations
            };
        }

        private async Task<DistanceMatrixApiResponse> FetchDistanceMatrixDataAsync(string url, string apiKey)
        {
            if (await _cacheService.GetAsync<DistanceMatrixApiResponse>(url) is DistanceMatrixApiResponse cachedResponse)
            {
                return cachedResponse;
            }

            var response = await _httpClient.GetFromJsonAsync<DistanceMatrixApiResponse>(url + "&key={_apiKey}");

            if (response == null || response.Rows == null)
            {
                throw new Exception("Google Distance Matrix API'den sonuç alınamadı.");
            }

            await _cacheService.SetAsync(url, response);

            return response;
        }

        private List<List<int>> ConvertToNestedList(int[,] array)
        {
            var list = new List<List<int>>();
            for (int i = 0; i < array.GetLength(0); i++)
            {
                var innerList = new List<int>();
                for (int j = 0; j < array.GetLength(1); j++)
                {
                    innerList.Add(array[i, j]);
                }
                list.Add(innerList);
            }
            return list;
        }


    }

    public class DistanceMatrixResult
    {
        public List<List<int>> Distances { get; set; }
        public List<List<int>> Durations { get; set; }
    }

    public class Coordinate
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class DistanceMatrixApiResponse
    {
        public string Status { get; set; }
        public List<Row> Rows { get; set; }

        public class Row
        {
            public List<Element> Elements { get; set; }
        }

        public class Element
        {
            public Distance Distance { get; set; }
            public Duration Duration { get; set; }
        }

        public class Distance
        {
            public int Value { get; set; } // Metre cinsinden
        }

        public class Duration
        {
            public int Value { get; set; } // Saniye cinsinden
        }
    }
}
