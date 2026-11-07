using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Deja el juego listo para publicarlo en itch.io (WebGL, horizontal 1280x720):
// - La interfaz escala con el tamaño de la pantalla (Scale With Screen Size).
// - Botones en fila abajo, nombre arriba, controles arriba a la izquierda.
// - Cámara con un poco más de zoom.
// - Ajustes de WebGL que funcionan en itch.io (Gzip + Decompression Fallback).
[InitializeOnLoad]
public static class PrepararItch
{
    const float Ancho = 1280f, Alto = 720f;

    static PrepararItch()
    {
        EditorApplication.delayCall += () => Preparar(false);
    }

    [MenuItem("Tools/Preparar para itch.io")]
    static void PrepararDesdeMenu() => Preparar(true);

    static void Preparar(bool forzar)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        AjustesWebGL();

        GameObject canvasGo = GameObject.Find("Canvas");
        if (canvasGo == null) return;
        CanvasScaler escalador = canvasGo.GetComponent<CanvasScaler>();
        if (!forzar && escalador.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize) return;

        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(Ancho, Alto);
        escalador.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        escalador.matchWidthOrHeight = 0.5f;

        Transform canvas = canvasGo.transform;
        // Tres botones en fila, abajo al centro.
        Boton(canvas.Find("Button"), -290f);
        Boton(canvas.Find("Encender"), 0f);
        Boton(canvas.Find("Desinstanciar"), 290f);

        // Mensajes justo encima de los botones.
        Transform mensaje = canvas.Find("Mensaje");
        if (mensaje != null)
        {
            RectTransform rt = (RectTransform)mensaje;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 105f);
            rt.sizeDelta = new Vector2(900f, 50f);
            if (mensaje.TryGetComponent(out TMPro.TMP_Text t)) t.fontSize = 30f;
        }

        GestorGemas.ArmarPanel(canvas);

        // Un poco más de zoom para que las lámparas se vean grandes en horizontal.
        Camera cam = Camera.main;
        if (cam != null && cam.orthographic) cam.orthographicSize = 3.6f;

        EditorSceneManager.MarkSceneDirty(canvasGo.scene);
        EditorSceneManager.SaveScene(canvasGo.scene);
        Debug.Log("Escena lista para itch.io (1280x720). Pon el Game view en 16:9 para verla igual.");
    }

    static void Boton(Transform t, float x)
    {
        if (t == null) return;
        RectTransform rt = (RectTransform)t;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.localScale = Vector3.one * 1.6f;          // 160x30 -> 256x48
        rt.anchoredPosition = new Vector2(x, 28f);
    }

    static void AjustesWebGL()
    {
        bool cambio = PlayerSettings.defaultWebScreenWidth != (int)Ancho
                   || PlayerSettings.defaultWebScreenHeight != (int)Alto
                   || PlayerSettings.WebGL.compressionFormat != WebGLCompressionFormat.Gzip
                   || !PlayerSettings.WebGL.decompressionFallback;
        if (!cambio) return;

        PlayerSettings.defaultWebScreenWidth = (int)Ancho;
        PlayerSettings.defaultWebScreenHeight = (int)Alto;
        PlayerSettings.runInBackground = true;
        // itch.io no manda las cabeceras de Brotli: con Gzip + fallback carga siempre.
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        AssetDatabase.SaveAssets();
    }
}
