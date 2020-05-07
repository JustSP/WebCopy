using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace webcopy
{
    public class CSSParser
    {
        public List<string> ExtractUris(string cssContent)
        {
            List<string> uris = new List<string>();

            if (cssContent.ToLowerInvariant().Contains("url"))
            {
                string[] subs = cssContent.ToLowerInvariant ().Split(new string[] { "url" }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string sub in subs)
                {
                    if ((sub.Trim().IndexOf ("(") > -1) & (sub.Trim().IndexOf("(") < 3))
                    {
                        int length = sub.IndexOf(")");

                        if (length > 0)
                        {
                            string uri = sub.Substring(0, length);

                            uri = uri.Replace("(", string.Empty);

                            if (!uri.Contains("data:image"))
                                uris.Add(uri);
                        }
                    }
                    
                }
            }

            return uris;
        }
    }
}
