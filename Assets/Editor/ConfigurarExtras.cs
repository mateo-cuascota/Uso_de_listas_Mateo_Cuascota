using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

// Arma en la escena todo lo extra: textos (contador, inventario, ayuda, mensaje),
// GestorGemas, cámara que gira, Bloom, piso de mármol negro con reflejos
// y el material de las partículas. Solo crea lo que falta, así que no duplica nada.
[InitializeOnLoad]
public static class ConfigurarExtras
{
    const string RutaPrefab = "Assets/totalLamp.prefab";
    const string RutaBrillo = "Assets/Resources/Brillo.png";
    const string RutaMatBrillo = "Assets/Resources/BrilloParticula.mat";
    const string RutaMarmolNegro = "Assets/Materiales/MarmolNegro.png";
    const string RutaMatPiso = "Assets/Materiales/MarmolNegro.mat";

    static ConfigurarExtras()
    {
        EditorApplication.delayCall += Configurar;
    }

    [MenuItem("Tools/Configurar Extras (gemas, piso, bloom)")]
    static void Configurar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        GameObject canvasGo = GameObject.Find("Canvas");
        if (canvasGo == null) return;

        bool cambio = false;
        CrearMaterialBrillo();
        cambio |= CrearUIyGestor(canvasGo.transform);
        cambio |= CrearCamaraOrbita();
        cambio |= ActivarBloom();
        cambio |= CrearPiso();

        if (cambio)
        {
            EditorSceneManager.MarkSceneDirty(canvasGo.scene);
            EditorSceneManager.SaveScene(canvasGo.scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Extras configurados: gemas, inventario, contador, cámara, bloom y piso.");
        }
    }

    // ---------- Partículas ----------

