使用.NET开发控制台应用程序，实现对苹果公司官网招聘页面的岗位信息采集功能，具体要求如下：

1. 技术栈要求：
   - 采用 C# 开发，基于 .NET 8 或以上版本的框架。
   - 使用 `HttpClient` 进行网页请求，并集成 `HtmlAgilityPack` 定位页面中的关键节点。
   - 采用提取并反序列化页面预加载 JSON 数据（Hydration Data）的方案进行数据获取，避免直接解析客户端渲染（CSR）的易变 DOM 结构。
   - 集成 `System.Text.Json` 进行高效的 JSON 反序列化及数据清洗。

2. 页面分析与爬取逻辑：
   - 针对目标网页（列表页 `https://jobs.apple.com/{locale}/search` 和详情页 `https://jobs.apple.com/{locale}/details/{positionId}`），分析其前端页面渲染机制。
   - 定位页面 HTML 中嵌入的 `<script id="__staticRouterHydrationData" ...>` 或包含 `window.__staticRouterHydrationData` 的渲染状态脚本。
   - 提取出其 `JSON.parse` 参数中的转义 JSON 字符串，并使用 `System.Text.Json` 进行反序列化，获得结构化的原始岗位数据，规避前端 JavaScript 异步请求延时和传统 CSS/XPath 节点解析器的不稳定性。

3. 功能实现要求：
   - 实现自动遍历搜索列表页面的所有分页，提取每个岗位的唯一标识 `positionId` 与基本元数据。
   - 针对提取到的每个岗位，自动请求其岗位详情页。通过解析详情页的 hydration 数据，精准提取以下字段：
     - 岗位唯一标识 (`positionId` 或 `id`)
     - 岗位名称 (`postingTitle`)
     - 简要描述 (`jobSummary`)
     - 详细描述内容 (`description`)
     - 优先条件/任职要求 (`preferredQualifications` 与 `minimumQualifications`）
     - 所属团队与部门 (`teamNames`)
     - 工作地点 (`locations` 数组列表)
     - 采集时间与发布时间 (`postingDate` / `postDateInGMT`)
     - 原详情页 URL
   - 采集过程中需添加合规的请求间隔（每次请求间隔不低于 2 秒），遵守网站 robots 协议，避免对目标服务器造成压力。
   - 异常处理：实现网络请求超时/失败、数据解析异常的重试与日志记录机制（包含日志等级与时间戳），确保程序的稳定性与健壮性。

4. 数据存储要求：
   - 将采集到的所有岗位信息以结构化格式存储，支持导出为 JSON 或 CSV 文件。
   - 每条导出的岗位数据需包含：唯一标识、岗位名称、简要描述、详细描述、优先条件、最低任职条件、所属团队、工作地点列表、发布时间、采集时间、原详情页 URL 字段。

5. 测试验证要求：
   - 完成开发后，测试程序的全流程跑通能力，验证是否能够完整采集并清洗出结构化的岗位信息。
   - 确认程序无内存泄漏、请求异常等问题，且导出的文件格式规范。