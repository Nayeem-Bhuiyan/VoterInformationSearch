using Microsoft.AspNetCore.Http;
using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using VoterSearchApp.Models;

namespace VoterSearchApp.Services
{
    public interface IPdfParserService
    {
        Task<List<Voter>> ParseVoterDataAsync(IFormFile pdfFile, string fileName);
        List<Voter> ParseVoterData(string filePath);
        List<Voter> ExtractVotersFromPdfFolder(string folderPath = @"E:\Voter_List\BULTA\");
    }

    public class BanglaPdfParserService : IPdfParserService
    {
        private readonly ILogger<BanglaPdfParserService> _logger;

        public BanglaPdfParserService(ILogger<BanglaPdfParserService> logger)
        {
            _logger = logger;
        }

        public async Task<List<Voter>> ParseVoterDataAsync(IFormFile pdfFile, string fileName)
        {
            var voters = new List<Voter>();

            if (pdfFile == null || pdfFile.Length == 0)
            {
                _logger.LogError("No file uploaded or file is empty");
                return voters;
            }

            var tempFilePath = Path.GetTempFileName() + ".pdf";

            try
            {
                await using (var stream = new FileStream(tempFilePath, FileMode.Create))
                {
                    await pdfFile.CopyToAsync(stream);
                }

                voters = ParseVoterData(tempFilePath);

                foreach (var voter in voters)
                {
                    voter.FileName = fileName;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing PDF file");
                throw;
            }
            finally
            {
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }

            return voters;
        }

        public List<Voter> ParseVoterData(string filePath)
        {
            var voters = new List<Voter>();

            if (!File.Exists(filePath))
            {
                _logger.LogError("File not found: {FilePath}", filePath);
                return voters;
            }

            try
            {
                // Extract raw text from PDF
                string rawText = ExtractTextFromPdf(filePath);

                if (string.IsNullOrWhiteSpace(rawText))
                {
                    _logger.LogWarning("No text extracted from PDF");
                    return voters;
                }

                _logger.LogInformation("Raw extracted text length: {Length}", rawText.Length);

                // Clean and normalize the text
                string cleanedText = CleanAndNormalizeText(rawText);

                _logger.LogInformation("Cleaned text sample: {Sample}",
                    cleanedText.Length > 500 ? cleanedText.Substring(0, 500) : cleanedText);

                // Try different parsing strategies
                voters = ParseVoterEntries(cleanedText);

                _logger.LogInformation("Successfully parsed {Count} voters", voters.Count);

                return voters;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing voter data from {FilePath}", filePath);
                return voters;
            }
        }

        private string ExtractTextFromPdf(string filePath)
        {
            var textBuilder = new StringBuilder();

            try
            {
                using var document = PdfDocument.Open(filePath);

                foreach (var page in document.GetPages())
                {
                    var pageText = page.Text;
                    if (!string.IsNullOrWhiteSpace(pageText))
                    {
                        textBuilder.AppendLine(pageText);
                    }
                }

                return textBuilder.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting text from PDF");
                return string.Empty;
            }
        }


        public List<Voter> ExtractVotersFromPdfFolder(string folderPath = @"E:\Voter_List\BULTA\")
        {
            var allVoters = new List<Voter>();

            try
            {
                if (!Directory.Exists(folderPath))
                {
                    _logger.LogError($"Folder not found: {folderPath}");
                    return allVoters;
                }

                var fileList = Directory.GetFiles(folderPath, "*.pdf", SearchOption.AllDirectories);
                _logger.LogInformation($"Found {fileList.Length} PDF files");

                foreach (var pdfFile in fileList)
                {
                    try
                    {
                        string pdfText = ExtractTextFromSinglePdf(pdfFile);


                        if (string.IsNullOrWhiteSpace(pdfText))
                        {
                            _logger.LogWarning("No text extracted from PDF");
                            return allVoters;
                        }

                        _logger.LogInformation("Raw extracted text length: {Length}", pdfText.Length);

                        // Clean and normalize the text
                        string cleanedText = CleanAndNormalizeText(pdfText);

                        _logger.LogInformation("Cleaned text sample: {Sample}",
                            cleanedText.Length > 500 ? cleanedText.Substring(0, 500) : cleanedText);

                        // Try different parsing strategies
                        var current_VoterList = ParseVoterEntries(cleanedText);
                        if (current_VoterList!=null)
                        {
                            allVoters.AddRange(current_VoterList);
                        }
                       

                        _logger.LogInformation($"Added {allVoters.Count} voters from {Path.GetFileName(pdfFile)}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error processing {pdfFile}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scanning folder");

            }

            return allVoters;
        }

        private string ExtractTextFromSinglePdf(string filePath)
        {
            var textBuilder = new StringBuilder();
            using var document = PdfDocument.Open(filePath);
            foreach (var page in document.GetPages())
            {
                var pageText = page.Text;
                if (!string.IsNullOrWhiteSpace(pageText))
                    textBuilder.AppendLine(pageText);
            }
            return textBuilder.ToString();
        }


        private string CleanAndNormalizeText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            // First, fix the common encoding issues from your sample
            text = FixCommonEncodingIssues(text);

            // Normalize Bengali text
            text = NormalizeBengaliText(text);

            // Remove extra whitespace but preserve structure
            text = Regex.Replace(text, @"\s+", " ");

            // Fix serial number formatting
            text = Regex.Replace(text, @"(\d{3,4})\.(\w)", "$1. $2");

            // Fix labels
            text = Regex.Replace(text, @"মোভাটার", "ভোটার");
            text = Regex.Replace(text, @"মোজলা", "জেলা");
            text = Regex.Replace(text, @"উপেজলা", "উপজেলা");
            text = Regex.Replace(text, @"মোপশা", "পেশা");
            text = Regex.Replace(text, @"মোপাড়েকাড", "পোস্টকোড");
            text = Regex.Replace(text, @"মোসানাব", "সোনাব");
            text = Regex.Replace(text, @"মোবাঃ", "মোবাইল:");
            text = Regex.Replace(text, @"িঠকানা", "ঠিকানা");
            text = Regex.Replace(text, @"তািলকা", "তালিকা");
            text = Regex.Replace(text, @"মোপৗরসভা", "পৌরসভা");
            text = Regex.Replace(text, @"মোভাটার", "ভোটার");

            return text.Trim();
        }

        private string FixCommonEncodingIssues(string text)
        {
            // Comprehensive encoding fixes based on your sample
            var encodingFixes = new Dictionary<string, string>
            {
                // Fix individual corrupted characters
                ["ĥ"] = "ম",
                ["Ĵ"] = "অ",
                ["Ō"] = "দ",
                ["ƀ"] = "ফ",
                ["Ɓ"] = "ফ",
                ["Ë"] = "ল",
                ["į"] = "য",
                ["Ģ"] = "ণ",
                ["Î"] = "র",
                ["Ş"] = "ড়",
                ["ń"] = "ব",
                ["û"] = "ধ",
                ["ă"] = "ত",
                ["Ĩ"] = "হ",
                ["ſ"] = "র",
                ["ƣ"] = "ক",
                ["Ň"] = "জ",
                ["Ù"] = "ল",
                ["ė"] = "্",
                ["å"] = "গ্র",

                // Fix common corrupted words from your sample
                ["চূড়াণ"] = "চূড়ান্ত",
                ["তািলকা"] = "তালিকা",
                ["ছিব"] = "ছবি",
                ["ছাড়া"] = "ছাড়া",
                ["অকােশর"] = "অফিসের",
                ["মোভাটার"] = "ভোটার",
                ["িঠকানা"] = "ঠিকানা",
                ["মোপশা"] = "পেশা",
                ["মোসাঃ"] = "মোসাঃ",
                ["মোমাঃ"] = "মোঃ",
                ["মোমাসাঃ"] = "মোসাঃ",
                ["মোভাটার"] = "ভোটার",
                ["মোসানাব"] = "সোনাব",
                ["Ƃপগý"] = "রূপগঞ্জ",
                ["নারায়ণগý"] = "নারায়ণগঞ্জ",
                ["যম"] = "জন্ম",
                ["মোভাটার নং"] = "ভোটার নং",
                ["মোপাড়েকাড"] = "পোস্টকোড",
                ["åামীন"] = "গ্রামীণ",
                ["মোটকান"] = "ঠিকানা",
                ["Ðবরাব"] = "দ্বারাব",
                ["Řিমক"] = "ছাত্র/ছাত্রী", // Based on context
                ["িংলাব"] = "সিংলাব",
                ["ভােয়লা"] = "ভেল্লা",
                ["ভােয়ল"] = "ভেল্লা",
                ["ভূলতা"] = "ভুলতা",
                ["মাইেåট"] = "মাইগ্রেট",
                ["আ×ার"] = "আক্তার",
                ["শাহাজিėন"] = "শাহাজাহান",
                ["আেলহামোপ"] = "আলহামদুলিল্লাহ",
                ["মোশখ"] = "মোশাররফ",
                ["হােজরা"] = "হাজেরা",
                ["রর"] = "রহিম",
                ["মাƁফা"] = "মাফুজা",
                ["আেয়শা"] = "আয়েশা",
                ["মোববী"] = "মোবিব্বী",
                ["মোফলু"] = "মোফাজ্জল",
                ["মিজরনা"] = "মিজানুর",
                ["িশউিল"] = "শিউলি",
                ["আিবয়া"] = "আবিয়া"
            };

            foreach (var fix in encodingFixes)
            {
                text = text.Replace(fix.Key, fix.Value);
            }

            return text;
        }

        private string NormalizeBengaliText(string text)
        {
            // Fix Bengali character combinations
            text = text.Replace("ি ", "ি")
                       .Replace(" ো", "ো")
                       .Replace("ো", "ো")
                       .Replace(" ৌ", "ৌ")
                       .Replace(" ্র", "্র")
                       .Replace("্র ", "্র")
                       .Replace(" ্", "্")
                       .Replace("্ ", "্")
                       .Replace("ং ", "ং")
                       .Replace("ঃ ", "ঃ")
                       .Replace("ঁ ", "ঁ");

            return text;
        }

        private List<Voter> ParseVoterEntries(string text)
        {
            var voters = new List<Voter>();

            try
            {
                // Split text by serial numbers to get individual voter entries
                string[] entries = Regex.Split(text, @"(?=\d{3,4}\.\s*নাম:)");

                foreach (string entry in entries)
                {
                    if (string.IsNullOrWhiteSpace(entry) || !entry.Contains("নাম:"))
                        continue;

                    var voter = ParseSingleVoterEntry(entry);
                    if (voter != null)
                    {
                        voters.Add(voter);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing voter entries");
            }

            return voters;
        }

        private Voter ParseSingleVoterEntry(string entryText)
        {
            try
            {
                var voter = new Voter
                {
                    CreatedDate = DateTime.UtcNow
                };

                // Clean the entry text first to fix encoding issues
                entryText = CleanEntryText(entryText);

                // Extract serial number - handle variations like "০০১." or "001."
                var serialMatch = Regex.Match(entryText, @"^(\d{3,4})\.\s*নাম:");
                if (serialMatch.Success)
                {
                    voter.SerialNumber = serialMatch.Groups[1].Value.Trim();
                }

                // Extract name - handle variations with special characters
                var nameMatch = Regex.Match(entryText, @"নাম:\s*(.+?)(?=\s*(?:ভোটার|মোভাটার|Ïভাটার|পিতা|$))", RegexOptions.IgnoreCase);
                if (nameMatch.Success)
                {
                    voter.Name = CleanField(nameMatch.Groups[1].Value);
                }

                // Extract voter number - handle multiple variations
                string[] voterPatterns = new[]
                {
                    @"ভোটার\s*নং:\s*(\d+)",           // Normal
                    @"মোভাটার\s*নং:\s*(\d+)",         // Alternative
                    @"Ïভাটার\s*নং:\s*(\d+)",          // Encoded
                    @"ভোটার\s*নম্বর:\s*(\d+)",       // With full "নম্বর"
                    @"ভোটার\s*:\s*(\d+)"             // Without "নং"
                };

                foreach (var pattern in voterPatterns)
                {
                    var voterMatch = Regex.Match(entryText, pattern);
                    if (voterMatch.Success)
                    {
                        voter.VoterNumber = CleanField(voterMatch.Groups[1].Value);
                        break;
                    }
                }

                // Extract father's name - handle encoding variations
                var fatherMatch = Regex.Match(entryText, @"িপতা|পিতা:\s*(.+?)(?=\s*(?:মাতা|মােতা|পেশা|ভোটার|$))", RegexOptions.IgnoreCase);
                if (fatherMatch.Success)
                {
                    voter.FatherName = CleanField(fatherMatch.Groups[1].Value);
                }

                // Extract mother's name - handle encoding variations
                var motherMatch = Regex.Match(entryText, @"মাতা:\s*(.+?)(?=\s*(?:পেশা|মোপশা|Ïপশা|জন্ম|ভোটার|$))", RegexOptions.IgnoreCase);
                if (motherMatch.Success)
                {
                    voter.MotherName = CleanField(motherMatch.Groups[1].Value);
                }

                // Extract profession - handle corrupted text like "Ïপশা"
                var professionMatch = Regex.Match(entryText, @"(?:পেশা|মোপশা|Ïপশা):\s*(.+?)(?:,|\s*)(?=\s*(?:জন্ম|জম|তািরখ|ঠিকানা|$))", RegexOptions.IgnoreCase);
                if (professionMatch.Success)
                {
                    voter.Profession = CleanField(professionMatch.Groups[1].Value);
                }

                // Extract date of birth - handle variations like "জম তািরখ" or "জন্ম তারিখ"
                var dobMatch = Regex.Match(entryText, @"(?:জন্ম\s*তারিখ|জম\s*তািরখ|জম\s*তারিখ|জন্ম\s*তািরখ):\s*(\d{1,2}/\d{1,2}/\d{2,4})");
                if (dobMatch.Success)
                {
                    string dobStr = dobMatch.Groups[1].Value;
                    if (TryParseBengaliDate(dobStr, out DateTime dob))
                    {
                        voter.DateOfBirth = dob;
                    }
                }

                // Extract address - handle corrupted "ঠিকানা"
                var addressMatch = Regex.Match(entryText, @"(?:ঠিকানা|িঠকানা|ঠকানা):\s*(.+?)(?=\s*\d{3,4}\.\s*নাম:|$)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                if (addressMatch.Success)
                {
                    voter.Address = CleanField(addressMatch.Groups[1].Value);
                }

                // If VoterNumber still not found, try a more aggressive search
                if (string.IsNullOrWhiteSpace(voter.VoterNumber))
                {
                    // Look for any 10-17 digit number that might be the voter number
                    var numberMatch = Regex.Match(entryText, @"\b(\d{10,17})\b");
                    if (numberMatch.Success)
                    {
                        voter.VoterNumber = numberMatch.Groups[1].Value.Trim();
                    }
                }

                // Validate required fields
                if (string.IsNullOrWhiteSpace(voter.Name) || string.IsNullOrWhiteSpace(voter.VoterNumber))
                {
                    _logger.LogWarning("Skipping voter due to missing required fields. Name: '{Name}', VoterNumber: '{VoterNumber}'",
                        voter.Name, voter.VoterNumber);
                    return null;
                }

                return voter;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse voter entry: {EntryText}",
                    entryText.Substring(0, Math.Min(200, entryText.Length)));
                return null;
            }
        }

        private string CleanEntryText(string entryText)
        {
            if (string.IsNullOrWhiteSpace(entryText))
                return entryText;

            // Fix specific corrupted patterns in entry text
            var fixes = new Dictionary<string, string>
            {
                ["Ïভাটার"] = "ভোটার",
                ["মোভাটার"] = "ভোটার",
                ["Ïপশা"] = "পেশা",
                ["মোপশা"] = "পেশা",
                ["িঠকানা"] = "ঠিকানা",
                ["জম তািরখ"] = "জন্ম তারিখ",
                ["জম তারিখ"] = "জন্ম তারিখ",
                ["Ïমাঃ"] = "মোঃ",
                ["Ïমাসাঃ"] = "মোসাঃ",
                ["গৃিহনী"] = "গৃহিণী",
                ["িপতা"] = "পিতা",
                ["Ïমাঃ"] = "মোঃ",
                ["Ïবগম"] = "বেগম",
                ["ý"] = "ঞ্জ",
                [" িময়া"] = "মিয়া",
                [" িবিব"] = "বিবি",
                ["ÏĨছা"] = "ন্নেছা",
                ["বÔ"] = "বক্স",
                ["উিėন"] = "উদ্দিন",
                ["আেবদীন"] = "আবেদিন",
                ["বলবসা"] = "ব্যবসা",
            };

            foreach (var fix in fixes)
            {
                entryText = entryText.Replace(fix.Key, fix.Value);
            }

            return entryText;
        }




        private string CleanField(string field)
        {
            if (string.IsNullOrWhiteSpace(field))
                return string.Empty;

            // First clean encoding issues
            field = field.Trim()
                         .Replace("\n", " ")
                         .Replace("\r", " ")
                         .Replace("  ", " ");

            // Fix specific encoding issues in field values
            var fieldFixes = new Dictionary<string, string>
            {
                ["Ï"] = "",
                ["মোঃ"] = "মোঃ",
                ["মোসাঃ"] = "মোসাঃ"
            };

            foreach (var fix in fieldFixes)
            {
                field = field.Replace(fix.Key, fix.Value);
            }

            return field;
        }
        private bool TryParseBengaliDate(string dateStr, out DateTime result)
        {
            result = DateTime.MinValue;

            try
            {
                // Convert Bengali numerals to Arabic numerals
                dateStr = ConvertBengaliNumerals(dateStr);

                // Handle different date formats
                string[] formats = { "d/M/yyyy", "dd/MM/yyyy", "d/M/yy", "dd/MM/yy" };

                if (DateTime.TryParseExact(dateStr, formats,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out DateTime parsedDate))
                {
                    // Adjust two-digit years
                    if (parsedDate.Year < 100)
                    {
                        int year = parsedDate.Year;
                        if (year < 30) // Assuming years 00-29 are 2000-2029
                            year += 2000;
                        else
                            year += 1900;

                        result = new DateTime(year, parsedDate.Month, parsedDate.Day);
                    }
                    else
                    {
                        result = parsedDate;
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse date: {DateStr}", dateStr);
            }

            return false;
        }

        private string ConvertBengaliNumerals(string text)
        {
            var bengaliToArabic = new Dictionary<char, char>
            {
                ['০'] = '0',
                ['১'] = '1',
                ['২'] = '2',
                ['৩'] = '3',
                ['৪'] = '4',
                ['৫'] = '5',
                ['৬'] = '6',
                ['৭'] = '7',
                ['৮'] = '8',
                ['৯'] = '9',
                ['/'] = '/',
                ['-'] = '-',
                ['.'] = '.'
            };

            var result = new StringBuilder();
            foreach (char c in text)
            {
                if (bengaliToArabic.TryGetValue(c, out char arabic))
                {
                    result.Append(arabic);
                }
                else
                {
                    result.Append(c);
                }
            }

            return result.ToString();
        }

    }
}

