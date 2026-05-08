using ApiFunctionTrainReceiveCsv.Models;
using ApiFunctionTrainReceiveCsv.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ApiFunctionTrainReceiveCsv;

public class Function1
{
    private readonly ILogger<Function1> _logger;

    private UploadStorageService _uploadStorageService;

    private NormalizationCsvService _normalizationCsvService;

    public Function1(ILogger<Function1> logger, UploadStorageService uploadStorageService, NormalizationCsvService normalizationCsvService)
    {
        _logger = logger;
        _uploadStorageService = uploadStorageService;
        _normalizationCsvService = normalizationCsvService;
    }

    [Function("Function1")]
    public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
    {

        if (req.ContentType.Contains("csv"))
        {
            if(req.Body is Stream)
            {
                var file = new FileCsv
                {
                    NameFile = $"{Guid.NewGuid().ToString()}.csv",
                    content = req.Body
                };
                await _uploadStorageService.UploadOnBlobStorage(file);
            }

            
        }
        else
        {
            Console.WriteLine("Non hai inviato un csv");
        }
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        return new OkObjectResult("Welcome to Azure Functions!");
    }

    [Function("ProcessCsv")]
    public async Task OnBlobUploaded(
        [BlobTrigger("train-csv-storage/{name}", Connection = "AzureWebJobsStorage")] Stream blobStream,
        string name)
    {
        _logger.LogInformation($"Nuovo file rilevato: {name}");


        await _normalizationCsvService.NormilizeCsvStream(blobStream);
        
    }
}

