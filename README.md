# AppleJobGather (苹果官网岗位信息采集程序)

`AppleJobGather` 是一个基于 .NET 开发的轻量级控制台应用程序，用于高效、稳定地采集苹果公司官网招聘页面（[jobs.apple.com](https://jobs.apple.com/)）的岗位信息。

与传统的基于脆弱 HTML DOM 结构解析的爬虫不同，本项目通过深度分析苹果官网的 React Router / Remix 架构，采用直接提取页面嵌入的 **Hydration JSON 数据（`window.__staticRouterHydrationData`）** 的先进技术方案，提供了极高的采集稳定性与数据准确性。

## 功能特性

- **现代且稳健的解析方案**：定位并解密页面初始 HTML 中的 Hydration 状态，直接反序列化获取结构化的原始岗位信息，规避前端 JavaScript 异步渲染延时及易变的 CSS/XPath DOM 节点所带来的采集故障。
- **丰富的字段提取**：
  - 岗位唯一标识 (`positionId`)
  - 岗位名称 (`postingTitle`)
  - 简要描述 (`jobSummary`)
  - 详细描述内容 (`description`)
  - 优先条件与任职要求 (`preferredQualifications` & `minimumQualifications`)
  - 所属团队与部门 (`teamNames`)
  - 工作地点列表 (`locations`)
  - 发布时间与采集时间
  - 原详情页 URL
- **多格式导出支持**：
  - **JSON**：输出排版整齐的结构化 JSON。
  - **CSV**：遵循 RFC 4180 标准安全转义，并且使用 **UTF-8 BOM 编码** 保存，Microsoft Excel 可直接双击打开中文排版，无乱码。
- **极度友好的命令行选项**：支持配置过滤词、国家/语言区域、最大抓取页数、限制岗位数、自定义请求延迟等。
- **企业级健壮性**：内置指数补偿的自动重试机制（默认重试 3 次，间隔递增），可自适应网络波动与临时请求超时；严格遵守 robots 协议与每两次请求间不低于 2 秒的安全间隔。

## 环境要求

- .NET 8.0 SDK 或更高版本 (.NET 10.0+ 推荐)

## 快速上手

### 1. 克隆并编译项目

在项目根目录下，使用 dotnet 命令行工具进行还原与编译：

```bash
dotnet build
```

### 2. 运行程序

#### 获取帮助信息

查看所有支持的命令行参数：

```bash
dotnet run -- --help
```

#### 典型采集示例

*   **极简测试（仅爬取第一页前 3 个岗位，输出 JSON）**：
    ```bash
    dotnet run -- --max-pages 1 --max-jobs 3 --output test_jobs.json
    ```

*   **采集中国区（zh-cn）的“软件”相关岗位，输出到 CSV 文件中**：
    ```bash
    dotnet run -- --locale zh-cn --query "软件" --output jobs_china.csv
    ```

*   **全量采集全球英语区（en-us）岗位，设定每次请求安全延迟为 3000ms，输出 JSON**：
    ```bash
    dotnet run -- --locale en-us --delay 3000 --output all_jobs.json
    ```

## 命令行参数说明

| 参数选项 | 类型 | 默认值 | 说明 |
| :--- | :--- | :--- | :--- |
| `--locale` | `string` | `en-us` | 国家与语言区域代码（例如 `en-us`, `zh-cn` 等） |
| `--query` | `string` | *(空)* | 搜索岗位名称或关键词过滤 |
| `--max-pages`| `int` | *(无限制)* | 扫描搜索列表的最大分页数 |
| `--max-jobs` | `int` | *(无限制)* | 采集并导出的最大岗位数量 |
| `--delay` | `int` | `2000` | 两次网络请求之间的休眠间隔（毫秒），不建议设置低于 2000 |
| `--output` | `string` | `jobs.json` | 导出文件路径，支持以 `.json` 或 `.csv` 结尾 |
| `--help, -h` | `switch` | | 显示说明文档与示例 |

## 项目目录结构

```text
├── AppleJobGather.csproj   # 项目工程文件
├── Models.cs               # 预加载 JSON 反序列化数据模型与导出模型
├── AppleJobCrawler.cs      # 网络请求、JSON 提取器、遍历和抓取核心逻辑
├── Program.cs              # 程序入口、参数解析和文件导出调度
├── spec/
│   └── sys-requirement.md  # 功能需求规范文件
└── LICENSE                 # 许可证
```

## 技术方案优势分析

通过比对，本项目与传统解析方案的数据差异如下：

| 特性维度 | 传统 DOM 解析 (XPath/CSS) | 本项目 (Hydration JSON 解析) |
| :--- | :--- | :--- |
| **防失效能力** | 非常脆弱，前端 UI 改版（修改 class、嵌套结构）即会报错 | 极强，只要苹果后端返回的 React Router 预加载状态结构不变即可用 |
| **异步加载处理** | 必须集成 Selenium/Playwright 渲染浏览器，极为臃肿 | 直接利用 HttpClient 读取请求响应中的渲染状态，极轻量 |
| **数据洁净度** | 需过滤大量的 HTML 标签、不可见字符，极易出现文本拼接错误 | 苹果后端已完全格式化好的键值对数据，直读即可 |
| **字段完整度** | 很难将基本任职要求和优先条件从复杂正文中完美分离 | 字段天生独立（`minimumQualifications` 与 `preferredQualifications`） |
