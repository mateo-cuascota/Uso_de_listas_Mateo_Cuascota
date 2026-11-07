using UnityEditor;
using UnityEngine;

// Da texturas realistas a la lámpara: mármol en la base y metal cepillado en el soporte.
// Los modelos de MagicaVoxel traen una sola UV, así que primero se crea una copia
// de cada malla con UVs proyectadas por caras (como una caja).
[InitializeOnLoad]
public static class TexturasRealistas
{
    const string Carpeta = "Assets/Materiales/";
    const string RutaPrefab = "Assets/totalLamp.prefab";

    static TexturasRealistas()
    {
        EditorApplication.delayCall += Aplicar;
    }

    [MenuItem("Tools/Aplicar Texturas Realistas")]
    static void Aplicar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (AssetDatabase.LoadAssetAtPath<Material>(Carpeta + "Marmol.mat") != null) return;

        Material marmol = CrearMaterial("Marmol", "Marmol.png", "MarmolNormal.png", 0f, 0.8f, 0.6f);
        Material metal = CrearMaterial("MetalCepillado", "MetalCepillado.png", "MetalCepilladoNormal.png", 0.9f, 0.6f, 0.4f);
        if (marmol == null || metal == null) return;

        GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefab);
        bool listo = Asignar(raiz.transform, "Base", marmol, "BaseUV", 1.5f)
                   & Asignar(raiz.transform, "LampSupport", metal, "SoporteUV", 2f);
        if (listo)
        {
            PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefab);
            Debug.Log("Texturas realistas aplicadas: mármol en la base y metal en el soporte.");
        }
        PrefabUtility.UnloadPrefabContents(raiz);
        AssetDatabase.SaveAssets();
    }

    static Material CrearMaterial(string nombre, string albedo, string normal, float metalico, float liso, float fuerzaNormal)
    {
        TextureImporter imp = AssetImporter.GetAtPath(Carpeta + normal) as TextureImporter;
        if (imp != null && imp.textureType != TextureImporterType.NormalMap)
        {
            imp.textureType = TextureImporterType.NormalMap;
            imp.SaveAndReimport();
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        Texture2D texAlbedo = AssetDatabase.LoadAssetAtPath<Texture2D>(Carpeta + albedo);
        Texture2D texNormal = AssetDatabase.LoadAssetAtPath<Texture2D>(Carpeta + normal);
        if (shader == null || texAlbedo == null) return null;

        Material mat = new Material(shader) { name = nombre };
        mat.SetTexture("_BaseMap", texAlbedo);
        mat.SetColor("_BaseColor", Color.white);
        mat.SetFloat("_Metallic", metalico);
        mat.SetFloat("_Smoothness", liso);
        if (texNormal != null)
        {
            mat.SetTexture("_BumpMap", texNormal);
            mat.SetFloat("_BumpScale", fuerzaNormal);
            mat.EnableKeyword("_NORMALMAP");
        }
        AssetDatabase.CreateAsset(mat, Carpeta + nombre + ".mat");
        return mat;
    }

    static bool Asignar(Transform raiz, string nombreObjeto, Material mat, string nombreMalla, float repeticiones)
    {
        foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
        {
            if (t.name != nombreObjeto) continue;
            MeshFilter filtro = t.GetComponentInChildren<MeshFilter>(true);
            MeshRenderer render = t.GetComponentInChildren<MeshRenderer>(true);
            if (filtro == null || render == null || filtro.sharedMesh == null) return false;

            filtro.sharedMesh = MallaConUVs(filtro.sharedMesh, nombreMalla, repeticiones);
            render.sharedMaterial = mat;
            return true;
        }
        return false;
    }

    // Proyecta la textura según hacia dónde mira cada cara (X, Y o Z).
    static Mesh MallaConUVs(Mesh original, string nombre, float repeticiones)
    {
        Mesh malla = Object.Instantiate(original);
        malla.name = nombre;

        Vector3[] vertices = malla.vertices;
        Vector3[] normales = malla.normals;
        Bounds b = malla.bounds;
        float tam = Mathf.Max(b.size.x, b.size.y, b.size.z);
        if (tam <= 0f) tam = 1f;

        Vector2[] uvs = new Vector2[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 p = (vertices[i] - b.min) / tam * repeticiones;
            Vector3 n = normales.Length > i ? normales[i] : Vector3.up;
            float ax = Mathf.Abs(n.x), ay = Mathf.Abs(n.y), az = Mathf.Abs(n.z);
            if (ax >= ay && ax >= az) uvs[i] = new Vector2(p.z, p.y);
            else if (ay >= az) uvs[i] = new Vector2(p.x, p.z);
            else uvs[i] = new Vector2(p.x, p.y);
        }
        malla.uv = uvs;
        malla.RecalculateTangents();

        AssetDatabase.CreateAsset(malla, Carpeta + nombre + ".asset");
        return malla;
    }
}
