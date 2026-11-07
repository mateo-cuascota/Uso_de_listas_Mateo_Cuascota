using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Agrega a la escena el fondo del espacio y el botón "Desinstanciar".
// Solo crea lo que todavía no existe, así que se puede volver a ejecutar sin duplicar nada.
[InitializeOnLoad]
public static class ConfigurarEscena
{
    const string RutaFondo = "Assets/Fondo/FondoEspacio.png";

    static ConfigurarEscena()
    {
        EditorApplication.delayCall += Configurar;
    }

    [MenuItem("Tools/Configurar Escena (fondo y Desinstanciar)")]
    static void Configurar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        Canvas canvas = null;
        foreach (Canvas c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            if (c.name == "Canvas") canvas = c;
        if (canvas == null) return;

        bool cambio = false;
        cambio |= CrearFondo();
        cambio |= CrearBotonDesinstanciar(canvas);

        if (cambio)
        {
            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            EditorSceneManager.SaveScene(canvas.gameObject.scene);
            Debug.Log("Escena configurada: fondo y botón Desinstanciar.");
        }
    }

    // Canvas en modo cámara, muy lejos, para que las lámparas 3D se vean delante del fondo.
    static bool CrearFondo()
    {
        if (GameObject.Find("Fondo") != null) return false;

        Texture2D textura = AssetDatabase.LoadAssetAtPath<Texture2D>(RutaFondo);
        Camera camara = Camera.main;
        if (textura == null || camara == null) return false;

        GameObject fondo = new GameObject("Fondo", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(fondo, "Crear fondo");
        Canvas canvas = fondo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camara;
        canvas.planeDistance = camara.farClipPlane * 0.9f;
        canvas.sortingOrder = -100;
        fondo.AddComponent<CanvasScaler>();

        GameObject imagen = new GameObject("Imagen", typeof(RectTransform));
        imagen.transform.SetParent(fondo.transform, false);
        RectTransform rt = imagen.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        RawImage raw = imagen.AddComponent<RawImage>();
        raw.texture = textura;
        raw.raycastTarget = false;

        AspectRatioFitter ajuste = imagen.AddComponent<AspectRatioFitter>();
        ajuste.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        ajuste.aspectRatio = (float)textura.width / textura.height;
        return true;
    }

    // Copia el botón "Encender" (mismo diseño), sin su script, debajo de él.
    static bool CrearBotonDesinstanciar(Canvas canvas)
    {
        if (canvas.transform.Find("Desinstanciar") != null) return false;

        Transform encender = canvas.transform.Find("Encender");
        if (encender == null) return false;

        GameObject boton = Object.Instantiate(encender.gameObject, canvas.transform);
        Undo.RegisterCreatedObjectUndo(boton, "Crear Desinstanciar");
        boton.name = "Desinstanciar";
        boton.transform.SetSiblingIndex(encender.GetSiblingIndex() + 1);
        Object.DestroyImmediate(boton.GetComponent<ControlLamparas>());

        RectTransform rt = boton.GetComponent<RectTransform>();
        RectTransform rtEncender = encender.GetComponent<RectTransform>();
        rt.anchoredPosition = rtEncender.anchoredPosition + new Vector2(0f, -110f);

        TMP_Text texto = boton.GetComponentInChildren<TMP_Text>();
        if (texto != null) texto.text = "Desinstanciar";

        ControlLamparas control = encender.GetComponent<ControlLamparas>();
        if (control != null)
        {
            SerializedObject so = new SerializedObject(control);
            so.FindProperty("botonDesinstanciar").objectReferenceValue = boton.GetComponent<Button>();
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        return true;
    }
}
