using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Order.Domain.Interfaces;
using Order.Infrastructure.Data;
using Order.Infrastructure.Repositories;
using Shared.Infrastructure.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<OrderDbContext>(tags: ["ready"]);

var app = builder.Build();

if (builder.Configuration.GetValue("Database:ApplyMigrationsOnStartup", true))
    await MigrateWithRetryAsync(app);

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

// Liveness: process is up. Readiness: database is reachable.
app.MapHealthChecks("/healthz", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/readyz", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

app.Run();

// PostgreSQL may still be starting (compose, pod restarts); retry transient failures before giving up.
static async Task MigrateWithRetryAsync(WebApplication app)
{
    var maxAttempts = app.Configuration.GetValue("Database:MigrationMaxAttempts", 10);
    var delay = TimeSpan.FromSeconds(app.Configuration.GetValue("Database:MigrationRetryDelaySeconds", 3));
    var stopping = app.Lifetime.ApplicationStopping;

    for (var attempt = 1; ; attempt++)
    {
        try
        {
            using var scope = app.Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<OrderDbContext>().Database.MigrateAsync(stopping);
            return;
        }
        catch (NpgsqlException ex) when (ex.IsTransient && attempt < maxAttempts)
        {
            app.Logger.LogWarning(ex, "Database not ready (attempt {Attempt}/{MaxAttempts}); retrying in {Delay}",
                attempt, maxAttempts, delay);
            await Task.Delay(delay, stopping);
        }
    }
}

public partial class Program;
