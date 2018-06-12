using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Joonasw.AzureStorageAadAuthentication.Models;
using Microsoft.Extensions.Configuration;
using System.IO;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.AspNetCore.Hosting;

namespace Joonasw.AzureStorageAadAuthentication.Controllers
{
    public class HomeController : Controller
    {
        private readonly string _storageAccountName;
        private readonly string _containerName;
        private readonly string _fileName;
        private readonly string _tenantId;
        private readonly IHostingEnvironment _environment;

        public HomeController(IConfiguration configuration, IHostingEnvironment environment)
        {
            _storageAccountName = configuration["Storage:Account"];
            _containerName = configuration["Storage:Container"];
            _fileName = configuration["Storage:File"];
            _tenantId = configuration["Authentication:TenantId"];
            _environment = environment;
        }

        [HttpGet, HttpHead]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> DownloadFile()
        {
            string accessToken = await AcquireAccessTokenAsync();

            var (stream, contentType, fileName) = await DownloadFileFromStorageAsync(accessToken);

            return File(stream, contentType, fileName);
        }

        private async Task<string> AcquireAccessTokenAsync()
        {
            var tokenProvider = new AzureServiceTokenProvider();
            return await tokenProvider.GetAccessTokenAsync("https://storage.azure.com/", _tenantId);
        }

        private async Task<(Stream stream, string contentType, string fileName)> DownloadFileFromStorageAsync(string accessToken)
        {
            var tokenCredential = new TokenCredential(accessToken);
            var storageCredentials = new StorageCredentials(tokenCredential);
            var blob = new CloudBlockBlob(new Uri($"https://{_storageAccountName}.blob.core.windows.net/{_containerName}/{_fileName}"), storageCredentials);

            await blob.FetchAttributesAsync();

            var stream = await blob.OpenReadAsync();
            string contentType = blob.Properties.ContentType;
            string fileName = blob.Name;

            return (stream, contentType, fileName);
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
