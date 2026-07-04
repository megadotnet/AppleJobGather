using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace AppleJobGather
{
    public class AppleJobCrawler
    {
        private readonly HttpClient _httpClient;

        public AppleJobCrawler()
        {
            var handler = new HttpClientHandler
            {
                UseCookies = true,
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
            };
            
            _httpClient = new HttpClient(handler);
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            _httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
            _httpClient.DefaultRequestHeaders.Add("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8");
        }

        public async Task<List<JobOutput>> CrawlJobsAsync(string locale, string query, int? maxPages, int? maxJobs, int delayMs)
        {
            var allJobs = new List<JobOutput>();
            var visitedPositionIds = new HashSet<string>();

            int currentPage = 1;
            int totalRecords = -1;
            bool hasMorePages = true;

            Console.WriteLine($"[INFO] [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 开始采集苹果官网招聘岗位. 地区: {locale}, 搜索关键词: '{query}', 延时: {delayMs}ms.");

            // 1. 列表页循环遍历
            while (hasMorePages)
            {
                if (maxPages.HasValue && currentPage > maxPages.Value)
                {
                    Console.WriteLine($"[INFO] [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 已达到最大指定爬取页数 {maxPages.Value}，停止列表扫描.");
                    break;
                }

                string searchUrl = $"https://jobs.apple.com/{locale.ToLower()}/search?sort=submitDate&page={currentPage}";
                if (!string.IsNullOrEmpty(query))
                {
                    searchUrl += $"&query={Uri.EscapeDataString(query)}";
                }

                Console.WriteLine($"[INFO] [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 正在请求列表页 第 {currentPage} 页: {searchUrl}");
                
                string? listHtml = await GetHtmlWithRetryAsync(searchUrl);
                if (string.IsNullOrEmpty(listHtml))
                {
                    Console.WriteLine($"[ERROR] [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 列表页 第 {currentPage} 页请求失败且超出重试次数，终止列表扫描.");
                    break;
                }

                string? hydrationJson = ExtractHydrationJson(listHtml);
                if (string.IsNullOrEmpty(hydrationJson))
                {
                    Console.WriteLine($"[ERROR] [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 列表页 第 {currentPage} 页未能提取到 window.__staticRouterHydrationData，终止列表扫描.");
                    break;
                }

                HydrationEnvelope? envelope = null;
                try
                {
                    envelope = JsonSerializer.Deserialize<HydrationEnvelope>(hydrationJson);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 解析列表页 Hydration JSON 失败: {ex.Message}");
                    break;
                }

                var searchData = envelope?.LoaderData?.Search;
                if (searchData == null || searchData.SearchResults == null || searchData.SearchResults.Length == 0)
                {
                    Console.WriteLine($"[INFO] [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 列表页 第 {currentPage} 页没有返回岗位数据，扫描结束.");
                    break;
                }

                if (totalRecords == -1)
                {
                    totalRecords = searchData.TotalRecords;
                    Console.WriteLine($"[INFO] [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 苹果官网招聘系统匹配到总岗位数: {totalRecords}");
                }

                Console.WriteLine($"[INFO] [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 列表页 第 {currentPage} 页成功解析出 {searchData.SearchResults.Length} 个岗位.");

                // 收集岗位标识
                var pagePositionIds = new List<string>();
                foreach (var result in searchData.SearchResults)
                {
                    string? posId = result.PositionId ?? result.Id;
                    if (!string.IsNullOrEmpty(posId) && visitedPositionIds.Add(posId))
                    {
                        pagePositionIds.Add(posId);
                    }
                }

                Console.WriteLine($"[INFO] [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 列表页 第 {currentPage} 页新增未抓取岗位数: {pagePositionIds.Count}");

                // 针对这一页的新岗位开始抓取详情页
                foreach (var posId in pagePositionIds)
                {
                    if (maxJobs.HasValue && allJobs.Count >= maxJobs.Value)
                    {
                        Console.WriteLine($"[INFO] [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 已达到最大指定采集岗位数 {maxJobs.Value}，停止采集.");
                        hasMorePages = false;
                        break;
                    }

                    // 合规睡眠延时
                    await Task.Delay(delayMs);

                    string detailUrl = $"https://jobs.apple.com/{locale.ToLower()}/details/{posId}";
                    Console.WriteLine($"[INFO] [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 正在采集岗位详情 ({allJobs.Count + 1}/{maxJobs?.ToString() ?? "无限制"}): {detailUrl}");

                    string? detailHtml = await GetHtmlWithRetryAsync(detailUrl);
                    if (string.IsNullOrEmpty(detailHtml))
                    {
                        Console.WriteLine($"[ERROR] [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 岗位详情页 {posId} 请求失败，跳过该岗位.");
                        continue;
                    }

                    string? detailHydrationJson = ExtractHydrationJson(detailHtml);
                    if (string.IsNullOrEmpty(detailHydrationJson))
                    {
                        Console.WriteLine($"[ERROR] [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 岗位详情页 {posId} 未能提取到 window.__staticRouterHydrationData，跳过该岗位.");
                        continue;
                    }

                    HydrationEnvelope? detailEnvelope = null;
                    try
                    {
                        detailEnvelope = JsonSerializer.Deserialize<HydrationEnvelope>(detailHydrationJson);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 解析岗位 {posId} 详情页 Hydration JSON 失败: {ex.Message}");
                        continue;
                    }

                    var jobData = detailEnvelope?.LoaderData?.JobDetails?.JobsData;
                    if (jobData == null)
                    {
                        Console.WriteLine($"[WARN] [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 岗位 {posId} 未返回 jobsData 字段（可能已下架/过期），跳过.");
                        continue;
                    }

                    // 映射到 JobOutput
                    var jobOutput = new JobOutput
                    {
                        JobId = jobData.PositionId ?? jobData.Id,
                        JobName = jobData.PostingTitle,
                        JobSummary = jobData.JobSummary,
                        Description = jobData.Description,
                        PreferredQualifications = jobData.PreferredQualifications,
                        MinimumQualifications = jobData.MinimumQualifications,
                        Team = jobData.TeamNames != null ? string.Join(", ", jobData.TeamNames) : "",
                        Location = jobData.Locations != null ? string.Join("; ", jobData.Locations.Select(l => $"{l.Name} (City: {l.City}, State: {l.StateProvince}, Country: {l.CountryName})")) : "",
                        PostingDate = jobData.PostingDate,
                        CrawlTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        SourceUrl = detailUrl
                    };

                    allJobs.Add(jobOutput);
                }

                if (!hasMorePages) break;

                // 判断是否还有下一页
                if (searchData.SearchResults.Length < 20 || (maxJobs.HasValue && allJobs.Count >= maxJobs.Value))
                {
                    Console.WriteLine($"[INFO] [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 列表扫描完成，已无更多数据或满足岗位上限条件.");
                    hasMorePages = false;
                }
                else
                {
                    currentPage++;
                    // 翻页前睡眠
                    await Task.Delay(delayMs);
                }
            }

            Console.WriteLine($"[INFO] [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 岗位采集结束. 共成功采集到 {allJobs.Count} 条岗位数据.");
            return allJobs;
        }

        private string? ExtractHydrationJson(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var scripts = doc.DocumentNode.SelectNodes("//script");
            if (scripts == null) return null;

            foreach (var script in scripts)
            {
                var text = script.InnerText;
                if (text.Contains("window.__staticRouterHydrationData"))
                {
                    int startQuote = text.IndexOf("JSON.parse(\"");
                    if (startQuote == -1) continue;
                    startQuote += 11; // 获取 JSON.parse(" 的后引号位置

                    int endQuote = text.LastIndexOf("\")");
                    if (endQuote == -1 || endQuote <= startQuote) continue;

                    var escapedJson = text.Substring(startQuote + 1, endQuote - startQuote - 1);
                    var jsonStringToParse = "\"" + escapedJson + "\"";
                    try
                    {
                        return JsonSerializer.Deserialize<string>(jsonStringToParse);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 反序列化转义的 Hydration JSON 字符串失败: {ex.Message}");
                    }
                }
            }
            return null;
        }

        private async Task<string?> GetHtmlWithRetryAsync(string url, int maxRetries = 3)
        {
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    var response = await _httpClient.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        return await response.Content.ReadAsStringAsync();
                    }
                    
                    Console.WriteLine($"[WARN] [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] HTTP 请求失败. URL: {url}, HTTP 状态码: {response.StatusCode}. 重试 {attempt}/{maxRetries}...");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WARN] [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] HTTP 请求异常. URL: {url}, 异常类型: {ex.GetType().Name}, 异常信息: {ex.Message}. 重试 {attempt}/{maxRetries}...");
                }

                if (attempt < maxRetries)
                {
                    // 渐进式延迟重试：2秒、4秒、6秒
                    await Task.Delay(attempt * 2000);
                }
            }
            return null;
        }
    }
}
