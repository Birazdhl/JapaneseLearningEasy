/*
 * SETUP AFTER CLONING ---------------------------------------------------------
 * 1. Update-Database in Package Manager Console
 * 2. Run the app
 * 3. Go to /Update first to import your Excel file
 * ------------------------------------------------------------------------------
 * CLI equivalent: dotnet ef database update --project JapaneseLearningApp
 * Session state ships with ASP.NET Core 8 via the shared runtime (there is no
 * discrete Microsoft.AspNetCore.Session NuGet package that targets net8.0 yet).
 */

using JapaneseLearningApp.Configuration;
using JapaneseLearningApp.Data;
using JapaneseLearningApp.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

// EPPlus licensing (noncommercial / educational scenarios).
ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = ExcelUploadLimits.MultipartEnvelopeBytes;
    options.ValueLengthLimit = int.MaxValue;
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = ExcelUploadLimits.MultipartEnvelopeBytes;
});

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".JapaneseLearning.Session";
});

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});

builder.Services.AddHttpContextAccessor();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IWordTestSessionService, WordTestSessionService>();

builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.MaxAge = TimeSpan.FromDays(60);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseStatusCodePagesWithReExecute("/Home/Error");

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

await app.RunAsync();
