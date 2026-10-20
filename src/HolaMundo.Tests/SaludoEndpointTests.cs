using System.Net;
using System.Net.Http.Json;
using HolaMundo.Api;
using Microsoft.AspNetCore.Mvc.Testing;

namespace HolaMundo.Tests;

// Levanta la API real en memoria y le hace requests HTTP, sin abrir puertos.
public class SaludoEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task GetRaiz_Responde200()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetRaiz_DevuelveMensaje_YVersionDevPorDefecto()
    {
        var client = factory.CreateClient();

        var saludo = await client.GetFromJsonAsync<Saludo>("/");

        // No se compara el texto exacto: cambiar el saludo (acto 1 de la demo) no debe romper el build.
        Assert.False(string.IsNullOrWhiteSpace(saludo!.Mensaje));
        Assert.Equal("dev", saludo.Version);
    }

    [Fact]
    public async Task GetRaiz_UsaLaVersionQueLlegaDeAfuera()
    {
        // Simula lo que hará el deploy: pasarle APP_VERSION a la app.
        var client = factory
            .WithWebHostBuilder(b => b.UseSetting("APP_VERSION", "42"))
            .CreateClient();

        var saludo = await client.GetFromJsonAsync<Saludo>("/");

        Assert.Equal("42", saludo!.Version);
    }
}
