using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NexTraceOne.Licensing.Infrastructure.Persistence;

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<LicensingDbContext>();
    var environment = app.Environment;
    #if DEBUG
        dbContext.Database.Migrate();
    #else
        if (string.Equals(Environment.GetEnvironmentVariable("NEXTRACE_AUTO_MIGRATE"), "true", StringComparison.OrdinalIgnoreCase))
        {
            dbContext.Database.Migrate();
        }
    #endif
}