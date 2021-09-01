using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Flurl.Http;
using FreeSql;
using HtmlAgilityPack;

// ReSharper disable ReplaceWithSingleCallToFirstOrDefault

namespace App
{
    class Program
    {
        private const string DbName = "data.db";

        private static IFreeSql FSql = new FreeSqlBuilder()
                                       .UseConnectionString(DataType.Sqlite, $"Data Source=|DataDirectory|\\{DbName}")
                                       .UseAutoSyncStructure(true)
                                       .Build();

        private static string BaseUrl = "http://www.stats.gov.cn/tjsj/tjbz/tjyqhdmhcxhfdm/2020/index.html";

        private static readonly Dictionary<GradeEnum, string> Mapper = new()
        {
            {GradeEnum.Province, "//tr[@class='provincetr']/td/a"},
            {GradeEnum.City, "//tr[@class='citytr']"},
            {GradeEnum.County, "//tr[@class='countytr']"},
            {GradeEnum.Town, "//tr[@class='towntr']"},
            {GradeEnum.Village, "//tr[@class='villagetr']"},
        };

        static int Main(string[] args)
        {
            FSql.CodeFirst.SyncStructure<AreaBase>();
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var rootCmd = new RootCommand
            {
                Description = "爬取2020年度全国统计用区划代码和城乡划分代码",
            };
            rootCmd.Add(new Command("clear", "清理")
            {
                Handler = CommandHandler.Create(() =>
                {
                    if (File.Exists(DbName)) File.Delete(DbName);
                })
            });
            rootCmd.Add(new Command("crawl", "爬取")
            {
                Handler = CommandHandler.Create(() =>
                {
                    GetArea(GradeEnum.Province);
                    GetArea(GradeEnum.City);
                    GetArea(GradeEnum.County);
                    GetArea(GradeEnum.Town);
                    GetArea(GradeEnum.Village);
                })
            });

            return rootCmd.InvokeAsync(args).Result;
        }


        private static void GetArea(GradeEnum grade)
        {
            if (grade == GradeEnum.Province)
            {
                var html = GetHtml(BaseUrl);
                if (html == null) return;
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var data  = new List<AreaBase>();
                var nodes = doc.DocumentNode.SelectNodes(Mapper[GradeEnum.Province]);
                if (nodes == null)
                {
                    Console.WriteLine("Error");
                    return;
                }

                foreach (var node in nodes)
                {
                    var id       = Guid.NewGuid();
                    var parentId = Guid.Empty;

                    var name     = node.InnerText?.Trim() ?? string.Empty;
                    var code     = string.Empty;
                    var href     = node.GetAttributeValue("href", null);
                    var childUrl = GetUrl(BaseUrl, href);


                    if (childUrl == null) continue;
                    data.Add(new AreaBase
                    {
                        Id          = id,
                        ParentId    = parentId,
                        Grade       = GradeEnum.Province,
                        ParentGrade = GradeEnum.None,
                        Code        = code,
                        Name        = name,
                        ChildUrl    = childUrl
                    });
                }

                FSql.Insert(data).ExecuteAffrows();
                data.ForEach(_ => Console.WriteLine($"增加[{Enum.GetName(GradeEnum.Province)}] | [{_.Name}]"));
                return;
            }

            var pGrade = (GradeEnum) (int) grade - 1;

            var areas = FSql.Select<AreaBase>()
                            .Where(x => x.Grade == pGrade)
                            .Where(x => !string.IsNullOrEmpty(x.ChildUrl))
                            .ToList();
            var areaIndex = 1;
            Parallel.For(0, areas.Count, new ParallelOptions
            {
                MaxDegreeOfParallelism = 5,
            }, i =>
            {
                var area = areas[i];
                var html = GetHtml(area.ChildUrl);
                var doc  = new HtmlDocument();
                doc.LoadHtml(html);

                var hasCity    = (doc.DocumentNode.SelectNodes("//table[@class='citytable']")?.Count ?? 0) > 0;
                var hasCounty  = (doc.DocumentNode.SelectNodes("//table[@class='countytable']")?.Count ?? 0) > 0;
                var hasTown    = (doc.DocumentNode.SelectNodes("//table[@class='towntable']")?.Count ?? 0) > 0;
                var hasVillage = (doc.DocumentNode.SelectNodes("//table[@class='villagetable']")?.Count ?? 0) > 0;

                var currentGrade = GradeEnum.None;
                if (hasCity) currentGrade         = GradeEnum.City;
                else if (hasCounty) currentGrade  = GradeEnum.County;
                else if (hasTown) currentGrade    = GradeEnum.Town;
                else if (hasVillage) currentGrade = GradeEnum.Village;

                if (currentGrade == GradeEnum.None) return;

                var data  = new List<AreaBase>();
                var nodes = doc.DocumentNode.SelectNodes(Mapper[currentGrade]);
                foreach (var node in nodes)
                {
                    var id = Guid.NewGuid();

                    var tdTags = node.SelectNodes("td");
                    if (tdTags.Count < 2)
                    {
                        Console.WriteLine("无法获取名称、代码");
                        continue;
                    }

                    if (tdTags.Count == 3)
                    {
                    }

                    var name = tdTags.Count == 3
                        ? tdTags[2]?.InnerText?.Trim()
                        : tdTags[1]?.InnerText?.Trim();
                    var code = tdTags[0]?.InnerText?.Trim();

                    var href     = tdTags[0].SelectSingleNode("a")?.GetAttributeValue("href", null);
                    var childUrl = GetUrl(area.ChildUrl, href) ?? string.Empty;

                    data.Add(new AreaBase
                    {
                        Id          = id,
                        ParentId    = area.Id,
                        Grade       = currentGrade,
                        ParentGrade = pGrade,
                        Code        = code,
                        Name        = name,
                        ChildUrl    = childUrl
                    });
                }

                var rows = FSql.Insert(data).ExecuteAffrows();
                // data.ForEach(_ => Console.WriteLine($"增加 [{Enum.GetName(currentGrade)}] | [{area.Name}] - [{_.Name}]"));
                var cc = (decimal) areaIndex / areas.Count * 100;
                Console.WriteLine($"[{areaIndex}/{areas.Count}] {cc:F}% | +{rows}");
                Interlocked.Add(ref areaIndex, 1);
            });
        }

        private static string GetUrl(string url, string href)
        {
            var tmp = url[..url.LastIndexOf("/", StringComparison.Ordinal)];
            return href == null ? null : $"{tmp}/{href}";
        }

        private static string GetHtml(string url)
        {
            var    retry      = 0;
            var    retryCount = 10;
            string result     = null;
            while (retry < retryCount)
            {
                try
                {
                    var time = new Random().Next(300, 800);
                    Console.WriteLine($"sleep {time} ms | {url}");
                    Thread.Sleep(time);
                    var req = url
                              .WithHeader("User-Agent", "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/51.0.2704.103 Safari/537.36")
                              .GetBytesAsync().Result;
                    result = Encoding.GetEncoding("gb2312").GetString(req);
                    break;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    retry += 1;
                    Console.WriteLine($"重试 {retry}/{retryCount}");
                }
            }

            return result;
        }
    }
}