using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Unity.Burst;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// 1) Apaga la compilación Burst en el editor: este proyecto no la usa y en Windows
//    la caché de Burst (que venía de un Mac) da "Unexpected error in Burst compilation".
// 2) Menú Tools > Build para itch.io (.zip): hace el build Web y crea el .zip listo para subir.
[InitializeOnLoad]
public static class BuildItch
{
    const string NombreJuego = "LamparasGemas";

    static BuildItch()
    {
        if (EditorPrefs.GetBool("BurstCompilation", true))
        {
            EditorPrefs.SetBool("BurstCompilation", false);
            BurstCompiler.Options.EnableBurstCompilation = false;
            Debug.Log("Burst desactivado en el editor (no se usa en este proyecto).");
        }
    }

    [MenuItem("Tools/Build para itch.io (.zip)")]
    static void Construir()
    {
        string documentos = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        string destino = EditorUtility.SaveFolderPanel("¿Dónde guardo el build para itch.io?", documentos, "");
        if (string.IsNullOrEmpty(destino)) return;

        string carpetaBuild = Path.Combine(destino, NombreJuego + "_Web");
        string zip = Path.Combine(destino, NombreJuego + "_itch.zip");

        // La carpeta del build es solo nuestra: se limpia para que no queden archivos viejos.
        if (Directory.Exists(carpetaBuild)) Directory.Delete(carpetaBuild, true);
        if (File.Exists(zip)) File.Delete(zip);

        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);

        BuildPlayerOptions opciones = new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = carpetaBuild,
            target = BuildTarget.WebGL,
            targetGroup = BuildTargetGroup.WebGL,
            options = BuildOptions.None
        };

        BuildReport reporte = BuildPipeline.BuildPlayer(opciones);
        if (reporte.summary.result != BuildResult.Succeeded)
        {
            EditorUtility.DisplayDialog("Build para itch.io",
                "El build falló. Revisa los errores en la pestaña Console.", "OK");
            return;
        }

        // El .zip lleva index.html en la raíz, como lo pide itch.io.
        ZipFile.CreateFromDirectory(carpetaBuild, zip, System.IO.Compression.CompressionLevel.Optimal, false);
        EditorUtility.RevealInFinder(zip);
        EditorUtility.DisplayDialog("Build para itch.io",
            "¡Listo!\n\nSube este archivo a itch.io:\n" + zip, "OK");
    }
}
