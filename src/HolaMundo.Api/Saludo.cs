namespace HolaMundo.Api;

// Lo que devuelve GET /. Se serializa como { "mensaje": "...", "version": "..." }.
public record Saludo(string Mensaje, string Version)
{
    // Acto 1 de la demo: cambiar este texto, hacer push y verlo en staging.
    public const string MensajeActual = "Hola mundo desde Jenkins, disparado por webhook";
}
