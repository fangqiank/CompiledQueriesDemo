# CompiledQueriesDemo

EF Core 编译查询性能基准测试 | EF Core Compiled Queries Performance Benchmark

![Architecture](CompiledQueriesDemo-architecture.svg)

## Tech Stack / 技术栈

| Component | Version |
|-----------|---------|
| .NET | 10.0 |
| EF Core | 10.0.9 |
| Npgsql.EntityFrameworkCore.PostgreSQL | 10.0.2 |
| BenchmarkDotNet | 0.15.8 |
| PostgreSQL | 16 (Docker) |

## Project Structure / 项目结构

| Module | File | Responsibility |
|--------|------|---------------|
| Entry Point | `Program.cs` | 快速验证 + 启动 BenchmarkDotNet |
| Shared | `SeedData.cs` | 连接字符串、种子数据、4 个编译查询定义 |
| Benchmarks | `Benchmarks/QueryBenchmarks.cs` | 8 个基准测试（4 组 Normal vs Compiled） |
| Data Access | `Data/AppDbContext.cs` | EF Core DbContext，`customers` 表，Name+Age 复合索引 |
| Model | `Models/Customer.cs` | Customer 实体（Id, Name, Age） |
| Infrastructure | `docker-compose.yml` | PostgreSQL 16 容器（端口 5433:5432） |

## Quick Start / 快速开始

```bash
# 1. 启动 PostgreSQL
docker compose up -d

# 2. 运行基准测试（必须 Release 模式）
dotnet run -c Release --project CompiledQueriesDemo

# 3. 或仅构建验证
dotnet build
```

## Benchmark Results / 基准测试结果

> BenchmarkDotNet v0.15.8, AMD Ryzen 7 6800H, .NET 10.0.9, PostgreSQL 16 (Docker)
> MemoryDiagnoser + RankColumn + P95 | 4 groups x 2 methods = 8 benchmarks
> WarmupCount=10, IterationCount=50

| Method | Mean | Error | StdDev | P95 | Allocated |
|--------|------|-------|--------|-----|-----------|
| **Group 1: By ID (Tracking)** | | | | | |
| 1. Normal | 531.9 us | 5.69 us | 10.97 us | 547.9 us | 40.46 KB |
| 2. Compiled | 492.0 us | 6.40 us | 12.48 us | 511.4 us | 35.24 KB |
| **Group 2: By ID (NoTracking)** | | | | | |
| 3. Normal | 523.2 us | 6.10 us | 12.33 us | 545.3 us | 40.53 KB |
| 4. Compiled | 497.4 us | 4.30 us | 8.59 us | 514.5 us | 34.24 KB |
| **Group 3: By Name+Age (Tracking)** | | | | | |
| 5. Normal | 537.1 us | 14.46 us | 28.20 us | 598.9 us | 41.58 KB |
| 6. Compiled | 515.5 us | 11.73 us | 23.69 us | 556.2 us | 35.54 KB |
| **Group 4: By Name+Age (NoTracking)** | | | | | |
| 7. Normal | 541.0 us | 13.42 us | 26.49 us | 591.2 us | 41.94 KB |
| 8. Compiled | 534.5 us | 13.68 us | 27.00 us | 585.5 us | 34.54 KB |

### Key Findings / 关键发现

**速度方面：**
- **Compiled Query 在 ID 查询上提速最大（5-8%）**，简单查询中表达式解析开销占比更高
- **Name+Age 场景优势较小（1-4%）**，数据库 I/O 占主导，编译优化的边际效益被稀释
- **NoTracking + Compiled 组合下速度优势最不明显**，NoTracking 本身已省去 change tracking 开销

**内存方面（核心优势）：**
- **内存分配稳定减少 13-18%**（~34-35 KB vs ~40-42 KB），这是编译查询最显著的收益
- 所有 Compiled 查询的 Gen0 GC 次数从 4.88 降至 3.91（减少约 20%）

## How EF.CompileQuery Works

`EF.CompileQuery` 将 LINQ 表达式树预编译为缓存的委托，跳过每次调用时的表达式解析开销：

```csharp
// 定义编译查询（SeedData.cs 中集中管理）
public static readonly Func<AppDbContext, int, Customer?> GetByIdCompiled =
    EF.CompileQuery((AppDbContext ctx, int id) =>
        ctx.Customers.FirstOrDefault(c => c.Id == id));

// 使用 - 无需每次编译表达式
var customer = SeedData.GetByIdCompiled(context, 42);
```

## License

MIT
