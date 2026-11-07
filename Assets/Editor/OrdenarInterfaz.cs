using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Deja arriba solo el nombre: borra Contador e Inventario y arma el recuadro
// de controles en la esquina superior izquierda (también visible sin darle a Play).
[InitializeOnLoad]
public static class OrdenarInterfaz
{
    static OrdenarInterfaz()
    {
        EditorApplication.delayCall += Ordenar;
    }

    [MenuItem("Tools/Ordenar Interfaz")]
    static void Ordenar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        GameObject canvas = GameObject.Find("Canvas");
        if (canvas == null) return;
        if (canvas.transform.Find("PanelAyuda") != null && canvas.transform.Find("Contador") == null) return;

        foreach (string nombre in new[] { "Contador", "Inventario", "PistaAyuda" })
        {
            Transform viejo = canvas.transform.Find(nombre);
            if (viejo != null) Object.DestroyImmediate(viejo.gameObject);
        }

        GestorGemas.ArmarPanel(canvas.transform);

        EditorSceneManager.MarkSceneDirty(canvas.scene);
        EditorSceneManager.SaveScene(canvas.scene);
        Debug.Log("Interfaz ordenada: solo el nombre arriba y los controles a la izquierda.");
    }
}
