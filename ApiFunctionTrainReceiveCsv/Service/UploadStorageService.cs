using ApiFunctionTrainReceiveCsv.Models;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace ApiFunctionTrainReceiveCsv.Service
{
    public class UploadStorageService
    {
        private BlobServiceClient blobServiceClient;

        private string _urlStorage ;

        public UploadStorageService(IConfiguration configuration)
        {
            _urlStorage = configuration["AzureWebJobsStorage"] ?? string.Empty;
            blobServiceClient = new BlobServiceClient(_urlStorage);

        }


        public async Task UploadOnBlobStorage(FileCsv fileCsv)
        {
            var serviceBlob = blobServiceClient.GetBlobContainerClient("train-csv-storage");

            await serviceBlob.UploadBlobAsync(fileCsv.NameFile, fileCsv.content);
        }
    }
}
