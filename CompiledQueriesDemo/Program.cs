using BenchmarkDotNet.Running;
using CompiledQueriesDemo.Benchmarks;
using CompiledQueriesDemo.Data;
using CompiledQueriesDemo.Models;
using Microsoft.EntityFrameworkCore;

Console.WriteLine("=== EF Core Compiled Queries Demo (.NET 10 + PostgreSQL) ===");
Console.WriteLine();


// 先运行一次快速验证
Console.WriteLine("Running quick validation...");
RunQuickValidation();

// 运行完整基准测试
Console.WriteLine("Running benchmarks...");
BenchmarkRunner.Run<QueryBenchmarks>();

static void RunQuickValidation()
{
    var options = new DbContextOptionsBuilder<AppDbContext>()
        .UseNpgsql("Host=localhost;Port=5433;Database=compiled_queries_demo;Username=postgres;Password=postgres")
        .Options;

    using var context = new AppDbContext(options);
    context.Database.EnsureCreated();

    if (!context.Customers.Any())
    {
        Console.WriteLine("Seeding database...");
        var customers = Enumerable.Range(1, 10000)
            .Select(i => new Customer { Name = $"Customer_{i}", Age = 20 + (i % 50) });
        context.Customers.AddRange(customers);
        context.SaveChanges();
        Console.WriteLine($"Inserted {customers.Count()} customers");
    }

    // 编译查询定义
    var compiledById = EF.CompileQuery((AppDbContext ctx, int id) =>
        ctx.Customers.FirstOrDefault(c => c.Id == id));

    var compiledByNameAndAge = EF.CompileQuery((AppDbContext ctx, string name, int age) =>
        ctx.Customers.FirstOrDefault(c => c.Name == name && c.Age == age));

    // 验证查询结果一致性
    var testId = context.Customers.Skip(5000).First().Id;

    var normal = context.Customers.FirstOrDefault(c => c.Id == testId);
    var compiled = compiledById(context, testId);

    Console.WriteLine($"Normal query result: ID={normal?.Id}, Name={normal?.Name}");
    Console.WriteLine($"Compiled query result: ID={compiled?.Id}, Name={compiled?.Name}");
    Console.WriteLine($"Results match: {normal?.Id == compiled?.Id}");
}

Console.WriteLine();