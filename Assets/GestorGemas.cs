using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

// Diccionarios + inventario de gemas.
// - Dictionary<string, Color>: nombre de la gema -> color de la lámpara (teclas 0, 1, 2, 3).
// - Dictionary<string, int>: nombre de la gema -> daño (negativo) o curación (positivo).
// - Dictionary<string, Action<Lampara>>: nombre de la gema -> efecto visual al usarla (tecla E).
// - List<string>: inventario con las gemas recogidas (clic derecho en una lámpara encendida).
public class GestorGemas : MonoBehaviour
{
    [SerializeField] ControlLamparas control;
    [SerializeField] CamaraOrbita camara;
    [SerializeField] TMP_Text textoMensaje;
    [SerializeField] float volumenMusica = 0.35f;

    readonly Dictionary<string, Color> colores = new Dictionary<string, Color>();
    readonly Dictionary<string, int> danoCuracion = new Dictionary<string, int>();
    readonly Dictionary<string, Action<Lampara>> efectos = new Dictionary<string, Action<Lampara>>();
    readonly Dictionary<string, string> descripciones = new Dictionary<string, string>();
    readonly List<string> inventario = new List<string>();

    // Tecla 0 -> Diamante, 1 -> Rubí, 2 -> Esmeralda, 3 -> Zafiro
    readonly string[] gemaPorTecla = { "Diamante", "Rubí", "Esmeralda", "Zafiro" };

    AudioSource musica;
    float tiempoMensaje;

    void Awake()
    {
        //          nombre       color                          daño/curación  efecto                    descripción
        AgregarGema("Diamante", new Color(0.55f, 0.9f, 1f),   15,           l => l.LluviaBrillos(),   "lluvia de brillos");
        AgregarGema("Rubí",     new Color(1f, 0.12f, 0.25f),  -25,          l => l.Explotar(),        "¡explosión!");
        AgregarGema("Esmeralda",new Color(0.15f, 1f, 0.45f),  30,           l => l.BrillarFuerte(),   "brillo intenso");
        AgregarGema("Zafiro",   new Color(0.25f, 0.4f, 1f),   -10,          l => l.Saltar(),          "salto con giro");

        if (control == null) control = FindFirstObjectByType<ControlLamparas>();
        if (camara == null) camara = FindFirstObjectByType<CamaraOrbita>();
    }

    void Start()
    {
        AudioClip clip = Sonidos.Cargar("musica");
        if (clip != null)
        {
            musica = gameObject.AddComponent<AudioSource>();
            musica.clip = clip;
            musica.loop = true;
            musica.volume = volumenMusica;
            musica.Play();
        }
        Mensaje("");

        CrearPanelAyuda();
    }

    // ---------- Recuadro de controles (arriba a la izquierda, tecla H) ----------

    GameObject panelAyuda;
    TMP_Text textoPanel;

    const string TextoControles =
        "<b><color=#FFD966>CONTROLES</color></b>\n" +
        "<b>Clic izq</b>  Prender\n" +
        "<b>Clic der</b>  Recoger gema\n" +
        "<b>Supr</b>  Eliminar\n" +
        "<b>E</b>  Usar gema\n" +
        "<b>0-3</b>  Cambiar gema\n" +
        "<b>H</b>  Ocultar";

    void CrearPanelAyuda()
    {
        // Lo de arriba (contador, inventario, ayuda vieja) ya no se muestra: solo queda el nombre.
        foreach (string nombre in new[] { "Contador", "Inventario", "PistaAyuda" })
        {
            GameObject viejo = GameObject.Find(nombre);
            if (viejo != null) viejo.SetActive(false);
        }

        GameObject canvas = GameObject.Find("Canvas");
        if (canvas == null) return;
        textoPanel = ArmarPanel(canvas.transform);
        panelAyuda = textoPanel != null ? textoPanel.transform.parent.gameObject : null;
    }

