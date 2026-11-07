using System.Collections.Generic;
using UnityEngine;

// Carga los sonidos de Assets/Resources/Sonidos y los reproduce.
// Usa un Dictionary<string, AudioClip> como caché para no cargar dos veces el mismo sonido.
public static class Sonidos
{
    static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();
    static AudioSource fuente;

    public static AudioClip Cargar(string nombre)
    {
        if (!cache.TryGetValue(nombre, out AudioClip clip) || clip == null)
        {
            clip = Resources.Load<AudioClip>("Sonidos/" + nombre);
            cache[nombre] = clip;
        }
        return clip;
    }

    public static void Reproducir(string nombre, float volumen = 1f)
    {
        AudioClip clip = Cargar(nombre);
        if (clip == null) return;

        if (fuente == null)
        {
            GameObject go = new GameObject("Sonidos");
            fuente = go.AddComponent<AudioSource>();
            fuente.playOnAwake = false;
        }
        fuente.PlayOneShot(clip, volumen);
    }
}
