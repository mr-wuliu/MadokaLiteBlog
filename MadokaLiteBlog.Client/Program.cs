using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MadokaLiteBlog.Client;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
builder.Services.AddScoped<AuthorizationMessageHandler>();
builder.Services.AddScoped<PostListState>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped(sp => 
{
    var config = new ConfigurationBuilder()
        .SetBasePath(Environment.CurrentDirectory)
        .AddJsonFile("wwwroot/appsettings.json", optional: false)
        .Build();
    return config;
});

builder.Services.AddScoped(sp => 
{
    var config = sp.GetRequiredService<IConfiguration>();
    var handler = sp.GetRequiredService<AuthorizationMessageHandler>();
    return new HttpClient(handler)
    {
        BaseAddress = new Uri(config["Api:BaseUrl"] ?? "http://localhost:5254/")
    };
});

await builder.Build().RunAsync();
