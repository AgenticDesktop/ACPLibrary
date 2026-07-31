---
kind: logging_system
name: 基于 Microsoft.Extensions.Logging 的依赖注入式日志系统
category: logging_system
scope:
    - '**'
source_files:
    - Client/AcpClient.cs
    - Infrastructure/ServiceCollectionExtensions.cs
---

该 .NET 客户端库使用 Microsoft.Extensions.Logging 作为统一的日志抽象框架，通过依赖注入在 AcpClient 中集成结构化日志记录能力。

**使用的框架与工具**
- 核心框架：Microsoft.Extensions.Logging（抽象接口）
- 空实现：Microsoft.Extensions.Logging.Abstractions.NullLogger<T>（未提供 ILogger 实例时的降级方案）
- 日志级别：遵循标准五级体系 — Debug、Information、Warning、Error、Critical

**关键文件与位置**
- `Client/AcpClient.cs`：唯一实际调用日志的地方，通过构造函数注入 `ILogger<AcpClient>`
- `Infrastructure/ServiceCollectionExtensions.cs`：DI 容器扩展方法，注册 ACP 相关服务（当前未显式注册 ILogger，由宿主应用提供）

**架构与设计决策**
1. **构造器注入模式**：AcpClient 通过构造函数接收可选的 `ILogger<AcpClient>` 参数，默认回退到 `NullLogger<AcpClient>.Instance`，确保无外部 DI 时仍可正常运行。
2. **结构化日志字段**：所有日志消息均使用占位符语法 `{SessionId}`、`{Cwd}`、`{ExitCode}`、`{AgentName}` 等，便于下游日志聚合系统提取结构化字段。
3. **日志级别策略**：
   - `LogDebug`：仅用于调试发送 prompt 的内部细节（第 211 行）
   - `LogInformation`：关键生命周期事件（初始化、会话创建/加载/取消、关闭）
   - `LogWarning`：异常或潜在问题（进程退出、协议版本不匹配）
4. **无全局配置**：库本身不配置日志输出目标（控制台、文件、ELK 等），完全交由宿主应用通过 DI 容器绑定具体实现。

**开发者应遵循的规则**
- 新增类如需日志，应通过构造函数注入 `ILogger<T>`，而非静态 Logger 单例
- 使用结构化占位符而非字符串拼接，保证日志可被机器解析
- 合理选择日志级别：业务正常流程用 Information，调试信息用 Debug，异常情况用 Warning/Error
- 不要在库内部配置具体的日志 sink，保持解耦