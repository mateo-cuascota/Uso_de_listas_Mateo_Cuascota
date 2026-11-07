using System;
using System.Collections;
using TMPro;
using UnityEngine;

// Se agrega sola a cada lámpara instanciada (lo hace ControlLamparas).
// Maneja su estado (encendida/apagada), su gema, la luz, las partículas y los efectos.
[DisallowMultipleComponent]
public class Lampara : MonoBehaviour
{
    const string ParametroAnimator = "IsActive";
    const float IntensidadLuz = 6f;
    const float RangoLuz = 4f;

    public bool Encendida { get; private set; }
    public bool Desapareciendo { get; private set; }
    public string Gema { get; private set; } = "Diamante";
    public Color ColorGema { get; private set; } = new Color(0.55f, 0.9f, 1f);

    // Energía: el daño la baja y la curación la sube. Si llega a 0 la lámpara se elimina.
    public const int EnergiaMaxima = 100;
    public int Energia { get; private set; } = EnergiaMaxima;
    public event Action<Lampara> SinEnergia;

    Transform barra;
    SpriteRenderer relleno;
    static Sprite spriteBlanco;

    Animator animator;
    Renderer cubo;
    Transform cabeza;
    Light luz;
    ParticleSystem brillos;
    MaterialPropertyBlock bloque;
    Vector3 escalaOriginal;
    float multiplicadorBrillo = 1f;
    bool ocupada;   // mientras salta o aparece no se mueve la posición

    static Material materialBrillo;

    void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        escalaOriginal = transform.localScale;
        cabeza = BuscarHijo("LampHead");
        Transform t = BuscarHijo("Ligth");
        if (t != null) cubo = t.GetComponent<Renderer>();
        bloque = new MaterialPropertyBlock();

        CrearCollider();
        CrearBarra();
        CrearLuz();
        CrearBrillos();
        AplicarColor();

