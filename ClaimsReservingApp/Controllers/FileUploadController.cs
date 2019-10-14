using ClaimsReservingApp.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ClaimsReservingApp.Controllers
{
    public class FileUploadController : Controller
    {
        private IWebHostEnvironment _hostingEnvironment;

        public FileUploadController(IWebHostEnvironment environment)
        {
            _hostingEnvironment = environment;
        }
        public IActionResult Index()
        {
            var items = GetFiles();
            return View(items);
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [HttpPost]

        public ActionResult Index(IFormFile file)
        {
            if (CheckIfValid(file))
            {
                ClaimsReservingProcess(file);
            }
            var items = GetFiles();
            return View(items);
        }

        public bool CheckIfValid(IFormFile file)
        {
            bool isValid = true;
            if (file == null)
            {
                ViewBag.Message = "You have not selected a file";
                isValid = false;
            }
            else if (Path.GetExtension(file.FileName) != ".txt" && Path.GetExtension(file.FileName) != ".csv")
            {
                ViewBag.Message = "File must be either .txt or .csv extension.";
                isValid = false;
            }
            else if (file.Length == 0)
            {
                ViewBag.Message = "The file is empty";
                isValid = false;
            }
            return isValid;
        }

        private void ClaimsReservingProcess(IFormFile file)
        {
            //Get Records
            List<PaymentRecord> records = getFileRecords(file);
            if (records.Count == 0)
            {
                return;
            }
            //Process Records
            List<string> processedRecords = calculateCumulativeData(records);

            //Store processed Records
            StoreFiles(Path.GetFileNameWithoutExtension(file.FileName), processedRecords);

        }

        private void StoreFiles(string fileName, List<string> processedRecords)
        {
            var processedFilePath = Path.Combine(_hostingEnvironment.ContentRootPath, $"ProcessedFiles/{fileName}_CumulativeData.txt");
            try
            {
                using (TextWriter tw = new StreamWriter(processedFilePath))
                {
                    foreach (string s in processedRecords)
                        tw.WriteLine(s);
                }
                ViewBag.Message = "The file was processed successfully!";
            }
            catch
            {
                ViewBag.Message = "There was a problem uploading the file";
            }

        }

        public List<string> calculateCumulativeData(List<PaymentRecord> records)
        {
            List<string> cumulativeData = new List<string>();

            //Get min and max year
            int minYear = (int)records.Min(r => r.OriginYear);

            int maxYear = records.Max(r => r.DevelopmentYear);

            //Create the header: earliest origin year and number of development years
            cumulativeData.Add($"{minYear.ToString()}, {maxYear - minYear + 1}");

            //Get Different Products
            List<string> distinctProducts = records.Select(s => s.Product).Distinct().ToList();

            //Iterate Products
            foreach (string product in distinctProducts)
            {
                string processedLine = product;
                for (int i = minYear; i <= maxYear; i++)
                {
                    decimal amount = 0;
                    for (int j = i; j <= maxYear; j++)
                    {
                        decimal? accumulatedAmount = records.FirstOrDefault(r => r.Product == product && r.OriginYear == i && r.DevelopmentYear == j)?.IncrementalValue;
                        amount += accumulatedAmount ?? 0;
                        processedLine += $", {amount.ToString("G29")}";
                    }
                }
                cumulativeData.Add(processedLine);
            }

            return cumulativeData;
        }

        public List<PaymentRecord> getFileRecords(IFormFile file)
        {
            List<PaymentRecord> records = new List<PaymentRecord>();
            try
            {
                using (var reader = new StreamReader(file.OpenReadStream()))
                using (var csv = new CsvHelper.CsvReader(reader))
                {
                    csv.Configuration.HeaderValidated = null;
                    csv.Configuration.MissingFieldFound = null;
                    records = csv.GetRecords<PaymentRecord>().ToList();
                }
            }
            catch (Exception ex)
            {
                ViewBag.Message = "Error:" + ex.Message;
            }
            return records;
        }

        public FileResult Download(string FileName)
        {
            var processedFilesPath = Path.Combine(_hostingEnvironment.ContentRootPath, "ProcessedFiles");
            var FileVirtualPath = Path.Combine(processedFilesPath,
                                                Path.GetFileName(FileName));

            byte[] fileBytes = System.IO.File.ReadAllBytes(FileVirtualPath);
            return File(fileBytes, "application/x-msdownload", FileName);
        }

        private List<string> GetFiles()
        {
            var processedFilesPath = Path.Combine(_hostingEnvironment.ContentRootPath, "ProcessedFiles");
            var dir = new DirectoryInfo(processedFilesPath);
            FileInfo[] fileNames = dir.GetFiles("*.*");

            List<string> items = new List<string>();
            foreach (var file in fileNames)
            {
                items.Add(file.Name);
            }

            return items;
        }
    }
}