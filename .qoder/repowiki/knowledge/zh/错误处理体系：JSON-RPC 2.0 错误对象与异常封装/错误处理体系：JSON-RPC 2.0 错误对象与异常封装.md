---
kind: error_handling
name: 错误处理体系：JSON-RPC 2.0 错误对象与异常封装
category: error_handling
scope:
    - '**'
source_files:
    - JsonRpc/JsonRpcError.cs
    - JsonRpc/JsonRpcResponse.cs
    - Protocol/RequestTracker.cs
    - Protocol/JsonRpcDispatcher.cs
    - Transport/IAgentTransport.cs
---

该 .NET 客户端库实现了基于 JSON-RPC 2.0 规范的错误处理机制，采用「协议层错误对象 + 应用层异常」的分层设计。

## 架构分层

**协议层错误模型**：`JsonRpcError` record（Code、Message、Data）严格遵循 JSON-RPC 2.0 规范，通过 `JsonRpcResponse.Error` 字段在响应中携带错误信息。

**异常封装层**：`RequestTracker.TryCompleteRequest` 在收到带 Error 的响应时，将协议错误转换为 `JsonRpcException`（包含 Code 和 Message），通过 TaskCompletionSource 抛出给调用方。

**传输层故障事件**：`IAgentTransport.TransportFaulted` 事件用于通知底层传输故障（如进程崩溃、管道断开），由上层订阅处理。

**处理器级错误返回**：当可选能力未实现（PermissionHandler/FileSystemHandler/TerminalHandler 为 null）时，直接返回带有 -32601 标准错误的 JsonRpcResponse。

## 关键约定

- 所有 JSON-RPC 请求失败统一通过 JsonRpcError 序列化到 wire 层，不抛异常
- 接收端将协议错误转换为 JsonRpcException 向上抛出，调用方可捕获 Code 判断错误类型
- 不可用能力返回 -32601（Method not found）标准错误码并附带描述性 Message
- 传输层故障通过事件而非异常传播，避免中断消息处理循环
- OnMessageReceivedAsync 中的 catch(Exception) 吞掉反序列化/处理异常，保证单条消息失败不影响后续处理