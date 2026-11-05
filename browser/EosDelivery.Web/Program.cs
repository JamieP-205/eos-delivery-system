using System.Globalization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using EosDelivery.Web;

// Same culture as the Windows app, so dates and money format identically.
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("en-GB");
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("en-GB");

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
await builder.Build().RunAsync();
