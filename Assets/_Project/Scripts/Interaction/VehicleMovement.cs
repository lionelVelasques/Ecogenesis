using UnityEngine;

public class AutoEnMovimiento : MonoBehaviour
{
    [Header("Configuración del recorrido")]
    public Transform puntoInicio;
    public Transform puntoFin;
    public float velocidad = 6f;

    [Header("Modelos de autos distintos")]
    public GameObject[] modelosAutos;

    void Start()
    {
        ReiniciarAuto();
    }

    void Update()
    {
        if (puntoInicio == null || puntoFin == null) return;

        transform.position = Vector3.MoveTowards(transform.position, puntoFin.position, velocidad * Time.deltaTime);

        if (Vector3.Distance(transform.position, puntoFin.position) < 0.2f)
        {
            ReiniciarAuto();
        }
    }

    void ReiniciarAuto()
    {
        if (puntoInicio != null)
        {
            transform.position = puntoInicio.position;
            if (puntoFin != null) transform.LookAt(puntoFin);
        }

        if (modelosAutos != null && modelosAutos.Length > 0)
        {
            int indiceAleatorio = Random.Range(0, modelosAutos.Length);
            for (int i = 0; i < modelosAutos.Length; i++)
            {
                if (modelosAutos[i] != null)
                {
                    modelosAutos[i].SetActive(i == indiceAleatorio);
                }
            }
        }
    }
}

