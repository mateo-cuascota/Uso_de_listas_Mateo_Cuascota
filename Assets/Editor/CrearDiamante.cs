using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Crea el material "Diamante" (URP Lit transparente, muy brillante, con facetas)
// y se lo pone al cubo de la lámpara (objeto "Ligth" del prefab totalLamp).
[InitializeOnLoad]
public static class CrearDiamante
{
    const string RutaMaterial = "Assets/Materiales/Diamante.mat";
    const string RutaNormal = "Assets/Materiales/DiamanteNormal.png";
    const string RutaPrefab = "Assets/totalLamp.prefab";

    static CrearDiamante()
    {
        EditorApplication.delayCall += Crear;
    }

    [MenuItem("Tools/Crear Material Diamante")]
    static void Crear()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (AssetDatabase.LoadAssetAtPath<Material>(RutaMaterial) != null) return;

        TextureImporter importador = AssetImporter.GetAtPath(RutaNormal) as TextureImporter;
        if (importador != null && importador.textureType != TextureImporterType.NormalMap)
        {
            importador.textureType = TextureImporterType.NormalMap;
            importador.SaveAndReimport();
        }
        Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(RutaNormal);

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) return;

        Material mat = new Material(shader) { name = "Diamante" };

        // Superficie transparente
        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", 0f);
        mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        mat.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_ZWrite", 0f);
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.renderQueue = (int)RenderQueue.Transparent;
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

        // Color hielo, muy liso y con reflejos
        Color colorDiamante = new Color(0.78f, 0.95f, 1f, 0.6f);
        mat.SetColor("_BaseColor", colorDiamante);
        mat.SetFloat("_Metallic", 0.35f);
        mat.SetFloat("_Smoothness", 0.97f);
        mat.SetFloat("_SpecularHighlights", 1f);
        mat.SetFloat("_EnvironmentReflections", 1f);

        // Facetas
        if (normal != null)
        {
            mat.SetTexture("_BumpMap", normal);
            mat.SetFloat("_BumpScale", 1f);
            mat.EnableKeyword("_NORMALMAP");
        }

        // Brillo propio celeste
        mat.SetColor("_EmissionColor", new Color(0.25f, 0.65f, 0.9f) * 1.2f);
        mat.EnableKeyword("_EMISSION");
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;

        AssetDatabase.CreateAsset(mat, RutaMaterial);

        GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefab);
        Transform cubo = BuscarHijo(raiz.transform, "Ligth");
        if (cubo != null && cubo.TryGetComponent(out MeshRenderer renderer))
        {
            renderer.sharedMaterial = mat;
            PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
            Debug.Log("Material Diamante aplicado al cubo de la lámpara.");
        }
        PrefabUtility.UnloadPrefabContents(raiz);
        AssetDatabase.SaveAssets();
    }

    static Transform BuscarHijo(Transform padre, string nombre)
    {
        foreach (Transform hijo in padre.GetComponentsInChildren<Transform>(true))
            if (hijo.name == nombre) return hijo;
        return null;
    }
}
