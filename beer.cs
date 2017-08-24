using System;
using System.Linq;
using Discord;
using Supremes;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CataBot
{
    public struct LinkItem
    {
        // Create a list item that contains the link and link text seperated by a space.
        public string Href;
        public string Text;
        public override string ToString()
        {
            return Href + " " + Text;
        }
    }
    static class LinkFinder
    {
        public static List<LinkItem> Find(string file)
        {
            List<LinkItem> list = new List<LinkItem>();
            // Find all matches in file.
            MatchCollection m1 = Regex.Matches(file, @"(<a.*?>.*?</a>)",
                RegexOptions.Singleline);
            // Loop over each match.
            foreach (Match m in m1)
            {
                string value = m.Groups[1].Value;
                LinkItem i = new LinkItem();
                // Get href attribute.
                Match m2 = Regex.Match(value, @"href=\""(.*?)\""",
                    RegexOptions.Singleline);
                if (m2.Success)
                {
                    i.Href = m2.Groups[1].Value;
                }
                // 4.
                // Remove inner tags from text.
                string t = Regex.Replace(value, @"\s*<.*?>\s*", "",
                    RegexOptions.Singleline);
                i.Text = t;
                list.Add(i);
            }
            return list;
        }
    }
    class beer
    {
        public static string Search(string search, List<string> unwanted)
        {
            string sitemap = "https://www.beermerchants.com/catalog/sitemap/";
            string sm = Dcsoup.Parse(new Uri(sitemap), 5000).ToString();
            List<LinkItem> links = LinkFinder.Find(sm);
            int[] distances = new int[links.Count];
            int[] matches = new int[links.Count];
            List<int> PossibleIndices = new List<int>();
            for (int i = 0; i < links.Count; i++)
            {
                string[] substrings = search.Split(' ');
                matches[i] = 0;
                foreach (var substring in substrings)
                {
                    if (links[i].ToString().Substring(links[i].ToString().IndexOf(" ") + 1).ToUpper().Contains(substring.ToUpper()))
                    {
                        matches[i]++;
                    }
                }
                bool ignore = new bool();
                foreach (var item in unwanted)
                {
                    if (links[i].ToString().Contains(item.ToString()))
                    {
                        ignore = true;
                        break;
                    }
                }
                if (ignore == false)
                {
                    if ((search.ToLower().Contains("glass") & links[i].ToString().Contains("glass")) || (!search.ToLower().Contains("glass") & !links[i].ToString().Contains("glass")))
                    {
                        distances[i] = functions.LevenshteinDistance(search, links[i].ToString().Substring(links[i].ToString().IndexOf(" ") + 1));
                        PossibleIndices.Add(i);
                    }
                    else
                    {
                        distances[i] = -1;
                    }
                }
                else
                {
                    distances[i] = -1;
                }
            }
            if (search == "RandomBeer")
            {
                int randomIndex = functions.GenIndex(PossibleIndices.Count);
                int secondIndex = PossibleIndices[randomIndex];
                return links[secondIndex].ToString().Substring(0, links[secondIndex].ToString().IndexOf(" "));
            }
            int bestmatch = matches.Max();
            if (bestmatch == 0)
            {
                return ("Not found!");
            }
            for (int i = 0; i < matches.Length; i++)
            {
                if (matches[i] != bestmatch)
                {
                    distances[i] = -1;
                }
            }
            int minDist = distances.Where(x => x >= 0).Min();
            int IndexOfMinDist = 0;
            for (int i = 0; i < distances.Length; i++)
            {
                if (distances[i] == minDist)
                {
                    IndexOfMinDist = i;
                    break;
                }
                else { }
            }
            string ClosestMatch = links[IndexOfMinDist].ToString();
            return ClosestMatch.Substring(0, ClosestMatch.IndexOf(" "));
        }
        public static List<string> Retrieve(string url, Channel chnl, string search)
        {
            string description = null;
            string beername = null;
            string imageurl = null;
            try
            {
                var page = Dcsoup.Parse(new Uri(url), 5000);
                description = page.GetElementById("description").Text.ToString();
                beername = page.Select("h1").Text.ToString();
                imageurl = page.Select("img.img-responsive.block-center").ToString();
            }
            catch
            {
                return null;
            }
            int srcmarker = imageurl.IndexOf("src=");
            int altmarker = imageurl.IndexOf("alt=");
            int imageurllength = altmarker - srcmarker - 7;
            imageurl = imageurl.Substring(srcmarker + 5, imageurllength);
            List<string> MessageItem = new List<string>();
            MessageItem.Add(imageurl);
            MessageItem.Add(beername);
            int brwrymarker = -1;
            int cntrymarker = -1;
            int stylemarker = -1;
            int abvmarker = -1;
            int sizemarker = -1;
            if (description.Contains("brewery"))
            {
                brwrymarker = description.LastIndexOf("brewery");
            }
            if (description.Contains("country"))
            {
                cntrymarker = description.LastIndexOf("country");
            }
            if (description.Contains("styles"))
            {
                stylemarker = description.LastIndexOf("styles");
            }
            if (description.Contains("ABV%"))
            {
                abvmarker = description.LastIndexOf("ABV%");
            }
            if (description.Contains("Size"))
            {
                sizemarker = description.LastIndexOf("Size");
            }
            int[] indices = { cntrymarker, stylemarker, abvmarker, brwrymarker, sizemarker };
            string brewery = "brewery";
            string country = "country";
            string abv = "abv";
            string style = "style";
            if (brwrymarker != -1)
            {
                if (brwrymarker != indices.Max())
                {
                    brewery = description.Substring(brwrymarker + 8, indices.Where(x => x > brwrymarker).Min() - brwrymarker - 9);
                }
                else
                {
                    brewery = description.Substring(brwrymarker + 8);
                }
                MessageItem.Add("Brewery: *" + brewery + "*");
            }
            if (cntrymarker != -1)
            {
                if (cntrymarker != indices.Max())
                {
                    country = description.Substring(cntrymarker + 8, indices.Where(x => x > cntrymarker).Min() - cntrymarker - 14);
                }
                else
                {
                    country = description.Substring(cntrymarker + 8);
                }
                MessageItem.Add("Nationality: *" + country + "*");
            }
            if (stylemarker != -1)
            {
                if (stylemarker != indices.Max())
                {
                    style = description.Substring(stylemarker + 7, indices.Where(x => x > stylemarker).Min() - stylemarker - 8);
                }
                else
                {
                    style = description.Substring(stylemarker + 7);
                }
                MessageItem.Add("Style: *" + style + "*");
            }
            if (abvmarker != -1)
            {
                abv = description.Substring(abvmarker + 5, 5);
                if (abv.Contains(" "))
                {
                    abv = abv.Substring(0, abv.IndexOf(" "));
                }
                MessageItem.Add("ABV: " + abv);
            }
            string notes = description.Substring(sizemarker);
            notes = notes.Substring(notes.IndexOf(" ") + 1);
            notes = notes.Substring(notes.IndexOf(" ") + 1);
            notes.Replace(' ', ',');
            description = description.Substring(0, indices.Where(x => x > 0).Min() - 1);
            if (description.Contains("Sign up to get notified"))
            {
                int cut = description.IndexOf("Sign up");
                description = description.Substring(0, cut);
            }
            if (description != null)
            {
                MessageItem.Add("Description:\n```" + description + "```");
            }
            if (notes != null)
            {
                MessageItem.Add("Notes: *" + notes + "*");
            }
            string Message = string.Join("\n", MessageItem.ToArray());
            return MessageItem;
        }
    }
}