    // Crea (o reutiliza) el recuadro arriba a la izquierda, debajo del nombre.
    // Es público y estático para que el script de editor lo deje armado en la escena.
    public static TMP_Text ArmarPanel(Transform canvas)
    {
        Transform panel = canvas.Find("PanelAyuda");
        if (panel == null)
        {
            panel = new GameObject("PanelAyuda", typeof(RectTransform)).transform;
            panel.SetParent(canvas, false);
        }

        RectTransform rtPanel = (RectTransform)panel;
        rtPanel.anchorMin = rtPanel.anchorMax = rtPanel.pivot = new Vector2(0f, 1f);
        rtPanel.anchoredPosition = new Vector2(16f, -16f);
        rtPanel.sizeDelta = new Vector2(270f, 0f);

        UnityEngine.UI.Image fondo = panel.GetComponent<UnityEngine.UI.Image>();
        if (fondo == null) fondo = panel.gameObject.AddComponent<UnityEngine.UI.Image>();
        fondo.color = new Color(0.05f, 0.03f, 0.12f, 0.8f);
        fondo.raycastTarget = false;

        // El alto se ajusta solo al texto.
        UnityEngine.UI.VerticalLayoutGroup grupo = panel.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
        if (grupo == null) grupo = panel.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        grupo.padding = new RectOffset(16, 16, 12, 14);
        grupo.childControlWidth = true;
        grupo.childControlHeight = true;
        grupo.childForceExpandWidth = true;
        grupo.childForceExpandHeight = false;
        UnityEngine.UI.ContentSizeFitter ajuste = panel.GetComponent<UnityEngine.UI.ContentSizeFitter>();
        if (ajuste == null) ajuste = panel.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();
        ajuste.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;

        Transform ayuda = panel.Find("Ayuda");
        if (ayuda == null)
        {
            GameObject suelta = GameObject.Find("Ayuda");
            ayuda = suelta != null ? suelta.transform : new GameObject("Ayuda", typeof(RectTransform)).transform;
            ayuda.SetParent(panel, false);
        }
        TMP_Text texto = ayuda.GetComponent<TMP_Text>();
        if (texto == null) texto = ayuda.gameObject.AddComponent<TextMeshProUGUI>();

        Transform nombre = canvas.Find("NombreAlumno");
        if (nombre != null && nombre.TryGetComponent(out TMP_Text tn)) texto.font = tn.font;
        texto.text = TextoControles;
        texto.fontSize = 22f;
        texto.color = Color.white;
        texto.alignment = TextAlignmentOptions.TopLeft;
        texto.lineSpacing = 6f;
        texto.textWrappingMode = TextWrappingModes.Normal;
        texto.raycastTarget = false;
        return texto;
    }

    void MostrarAyuda(bool mostrar)
    {
        if (panelAyuda != null) panelAyuda.SetActive(mostrar);
        if (!mostrar) Mensaje("H: ver controles");
    }

    // Controles + lámparas + gemas, todo dentro del recuadro.
    void ActualizarPanel()
    {
        if (textoPanel == null || !panelAyuda.activeSelf) return;
        int cantidad = control != null ? control.Lamparas.Count : 0;
        int total = control != null ? control.Total : 0;
        textoPanel.text = TextoControles +
                          $"\n\n<b>Lámparas</b>  {cantidad}/{total}\n<b>Gemas</b>  {TextoInventario()}";
    }

    void AgregarGema(string nombre, Color color, int valor, Action<Lampara> efecto, string descripcion)
    {
        if (colores.ContainsKey(nombre)) return;   // no repetir la misma llave
        colores.Add(nombre, color);
        danoCuracion.Add(nombre, valor);
        efectos.Add(nombre, efecto);
        descripciones.Add(nombre, descripcion);
    }

    void Update()
    {
        Keyboard teclado = Keyboard.current;
        if (teclado != null)
        {
            if (teclado.digit0Key.wasPressedThisFrame || teclado.numpad0Key.wasPressedThisFrame) CambiarColor(gemaPorTecla[0]);
            if (teclado.digit1Key.wasPressedThisFrame || teclado.numpad1Key.wasPressedThisFrame) CambiarColor(gemaPorTecla[1]);
            if (teclado.digit2Key.wasPressedThisFrame || teclado.numpad2Key.wasPressedThisFrame) CambiarColor(gemaPorTecla[2]);
            if (teclado.digit3Key.wasPressedThisFrame || teclado.numpad3Key.wasPressedThisFrame) CambiarColor(gemaPorTecla[3]);
            if (teclado.eKey.wasPressedThisFrame) UsarGema();
            if (teclado.hKey.wasPressedThisFrame && panelAyuda != null) MostrarAyuda(!panelAyuda.activeSelf);
            if (teclado.cKey.wasPressedThisFrame && camara != null)
            {
                camara.Girando = !camara.Girando;
                Mensaje(camara.Girando ? "Cámara girando" : "Cámara detenida");
            }
            if (teclado.mKey.wasPressedThisFrame && musica != null)
            {
                musica.mute = !musica.mute;
                Mensaje(musica.mute ? "Música apagada" : "Música encendida");
            }
        }

        Mouse raton = Mouse.current;
        if (raton != null)
        {
            Vector2 pos = raton.position.ReadValue();
            if (raton.leftButton.wasPressedThisFrame) ClicEnLampara(false, pos);
            if (raton.rightButton.wasPressedThisFrame) ClicEnLampara(true, pos);

            bool eliminar = raton.middleButton.wasPressedThisFrame
                            || (teclado != null && teclado.deleteKey.wasPressedThisFrame);
            if (eliminar)
            {
                Lampara l = LamparaBajoRaton(pos);
                if (l != null && control != null)
                {
                    control.Eliminar(l);
                    Mensaje("Lámpara eliminada de la lista");
                }
            }
        }

        ActualizarPanel();

        if (textoMensaje != null && tiempoMensaje > 0f)
        {
            tiempoMensaje -= Time.deltaTime;
            textoMensaje.alpha = Mathf.Clamp01(tiempoMensaje / 0.5f);
        }
    }

