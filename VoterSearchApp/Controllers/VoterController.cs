using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VoterSearchApp.Models;
using VoterSearchApp.Services;

namespace VoterSearchApp.Controllers
{
    public class VoterController : Controller
    {
        private readonly IDataStorageService _dataStorage;
        private readonly IPdfParserService _pdfParser;
        private readonly IWebHostEnvironment _environment;

        public VoterController(
            IDataStorageService dataStorage,
            IPdfParserService pdfParser,
            IWebHostEnvironment environment)
        {
            _dataStorage = dataStorage;
            _pdfParser = pdfParser;
            _environment = environment;
        }

        public IActionResult Index(VoterSearchModel searchModel)
        {
            // Set default values
            if (searchModel.Page < 1) searchModel.Page = 1;
            if (searchModel.PageSize < 1) searchModel.PageSize = 10;

            // Get paged results
            var pagedResult = _dataStorage.GetPagedVoters(
                searchModel.Page,
                searchModel.PageSize,
                searchModel.SearchText);

            // Set ViewBag values for pagination
            ViewBag.SearchText = searchModel?.SearchText ?? "";
            ViewBag.CurrentPage = searchModel.Page;
            ViewBag.PageSize = searchModel.PageSize;
            ViewBag.TotalPages = pagedResult.TotalPages;
            ViewBag.TotalCount = pagedResult.TotalCount;
            ViewBag.SearchMessage = string.IsNullOrEmpty(searchModel.SearchText)
                ? $"মোট ভোটার: {pagedResult.TotalCount} জন"
                : $"'{searchModel.SearchText}' এর জন্য {pagedResult.TotalCount} টি ফলাফল";

            return View(pagedResult.Items);
        }

        public IActionResult Upload()
        {
            ViewData["Title"] = "PDF ফাইল আপলোড";
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile pdfFile)
        {
            if (pdfFile == null || pdfFile.Length == 0)
            {
                ModelState.AddModelError("", "ফাইল সিলেক্ট করুন");
                return View();
            }

            // Validate PDF file
            if (Path.GetExtension(pdfFile.FileName).ToLower() != ".pdf")
            {
                ModelState.AddModelError("", "শুধুমাত্র PDF ফাইল আপলোড করুন");
                return View();
            }

            // File size limit (1000MB)
            if (pdfFile.Length > 1000 * 1024 * 1024)
            {
                ModelState.AddModelError("", "ফাইল সাইজ 10MB এর বেশি হতে পারবে না");
                return View();
            }

            try
            {
                // Create upload directory
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Save uploaded file
                var fileName = Guid.NewGuid().ToString() + ".pdf";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await pdfFile.CopyToAsync(stream);
                }

                // Parse PDF file
                //var voters = _pdfParser.ParseVoterData(filePath);
                string folderPath = Path.Combine(_environment.WebRootPath, "Voter_List", "BULTA");
                var voters = _pdfParser.ExtractVotersFromPdfFolder(folderPath);

                if (voters.Count == 0)
                {
                    ModelState.AddModelError("", "PDF ফাইল থেকে ভোটার তথ্য পাওয়া যায়নি");
                    return View();
                }

                // Add voters to storage
                _dataStorage.AddVoters(voters);

                TempData["SuccessMessage"] = $"{voters.Count} জন ভোটারের তথ্য সফলভাবে আপলোড হয়েছে";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"আপলোড ব্যর্থ: {ex.Message}");
                return View();
            }
        }

        [HttpPost]
        public IActionResult ClearAll()
        {
            _dataStorage.ClearAll();
            TempData["SuccessMessage"] = "সমস্ত তথ্য মুছে ফেলা হয়েছে";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Search(string term)
        {
            if (string.IsNullOrEmpty(term))
                return Json(new List<Voter>());

            var results = _dataStorage.SearchVoters(term)
                .Take(10)
                .Select(v => new {
                    id = v.VoterNumber,
                    text = $"{v.Name} - {v.VoterNumber} (পিতা: {v.FatherName})"
                })
                .ToList();

            return Json(results);
        }
    }
}