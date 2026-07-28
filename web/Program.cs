/*
 * Enveloppe web (ASP.NET Core) autour du convertisseur Unicode-32 <-> UTF-8.
 * Réutilise la classe UnicodeConverter du projet console d'origine, exposée
 * via une petite API JSON et une page web servie depuis wwwroot.
 */

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Render (et la plupart des hébergeurs) fournissent le port via la variable PORT.
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Urls.Clear();
app.Urls.Add($"http://0.0.0.0:{port}");

app.UseDefaultFiles();   // sert wwwroot/index.html à la racine
app.UseStaticFiles();

// --- Unicode-32 -> UTF-8 -------------------------------------------------
// Entrée : { "value": "20AC" } (code point en hexadécimal)
// Sortie : { "hex": "E2 82 AC", "bytes": ["0xE2","0x82","0xAC"] }
app.MapPost("/api/u32-to-utf8", (ConvU32Request req) =>
{
    try
    {
        var texte = (req.Value ?? "").Trim().Replace("0x", "", StringComparison.OrdinalIgnoreCase);
        if (texte.Length == 0)
            return Results.BadRequest(new ErrorResponse("Entre une valeur Unicode-32 en hexadécimal (ex : 20AC)."));

        uint unicode32 = Convert.ToUInt32(texte, 16);
        byte[] utf8 = UnicodeConverter.Unicode32ToUtf8(unicode32);

        var bytes = utf8.Select(b => $"0x{b:X2}").ToArray();
        return Results.Ok(new
        {
            input = $"0x{unicode32:X4} ({unicode32} en décimal)",
            hex = string.Join(" ", utf8.Select(b => b.ToString("X2"))),
            bytes = bytes
        });
    }
    catch (FormatException)
    {
        return Results.BadRequest(new ErrorResponse("Format hexadécimal invalide."));
    }
    catch (OverflowException)
    {
        return Results.BadRequest(new ErrorResponse("Valeur trop grande (max 0x10FFFF)."));
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new ErrorResponse(ex.Message));
    }
});

// --- UTF-8 -> Unicode-32 -------------------------------------------------
// Entrée : { "bytes": "E2 82 AC" } (octets en hexadécimal séparés par des espaces)
// Sortie : { "hex": "0x20AC", "decimal": 8364 }
app.MapPost("/api/utf8-to-u32", (ConvUtf8Request req) =>
{
    try
    {
        var brut = (req.Bytes ?? "").Trim();
        if (brut.Length == 0)
            return Results.BadRequest(new ErrorResponse("Entre les octets UTF-8 en hexadécimal (ex : E2 82 AC)."));

        string[] morceaux = brut.Split(new[] { ' ', ',', ';', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        byte[] utf8 = new byte[morceaux.Length];
        for (int i = 0; i < morceaux.Length; i++)
            utf8[i] = Convert.ToByte(morceaux[i].Replace("0x", "", StringComparison.OrdinalIgnoreCase), 16);

        int position = 0;
        uint unicode32 = UnicodeConverter.Utf8ToUnicode32(utf8, ref position);

        // Récupère le caractère correspondant, si imprimable
        string caractere = "";
        try { caractere = char.ConvertFromUtf32((int)unicode32); } catch { caractere = ""; }

        return Results.Ok(new
        {
            input = string.Join(" ", utf8.Select(b => $"0x{b:X2}")),
            hex = $"0x{unicode32:X4}",
            @decimal = unicode32,
            character = caractere
        });
    }
    catch (FormatException)
    {
        return Results.BadRequest(new ErrorResponse("Format hexadécimal invalide."));
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new ErrorResponse(ex.Message));
    }
});

app.Run();

// Modèles de requête/réponse
record ConvU32Request(string? Value);
record ConvUtf8Request(string? Bytes);
record ErrorResponse(string error);
