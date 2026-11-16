using System.Text.RegularExpressions;

namespace SIGA.E2ETests.Infrastructure;

public static class FrontendAssetScanner
{
    private static string? _frontendRoot;

    public static string GetFrontendRoot()
    {
        if (_frontendRoot != null && Directory.Exists(_frontendRoot))
            return _frontendRoot;

        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(current))
        {
            var candidate = Path.Combine(current, "frontend");
            if (Directory.Exists(candidate) && File.Exists(Path.Combine(candidate, "index.html")))
            {
                _frontendRoot = candidate;
                return _frontendRoot;
            }

            var slnCandidate = Directory.GetFiles(current, "SIGA.sln");
            if (slnCandidate.Length > 0)
            {
                var fPath = Path.Combine(current, "frontend");
                if (Directory.Exists(fPath))
                {
                    _frontendRoot = fPath;
                    return _frontendRoot;
                }
            }

            var parent = Directory.GetParent(current);
            if (parent == null) break;
            current = parent.FullName;
        }

        var cwdCandidate = Path.Combine(Directory.GetCurrentDirectory(), "frontend");
        if (Directory.Exists(cwdCandidate))
        {
            _frontendRoot = cwdCandidate;
            return _frontendRoot;
        }

        throw new DirectoryNotFoundException("No se encontró el directorio 'frontend' desde la ubicación de ejecución del test.");
    }

    public static string ReadFrontendFile(string relativePath)
    {
        var root = GetFrontendRoot();
        var fullPath = Path.Combine(root, relativePath);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"Archivo frontend no encontrado: {fullPath}");
        return File.ReadAllText(fullPath);
    }

    public static bool FileExists(string relativePath)
    {
        try
        {
            var root = GetFrontendRoot();
            return File.Exists(Path.Combine(root, relativePath));
        }
        catch
        {
            return false;
        }
    }

    public static string ReadCommercialMovementsView()
    {
        if (FileExists("js/views/movimientos-comerciales-view.js"))
            return ReadFrontendFile("js/views/movimientos-comerciales-view.js");

        return ReadFrontendFile("js/views/ventas-view.js");
    }

    public static bool IsCommercialMovementsUnifiedViewImplemented()
    {
        return FileExists("js/views/movimientos-comerciales-view.js");
    }

    public static bool IsPreciosReferenciaFullViewImplemented()
    {
        if (!FileExists("js/views/precios-referencia-view.js")) return false;
        var content = ReadFrontendFile("js/views/precios-referencia-view.js");
        return content.Contains("select-temporada") || content.Contains("btn-guardar-header") || content.Contains("btn-guardar-precios");
    }

    public static IEnumerable<string> GetAllFrontendFiles(string searchPattern = "*.*", SearchOption searchOption = SearchOption.AllDirectories)
    {
        var root = GetFrontendRoot();
        return Directory.GetFiles(root, searchPattern, searchOption);
    }
}
