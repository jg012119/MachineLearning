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
        public float tamaño;
        public bool sobrevivio;
        public int generacion;
        public int idPadre;
        public float tiempoVida; // Tiempo que la célula estuvo viva
    }

    private List<ExperienciaCelula> experienciasPrevias = new List<ExperienciaCelula>();
    public List<ExperienciaCelula> experienciasActuales = new List<ExperienciaCelula>();
    private List<ExperienciaCelula> experienciasSupervivientes = new List<ExperienciaCelula>();

    // Referencia a la última célula eliminada
    private ExperienciaCelula ultimaCelulaEliminada;

    // Variables para ajustar la tasa de mutación
    private float variacionInicial = 0.5f; // Mutación inicial alta
    private float variacionMinima = 0.01f; // Mutación mínima
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
            // Esperar hasta el próximo intervalo
            yield return new WaitForSeconds(intervaloTiempo);

            // Destruir células existentes
            foreach (GameObject celula in celulasExistentes)
            {
                Destroy(celula);
            }
            celulasExistentes.Clear();

            // Esperar un frame para asegurar que OnDestroy() se llama en las células
            yield return new WaitForEndOfFrame();

            // Preparar experiencias para la siguiente generación
            experienciasPrevias.Clear();
            experienciasPrevias.AddRange(experienciasActuales);
            experienciasActuales.Clear();

            // Filtrar experiencias para obtener las células supervivientes
            experienciasSupervivientes.Clear();
            foreach (ExperienciaCelula experiencia in experienciasPrevias)
            {
                if (experiencia.sobrevivio)
                {
                    experienciasSupervivientes.Add(experiencia);
                }
            }

            // Reducir gradualmente la variación de mutación
            ActualizarVariacionMutacion();

            // Generar nuevas células
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
            float tamañoAleatorio = Random.Range(0.5f, 1.5f);

            // Crear y configurar la nueva célula
            CrearNuevaCelula(colorAleatorio, tamañoAleatorio, -1);
        }
    }

    void GenerarNuevaGeneracion()
    {
        contadorGeneraciones++;

        if (experienciasSupervivientes.Count > 0)
        {
            // Si hay células supervivientes, usarlas como base para la nueva generación
            for (int i = 0; i < numeroDeCelulas; i++)
            {
                // Seleccionar un padre basado en su tiempo de vida
                ExperienciaCelula experienciaPadre = SeleccionarPadrePorTiempoDeVida();

                // Aplicar mutaciones
                Color colorMutado = MutarColor(experienciaPadre.color, variacionActual);
                float tamañoMutado = MutarTamaño(experienciaPadre.tamaño, variacionActual);

                CrearNuevaCelula(colorMutado, tamañoMutado, experienciaPadre.idPadre);
            }

            // Resetear ultimaCelulaEliminada ya que tenemos supervivientes
            ultimaCelulaEliminada = null;
        }
        else
        {
            // Si no hay células supervivientes, usar la última célula eliminada
            if (ultimaCelulaEliminada != null)
            {
                for (int i = 0; i < numeroDeCelulas; i++)
                {
                    Color colorMutado = MutarColor(ultimaCelulaEliminada.color, variacionActual);
                    float tamañoMutado = MutarTamaño(ultimaCelulaEliminada.tamaño, variacionActual);

                    CrearNuevaCelula(colorMutado, tamañoMutado, ultimaCelulaEliminada.idPadre);
                }
            }
            else
            {
                // Si no hay información, generar células aleatorias
                GenerarCelulasIniciales();
            }
        }
    }

    // Método para crear y configurar una nueva célula
    void CrearNuevaCelula(Color color, float tamaño, int idPadre)
    {
        // Instanciar la célula
        GameObject nuevaCelula = Instantiate(celulaPrefab);

        // Asignar posición aleatoria
        float x = Random.Range(-8f, 8f);
        float y = Random.Range(-4f, 4f);
        nuevaCelula.transform.position = new Vector2(x, y);

        // Asignar tamaño y color
        nuevaCelula.transform.localScale = new Vector3(tamaño, tamaño, 1);
        nuevaCelula.GetComponent<SpriteRenderer>().color = color;

        // Añadir a la lista de células existentes
        celulasExistentes.Add(nuevaCelula);

        // Registrar la experiencia inicial
        ExperienciaCelula nuevaExperiencia = new ExperienciaCelula();
        nuevaExperiencia.color = color;
        nuevaExperiencia.tamaño = tamaño;
        nuevaExperiencia.sobrevivio = false; // Por defecto, asumimos que no sobrevivirá
        nuevaExperiencia.generacion = contadorGeneraciones;
        nuevaExperiencia.idPadre = idPadre;
        nuevaExperiencia.tiempoVida = 0f; // Inicializar el tiempo de vida

        experienciasActuales.Add(nuevaExperiencia);

        // Asignar el índice de experiencia a la célula
        Cell scriptCelula = nuevaCelula.GetComponent<Cell>();
        scriptCelula.indiceExperiencia = experienciasActuales.Count - 1;

        // Asignar un identificador único a la célula
        scriptCelula.idCelula = contadorCelulas;
        contadorCelulas++;
    }

    // Función para mutar el color con variación adaptativa
    Color MutarColor(Color colorOriginal, float variacion)
    {
        float r = Mathf.Clamp01(colorOriginal.r + Random.Range(-variacion, variacion));
        float g = Mathf.Clamp01(colorOriginal.g + Random.Range(-variacion, variacion));
        float b = Mathf.Clamp01(colorOriginal.b + Random.Range(-variacion, variacion));
        return new Color(r, g, b);
    }

    // Función para mutar el tamaño con variación adaptativa
    float MutarTamaño(float tamañoOriginal, float variacion)
    {
        float tamañoMutado = tamañoOriginal + Random.Range(-variacion, variacion);
        return Mathf.Clamp(tamañoMutado, 0.5f, 1.5f);
    }

    // Seleccionar un padre basado en el tiempo de vida (más tiempo de vida = más probabilidad)
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

        // En caso de que no se haya retornado aún, devolver el último
        return experienciasSupervivientes[experienciasSupervivientes.Count - 1];
    }

    // Método para registrar la última célula eliminada
    public void RegistrarUltimaCelulaEliminada(ExperienciaCelula experiencia)
    {
        ultimaCelulaEliminada = experiencia;
    }

    // Método para reducir gradualmente la variación de mutación
    void ActualizarVariacionMutacion()
    {
        // Reducir la variación en un porcentaje cada generación
        float factorReduccion = 0.9f; // Reduce la variación en un 10% cada generación
        variacionActual *= factorReduccion;

        // Asegurar que la variación no sea menor que la mínima establecida
        variacionActual = Mathf.Max(variacionActual, variacionMinima);
    }
}
