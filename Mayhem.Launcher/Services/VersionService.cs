using CryptoMayhemLauncher.Interfaces;
using CryptoMayhemLauncher.Models;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Net.Http;
using System;
using Newtonsoft.Json;
using Mayhem.Util.Exceptions;
using Mayhem.Dal.Tables;
using Mayhem.Dal.Dto.Dtos;

namespace CryptoMayhemLauncher.Services
{
    public class VersionService : IVersionService
    {
        private const string errorMessage = "Cannot communicate with version api.";
        private readonly HttpClient httpClient;
        private readonly ILogger<VersionService> logger;

        public VersionService(IHttpClientFactory httpClientFactory, ILogger<VersionService> logger)
        {
            this.logger = logger;

            httpClient = httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<LatestVersion> GetLatestVersion()
        {
            HttpResponseMessage response;
            try
            {
                response = await httpClient.GetAsync($"https://mayhemtdsversionapi.azurewebsites.net/api/GameVersion/GameVersion");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, errorMessage);
                throw new InternalException(new ValidationMessage(ResponseCodes.CantCommunicateWithVersionApi, errorMessage));
            }

            if (response.IsSuccessStatusCode)
            {
                string products = await response.Content.ReadAsStringAsync();

                LatestVersionDto latestVersionDto = JsonConvert.DeserializeObject<LatestVersionDto>(products);

                LatestVersion latestVersion = new LatestVersion()
                {
                    BuildURL = latestVersionDto.BuildURL,
                    Version = new BuildVersion(latestVersionDto.Version)

                };

                return latestVersion;

            }
            else
            {
                logger.LogError(errorMessage);
                throw new InternalException(new ValidationMessage(ResponseCodes.CantCommunicateWithVersionApi, errorMessage));
            }

        }
    }
}
