using System.Globalization;
using System.Text.Json;
using Blazored.LocalStorage;
using Finanzas;
using Finanzas.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

CultureInfo.DefaultThreadCurrentCulture = Formato.Mexico;
CultureInfo.DefaultThreadCurrentUICulture = Formato.Mexico;
CultureInfo.CurrentCulture = Formato.Mexico;
CultureInfo.CurrentUICulture = Formato.Mexico;

var apiUrl = builder.Configuration["ApiSettings:BaseUrl"]
    ?? throw new Exception("Falta ApiSettings:BaseUrl en wwwroot/appsettings.json");

builder.Services.AddBlazoredLocalStorage();
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthProvider>();

builder.Services.AddScoped<AuthHttpHandler>();
builder.Services.AddHttpClient("Api", client => client.BaseAddress = new Uri(apiUrl))
    .AddHttpMessageHandler<AuthHttpHandler>();

builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("Api"));
builder.Services.AddSingleton(new JsonSerializerOptions(JsonSerializerDefaults.Web));

builder.Services.AddScoped<UsuarioService>();
builder.Services.AddScoped<CuentaService>();
builder.Services.AddScoped<CategoriaService>();
builder.Services.AddScoped<PersonaService>();
builder.Services.AddScoped<OperacionService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<MsiService>();
builder.Services.AddScoped<AnalisisService>();
builder.Services.AddScoped<RecurrenteService>();
builder.Services.AddScoped<PresupuestoService>();

await builder.Build().RunAsync();