    static void CrearMaterialBrillo()
    {
        if (AssetDatabase.LoadAssetAtPath<Material>(RutaMatBrillo) != null) return;

        TextureImporter imp = AssetImporter.GetAtPath(RutaBrillo) as TextureImporter;
        if (imp != null && !imp.alphaIsTransparency)
        {
            imp.alphaIsTransparency = true;
            imp.SaveAndReimport();
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) return;

        Material mat = new Material(shader) { name = "BrilloParticula" };
        mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(RutaBrillo));
        mat.SetColor("_BaseColor", Color.white);
        // Transparente aditivo: los brillos suman luz.
        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", 2f);
        mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)BlendMode.One);
        mat.SetFloat("_ZWrite", 0f);
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.renderQueue = (int)RenderQueue.Transparent;
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        AssetDatabase.CreateAsset(mat, RutaMatBrillo);
    }

    // ---------- Botones e interfaz ----------

    // Instanciar, Encender y Desinstanciar centrados y dentro de la pantalla.
    static bool OrdenarBotones(Transform canvas)
    {
        bool cambio = false;
        cambio |= Mover(canvas.Find("Button"), -330f);
        cambio |= Mover(canvas.Find("Encender"), -430f);
        cambio |= Mover(canvas.Find("Desinstanciar"), -530f);
        return cambio;
    }

    static bool Mover(Transform t, float y)
    {
        if (t == null) return false;
        RectTransform rt = (RectTransform)t;
        Vector2 nueva = new Vector2(0f, y);
        if (rt.anchoredPosition == nueva) return false;
        rt.anchoredPosition = nueva;
        return true;
    }

    static bool CrearUIyGestor(Transform canvas)
    {
        if (GameObject.Find("GestorGemas") != null) return false;

        TMP_FontAsset fuente = null;
        Transform nombre = canvas.Find("NombreAlumno");
        if (nombre != null) fuente = nombre.GetComponent<TMP_Text>().font;

        TMP_Text contador = CrearTexto(canvas, "Contador", "Lámparas: 0/5", -240f, 40f, 60f, Color.white, fuente, true);
        TMP_Text inventario = CrearTexto(canvas, "Inventario", "Inventario: vacío", -295f, 30f, 50f, new Color(1f, 0.85f, 1f), fuente, true);
        CrearTexto(canvas, "Ayuda",
            "Clic izq: prender/apagar · Clic der: recoger gema · E: usar gema\n0 Diamante · 1 Rubí · 2 Esmeralda · 3 Zafiro · C: cámara · M: música",
            -345f, 20f, 70f, new Color(0.8f, 0.85f, 1f, 0.9f), fuente, true);
        TMP_Text mensaje = CrearTexto(canvas, "Mensaje", "", -220f, 34f, 60f, new Color(1f, 0.9f, 0.4f), fuente, false);

        GameObject gestor = new GameObject("GestorGemas");
        Undo.RegisterCreatedObjectUndo(gestor, "Crear GestorGemas");
        GestorGemas g = gestor.AddComponent<GestorGemas>();

        SerializedObject so = new SerializedObject(g);
        so.FindProperty("control").objectReferenceValue = Object.FindFirstObjectByType<ControlLamparas>();
        Camera cam = Camera.main;
        if (cam != null)
        {
            CamaraOrbita orbita = cam.GetComponent<CamaraOrbita>();
            if (orbita == null) orbita = cam.gameObject.AddComponent<CamaraOrbita>();
            so.FindProperty("camara").objectReferenceValue = orbita;
        }
        so.FindProperty("textoContador").objectReferenceValue = contador;
        so.FindProperty("textoInventario").objectReferenceValue = inventario;
        so.FindProperty("textoMensaje").objectReferenceValue = mensaje;
        so.ApplyModifiedPropertiesWithoutUndo();
        return true;
    }

    static TMP_Text CrearTexto(Transform canvas, string nombre, string texto, float y, float tam, float alto,
                               Color color, TMP_FontAsset fuente, bool arriba)
    {
        Transform existente = canvas.Find(nombre);
        GameObject go = existente != null ? existente.gameObject : new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(canvas, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        Vector2 ancla = arriba ? new Vector2(0.5f, 1f) : new Vector2(0.5f, 0.5f);
        rt.anchorMin = ancla;
        rt.anchorMax = ancla;
        rt.pivot = new Vector2(0.5f, arriba ? 1f : 0.5f);
        rt.anchoredPosition = new Vector2(0f, y);
        rt.sizeDelta = new Vector2(700f, alto);

        TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>();
        if (t == null) t = go.AddComponent<TextMeshProUGUI>();
        if (fuente != null) t.font = fuente;
        t.text = texto;
        t.fontSize = tam;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.raycastTarget = false;
        return t;
    }

    // ---------- Cámara y Bloom ----------

    static bool CrearCamaraOrbita()
    {
        Camera cam = Camera.main;
        if (cam == null || cam.GetComponent<CamaraOrbita>() != null) return false;
        cam.gameObject.AddComponent<CamaraOrbita>();
        return true;
    }

    static bool ActivarBloom()
    {
        Volume volumen = Object.FindFirstObjectByType<Volume>();
        if (volumen == null || volumen.sharedProfile == null) return false;

        VolumeProfile perfil = volumen.sharedProfile;
        if (perfil.TryGet(out Bloom existente) && existente.active && existente.intensity.overrideState) return false;

        Bloom bloom = existente != null ? existente : perfil.Add<Bloom>(true);
        bloom.active = true;
        bloom.intensity.Override(1.3f);
        bloom.threshold.Override(0.85f);
        bloom.scatter.Override(0.7f);
        EditorUtility.SetDirty(perfil);

        Camera cam = Camera.main;
        if (cam != null)
        {
            UniversalAdditionalCameraData datos = cam.GetUniversalAdditionalCameraData();
            if (datos != null) datos.renderPostProcessing = true;
        }
        return true;
    }

    // ---------- Piso ----------

    static bool CrearPiso()
    {
        if (GameObject.Find("Piso") != null) return false;

        // Altura de la parte de abajo de la lámpara, para que el piso quede justo debajo.
        float alturaPiso = 0f;
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefab);
        if (prefab != null)
        {
            GameObject prueba = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            prueba.transform.position = Vector3.zero;
            Renderer[] renders = prueba.GetComponentsInChildren<Renderer>();
            if (renders.Length > 0)
            {
                Bounds b = renders[0].bounds;
                foreach (Renderer r in renders) b.Encapsulate(r.bounds);
                alturaPiso = b.min.y;
            }
            Object.DestroyImmediate(prueba);
        }

        Material mat = AssetDatabase.LoadAssetAtPath<Material>(RutaMatPiso);
        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) return false;
            mat = new Material(shader) { name = "MarmolNegro" };
            mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(RutaMarmolNegro));
            mat.SetTextureScale("_BaseMap", new Vector2(2f, 2f));
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Metallic", 0.15f);
            mat.SetFloat("_Smoothness", 0.92f);
            AssetDatabase.CreateAsset(mat, RutaMatPiso);
        }

        GameObject piso = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Undo.RegisterCreatedObjectUndo(piso, "Crear piso");
        piso.name = "Piso";
        Object.DestroyImmediate(piso.GetComponent<Collider>());
        piso.transform.position = new Vector3(0f, alturaPiso - 0.05f, 0f);
        piso.transform.localScale = new Vector3(7.5f, 0.05f, 7.5f);
        piso.GetComponent<MeshRenderer>().sharedMaterial = mat;

        // Sonda de reflejos en tiempo real para que el piso refleje las lámparas.
        GameObject sonda = new GameObject("ReflejoPiso");
        sonda.transform.position = new Vector3(0f, alturaPiso + 1f, 0f);
        ReflectionProbe probe = sonda.AddComponent<ReflectionProbe>();
        probe.mode = ReflectionProbeMode.Realtime;
        probe.refreshMode = ReflectionProbeRefreshMode.EveryFrame;
        probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.NoTimeSlicing;
        probe.size = new Vector3(12f, 6f, 12f);
        probe.resolution = 128;
        probe.importance = 1;
        return true;
    }
}
