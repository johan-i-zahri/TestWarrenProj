using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using System.Xml.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Quran.Controllers
{
    public class ConcordanceController : Controller
    {
        public ActionResult Word(string id)
        {
            string concordanceDir = Server.MapPath("~/concordance/");
            bool useStaticFiles = false; // toggle via config

            if (id.Equals("index", StringComparison.OrdinalIgnoreCase))
            {
                if (useStaticFiles)
                {
                    string indexPath = Path.Combine(concordanceDir, "index.html");
                    if (!System.IO.File.Exists(indexPath))
                        PreGenerateConcordanceIndex();
                    return File(indexPath, "text/html");
                }
                else
                {
                    string html = BuildConcordanceIndexHtml(); // new helper returns string
                    return Content(html, "text/html", Encoding.UTF8);
                }
            }

            if (useStaticFiles)
            {
                string filePath = Path.Combine(concordanceDir, id + ".html");
                if (!System.IO.File.Exists(filePath))
                    GenerateConcordanceFile(id);
                return File(filePath, "text/html");
            }
            else
            {
                string html = GenerateConcordanceHtml(id); // new helper returns string
                return Content(html, "text/html", Encoding.UTF8);
            }
        }


        // Chapter entry point: /chapter/<id>.html
        public ActionResult Chapter(string id)
        {
            string chapterDir = Server.MapPath("~/chapter/");
            string filePath = Path.Combine(chapterDir, id + ".html");

            if (!System.IO.File.Exists(filePath))
            {
                string xmlPath = Server.MapPath("~/quran-simple-clean.xml");
                var doc = XDocument.Load(xmlPath);
                var sura = doc.Descendants("sura")
                              .FirstOrDefault(s => s.Attribute("index").Value == id);

                if (sura != null)
                {
                    GenerateChapterFile(id, sura);
                }
            }

            return File(filePath, "text/html");
        }

        private string GenerateConcordanceHtml(string word)
        {
            string xmlPath = Server.MapPath("~/quran-simple-clean.xml");
            var doc = XDocument.Load(xmlPath);
            var matches = new List<string>();

            foreach (var sura in doc.Descendants("sura"))
            {
                string surahNum = sura.Attribute("index").Value;
                string surahName = sura.Attribute("name").Value;

                foreach (var aya in sura.Descendants("aya"))
                {
                    string text = aya.Attribute("text").Value;
                    string verseNum = aya.Attribute("index").Value;

                    var words = text.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (words.Contains(word))
                    {
                        string link = "/chapter/" + surahNum + ".html#verse-" + verseNum;
                        matches.Add(surahName + " " + verseNum + ": <a href='" + link + "'>" + text + "</a>");
                    }
                }
            }

            var sb = new StringBuilder();
            sb.Append("<html><head><meta charset='UTF-8'><title>Concordance for " + word + "</title></head><body>");
            sb.Append("<h2>Concordance for '" + word + "'</h2><hr>");
            foreach (var m in matches) sb.Append("<p>" + m + "</p>");
            sb.Append("</body></html>");
            return sb.ToString();
        }

        private void GenerateConcordanceFile(string word)
        {
            string xmlPath = Server.MapPath("~/quran-simple-clean.xml");
            var doc = XDocument.Load(xmlPath);
            var matches = new List<string>();

            foreach (var sura in doc.Descendants("sura"))
            {
                string surahNum = sura.Attribute("index").Value;
                string surahName = sura.Attribute("name").Value;

                foreach (var aya in sura.Descendants("aya"))
                {
                    string text = aya.Attribute("text").Value;
                    string verseNum = aya.Attribute("index").Value;

                    var words = text.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (words.Contains(word))
                    {
                        GenerateChapterFile(surahNum, sura);

                        string link = "/chapter/" + surahNum + ".html#verse-" + verseNum;
                        matches.Add(surahName + " " + verseNum + ": <a href='" + link + "'>" + text + "</a>");
                    }
                }
            }

            string html = "<html><head><meta charset='UTF-8'><title>Concordance for " + word + "</title></head><body>";
            html += "<h2>Concordance for '" + word + "'</h2><hr>";
            foreach (var m in matches) html += "<p>" + m + "</p>";
            html += "</body></html>";

            string concordanceDir = Server.MapPath("~/concordance/");
            if (!Directory.Exists(concordanceDir)) Directory.CreateDirectory(concordanceDir);
            System.IO.File.WriteAllText(Path.Combine(concordanceDir, word + ".html"), html, Encoding.UTF8);
        }

        private void GenerateChapterFile(string surahNum, XElement sura)
        {
            string chapterDir = Server.MapPath("~/chapter/");
            string filePath = Path.Combine(chapterDir, surahNum + ".html");

            if (System.IO.File.Exists(filePath)) return;

            string surahName = sura.Attribute("name").Value;
            string html = "<html><head><meta charset='UTF-8'><title>" + surahName + "</title></head><body>";
            html += "<h2>" + surahName + "</h2><hr>";

            foreach (var aya in sura.Descendants("aya"))
            {
                string verseNum = aya.Attribute("index").Value;
                string text = aya.Attribute("text").Value;

                html += "<p id='verse-" + verseNum + "'><strong>" + verseNum + ":</strong> ";

                var words = text.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var w in words)
                {
                    string link = "/concordance/" + w + ".html";
                    html += "<a href='" + link + "'>" + w + "</a> ";
                }

                html += "</p>";
            }

            html += "</body></html>";

            if (!Directory.Exists(chapterDir)) Directory.CreateDirectory(chapterDir);

            lock (ChapterLock)
            {
                if (!System.IO.File.Exists(filePath))
                {
                    System.IO.File.WriteAllText(filePath, html, Encoding.UTF8);
                }
            }
        }

        public static void PreGenerateAllChapters()
        {
            string xmlPath = System.Web.Hosting.HostingEnvironment.MapPath("~/quran-simple-clean.xml");
            string chapterDir = System.Web.Hosting.HostingEnvironment.MapPath("~/chapter/");

            if (!Directory.Exists(chapterDir)) Directory.CreateDirectory(chapterDir);

            var doc = XDocument.Load(xmlPath);

            foreach (var sura in doc.Descendants("sura"))
            {
                string surahNum = sura.Attribute("index").Value;
                string filePath = Path.Combine(chapterDir, surahNum + ".html");

                if (!System.IO.File.Exists(filePath))
                {
                    string surahName = sura.Attribute("name").Value;
                    string html = "<html><head><meta charset='UTF-8'><title>" + surahName + "</title></head><body>";
                    html += "<h2>" + surahName + "</h2><hr>";

                    foreach (var aya in sura.Descendants("aya"))
                    {
                        string verseNum = aya.Attribute("index").Value;
                        string text = aya.Attribute("text").Value;

                        html += "<p id='verse-" + verseNum + "'><strong>" + verseNum + ":</strong> ";

                        var words = text.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var w in words)
                        {
                            string link = "/concordance/" + w + ".html";
                            html += "<a href='" + link + "'>" + w + "</a> ";
                        }

                        html += "</p>";
                    }

                    html += "</body></html>";

                    System.IO.File.WriteAllText(filePath, html, Encoding.UTF8);
                }
            }
        }

        public static void PreGenerateAllConcordances()
        {
            string xmlPath = System.Web.Hosting.HostingEnvironment.MapPath("~/quran-simple-clean.xml");
            string concordanceDir = System.Web.Hosting.HostingEnvironment.MapPath("~/concordance/");
            string chapterDir = System.Web.Hosting.HostingEnvironment.MapPath("~/chapter/");

            if (!Directory.Exists(concordanceDir)) Directory.CreateDirectory(concordanceDir);
            if (!Directory.Exists(chapterDir)) Directory.CreateDirectory(chapterDir);

            var doc = XDocument.Load(xmlPath);

            // Collect all unique words across the Qur'an
            var allWords = new HashSet<string>();
            foreach (var aya in doc.Descendants("aya"))
            {
                string text = aya.Attribute("text").Value;
                var words = text.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var w in words) allWords.Add(w);
            }

            // Generate concordance file for each word
            foreach (var word in allWords)
            {
                string filePath = Path.Combine(concordanceDir, word + ".html");
                if (System.IO.File.Exists(filePath)) continue;

                var matches = new List<string>();

                foreach (var sura in doc.Descendants("sura"))
                {
                    string surahNum = sura.Attribute("index").Value;
                    string surahName = sura.Attribute("name").Value;

                    foreach (var aya in sura.Descendants("aya"))
                    {
                        string text = aya.Attribute("text").Value;
                        string verseNum = aya.Attribute("index").Value;

                        var words = text.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        if (words.Contains(word))
                        {
                            // Ensure chapter file exists
                            string chapterFile = Path.Combine(chapterDir, surahNum + ".html");
                            if (!System.IO.File.Exists(chapterFile))
                            {
                                // Reuse your GenerateChapterFile logic here
                                GenerateChapterFileStatic(chapterFile, surahName, surahNum, sura);
                            }

                            string link = "/chapter/" + surahNum + ".html#verse-" + verseNum;
                            matches.Add(surahName + " " + verseNum + ": <a href='" + link + "'>" + text + "</a>");
                        }
                    }
                }

                string html = "<html><head><meta charset='UTF-8'><title>Concordance for " + word + "</title></head><body>";
                html += "<h2>Concordance for '" + word + "'</h2><hr>";
                foreach (var m in matches) html += "<p>" + m + "</p>";
                html += "</body></html>";

                System.IO.File.WriteAllText(filePath, html, Encoding.UTF8);
            }
        }

        // Helper for static chapter generation
        private static void GenerateChapterFileStatic(string filePath, string surahName, string surahNum, XElement sura)
        {
            string html = "<html><head><meta charset='UTF-8'><title>" + surahName + "</title></head><body>";
            html += "<h2>" + surahName + "</h2><hr>";

            foreach (var aya in sura.Descendants("aya"))
            {
                string verseNum = aya.Attribute("index").Value;
                string text = aya.Attribute("text").Value;

                html += "<p id='verse-" + verseNum + "'><strong>" + verseNum + ":</strong> ";

                var words = text.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var w in words)
                {
                    string link = "/concordance/" + w + ".html";
                    html += "<a href='" + link + "'>" + w + "</a> ";
                }

                html += "</p>";
            }

            html += "</body></html>";
            System.IO.File.WriteAllText(filePath, html, Encoding.UTF8);
        }

        public static void PreGenerateConcordanceIndex()
        {
            string concordanceDir = System.Web.Hosting.HostingEnvironment.MapPath("~/concordance/");
            if (!Directory.Exists(concordanceDir)) Directory.CreateDirectory(concordanceDir);

            // Collect all concordance files already generated
            var files = Directory.GetFiles(concordanceDir, "*.html");

            if (files.Count() < 1)
            {
                PreGenerateAllChapters();
                files = Directory.GetFiles(concordanceDir, "*.html");
            }

            // Build index HTML
            string html = "<html><head><meta charset='UTF-8'><title>Concordance Index</title></head><body>";
            html += "<h2>Concordance Index</h2><hr><ul>";

            foreach (var file in files.OrderBy(f => f))
            {
                string word = Path.GetFileNameWithoutExtension(file);
                html += "<li><a href='/concordance/" + word + ".html'>" + word + "</a></li>";
            }

            html += "</ul></body></html>";

            string indexPath = Path.Combine(concordanceDir, "index.html");
            System.IO.File.WriteAllText(indexPath, html, Encoding.UTF8);
        }

        public static void PreGenerateConcordanceIndexes()
        {
            string concordanceDir = System.Web.Hosting.HostingEnvironment.MapPath("~/concordance/");
            if (!Directory.Exists(concordanceDir)) Directory.CreateDirectory(concordanceDir);

            var files = Directory.GetFiles(concordanceDir, "*.html");
            var grammarLinks = new List<string>();
            var nonGrammarLinks = new List<string>();

            foreach (var file in files.OrderBy(f => f))
            {
                string word = Path.GetFileNameWithoutExtension(file);
                string link = "<li><a href='/concordance/" + word + ".html'>" + word + "</a></li>";

                if (GrammarWords.Contains(word))
                    grammarLinks.Add(link);
                else
                    nonGrammarLinks.Add(link);
            }

            // Grammar index
            string grammarHtml = "<html><head><meta charset='UTF-8'><title>Grammar Concordance Index</title></head><body>";
            grammarHtml += "<h2>Grammar Words Concordance Index</h2><hr><ul>";
            grammarHtml += string.Join("", grammarLinks);
            grammarHtml += "</ul></body></html>";
            System.IO.File.WriteAllText(Path.Combine(concordanceDir, "index-grammar.html"), grammarHtml, Encoding.UTF8);

            // Non-grammar index
            string nonGrammarHtml = "<html><head><meta charset='UTF-8'><title>Content Concordance Index</title></head><body>";
            nonGrammarHtml += "<h2>Content Words Concordance Index</h2><hr><ul>";
            nonGrammarHtml += string.Join("", nonGrammarLinks);
            nonGrammarHtml += "</ul></body></html>";
            System.IO.File.WriteAllText(Path.Combine(concordanceDir, "index-content.html"), nonGrammarHtml, Encoding.UTF8);
        }

        public static string GenerateSummaryTable(List<ConcordanceSummary> summaries)
        {
            var sb = new StringBuilder();
            sb.Append("<html><head><meta charset='UTF-8'><title>Concordance Summary Index</title>");
            sb.Append("<style>table { border-collapse: collapse; width: 100%; } th, td { border: 1px solid #ccc; padding: 6px; text-align: center; } th { background: #f0f0f0; }</style>");
            sb.Append("</head><body>");
            sb.Append("<h2>Concordance Summary Index</h2>");
            sb.Append("<table><tr><th>Root</th>");

            foreach (InflectionCategory cat in Enum.GetValues(typeof(InflectionCategory)))
            {
                sb.Append("<th>" + cat + "</th>");
            }
            sb.Append("</tr>");

            foreach (var summary in summaries)
            {
                sb.Append("<tr><td>" + summary.Root + "</td>");
                foreach (InflectionCategory cat in Enum.GetValues(typeof(InflectionCategory)))
                {
                    if (summary.Categories.Contains(cat))
                    {
                        // Build link to concordance file for this root+category
                        string concordanceFile = "/concordance/" + summary.Root + "-" + cat + ".html";
                        sb.Append("<td><a href='" + concordanceFile + "'>✔</a></td>");
                    }
                    else
                    {
                        sb.Append("<td></td>");
                    }
                }
                sb.Append("</tr>");
            }

            sb.Append("</table></body></html>");
            return sb.ToString();
        }

        public static HashSet<InflectionCategory> DetectInflections(string word)
        {
            var categories = new HashSet<InflectionCategory>();

            // Verb forms (simplified templates)
            if (Regex.IsMatch(word, @"^فعل$")) categories.Add(InflectionCategory.FormI);
            if (Regex.IsMatch(word, @"^فعّل$")) categories.Add(InflectionCategory.FormII);
            if (Regex.IsMatch(word, @"^فاعل$")) categories.Add(InflectionCategory.FormIII);
            if (Regex.IsMatch(word, @"^أفعل$")) categories.Add(InflectionCategory.FormIV);
            if (Regex.IsMatch(word, @"^تفعّل$")) categories.Add(InflectionCategory.FormV);
            if (Regex.IsMatch(word, @"^تفاعل$")) categories.Add(InflectionCategory.FormVI);
            if (Regex.IsMatch(word, @"^انفعل$")) categories.Add(InflectionCategory.FormVII);
            if (Regex.IsMatch(word, @"^افتعل$")) categories.Add(InflectionCategory.FormVIII);
            if (Regex.IsMatch(word, @"^افعلّ$")) categories.Add(InflectionCategory.FormIX);
            if (Regex.IsMatch(word, @"^استفعل$")) categories.Add(InflectionCategory.FormX);

            // Prefixes
            if (word.StartsWith("م")) categories.Add(InflectionCategory.PrefixMu);
            if (word.StartsWith("ت")) categories.Add(InflectionCategory.PrefixTa);
            if (word.StartsWith("است")) categories.Add(InflectionCategory.PrefixIst);

            // Nominal derivatives
            if (word.EndsWith("ة") || word.EndsWith("ان") || word.EndsWith("ون"))
                categories.Add(InflectionCategory.Nominal);

            // Participles (active/passive often with patterns like فاعل / مفعول)
            if (Regex.IsMatch(word, @"^م.+ون$") || Regex.IsMatch(word, @"^م.+ول$"))
                categories.Add(InflectionCategory.Participle);

            // Plurals
            if (word.EndsWith("ون") || word.EndsWith("ين"))
                categories.Add(InflectionCategory.PluralSound);
            if (Regex.IsMatch(word, @"^أ.+اء$")) // e.g. أسماء, أنبياء
                categories.Add(InflectionCategory.PluralBroken);

            // Case endings (simplified detection)
            if (word.EndsWith("ٌ")) categories.Add(InflectionCategory.CaseNominative);
            if (word.EndsWith("ً")) categories.Add(InflectionCategory.CaseAccusative);
            if (word.EndsWith("ٍ")) categories.Add(InflectionCategory.CaseGenitive);

            return categories;
        }

        public static string ExtractRoot(string word)
        {
            // Normalize: remove diacritics
            string normalized = RemoveDiacritics(word);

            // Strip common prefixes
            string[] prefixes = { "ال", "و", "ف", "ب", "ك", "ل", "س", "ي", "ت", "م", "است" };
            foreach (var p in prefixes)
            {
                if (normalized.StartsWith(p))
                {
                    normalized = normalized.Substring(p.Length);
                    break;
                }
            }

            // Strip common suffixes
            string[] suffixes = { "ون", "ين", "ان", "ة", "ات", "وا" };
            foreach (var s in suffixes)
            {
                if (normalized.EndsWith(s))
                {
                    normalized = normalized.Substring(0, normalized.Length - s.Length);
                    break;
                }
            }

            // Very simplified triliteral root extraction:
            // Keep only consonants (skip alif, waw, ya if weak letters at edges)
            var consonants = normalized.Where(c => !"aeiouَُِْ".Contains(c)).ToList();

            if (consonants.Count >= 3)
                return string.Join("-", consonants.Take(3));
            else
                return normalized; // fallback if less than 3 consonants
        }

        private static string RemoveDiacritics(string input)
        {
            // Remove Arabic diacritics (harakat)
            string diacritics = "\u064B\u064C\u064D\u064E\u064F\u0650\u0651\u0652";
            foreach (char d in diacritics)
                input = input.Replace(d.ToString(), "");
            return input;
        }

        public static void PreGenerateSummaryIndex()
        {
            string xmlPath = System.Web.Hosting.HostingEnvironment.MapPath("~/quran-simple-clean.xml");
            string summaryDir = System.Web.Hosting.HostingEnvironment.MapPath("~/concordance/");
            if (!Directory.Exists(summaryDir)) Directory.CreateDirectory(summaryDir);

            var doc = XDocument.Load(xmlPath);
            var summaries = new List<ConcordanceSummary>();

            foreach (var aya in doc.Descendants("aya"))
            {
                string text = aya.Attribute("text").Value;
                var words = text.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var w in words)
                {
                    string root = ExtractRoot(w); // your triliteral root extraction logic
                    var categories = DetectInflections(w);

                    var summary = summaries.FirstOrDefault(s => s.Root == root);
                    if (summary == null)
                    {
                        summary = new ConcordanceSummary { Root = root };
                        summaries.Add(summary);
                    }

                    foreach (var cat in categories)
                        summary.Categories.Add(cat);
                }
            }

            // Generate HTML table
            string html = GenerateSummaryTable(summaries);

            string filePath = Path.Combine(summaryDir, "summary-index.html");
            System.IO.File.WriteAllText(filePath, html, Encoding.UTF8);

            PreGenerateRootConcordances(summaries, doc);
        }

        public static void PreGenerateRootConcordances(List<ConcordanceSummary> summaries, XDocument doc)
        {
            string concordanceDir = System.Web.Hosting.HostingEnvironment.MapPath("~/concordance/");
            string chapterDir = System.Web.Hosting.HostingEnvironment.MapPath("~/chapter/");

            if (!Directory.Exists(concordanceDir)) Directory.CreateDirectory(concordanceDir);
            if (!Directory.Exists(chapterDir)) Directory.CreateDirectory(chapterDir);

            foreach (var summary in summaries)
            {
                string fileName = summary.Root + ".html";
                string filePath = Path.Combine(concordanceDir, fileName);

                if (System.IO.File.Exists(filePath)) continue;

                // Collect matches grouped by category
                var categoryMatches = new Dictionary<InflectionCategory, List<string>>();
                foreach (var cat in summary.Categories)
                    categoryMatches[cat] = new List<string>();

                var allMatches = new List<string>();

                foreach (var sura in doc.Descendants("sura"))
                {
                    string surahNum = sura.Attribute("index").Value;
                    string surahName = sura.Attribute("name").Value;

                    foreach (var aya in sura.Descendants("aya"))
                    {
                        string text = aya.Attribute("text").Value;
                        string verseNum = aya.Attribute("index").Value;

                        var words = text.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var w in words)
                        {
                            if (ExtractRoot(w) == summary.Root)
                            {
                                var cats = DetectInflections(w);

                                string link = "/chapter/" + surahNum + ".html#verse-" + verseNum;
                                string entry = surahName + " " + verseNum + ": <a href='" + link + "'>" + w + "</a>";

                                allMatches.Add(entry);

                                foreach (var cat in cats)
                                {
                                    if (categoryMatches.ContainsKey(cat))
                                        categoryMatches[cat].Add(entry);
                                }
                            }
                        }
                    }
                }

                // Build HTML
                var sb = new StringBuilder();
                sb.Append("<html><head><meta charset='UTF-8'><title>Concordance for " + summary.Root + "</title></head><body>");
                sb.Append("<nav><a href='/concordance/summary-index.html'>Summary Index</a> &raquo; " + summary.Root + "</nav>");
                sb.Append("<h2>Concordance for root '" + summary.Root + "'</h2><hr>");

                // Top section: table
                sb.Append("<h3>Inflection Table</h3><table border='1'><tr><th>Verse</th>");
                foreach (InflectionCategory cat in Enum.GetValues(typeof(InflectionCategory)))
                    sb.Append("<th>" + cat + "</th>");
                sb.Append("</tr>");

                // Build rows by verse
                foreach (var entry in allMatches)
                {
                    sb.Append("<tr><td>" + entry + "</td>");
                    foreach (InflectionCategory cat in Enum.GetValues(typeof(InflectionCategory)))
                    {
                        var match = categoryMatches.ContainsKey(cat) ? categoryMatches[cat].FirstOrDefault(m => m == entry) : null;
                        sb.Append("<td>" + (match != null ? "✔" : "") + "</td>");
                    }
                    sb.Append("</tr>");
                }
                sb.Append("</table>");

                // Bottom section: full concordance list
                sb.Append("<h3>All Verses</h3>");
                foreach (var m in allMatches)
                    sb.Append("<p>" + m + "</p>");

                sb.Append("</body></html>");

                System.IO.File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
            }
        }

        public static void PreGenerateRootCategoryConcordances(List<ConcordanceSummary> summaries, XDocument doc)
        {
            string concordanceDir = System.Web.Hosting.HostingEnvironment.MapPath("~/concordance/");
            string chapterDir = System.Web.Hosting.HostingEnvironment.MapPath("~/chapter/");

            if (!Directory.Exists(concordanceDir)) Directory.CreateDirectory(concordanceDir);
            if (!Directory.Exists(chapterDir)) Directory.CreateDirectory(chapterDir);

            foreach (var summary in summaries)
            {
                foreach (var cat in summary.Categories)
                {
                    string fileName = summary.Root + "-" + cat + ".html";
                    string filePath = Path.Combine(concordanceDir, fileName);

                    if (System.IO.File.Exists(filePath)) continue;

                    var matches = new List<string>();

                    foreach (var sura in doc.Descendants("sura"))
                    {
                        string surahNum = sura.Attribute("index").Value;
                        string surahName = sura.Attribute("name").Value;

                        foreach (var aya in sura.Descendants("aya"))
                        {
                            string text = aya.Attribute("text").Value;
                            string verseNum = aya.Attribute("index").Value;

                            var words = text.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (var w in words)
                            {
                                if (ExtractRoot(w) == summary.Root && DetectInflections(w).Contains(cat))
                                {
                                    // Ensure chapter file exists
                                    string chapterFile = Path.Combine(chapterDir, surahNum + ".html");
                                    if (!System.IO.File.Exists(chapterFile))
                                    {
                                        GenerateChapterFileStatic(chapterFile, surahName, surahNum, sura);
                                    }

                                    string link = "/chapter/" + surahNum + ".html#verse-" + verseNum;
                                    matches.Add(surahName + " " + verseNum + ": <a href='" + link + "'>" + text + "</a>");
                                }
                            }
                        }
                    }

                    // Build HTML with breadcrumb + back-link
                    string html = "<html><head><meta charset='UTF-8'><title>Concordance for " 
                                  + summary.Root + " (" + cat + ")</title></head><body>";

                    html += "<nav style='margin-bottom:10px;'>";
                    html += "   <a href='/concordance/summary-index.html'>Summary Index</a> ";
                    html += "   &raquo; <a href='/concordance/" + summary.Root + ".html'>" + summary.Root + "</a> ";
                    html += "   &raquo; " + cat;
                    html += "</nav>";

                    html += "<h2>Concordance for root '" + summary.Root + "' (" + cat + ")</h2><hr>";

                    foreach (var m in matches) html += "<p>" + m + "</p>";

                    html += "</body></html>";

                    System.IO.File.WriteAllText(filePath, html, Encoding.UTF8);
                }
            }
        }


        private static readonly object ChapterLock = new object();
        static readonly HashSet<string> GrammarWords = new HashSet<string>
        {
            "و", "في", "من", "إلى", "على", "عن", "أن", "إن", "ما", "لا", "لن", "لم",
            "قد", "هل", "بل", "لكن", "ثم", "أو", "إما", "كل", "أي", "هذا", "هذه",
            "ذلك", "تلك", "هو", "هي", "هم", "هن", "أنا", "نحن", "أنت", "أنتم"
        };



    }
}
