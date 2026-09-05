using CleanCode.Web;
using CleanCode.Web.Components;
using CleanCode.Data;
using AiFoundryLocal;
using CleanCode.AiOrchestration;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddOutputCache();
builder.Services.AddSingleton<AlbumCatalog>(_ =>
    new AlbumCatalog(Path.Combine(builder.Environment.ContentRootPath, "App_Data", "albums.db")));
builder.Services.AddSingleton<PhotoTagCatalog>(_ =>
    new PhotoTagCatalog(Path.Combine(builder.Environment.ContentRootPath, "App_Data", "photo-tags.db")));
builder.Services.AddSingleton(_ =>
{
    var accountUri = builder.Configuration["AzureStorage:AccountUri"];
    var accountName = builder.Configuration["AzureStorage:AccountName"];
    if (string.IsNullOrWhiteSpace(accountName) && Uri.TryCreate(accountUri, UriKind.Absolute, out var parsedUri))
        accountName = parsedUri.Host.Split('.')[0];
    if (string.IsNullOrWhiteSpace(accountName))
        throw new InvalidOperationException("Configure AzureStorage:AccountName or AzureStorage:AccountUri.");

    return new AzurePhotoStorage(new AzurePhotoStorageOptions(
        accountName,
        builder.Configuration["AzureStorage:ContainerName"] ?? "photos",
        accountUri,
        builder.Configuration["photostorageaccountkey"]));
});

builder.Services.AddSingleton(_ =>
{
    var configuredPath = builder.Configuration["VisionModel:ModelDirectory"];
    var modelDirectory = Path.IsPathRooted(configuredPath ?? string.Empty)
        ? configuredPath!
        : Path.GetFullPath(Path.Combine(
            builder.Environment.ContentRootPath,
            configuredPath ?? "..\\..\\models\\onnx_phi3_vision\\cpu_and_mobile\\cpu-int4-rtn-block-32-acc-level-4"));
    return new Phi3VisionModel(new Phi3VisionOptions(
        modelDirectory,
        builder.Configuration.GetValue("VisionModel:MaxNewTokens", 128),
        builder.Configuration.GetValue("VisionModel:ContextLength", 4096)));
});
builder.Services.AddSingleton<FileIndexer>(_ => new FileIndexer(
    builder.Configuration["VisionModel:LocalPhotoDirectory"] ?? Path.Combine(builder.Environment.ContentRootPath, "photos")));
builder.Services.AddSingleton<PhotoVisionOrchestrator>();
builder.Services.AddSingleton<PhotoIndexQueue>();
builder.Services.AddHostedService<PhotoIndexBackgroundService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.UseOutputCache();

app.MapStaticAssets();

app.MapGet("/albums/{albumId}/photos/{photoId}/content", async (
    string albumId,
    string photoId,
    AlbumCatalog catalog,
    AzurePhotoStorage storage,
    CancellationToken ct) =>
{
    var photo = catalog.GetPhoto(albumId, photoId);
    if (photo is null)
        return Results.NotFound();

    var download = await storage.DownloadAsync(photo.BlobName, ct);
    return Results.Stream(download.Content, download.ContentType, enableRangeProcessing: true);
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();