    void CambiarColor(string gema)
    {
        if (control == null) return;
        if (!colores.TryGetValue(gema, out Color color)) return;

        control.ParaCada(l => l.CambiarGema(gema, color));   // List<T>.ForEach
        Sonidos.Reproducir("recoger", 0.4f);
        Mensaje($"Gema: {gema}");
    }

    // Clic izquierdo: prende/apaga esa lámpara. Clic derecho: recoge su gema (si está encendida).
    Lampara LamparaBajoRaton(Vector2 posicion)
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return null;
        Camera cam = Camera.main;
        if (cam == null) return null;

        if (!Physics.Raycast(cam.ScreenPointToRay(posicion), out RaycastHit golpe, 500f)) return null;
        Lampara lampara = golpe.collider.GetComponentInParent<Lampara>();
        return lampara != null && !lampara.Desapareciendo ? lampara : null;
    }

    void ClicEnLampara(bool recoger, Vector2 posicion)
    {
        Lampara lampara = LamparaBajoRaton(posicion);
        if (lampara == null) return;

        if (!recoger)
        {
            lampara.Alternar();
            return;
        }

        if (!lampara.Encendida)
        {
            Mensaje("Enciende la lámpara para recoger su gema");
            return;
        }

        inventario.Add(lampara.Gema);
        lampara.Apagar();
        Sonidos.Reproducir("recoger");
        Mensaje($"Recogiste: {lampara.Gema}");
    }

    // Usa la primera gema del inventario sobre una lámpara (de preferencia una encendida).
    void UsarGema()
    {
        if (inventario.Count == 0)
        {
            Mensaje("No tienes gemas. Clic derecho en una lámpara encendida.");
            return;
        }
        if (control == null || control.Lamparas.Count == 0)
        {
            Mensaje("No hay lámparas para usar la gema");
            return;
        }

        List<Lampara> encendidas = new List<Lampara>();
        control.ParaCada(l => { if (l.Encendida) encendidas.Add(l); });
        List<Lampara> opciones = encendidas.Count > 0 ? encendidas : new List<Lampara>(control.Lamparas);
        Lampara objetivo = opciones[Random.Range(0, opciones.Count)];

        string gema = inventario[0];
        inventario.RemoveAt(0);

        string detalle = "";
        if (efectos.TryGetValue(gema, out Action<Lampara> efecto))
        {
            efecto(objetivo);
            Sonidos.Reproducir("usar", 0.7f);
            detalle = descripciones[gema];
        }
        if (danoCuracion.TryGetValue(gema, out int valor))
        {
            detalle += valor < 0 ? $" ({-valor} de daño)" : $" (+{valor} de curación)";
            objetivo.CambiarEnergia(valor);   // si llega a 0 se elimina de la lista
        }
        Mensaje($"Usaste {gema}: {detalle}");
    }

    // Inventario agrupado: "Rubí x2, Zafiro x1".
    string TextoInventario()
    {
        if (inventario.Count == 0) return "ninguna";

        Dictionary<string, int> conteo = new Dictionary<string, int>();
        List<string> orden = new List<string>();
        foreach (string gema in inventario)
        {
            if (conteo.ContainsKey(gema)) conteo[gema]++;
            else
            {
                conteo.Add(gema, 1);
                orden.Add(gema);
            }
        }

        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < orden.Count; i++)
        {
            if (i > 0) sb.Append(", ");
            sb.Append(orden[i]).Append(" x").Append(conteo[orden[i]]);
        }
        return sb.ToString();
    }

    void Mensaje(string texto)
    {
        if (textoMensaje == null) return;
        textoMensaje.text = texto;
        textoMensaje.alpha = 1f;
        tiempoMensaje = string.IsNullOrEmpty(texto) ? 0f : 2.5f;
    }
}
