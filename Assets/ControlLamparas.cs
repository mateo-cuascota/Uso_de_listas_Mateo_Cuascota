using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

// Va en el botón "Encender".
// - Lee la lista ListaDeLamparas (Visual Scripting) y la copia a una List<Lampara> de C#.
// - Encender/Apagar prende o apaga todas las lámparas en cadena (una tras otra).
// - Oculta "Instanciar" al llegar al total y muestra "Desinstanciar" desde la primera lámpara.
// - "Desinstanciar" hace desaparecer las lámparas y deja todo como al inicio.
// - Eliminar() quita una sola lámpara con List.Remove() (clic central / Supr o sin energía).
public class ControlLamparas : MonoBehaviour
{
    [SerializeField] GameObject botonInstanciar;
    [SerializeField] Button botonDesinstanciar;
    [SerializeField] int totalLamparas = 5;
    [SerializeField] string variableLista = "ListaDeLamparas";
    [SerializeField] string variableContador = "MaxLamps";
    [SerializeField] float retrasoCadena = 0.2f;

    readonly List<Lampara> lamparas = new List<Lampara>();
    Button boton;
    TMP_Text texto;
    bool enCadena;

    public IReadOnlyList<Lampara> Lamparas => lamparas;
    public int Total => totalLamparas;

    // Ejecuta una acción sobre cada lámpara usando List<T>.ForEach.
    public void ParaCada(System.Action<Lampara> accion) => lamparas.ForEach(accion);

    void Awake()
    {
        boton = GetComponent<Button>();
        texto = GetComponentInChildren<TMP_Text>();
        boton.onClick.AddListener(CambiarEstado);
        if (botonDesinstanciar != null) botonDesinstanciar.onClick.AddListener(Desinstanciar);
    }

    void Update()
    {
        LeerLista();

        // Si se eliminaron todas una por una, se puede volver a empezar.
        if (lamparas.Count == 0 && ContadorAgotado()) Reiniciar();

        // Se oculta cuando ya se crearon todas (aunque después se eliminen algunas).
        if (botonInstanciar != null && botonInstanciar.activeSelf && (lamparas.Count >= totalLamparas || ContadorAgotado()))
            botonInstanciar.SetActive(false);

        // Desinstanciar solo aparece cuando ya hay al menos una lámpara.
        if (botonDesinstanciar != null)
        {
            bool hayLamparas = lamparas.Count > 0;
            if (botonDesinstanciar.gameObject.activeSelf != hayLamparas)
                botonDesinstanciar.gameObject.SetActive(hayLamparas);
        }

        boton.interactable = !enCadena && lamparas.Count > 0;
        if (texto != null) texto.text = HayEncendidas() ? "Apagar" : "Encender";
    }

    bool ContadorAgotado()
    {
        VariableDeclarations escena = Variables.Scene(gameObject);
        return escena.IsDefined(variableContador) && escena.Get(variableContador) is int n && n <= 0;
    }

    // Elimina UNA lámpara: la quita de las listas con Remove() y la hace desaparecer.
    public void Eliminar(Lampara lampara)
    {
        if (lampara == null || lampara.Desapareciendo) return;

        VariableDeclarations escena = Variables.Scene(gameObject);
        if (escena.IsDefined(variableLista) && escena.Get(variableLista) is IList lista)
            lista.Remove(lampara.gameObject);
        lamparas.Remove(lampara);

        lampara.Desaparecer();
        Sonidos.Reproducir("desinstanciar");
    }

    bool HayEncendidas()
    {
        foreach (Lampara l in lamparas)
            if (l.Encendida) return true;
        return false;
    }

    void CambiarEstado()
    {
        if (!enCadena) StartCoroutine(Cadena(!HayEncendidas()));
    }

    // Prende (o apaga) una lámpara cada "retrasoCadena" segundos, como una ola.
    IEnumerator Cadena(bool encender)
    {
        enCadena = true;
        List<Lampara> copia = new List<Lampara>(lamparas);
        foreach (Lampara l in copia)
        {
            if (l == null || l.Desapareciendo) continue;
            if (encender) l.Encender(); else l.Apagar();
            yield return new WaitForSeconds(retrasoCadena);
        }
        enCadena = false;
    }

    void Desinstanciar()
    {
        StopAllCoroutines();
        enCadena = false;

        LeerLista();
        lamparas.ForEach(l => l.Desaparecer());
        lamparas.Clear();
        Sonidos.Reproducir("desinstanciar");

        Reiniciar();
    }

    // Lista vacía, contador al máximo y botón Instanciar visible.
    void Reiniciar()
    {
        VariableDeclarations escena = Variables.Scene(gameObject);
        if (escena.IsDefined(variableLista) && escena.Get(variableLista) is IList lista)
            lista.Clear();
        escena.Set(variableContador, totalLamparas);

        if (botonInstanciar != null) botonInstanciar.SetActive(true);
    }

    // Copia la lista de Visual Scripting a la List<Lampara> y le pone el componente a las nuevas.
    void LeerLista()
    {
        lamparas.Clear();
        VariableDeclarations escena = Variables.Scene(gameObject);
        if (!escena.IsDefined(variableLista)) return;
        if (!(escena.Get(variableLista) is IEnumerable lista)) return;

        foreach (object item in lista)
        {
            if (!(item is GameObject go) || go == null) continue;
            Lampara l = go.GetComponent<Lampara>();
            if (l == null)
            {
                l = go.AddComponent<Lampara>();
                l.SinEnergia += Eliminar;   // sin energía -> se elimina de la lista
            }
            if (!l.Desapareciendo) lamparas.Add(l);
        }
    }
}
