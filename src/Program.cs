using System;
using System.Xml;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

using Microsoft.SyndicationFeed;
using Microsoft.SyndicationFeed.Rss;

namespace Syndication.Core.Console
{
    public class Program
    {
        public static void Main(string[] args)
        {
            MainAsync().Wait();
        }

        static async Task MainAsync()
        {
            var service = new NewsFeedService("https://www.asp.net/rss/spotlight");
            var newsItems = await service.GetNewsFeed();
            foreach (var item in newsItems)
            {
                System.Console.WriteLine(item.Title);
                System.Console.WriteLine(item.Excerpt);
                System.Console.WriteLine(item.Uri);
                System.Console.WriteLine("");
            }
            System.Console.ReadLine();
        }
    }


    //Model
    public class NewsItem
    {
        public NewsItem(string title, string excerpt, DateTime publishDate, string uri)
        {
            Title = title;
            Excerpt = excerpt;
            PublishDate = publishDate;
            Uri = uri;
        }

        public string Title { get; set; }
        public string Excerpt { get; set; }
        public DateTime PublishDate { get; set; }
        public string Uri { get; set; }
    }

    //Class that you will use to call
    public class NewsFeedService
    {

        private readonly string _FeedUri;
        public NewsFeedService(string feedUri)
        {
            _FeedUri = feedUri;
        }

        public async Task<NewsItem[]> GetNewsFeed()
        {
            var rssNewsItems = new List<NewsItem>();
            using (var xmlReader = XmlReader.Create(_FeedUri, new XmlReaderSettings() { Async = true }))
            {
                var feedReader = new RssFeedReader(xmlReader);
                while (await feedReader.Read())
                {
                    if (feedReader.ElementType == Microsoft.SyndicationFeed.SyndicationElementType.Item)
                    {
                        ISyndicationItem item = await feedReader.ReadItem();
                        rssNewsItems.Add(item.ConvertToNewsItem());
                    }
                }
            }
            return rssNewsItems.OrderByDescending(p => p.PublishDate).ToArray();
        }
    }

    //Extension Methods for converting a ISyndicationItem to a NewsItem
    public static class SyndicationExtensions
    {
        public static NewsItem ConvertToNewsItem(this ISyndicationItem item)
        {
            return new NewsItem(title: item.Title,
                excerpt: item.Description.PlainTextTruncate(200),
                publishDate: item.Published.UtcDateTime,
                uri: item.Links.First().Uri.AbsoluteUri);
        }
    }

    //String extension methods for converting HtmlToPlainTest
    public static class StringExtensions
    {
        public static string PlainTextTruncate(this string input, int length)
        {
            string text = HtmlToPlainText(input);
            if (text.Length < length)
            {
                return text;
            }

            char[] terminators = { '.', ',', ';', ':', '?', '!' };
            int end = text.LastIndexOfAny(terminators, length);
            if (end == -1)
            {
                end = text.LastIndexOf(" ", length);
                return text.Substring(0, end) + "...";
            }
            return text.Substring(0, end + 1);
        }

        //From https://stackoverflow.com/a/16407272/5
        //TODO: Use a proper sanitizer, perhaps https://github.com/atifaziz/High5
        public static string HtmlToPlainText(this string html)
        {
            const string tagWhiteSpace = @"(>|$)(\W|\n|\r)+<";//matches one or more (white space or line breaks) between '>' and '<'
            const string stripFormatting = @"<[^>]*(>|$)";//match any character between '<' and '>', even when end tag is missing
            const string lineBreak = @"<(br|BR)\s{0,1}\/{0,1}>";//matches: <br>,<br/>,<br />,<BR>,<BR/>,<BR />
            var lineBreakRegex = new Regex(lineBreak, RegexOptions.Multiline);
            var stripFormattingRegex = new Regex(stripFormatting, RegexOptions.Multiline);
            var tagWhiteSpaceRegex = new Regex(tagWhiteSpace, RegexOptions.Multiline);

            var text = html;
            //Decode html specific characters
            text = System.Net.WebUtility.HtmlDecode(text);
            //Remove tag whitespace/line breaks
            text = tagWhiteSpaceRegex.Replace(text, "><");
            //Replace <br /> with line breaks
            text = lineBreakRegex.Replace(text, Environment.NewLine);
            //Strip formatting
            text = stripFormattingRegex.Replace(text, string.Empty);

            return text;
        }
    }
}
