using Microsoft.EntityFrameworkCore;
using ShareForFuture.Data;

var builder = WebApplication.CreateBuilder(args);
var optionsBuilder = new DbContextOptionsBuilder<S4fDbContext>();
var context = new S4fDbContext(optionsBuilder.Options);

    // Add services to the container.
    builder.Services.AddControllers();

// Read more about EFCore-related developer exception page at
// https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.databasedeveloperpageexceptionfilterserviceextensions.adddatabasedeveloperpageexceptionfilter
builder.Services.AddDbContext<S4fDbContext>(
    options => options.UseSqlServer(builder.Configuration["ConnectionStrings:DefaultConnection"]))
    .AddDatabaseDeveloperPageExceptionFilter();

// Read more about health checks at
// https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks#entity-framework-core-dbcontext-probe
builder.Services.AddHealthChecks()
    .AddDbContextCheck<S4fDbContext>("UserGroupsAvailable", 
        customTestQuery: async (context, _) => await context.UserGroups.CountAsync() == 4);

builder.Services.AddScoped<DemoDataGenerator>();

var app = builder.Build();


// Configure the HTTP request pipeline.
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.UseHealthChecks("/health");

// Endpoint for Search requests
app.MapPost("/searchOfferings", ( string searchString) =>
{
    return Results.Ok(SearchOfferings(searchString));
});

IEnumerable<Offering> SearchOfferings(string searchString)
{
    var result  = new List<Offering>();
    searchString.Remove(' ');
    searchString.Split(',').Select(o => 
        result.Union(context.Offerings.Where(offering => offering.Title.Contains(searchString))
        .Where(offering => offering.Description.Contains(searchString))
        .Where(offering => offering.Tags.Where(tag => tag.Tag.Contains(searchString)).First() != null).ToList()));
    return result;
}

app.MapPost("/fill", async (DemoDataGenerator generator) =>
{
    await generator.ClearAll();
    await generator.Generate();
    return Results.Ok();
});

app.Run();