        StartCoroutine(Aparecer());
        Sonidos.Reproducir("instanciar", 0.8f);
    }

    // La barra siempre mira a la cámara.
    void LateUpdate()
    {
        Camera cam = Camera.main;
        if (barra != null && cam != null) barra.rotation = cam.transform.rotation;
    }

    void Update()
    {
        if (!Encendida || luz == null) return;

        float onda = Mathf.Sin(Time.time * 3f + transform.position.x);
        luz.intensity = IntensidadLuz * multiplicadorBrillo * (0.75f + 0.25f * onda);
        luz.color = Color.Lerp(ColorGema, Color.white, 0.2f + 0.15f * onda);
        AplicarColor(1.6f + 0.6f * onda);
    }

    // ---------- Encender / apagar ----------

    public void Alternar()
    {
        if (Encendida) Apagar(); else Encender();
    }

    public void Encender()
    {
        if (Encendida || Desapareciendo) return;
        Encendida = true;
        if (animator != null) animator.SetBool(ParametroAnimator, true);
        luz.enabled = true;

        ParticleSystem.EmissionModule emision = brillos.emission;
        emision.rateOverTime = 14f;
        brillos.Emit(30);
        Sonidos.Reproducir("encender", 0.6f);
    }

    public void Apagar()
    {
        if (!Encendida) return;
        Encendida = false;
        if (animator != null) animator.SetBool(ParametroAnimator, false);
        luz.enabled = false;

        ParticleSystem.EmissionModule emision = brillos.emission;
        emision.rateOverTime = 0f;
        AplicarColor();
        Sonidos.Reproducir("apagar", 0.5f);
    }

    public void CambiarGema(string nombre, Color color)
    {
        Gema = nombre;
        ColorGema = color;
        ParticleSystem.MainModule main = brillos.main;
        main.startColor = new ParticleSystem.MinMaxGradient(color, Color.white);
        if (!Encendida) AplicarColor();
    }

    // ---------- Efectos de las gemas ----------

    public void Explotar()
    {
        Vector3 centro = cubo != null ? cubo.bounds.center : transform.position + Vector3.up;
        GameObject go = new GameObject("Explosion");
        go.transform.position = centro;
        ParticleSystem ps = NuevoSistema(go, 400);
        ParticleSystem.MainModule main = ps.main;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.3f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.22f);
        main.gravityModifier = 0.4f;
        main.startColor = new ParticleSystem.MinMaxGradient(ColorGema, Color.white);
        ParticleSystem.ShapeModule forma = ps.shape;
        forma.radius = 0.15f;
        ps.Play();
        ps.Emit(180);
        Destroy(go, 2.5f);
        Sonidos.Reproducir("explosion", 0.9f);
    }

    public void BrillarFuerte(float segundos = 2.5f)
    {
        Encender();
        StartCoroutine(RutinaBrillo(segundos));
    }

    public void Saltar()
    {
        if (!ocupada) StartCoroutine(RutinaSalto());
    }

    public void LluviaBrillos()
    {
        Encender();
        brillos.Emit(120);
    }

    // ---------- Energía (daño y curación) ----------

    // valor negativo = daño, valor positivo = curación.
    public void CambiarEnergia(int valor)
    {
        if (Desapareciendo) return;
        Energia = Mathf.Clamp(Energia + valor, 0, EnergiaMaxima);
        ActualizarBarra();

        string texto = valor > 0 ? "+" + valor : valor.ToString();
        Color color = valor > 0 ? new Color(0.3f, 1f, 0.5f) : new Color(1f, 0.3f, 0.3f);
        StartCoroutine(TextoFlotante(texto, color));

        if (Energia == 0) SinEnergia?.Invoke(this);
    }

    void CrearBarra()
    {
        if (spriteBlanco == null)
            spriteBlanco = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0f, 0.5f), 4f);

        Collider col = GetComponent<Collider>();
        float alto = col != null ? col.bounds.max.y - transform.position.y : 2f;

        barra = new GameObject("BarraEnergia").transform;
        barra.SetParent(transform, false);
        barra.position = transform.position + Vector3.up * (alto + 0.35f);

        const float ancho = 1.2f, grosor = 0.14f;
        SpriteRenderer fondo = NuevaParteBarra("Fondo", new Color(0f, 0f, 0f, 0.6f), 0);
        fondo.transform.localPosition = new Vector3(-ancho / 2f - 0.03f, 0f, 0.001f);
        fondo.transform.localScale = new Vector3(ancho + 0.06f, grosor + 0.06f, 1f);

        relleno = NuevaParteBarra("Relleno", Color.green, 1);
        relleno.transform.localPosition = new Vector3(-ancho / 2f, 0f, 0f);
        relleno.transform.localScale = new Vector3(ancho, grosor, 1f);
        ActualizarBarra();
    }

    SpriteRenderer NuevaParteBarra(string nombre, Color color, int orden)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(barra, false);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = spriteBlanco;
        sr.color = color;
        sr.sortingOrder = 100 + orden;
        return sr;
    }

    void ActualizarBarra()
    {
        if (relleno == null) return;
        float k = (float)Energia / EnergiaMaxima;
        Vector3 escala = relleno.transform.localScale;
        relleno.transform.localScale = new Vector3(1.2f * k, escala.y, 1f);
        relleno.color = Color.Lerp(new Color(1f, 0.2f, 0.2f), new Color(0.3f, 1f, 0.4f), k);
    }

    // Número que sube y se desvanece encima de la lámpara (-25, +30...).
    IEnumerator TextoFlotante(string texto, Color color)
    {
        GameObject go = new GameObject("TextoFlotante");
        go.transform.position = barra != null ? barra.position + Vector3.up * 0.3f : transform.position + Vector3.up * 2.5f;
        TextMeshPro tmp = go.AddComponent<TextMeshPro>();
        tmp.text = texto;
        tmp.fontSize = 6f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.rectTransform.sizeDelta = new Vector2(3f, 1f);
        Destroy(go, 1.3f);   // por si la lámpara desaparece antes de terminar

        Vector3 inicio = go.transform.position;
        const float duracion = 1.2f;
        for (float t = 0f; t < duracion && go != null; t += Time.deltaTime)
        {
            float k = t / duracion;
            go.transform.position = inicio + Vector3.up * k * 0.9f;
            Camera cam = Camera.main;
            if (cam != null) go.transform.rotation = cam.transform.rotation;
            tmp.alpha = 1f - k * k;
            yield return null;
        }
        if (go != null) Destroy(go);
    }

    // ---------- Aparecer / desaparecer ----------

    public void Desaparecer()
    {
        if (Desapareciendo) return;
        Desapareciendo = true;
        StopAllCoroutines();
        StartCoroutine(RutinaDesaparecer());
    }

    IEnumerator Aparecer()
    {
        ocupada = true;
        const float duracion = 0.5f;
        for (float t = 0f; t < duracion; t += Time.deltaTime)
        {
            transform.localScale = escalaOriginal * RebotarSalida(t / duracion);
            yield return null;
        }
        transform.localScale = escalaOriginal;
        ocupada = false;
    }

    IEnumerator RutinaDesaparecer()
    {
        if (luz != null) luz.enabled = false;
        if (brillos != null) brillos.Emit(25);
        Vector3 inicio = transform.localScale;
        const float duracion = 0.35f;
        for (float t = 0f; t < duracion; t += Time.deltaTime)
        {
            float k = t / duracion;
            transform.localScale = inicio * (1f - k * k);
            transform.Rotate(0f, 720f * Time.deltaTime, 0f);
            yield return null;
        }
        Destroy(gameObject);
    }

    IEnumerator RutinaBrillo(float segundos)
    {
        multiplicadorBrillo = 3f;
        brillos.Emit(60);
        yield return new WaitForSeconds(segundos);
        multiplicadorBrillo = 1f;
    }

    IEnumerator RutinaSalto()
    {
        ocupada = true;
        Vector3 inicio = transform.position;
        Quaternion giroInicial = transform.rotation;
        const float duracion = 0.7f;
        for (float t = 0f; t < duracion; t += Time.deltaTime)
        {
            float k = t / duracion;
            transform.position = inicio + Vector3.up * (4f * k * (1f - k)) * 1.2f;
            transform.rotation = giroInicial * Quaternion.Euler(0f, 360f * k, 0f);
            yield return null;
        }
        transform.position = inicio;
        transform.rotation = giroInicial;
        brillos.Emit(40);
        ocupada = false;
    }

    static float RebotarSalida(float x)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float a = x - 1f;
        return 1f + c3 * a * a * a + c1 * a * a;
    }

    // ---------- Creación de partes ----------

    void AplicarColor(float emision = 0.6f)
    {
        if (cubo == null) return;
        cubo.GetPropertyBlock(bloque);
        bloque.SetColor("_BaseColor", new Color(ColorGema.r, ColorGema.g, ColorGema.b, 0.6f));
        bloque.SetColor("_EmissionColor", ColorGema * emision);
        cubo.SetPropertyBlock(bloque);
    }

    // Caja que cubre toda la lámpara para poder hacerle clic.
    void CrearCollider()
    {
        if (GetComponent<Collider>() != null) return;
        Renderer[] renders = GetComponentsInChildren<Renderer>();
        if (renders.Length == 0) return;

        Bounds b = renders[0].bounds;
        foreach (Renderer r in renders) b.Encapsulate(r.bounds);

        BoxCollider caja = gameObject.AddComponent<BoxCollider>();
        Vector3 escala = transform.lossyScale;
        caja.center = transform.InverseTransformPoint(b.center);
        caja.size = new Vector3(b.size.x / escala.x, b.size.y / escala.y, b.size.z / escala.z);
    }

    void CrearLuz()
    {
        Transform padre = cabeza != null ? cabeza : transform;
        Transform vieja = padre.Find("LuzLampara");
        GameObject go = vieja != null ? vieja.gameObject : new GameObject("LuzLampara");
        go.transform.SetParent(padre, false);

        luz = go.GetComponent<Light>();
        if (luz == null) luz = go.AddComponent<Light>();
        luz.type = LightType.Point;
        luz.range = RangoLuz;
        luz.intensity = IntensidadLuz;
        luz.shadows = LightShadows.None;
        luz.enabled = false;
    }

    void CrearBrillos()
    {
        Transform padre = cabeza != null ? cabeza : transform;
        GameObject go = new GameObject("Brillos");
        go.transform.SetParent(padre, false);

        brillos = NuevoSistema(go, 300);
        ParticleSystem.MainModule main = brillos.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 1.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.16f);
        main.gravityModifier = -0.05f;
        main.startColor = new ParticleSystem.MinMaxGradient(ColorGema, Color.white);

        ParticleSystem.EmissionModule emision = brillos.emission;
        emision.rateOverTime = 0f;
        brillos.Play();
    }

    // Sistema de partículas con brillo suave, transparente y que se desvanece.
    static ParticleSystem NuevoSistema(GameObject go, int maximo)
    {
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = ps.main;
        main.duration = 1f;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = maximo;

        ParticleSystem.EmissionModule emision = ps.emission;
        emision.rateOverTime = 0f;

        ParticleSystem.ShapeModule forma = ps.shape;
        forma.shapeType = ParticleSystemShapeType.Sphere;
        forma.radius = 0.35f;

        ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
        color.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0f, 1f) });
        color.color = g;

        ParticleSystem.SizeOverLifetimeModule tam = ps.sizeOverLifetime;
        tam.enabled = true;
        tam.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));

        ParticleSystemRenderer render = go.GetComponent<ParticleSystemRenderer>();
        render.material = MaterialBrillo();
        return ps;
    }

    static Material MaterialBrillo()
    {
        if (materialBrillo == null) materialBrillo = Resources.Load<Material>("BrilloParticula");
        if (materialBrillo == null)
        {
            Shader s = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (s != null) materialBrillo = new Material(s);
        }
        return materialBrillo;
    }

    Transform BuscarHijo(string nombre)
    {
        foreach (Transform t in GetComponentsInChildren<Transform>(true))
            if (t.name == nombre) return t;
        return null;
    }
}
