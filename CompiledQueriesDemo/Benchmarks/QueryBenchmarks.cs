using BenchmarkDotNet.Attributes;
using CompiledQueriesDemo.Data;
using CompiledQueriesDemo.Models;
using Microsoft.EntityFrameworkCore;

namespace CompiledQueriesDemo.Benchmarks
{
    public class QueryBenchmarks
    {
        private const string ConnectionString = "Host=localhost;Port=5433;Database=compiled_queries_demo;Username=postgres;Password=postgres";
        
        private DbContextOptions<AppDbContext> _options = null!;
        private int _testId;
        private string _testName = null!;
        private int _testAge;

        // ========== Compiled Queries 定义 ==========

        // 示例1：按ID查询（带Tracking）
        private static readonly Func<AppDbContext, int, Customer?> GetCustomerByIdCompiled = 
            EF.CompileQuery((AppDbContext context, int id) =>
                context.Customers.FirstOrDefault(c => c.Id == id));

        // 示例2：按ID查询（NoTracking）
        private static readonly Func<AppDbContext, int, Customer?> GetCustomerByIdNoTrackingCompiled =
            EF.CompileQuery((AppDbContext context, int id) =>
                context.Customers.AsNoTracking().FirstOrDefault(c => c.Id == id));

        // 示例3：按Name + Age查询
        private static readonly Func<AppDbContext, string, int, Customer?> GetCustomerByNameAndAgeCompiled =
            EF.CompileQuery((AppDbContext context, string name, int age) =>
                context.Customers.FirstOrDefault(c => c.Name == name && c.Age == age));


        [GlobalSetup]
        public void GlobalSetup()
        {
            _options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(ConnectionString)
                .Options;
            
            // 确保数据库和数据存在 
            using var context = new AppDbContext(_options);
            context.Database.EnsureCreated();

            if(!context.Customers.Any())
            {
                var customer = Enumerable.Range(1, 10000)
                    .Select(i => new Customer
                    {
                        Name = $"Customer {i}",
                        Age = 20 + (i % 50)
                    });

                context.Customers.AddRange(customer);
                context.SaveChanges();
            }

            _testId = context.Customers.OrderByDescending(c => c.Id).First().Id;

            var randomCustomer = context.Customers.Skip(5000).First();
            _testName = randomCustomer.Name;
            _testAge = randomCustomer.Age;
        }

        // ========== 示例1：按ID查询（带Tracking） ==========
        
        [Benchmark(Baseline = true, Description = "1. Normal Query (ID)")]
        public Customer? NormalQueryById()
        {
            using var context = new AppDbContext(_options);
            return context.Customers.FirstOrDefault(c => c.Id == _testId);
        }

        [Benchmark(Description = "2. Compiled Query (ID)")]
        public Customer? CompiledQueryById()
        {
            using var context = new AppDbContext(_options);
            return GetCustomerByIdCompiled(context, _testId);
        }

        // ========== 示例2：按ID查询（NoTracking） ==========
       
        [Benchmark(Description = "3. Normal Query (ID, NoTracking)")]
        public Customer? NormalQueryByIdNoTracking()
        {
            using var context = new AppDbContext(_options);
            return context.Customers.AsNoTracking().FirstOrDefault(c => c.Id == _testId);
        }

        [Benchmark(Description = "4. Compiled Query (ID, NoTracking)")]
        public Customer? CompiledQueryByIdNoTracking()
        {
            using var context = new AppDbContext(_options);
            return GetCustomerByIdNoTrackingCompiled(context, _testId);
        }

        // ========== 示例3：按Name + Age查询 ==========
        
        [Benchmark(Description = "5. Normal Query (Name + Age)")]
        public Customer? NormalQueryByNameAndAge()
        {
            using var context = new AppDbContext(_options);
            return context.Customers.FirstOrDefault(c => c.Name == _testName && c.Age == _testAge);
        }

        [Benchmark(Description = "6. Compiled Query (Name + Age)")]
        public Customer? CompiledQueryByNameAndAge()
        {
            using var context = new AppDbContext(_options);
            return GetCustomerByNameAndAgeCompiled(context, _testName, _testAge);
        }
    }
}
