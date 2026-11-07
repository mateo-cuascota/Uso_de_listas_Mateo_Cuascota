using UnityEngine;

// Hace girar la cámara lentamente alrededor del punto del piso al que está mirando.
public class CamaraOrbita : MonoBehaviour
{
    [SerializeField] float velocidad = 6f;   // grados por segundo
    public bool Girando = true;

    Vector3 pivote;

    void Start()
    {
        Plane piso = new Plane(Vector3.up, Vector3.zero);
        Ray rayo = new Ray(transform.position, transform.forward);
        pivote = piso.Raycast(rayo, out float distancia) ? rayo.GetPoint(distancia) : Vector3.zero;
    }

    void LateUpdate()
    {
        if (Girando) transform.RotateAround(pivote, Vector3.up, velocidad * Time.deltaTime);
    }
}
