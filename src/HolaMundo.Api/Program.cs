using HolaMundo.Api;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// La versión no está en el código: llega de afuera (variable de entorno APP_VERSION).
// Así la misma imagen sirve para cualquier entorno y cada uno le dice qué versión es.
// Se lee en cada request para que los tests puedan inyectarla por configuración.
app.MapGet("/", (IConfiguration config) =>
    new Saludo(Saludo.MensajeActual, config["APP_VERSION"] ?? "desarrollo"));

app.Run();

// Expone Program para que los tests levanten la API en memoria (WebApplicationFactory).
public partial class Program { }
