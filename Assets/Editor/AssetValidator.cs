
using UnityEngine;
using UnityEditor;
using System.Text;
using System.IO;

public static class AssetValidator
{
    const float MinHeight = 0.3f;
    const float MaxHeight = 4f;
    const float PivotTolerance = 0.05f;

    [MenuItem("Tools/Asset Pipeline/Validate Selected Models")]
    static void ValidateSelection()
    {
        if (Selection.gameObjects.Length == 0)
        {
            Debug.LogWarning(
                "Выделите в иерархии хотя бы один " +
                "объект для проверки.");
            return;
        }

        var report = new StringBuilder();
        report.AppendLine("# Отчёт валидации ассетов\n");
        int totalWarnings = 0;

        foreach (var obj in Selection.gameObjects)
        {
            report.AppendLine($"## {obj.name}");
            totalWarnings += CheckBoundsAndPivot(obj, report);
            totalWarnings += CheckMaterials(obj, report);
            report.AppendLine();
        }

        report.AppendLine(
            $"**Итого предупреждений: {totalWarnings}**");

        string path = "Assets/AssetValidationReport.md";
        File.WriteAllText(path, report.ToString());
        AssetDatabase.Refresh();

        Debug.Log(report.ToString());
        Debug.Log(totalWarnings == 0
            ? "Валидация пройдена без замечаний."
            : $"Найдено предупреждений: {totalWarnings}. " +
              $"Отчёт сохранён: {path}");
    }

    static int CheckBoundsAndPivot(
        GameObject obj, StringBuilder report)
    {
        var renderers =
            obj.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
        {
            report.AppendLine(
                "- (!) Не найдено ни одного Renderer.");
            return 1;
        }

        Bounds combined = renderers[0].bounds;

        foreach (var r in renderers)
            combined.Encapsulate(r.bounds);

        int warnings = 0;
        float height = combined.size.y;

        if (height < MinHeight || height > MaxHeight)
        {
            report.AppendLine(
                $"- (!) Подозрительная высота: {height:F2} м " +
                $"(ожидается {MinHeight}-{MaxHeight} м).");
            warnings++;
        }
        else
        {
            report.AppendLine(
                $"- (OK) Высота в разумных пределах: " +
                $"{height:F2} м.");
        }

        float pivotOffset =
            combined.min.y - obj.transform.position.y;

        if (Mathf.Abs(pivotOffset) > PivotTolerance)
        {
            report.AppendLine(
                $"- (!) Пивот не у основания: " +
                $"смещение {pivotOffset:F2} м " +
                "относительно нижней точки модели.");
            warnings++;
        }
        else
        {
            report.AppendLine(
                "- (OK) Пивот у основания модели.");
        }

        return warnings;
    }

    static int CheckMaterials(
        GameObject obj, StringBuilder report)
    {
        int warnings = 0;
        var renderers =
            obj.GetComponentsInChildren<Renderer>();

        foreach (var r in renderers)
        {
            foreach (var mat in r.sharedMaterials)
            {
                if (mat == null) continue;

                if (mat.HasProperty("_Metallic") &&
                    mat.HasProperty("_Smoothness"))
                {
                    float metallic = mat.GetFloat("_Metallic");
                    float smoothness = mat.GetFloat("_Smoothness");

                    if (Mathf.Approximately(metallic, 0f) &&
                        Mathf.Approximately(smoothness, 0.5f))
                    {
                        report.AppendLine(
                            $"- (!) Материал \"{mat.name}\": " +
                            "Metallic=0, Smoothness=0.5 - " +
                            "похоже на значения по умолчанию.");
                        warnings++;
                    }
                }

                if (mat.HasProperty("_BumpMap"))
                {
                    Texture normalMap = mat.GetTexture("_BumpMap");

                    if (normalMap != null)
                    {
                        string texPath =
                            AssetDatabase.GetAssetPath(normalMap);

                        var importer =
                            AssetImporter.GetAtPath(texPath)
                            as TextureImporter;

                        bool wrongType = importer != null &&
                            importer.textureType !=
                            TextureImporterType.NormalMap;

                        if (wrongType)
                        {
                            report.AppendLine(
                                $"- (!) Текстура " +
                                $"\"{normalMap.name}\" " +
                                "подключена как Normal Map, " +
                                "но Texture Type = " +
                                $"{importer.textureType}.");
                            warnings++;
                        }
                    }
                }
            }
        }

        return warnings;
    }
}
