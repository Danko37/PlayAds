using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Пакует готовый WebGL-билд в один self-contained index-single.html: тело игры
/// (loader/framework/wasm/data) зашивается инлайном как base64 data-URI. Внешних
/// зависимостей нет.
///
/// Требования к настройкам билда:
///  - Compression Format = Disabled (нужны сырые .js/.wasm/.data, без .br/.gz);
///  - WebGL Template = Minimal (иначе index.html тянет TemplateData/* — html не будет один).
/// </summary>
public class WebGLSingleFileBuilder : IPostprocessBuildWithReport
{
    // Большой порядок — чтобы отработать после стандартной генерации билда.
    public int callbackOrder => 9999;

    public void OnPostprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.WebGL)
            return;

        try
        {
            InlineToSingleHtml(report.summary.outputPath);
        }
        catch (Exception e)
        {
            Debug.LogError($"[WebGLSingleFile] Не удалось собрать одиночный html: {e.Message}\n{e}");
        }
    }

    [MenuItem("Tools/WebGL/Build Single HTML")]
    public static void BuildAndInline()
    {
        var scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            Debug.LogError("[WebGLSingleFile] В Build Settings нет активных сцен.");
            return;
        }

        var outputFolder = Path.Combine(Directory.GetCurrentDirectory(), "Builds", "WebGLSingle");
        Directory.CreateDirectory(outputFolder);

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputFolder,
            target = BuildTarget.WebGL,
            targetGroup = BuildTargetGroup.WebGL,
            options = BuildOptions.None
        };

        // Инлайн сделает OnPostprocessBuild автоматически по завершении сборки.
        var report = BuildPipeline.BuildPlayer(options);

        if (report.summary.result == BuildResult.Succeeded)
            Debug.Log($"[WebGLSingleFile] Сборка успешна: {report.summary.outputPath}");
        else
            Debug.LogError($"[WebGLSingleFile] Сборка не удалась: {report.summary.result}");
    }

    private static void InlineToSingleHtml(string buildFolder)
    {
        var indexPath = Path.Combine(buildFolder, "index.html");
        if (!File.Exists(indexPath))
            throw new FileNotFoundException($"index.html не найден: {indexPath}");

        var buildDir = Path.Combine(buildFolder, "Build");
        if (!Directory.Exists(buildDir))
            throw new DirectoryNotFoundException($"Папка Build не найдена: {buildDir}");

        var loader = FindSingle(buildDir, "*.loader.js");
        var framework = FindSingle(buildDir, "*.framework.js");
        var wasm = FindSingle(buildDir, "*.wasm");
        var data = FindSingle(buildDir, "*.data");

        var html = File.ReadAllText(indexPath);

        // buildUrl больше не нужен — все ссылки станут абсолютными data-URI.
        html = Regex.Replace(html, "var\\s+buildUrl\\s*=\\s*\"[^\"]*\";", "var buildUrl = \"\";");

        html = ReplaceRef(html, loader, ToDataUri(loader, "application/javascript"));
        html = ReplaceRef(html, framework, ToDataUri(framework, "application/javascript"));
        html = ReplaceRef(html, wasm, ToDataUri(wasm, "application/wasm"));
        html = ReplaceRef(html, data, ToDataUri(data, "application/octet-stream"));

        var outPath = Path.Combine(buildFolder, "index-single.html");
        File.WriteAllText(outPath, html);

        var sizeMb = new FileInfo(outPath).Length / (1024f * 1024f);
        Debug.Log($"[WebGLSingleFile] Готово: {outPath} ({sizeMb:F1} МБ). " +
                  "Открой этот файл в браузере (лучше через локальный http-сервер).");
    }

    private static string FindSingle(string dir, string mask)
    {
        var files = Directory.GetFiles(dir, mask);
        if (files.Length == 0)
            throw new FileNotFoundException(
                $"Не найден файл по маске {mask} в {dir}. " +
                "Проверь, что Compression Format = Disabled (нужны сырые .js/.wasm/.data).");
        if (files.Length > 1)
            throw new Exception($"Найдено несколько файлов по маске {mask} в {dir}.");

        return files[0];
    }

    private static string ToDataUri(string filePath, string mime)
    {
        var b64 = Convert.ToBase64String(File.ReadAllBytes(filePath));
        return $"data:{mime};base64,{b64}";
    }

    /// <summary>
    /// Заменяет ссылку на файл билда в html на data-URI. Покрывает обе формы шаблона:
    /// concat (buildUrl + "/Game.data" -> литерал "/Game.data") и полный путь "Build/Game.data".
    /// </summary>
    private static string ReplaceRef(string html, string filePath, string dataUri)
    {
        var name = Path.GetFileName(filePath);

        var concatForm = "\"/" + name + "\"";   // buildUrl + "/Game.data"
        var fullForm = "\"Build/" + name + "\""; // "Build/Game.data"
        var replacement = "\"" + dataUri + "\"";

        if (html.Contains(concatForm))
            return html.Replace(concatForm, replacement);
        if (html.Contains(fullForm))
            return html.Replace(fullForm, replacement);

        throw new Exception(
            $"Ссылка на {name} не найдена в index.html — формат шаблона изменился, " +
            "инлайнер нужно поправить под него.");
    }
}
