using System.Linq;
using Microsoft.AspNetCore.Mvc;
using ADXdbApp.Models;
using ADXdbApp.Services;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Http;

namespace ADXdbApp.Controllers
{
    public class TableController : Controller
    {
        private readonly ADXService _adxService;
        private string sortSettings;

        public TableController(ADXService adxService)
        {
            _adxService = adxService;
        }

        public IActionResult Index()
        {
            //  var data = _adxService.GetTableData();
            //  return View(data);

            var data = _adxService.GetTableData();
            var viewModel = new TableViewModel
            {
                TableNames = data.Select(result => result.TableName).ToList(),
                TableData = data.ToDictionary(result => result.TableName, result => result.Data)
            };
            
            return View(viewModel);

        }

        [HttpPost]
        public IActionResult FilterAndSortData(string searchTerm, string table, string sortSettings)
        {
            //var sortedAndFilteredData = _adxService.GetSortedAndFilteredData(searchTerm, table, sortSettings);
            //return PartialView("_partia", sortedAndFilteredData);
            var filteredAndSortedData = _adxService.GetSortedAndFilteredData(searchTerm, table, sortSettings);
            var jsonData = JsonConvert.SerializeObject(filteredAndSortedData);

            return Content(jsonData, "application/json");


        }



        [HttpPost]
        public IActionResult UploadData(string tableName, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("file", "Please select a file to upload.");
                return View("viewModel", Index());
            }

            // Call a service method to insert the data into ADX
            bool success = _adxService.InsertData(tableName, file);

            if (success)
            {
                // Data inserted successfully
                TempData["Message"] = "Data uploaded and inserted successfully.";
            }
            else
            {
                // Data insertion failed
                TempData["Error"] = "Data insertion failed. Please check the file and table name.";
            }

            return RedirectToAction("Index");
        }

    }

}
