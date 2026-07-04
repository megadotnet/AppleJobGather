using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace AppleJobGather
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // 默认参数
            string locale = "en-us";
            string query = "";
            int? maxPages = null;
            int? maxJobs = null;
            int delayMs = 2000;
            string outputPath = "jobs.json";

            // 解析命令行参数
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--help" || args[i] == "-h")
                {
                    PrintHelp();
                    return;
                }
                else if (args[i] == "--locale" && i + 1 < args.Length)
                {
                    locale = args[++i];
                }
                else if (args[i] == "--query" && i + 1 < args.Length)
                {
                    query = args[++i];
                }
                else if (args[i] == "--max-pages" && i + 1 < args.Length)
                {
                    if (int.TryParse(args[++i], out int pages))
                        maxPages = pages;
                }
                else if (args[i] == "--max-jobs" && i + 1 < args.Length)
                {
                    if (int.TryParse(args[++i], out int jobs))
                        maxJobs = jobs;
                }
                else if (args[i] == "--delay" && i + 1 < args.Length)
                {
                    if (int.TryParse(args[++i], out int delay))
                        delayMs = delay;
                }
                else if (args[i] == "--output" && i + 1 < args.Length)
                {
                    outputPath = args[++i];
                }
            }

            var crawler = new AppleJobCrawler();
            try
            {
                var jobs = await crawler.CrawlJobsAsync(locale, query, maxPages, maxJobs, delayMs);

                // 导出数据
                string ext = Path.GetExtension(outputPath).ToLower();
                if (ext == ".csv")
                {
                    await ExportToCsvAsync(jobs, outputPath);
                }
                else
                {
                    // 默认导出 JSON
                    await ExportToJsonAsync(jobs, outputPath);
                }

                Console.WriteLine($"[INFO] [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 数据已成功导出至: {Path.GetFullPath(outputPath)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FATAL] [{DateTime.Now:yyyy-MM-dd HH:mm:ss}] 采集任务发生未捕获的严重异常: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        private static void PrintHelp()
        {
            Console.WriteLine("苹果官网招聘岗位信息自动化采集控制台程序 (Apple Job Gatherer)");
            Console.WriteLine("用法 (Usage):");
            Console.WriteLine("  AppleJobGather [options]");
            Console.WriteLine();
            Console.WriteLine("选项 (Options):");
            Console.WriteLine("  --locale <str>    语言与国家区域设置 (默认: en-us，中国区可设为 zh-cn)");
            Console.WriteLine("  --query <str>     搜索关键词 (默认: 空，采集所有岗位)");
            Console.WriteLine("  --max-pages <num> 最大扫描的列表页数 (默认: 扫描所有页数)");
            Console.WriteLine("  --max-jobs <num>  最大采集并保存的岗位数 (默认: 采集所有符合的岗位)");
            Console.WriteLine("  --delay <num>     两次网络请求之间的休眠间隔(毫秒) (默认: 2000，不建议设置低于2000)");
            Console.WriteLine("  --output <path>   导出文件路径 (支持 .json 或 .csv，默认: jobs.json)");
            Console.WriteLine("  --help, -h        显示此帮助说明信息");
            Console.WriteLine();
            Console.WriteLine("示例 (Example):");
            Console.WriteLine("  dotnet run -- --locale zh-cn --query 软件工程师 --max-jobs 10 --output jobs_china.csv");
        }

        private static async Task ExportToJsonAsync(List<JobOutput> jobs, string path)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(jobs, options);
            await File.WriteAllTextAsync(path, json, Encoding.UTF8);
        }

        private static async Task ExportToCsvAsync(List<JobOutput> jobs, string path)
        {
            var csvLines = new List<string>
            {
                "JobId,JobName,JobSummary,Description,PreferredQualifications,MinimumQualifications,Team,Location,PostingDate,CrawlTime,SourceUrl"
            };

            foreach (var job in jobs)
            {
                csvLines.Add(string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10}",
                    EscapeCsvField(job.JobId),
                    EscapeCsvField(job.JobName),
                    EscapeCsvField(job.JobSummary),
                    EscapeCsvField(job.Description),
                    EscapeCsvField(job.PreferredQualifications),
                    EscapeCsvField(job.MinimumQualifications),
                    EscapeCsvField(job.Team),
                    EscapeCsvField(job.Location),
                    EscapeCsvField(job.PostingDate),
                    EscapeCsvField(job.CrawlTime),
                    EscapeCsvField(job.SourceUrl)
                ));
            }

            // 带 BOM 的 UTF-8 编码，防止 Excel 打开中文乱码
            await File.WriteAllLinesAsync(path, csvLines, new UTF8Encoding(true));
        }

        private static string EscapeCsvField(string? field)
        {
            if (string.IsNullOrEmpty(field)) return "";
            var escaped = field.Replace("\"", "\"\"");
            if (escaped.Contains(",") || escaped.Contains("\n") || escaped.Contains("\r") || escaped.Contains("\"\""))
            {
                return $"\"{escaped}\"";
            }
            return escaped;
        }
    }
}
