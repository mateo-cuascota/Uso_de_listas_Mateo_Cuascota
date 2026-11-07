using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Crea (o actualiza) el texto con el nombre del alumno arriba del Canvas.
[InitializeOnLoad]
public static class AgregarNombreAlumno
{
    const string NombreObjeto = "NombreAlumno";
    const string Nombre = "Mateo Cuascota";
    const float DistanciaArriba = 24f;
    const float TamanoLetra = 48f;

    static AgregarNombreAlumno()
    {
        EditorApplication.delayCall += Agregar;
    }

    [MenuItem("Tools/Agregar Nombre Alumno")]
    static void Agregar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        GameObject canvasGo = GameObject.Find("Canvas");
        Canvas canvas = canvasGo != null ? canvasGo.GetComponent<Canvas>() : null;
        if (canvas == null) return;

        Transform existente = canvas.transform.Find(NombreObjeto);
        GameObject go;
        if (existente != null)
        {
            go = existente.gameObject;
        }
        else
        {
            go = new GameObject(NombreObjeto, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(go, "Agregar nombre alumno");
            go.transform.SetParent(canvas.transform, false);
        }

        RectTransform rt = go.GetComponent<RectTransform>();
        TextMeshProUGUI texto = go.GetComponent<TextMeshProUGUI>();
        if (texto == null) texto = go.AddComponent<TextMeshProUGUI>();

        bool igual = texto.text == Nombre && Mathf.Approximately(texto.fontSize, TamanoLetra)
            && rt.anchoredPosition == new Vector2(0f, -DistanciaArriba);
        if (igual) return;

        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -DistanciaArriba);
        rt.sizeDelta = new Vector2(700f, 70f);

        texto.text = Nombre;
        texto.fontSize = TamanoLetra;
        texto.color = Color.white;
        texto.alignment = TextAlignmentOptions.Center;
        texto.raycastTarget = false;

        EditorUtility.SetDirty(texto);
        EditorUtility.SetDirty(rt);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        EditorSceneManager.SaveScene(canvas.gameObject.scene);
        Debug.Log("Texto '" + Nombre + "' actualizado y escena guardada.");
    }
}
