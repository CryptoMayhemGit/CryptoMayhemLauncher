using CryptoMayhemLauncher.Interfaces;
using CryptoMayhemLauncher.Models;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Net.Http;
using System;
using Newtonsoft.Json;
using Mayhem.Util.Exceptions;
using Mayhem.Dal.Dto.Dtos;
using System.Collections.Generic;

namespace CryptoMayhemLauncher.Services
{
    public class LivepeerService : ILivepeerService
    {
        private const string errorMessage = "Cannot communicate with version api.";
        private readonly HttpClient httpClient;
        private readonly ILogger<VersionService> logger;
        private List<LivepeerAssetDto> livepeerAssetDto = new List<LivepeerAssetDto>();
        private int index = 0;

        public LivepeerService(IHttpClientFactory httpClientFactory, ILogger<VersionService> logger)
        {
            this.logger = logger;

            httpClient = httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "7763535c-fafe-4e6f-bbf6-3aab1812fbd9");
        }

        public string GetNextAssetUrl()
        {
            string result = string.Empty;
            if (index < livepeerAssetDto.Count)
            {
                result = livepeerAssetDto[index].url;
                index++;
            }
            else
            {
                result = livepeerAssetDto[0].url;
                index = 1;
            }

            return result;
        }

        public async Task Init()
        {
            HttpResponseMessage response;
            try
            {
                response = await httpClient.GetAsync($"https://livepeer.studio/api/asset");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, errorMessage);
                throw new InternalException(new ValidationMessage(ResponseCodes.CantCommunicateWithVersionApi, errorMessage));
            }

            if (response.IsSuccessStatusCode)
            {
                string products = await response.Content.ReadAsStringAsync();

                livepeerAssetDto = JsonConvert.DeserializeObject< List<LivepeerAssetDto>>(products);
                livepeerAssetDto.RemoveAll(item => item.url == null);
            }
            else
            {
                logger.LogError(errorMessage);
                throw new InternalException(new ValidationMessage(ResponseCodes.CantCommunicateWithVersionApi, errorMessage));
            }

        }
    }
}
