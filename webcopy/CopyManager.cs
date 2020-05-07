using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace webcopy
{
    class CopyManager
    {
        private HtmlWeb web = new HtmlWeb();

        public Uri Website { get; private set; }

        public Exception LastException = null;

        public CopyManager(string WebURL)
        {
            Website = new Uri(WebURL);
        }

        public string GetSiteTitle()
        {
            try
            {
                HtmlDocument page = web.Load(Website);
                return page.DocumentNode.SelectSingleNode("//head/title").InnerText;
            }
            catch (Exception ex)
            {
                LastException = ex;
            }

            return string.Empty;
        }

        public CopyPageResult CopyPage(string webPage)
        {
            CopyPageResult result = new CopyPageResult();

            try
            {
                HtmlDocument page = web.Load(webPage);
                result.Page = new SiteFile(webPage, DownloadPage("C:\\Temp", webPage));

                if (webPage.ToLowerInvariant().Contains(".css"))
                    result.Links.AddRange(GetLinksFromCSS(Website.AbsoluteUri, webPage).ToArray());
                else
                    result.Links.AddRange(GetLinks(Website.AbsoluteUri, page).ToArray());
            }
            catch (Exception ex)
            {
                LastException = ex;
            }

            return result;
        }

        private bool DownloadPage(string downloadFolder, string webPage)
        {
            Uri pageUrl = new Uri(webPage);

            string localFilePath = string.Empty;
            string filename = "\\index.html";

            if (IsFile(pageUrl))
            {
                filename = pageUrl.Segments[pageUrl.Segments.Length - 1];
                localFilePath = string.Concat(downloadFolder, "\\", pageUrl.Host, "\\", pageUrl.LocalPath.Replace(filename, string.Empty).Replace("/", "\\"));
            }
            else
            {
                localFilePath = string.Concat(downloadFolder, "\\", pageUrl.Host, "\\", pageUrl.LocalPath.Replace("/", "\\"));
            }

            if (!Directory.Exists(localFilePath))
                Directory.CreateDirectory(localFilePath);

            try
            {
                using (WebClient client = new WebClient())
                {
                    localFilePath = string.Concat(localFilePath, filename).Replace("\\\\", "\\");

                    if (!File.Exists(localFilePath))
                        client.DownloadFile(webPage, localFilePath);

                    return true;
                }
            }
            catch (Exception ex)
            {
                LastException = ex;
            }

            return false;
        }

        private IEnumerable<string> GetLinks(string parentPagePath, HtmlDocument page)
        {
            List<string> files = new List<string>();

            try
            {
                HtmlNodeCollection links = page.DocumentNode.SelectNodes("//a | //link");

                if (links != null)
                {
                    foreach (HtmlNode link in links)
                    {
                        string value = string.Empty;

                        value = link.GetAttributeValue("href", string.Empty);
                        if (!string.IsNullOrEmpty(value) && !value.StartsWith("#", StringComparison.InvariantCultureIgnoreCase) && !value.Contains("javascript"))
                        {
                            string formattedLink = FormatFilePath(parentPagePath, value);

                            if (new Uri(parentPagePath).Host.Equals(new Uri(formattedLink).Host))
                                files.Add(formattedLink);
                        }
                    }
                }

                // src Parse
                links = page.DocumentNode.SelectNodes("//img | //script");

                if (links != null)
                {
                    foreach (HtmlNode link in links)
                    {
                        string value = string.Empty;

                        value = link.GetAttributeValue("src", string.Empty);
                        if (!string.IsNullOrEmpty(value) && !value.StartsWith("#", StringComparison.InvariantCultureIgnoreCase) && !value.Contains("javascript"))
                        {
                            string formattedLink = FormatFilePath(parentPagePath, value);

                            if (new Uri(parentPagePath).Host.Equals(new Uri(formattedLink).Host))
                                files.Add(formattedLink);
                        }
                    }
                }
            }
            catch
            {
                // Empty for now
            }

            return files.Distinct().ToList();
        }

        private IEnumerable<string> GetLinksFromCSS(string parentPagePath, string cssFile)
        {
            List<string> uris = new List<string>();
            CSSParser cssParser = new CSSParser();

            string cssContent = string.Empty;
            using (WebClient client = new WebClient ())
            {
                cssContent = client.DownloadString(cssFile);
            }

            if (!cssContent.Equals(string.Empty))
            {
                uris = cssParser.ExtractUris(cssContent);

                for (int index = 0; index < uris.Count; index++)
                {
                    uris[index] = FormatFilePath(cssFile, uris[index]);
                }
            }

            return uris;
        }

        private string FormatFilePath(string parentPage, string filePath)
        {
            Uri formatedFilePath = new Uri(new Uri(parentPage), filePath);

            if (!IsFile(formatedFilePath))
                formatedFilePath = new Uri(formatedFilePath, "index.html");

            return formatedFilePath.AbsoluteUri;
        }

        private bool IsFile(Uri url)
        {
            return url.AbsolutePath.Split('/').Last().Contains('.');
        }
    }

    public class CopyPageResult
    {
        public SiteFile Page { get; set; } = new SiteFile();
        public List<string> Links = new List<string>();
    }

    public class SiteFile
    {
        public string Filename = string.Empty;
        public bool Downloaded = false;

        public SiteFile()
        {

        }

        public SiteFile(string filename, bool downloaded)
        {
            this.Filename = filename;
            this.Downloaded = downloaded;
        }
    }
}
