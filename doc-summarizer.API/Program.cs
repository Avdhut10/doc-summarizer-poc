
using UglyToad.PdfPig;

var builder = WebApplication.CreateBuilder(args);

// Add services for Swagger UI documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Enable Swagger UI (useful for development and quick testing). If you want to restrict
// Swagger to development only, put these calls back inside an IsDevelopment() check.
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

// Document upload and parsing endpoint
app.MapPost("/api/summarize", async (IFormFile file) =>
{
    if (file == null || file.Length == 0)
    {
        return Results.BadRequest(new { error = "No file uploaded or file is empty." });
    }

    string extractedText = string.Empty;
    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

    try
    {
        using var stream = file.OpenReadStream();

        if (extension == ".txt" || extension == ".md")
        {
            using var reader = new StreamReader(stream);
            extractedText = await reader.ReadToEndAsync();
        }
        else if (extension == ".pdf")
        {
            using var pdf = PdfDocument.Open(stream);
            var textBuilder = new System.Text.StringBuilder();
            foreach (var page in pdf.GetPages())
            {
                textBuilder.AppendLine(page.Text);
            }
            extractedText = textBuilder.ToString();
        }
        else
        {
            return Results.BadRequest(new { error = "Unsupported file format. Please upload .txt, .md, or .pdf files." });
        }

        if (string.IsNullOrWhiteSpace(extractedText))
        {
            return Results.BadRequest(new { error = "Could not extract any text from the uploaded file." });
        }

        // Return the parsed text metadata and preview
        return Results.Ok(new
        {
            FileName = file.FileName,
            CharacterCount = extractedText.Length,
            Message = "File successfully parsed and ready for AI integration!",
            Preview = extractedText.Substring(0, Math.Min(500, extractedText.Length))
        });
    }
    catch (Exception ex)
    {
        // Log the exception for diagnostics and rethrow to let the host return a 500 response
        app.Logger.LogError(ex, "An error occurred while parsing the uploaded file.");
        throw;
    }
})
.DisableAntiforgery(); // Required for file upload endpoints in Minimal APIs

app.Run();