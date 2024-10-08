using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GameController : MonoBehaviour
{
    public GameObject celulaPrefab;
    public int numeroDeCelulas = 15;
    public float intervaloTiempo = 10f;

    private int contadorGeneraciones = 0;
    private int contadorCelulas = 0;

    private List<GameObject> celulasExistentes = new List<GameObject>();

    [System.Serializable]
    public class ExperienciaCelula
    {
        public Color color;
        public float tama�o;
        public bool sobrevivio;
        public int generacion;
        public int idPadre;
        public float tiempoVida; // Tiempo que la c�lula estuvo viva
    }

    private List<ExperienciaCelula> experienciasPrevias = new List<ExperienciaCelula>();
    public List<ExperienciaCelula> experienciasActuales = new List<ExperienciaCelula>();
    private List<ExperienciaCelula> experienciasSupervivientes = new List<ExperienciaCelula>();

    // Referencia a la �ltima c�lula eliminada
    private ExperienciaCelula ultimaCelulaEliminada;

    // Variables para ajustar la tasa de mutaci�n
    private float variacionInicial = 0.5f; // Mutaci�n inicial alta
    private float variacionMinima = 0.01f; // Mutaci�n m�nima
    private float variacionActual;

    void Start()
    {
        variacionActual = variacionInicial;
        GenerarCelulasIniciales();
        StartCoroutine(ControlarGeneraciones());
    }

    IEnumerator ControlarGeneraciones()
    {
        while (true)
        {
            // Esperar hasta el pr�ximo intervalo
            yield return new WaitForSeconds(intervaloTiempo);

            // Destruir c�lulas existentes
            foreach (GameObject celula in celulasExistentes)
            {
                Destroy(celula);
            }
            celulasExistentes.Clear();

            // Esperar un frame para asegurar que OnDestroy() se llama en las c�lulas
            yield return new WaitForEndOfFrame();

            // Preparar experiencias para la siguiente generaci�n
            experienciasPrevias.Clear();
            experienciasPrevias.AddRange(experienciasActuales);
            experienciasActuales.Clear();

            // Filtrar experiencias para obtener las c�lulas supervivientes
            experienciasSupervivientes.Clear();
            foreach (ExperienciaCelula experiencia in experienciasPrevias)
            {
                if (experiencia.sobrevivio)
                {
                    experienciasSupervivientes.Add(experiencia);
                }
            }

            // Reducir gradualmente la variaci�n de mutaci�n
            ActualizarVariacionMutacion();

            // Generar nuevas c�lulas
            GenerarNuevaGeneracion();
        }
    }

    void GenerarCelulasIniciales()
    {
        contadorGeneraciones = 1;
        for (int i = 0; i < numeroDeCelulas; i++)
        {
            // Generar atributos aleatorios
            Color colorAleatorio = new Color(Random.value, Random.value, Random.value);
            float tama�oAleatorio = Random.Range(0.5f, 1.5f);

            // Crear y configurar la nueva c�lula
            CrearNuevaCelula(colorAleatorio, tama�oAleatorio, -1);
        }
    }

    void GenerarNuevaGeneracion()
    {
        contadorGeneraciones++;

        if (experienciasSupervivientes.Count > 0)
        {
            // Si hay c�lulas supervivientes, usarlas como base para la nueva generaci�n
            for (int i = 0; i < numeroDeCelulas; i++)
            {
                // Seleccionar un padre basado en su tiempo de vida
                ExperienciaCelula experienciaPadre = SeleccionarPadrePorTiempoDeVida();

                // Aplicar mutaciones
                Color colorMutado = MutarColor(experienciaPadre.color, variacionActual);
                float tama�oMutado = MutarTama�o(experienciaPadre.tama�o, variacionActual);

                CrearNuevaCelula(colorMutado, tama�oMutado, experienciaPadre.idPadre);
            }

            // Resetear ultimaCelulaEliminada ya que tenemos supervivientes
            ultimaCelulaEliminada = null;
        }
        else
        {
            // Si no hay c�lulas supervivientes, usar la �ltima c�lula eliminada
            if (ultimaCelulaEliminada != null)
            {
                for (int i = 0; i < numeroDeCelulas; i++)
                {
                    Color colorMutado = MutarColor(ultimaCelulaEliminada.color, variacionActual);
                    float tama�oMutado = MutarTama�o(ultimaCelulaEliminada.tama�o, variacionActual);

                    CrearNuevaCelula(colorMutado, tama�oMutado, ultimaCelulaEliminada.idPadre);
                }
            }
            else
            {
                // Si no hay informaci�n, generar c�lulas aleatorias
                GenerarCelulasIniciales();
            }
        }
    }

    // M�todo para crear y configurar una nueva c�lula
    void CrearNuevaCelula(Color color, float tama�o, int idPadre)
    {
        // Instanciar la c�lula
        GameObject nuevaCelula = Instantiate(celulaPrefab);

        // Asignar posici�n aleatoria
        float x = Random.Range(-8f, 8f);
        float y = Random.Range(-4f, 4f);
        nuevaCelula.transform.position = new Vector2(x, y);

        // Asignar tama�o y color
        nuevaCelula.transform.localScale = new Vector3(tama�o, tama�o, 1);
        nuevaCelula.GetComponent<SpriteRenderer>().color = color;

        // A�adir a la lista de c�lulas existentes
        celulasExistentes.Add(nuevaCelula);

        // Registrar la experiencia inicial
        ExperienciaCelula nuevaExperiencia = new ExperienciaCelula();
        nuevaExperiencia.color = color;
        nuevaExperiencia.tama�o = tama�o;
        nuevaExperiencia.sobrevivio = false; // Por defecto, asumimos que no sobrevivir�
        nuevaExperiencia.generacion = contadorGeneraciones;
        nuevaExperiencia.idPadre = idPadre;
        nuevaExperiencia.tiempoVida = 0f; // Inicializar el tiempo de vida

        experienciasActuales.Add(nuevaExperiencia);

        // Asignar el �ndice de experiencia a la c�lula
        Cell scriptCelula = nuevaCelula.GetComponent<Cell>();
        scriptCelula.indiceExperiencia = experienciasActuales.Count - 1;

        // Asignar un identificador �nico a la c�lula
        scriptCelula.idCelula = contadorCelulas;
        contadorCelulas++;
    }

    // Funci�n para mutar el color con variaci�n adaptativa
    Color MutarColor(Color colorOriginal, float variacion)
    {
        float r = Mathf.Clamp01(colorOriginal.r + Random.Range(-variacion, variacion));
        float g = Mathf.Clamp01(colorOriginal.g + Random.Range(-variacion, variacion));
        float b = Mathf.Clamp01(colorOriginal.b + Random.Range(-variacion, variacion));
        return new Color(r, g, b);
    }

    // Funci�n para mutar el tama�o con variaci�n adaptativa
    float MutarTama�o(float tama�oOriginal, float variacion)
    {
        float tama�oMutado = tama�oOriginal + Random.Range(-variacion, variacion);
        return Mathf.Clamp(tama�oMutado, 0.5f, 1.5f);
    }

    // Seleccionar un padre basado en el tiempo de vida (m�s tiempo de vida = m�s probabilidad)
    ExperienciaCelula SeleccionarPadrePorTiempoDeVida()
    {
        float sumaTiempoVida = 0f;
        foreach (var exp in experienciasSupervivientes)
        {
            sumaTiempoVida += exp.tiempoVida;
        }

        float valorAleatorio = Random.Range(0f, sumaTiempoVida);
        float acumulado = 0f;

        foreach (var exp in experienciasSupervivientes)
        {
            acumulado += exp.tiempoVida;
            if (acumulado >= valorAleatorio)
            {
                return exp;
            }
        }

        // En caso de que no se haya retornado a�n, devolver el �ltimo
        return experienciasSupervivientes[experienciasSupervivientes.Count - 1];
    }

    // M�todo para registrar la �ltima c�lula eliminada
    public void RegistrarUltimaCelulaEliminada(ExperienciaCelula experiencia)
    {
        ultimaCelulaEliminada = experiencia;
    }

    // M�todo para reducir gradualmente la variaci�n de mutaci�n
    void ActualizarVariacionMutacion()
    {
        // Reducir la variaci�n en un porcentaje cada generaci�n
        float factorReduccion = 0.9f; // Reduce la variaci�n en un 10% cada generaci�n
        variacionActual *= factorReduccion;

        // Asegurar que la variaci�n no sea menor que la m�nima establecida
        variacionActual = Mathf.Max(variacionActual, variacionMinima);
    }
}
