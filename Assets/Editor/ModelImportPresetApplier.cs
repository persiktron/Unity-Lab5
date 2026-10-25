
using UnityEngine;
using UnityEditor;

public static class ModelImportPresetApplier
{
    const string MenuPath =
        "Tools/Asset Pipeline/Apply Recommended Model Settings";

    [MenuItem(MenuPath)]
    static void ApplyToSelection()
    {
        int applied = 0;

        foreach (var obj in Selection.objects)
        {
            string path = AssetDatabase.GetAssetPath(obj);

            var importer =
                AssetImporter.GetAtPath(path) as ModelImporter;

            if (importer == null) continue;

            importer.importBlendShapes = false;
            importer.meshCompression =
                ModelImporterMeshCompression.Off;
            importer.isReadable = false;

            importer.SaveAndReimport();
            applied++;

            Debug.Log($"Настройки применены: {path}");
        }

        Debug.Log(applied == 0
            ? "В выделении не найдено файлов моделей."
            : $"Готово. Настройки применены к {applied} ассетам.");
    }
}

