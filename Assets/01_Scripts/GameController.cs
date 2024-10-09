using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class GameController : MonoBehaviour
{
    public GameObject celulaPrefab;
    public int numberofcells = 15;
    public float intervalTime = 10f;

    private int counterGenerations = 0;
    private int countercells = 0;

    private List<GameObject> existingcells = new List<GameObject>();

    public Text quantityCells;
    private int counterDestroyedCells = 0;

    public Text rounds;
    private int countRounds = 0;

    [System.Serializable]
    public class ExperienceCell
    {
        public Color color;
        public float size;
        public bool survived;
        public int generation;
        public int idFather;
        public float timeLife; // Tiempo que la célula estuvo viva
    }

    private List<ExperienceCell> previousExperiences = new List<ExperienceCell>();
    public List<ExperienceCell> currentExperiences = new List<ExperienceCell>();
    private List<ExperienceCell> experiencesSurvivors = new List<ExperienceCell>();

    // Referencia a la última célula eliminada
    private ExperienceCell lastCellDeleted;

    // Variables para ajustar la tasa de mutación
    private float initialvariation = 0.5f; // Mutación inicial alta
    private float minimumVariation = 0.01f; // Mutación mínima
    private float currentvariation;

    void Start()
    {
        currentvariation = initialvariation;
        GenerateInitialCells();
        StartCoroutine(ControlGenerations());
    }
    public void UpdateCounter()
    {
        // Incrementar el contador
        counterDestroyedCells++;

        // Verificar si el objeto 'cantidadText' aún existe antes de actualizar el texto
        if (quantityCells != null)
        {
            // Actualizar el texto con la cantidad de células destruidas
            quantityCells.text = "Células Eliminadas: " + counterDestroyedCells;
        }
    }

    IEnumerator ControlGenerations()
    {
        while (true)
        {
            // Esperar hasta el próximo intervalo
            yield return new WaitForSeconds(intervalTime);

            // Destruir células existentes
            foreach (GameObject celula in existingcells)
            {
                Destroy(celula);
            }
            existingcells.Clear();

            // Esperar un frame para asegurar que OnDestroy() se llama en las células
            yield return new WaitForEndOfFrame();

            // Preparar experiencias para la siguiente generación
            previousExperiences.Clear();
            previousExperiences.AddRange(currentExperiences);
            currentExperiences.Clear();

            // Filtrar experiencias para obtener las células supervivientes
            experiencesSurvivors.Clear();
            foreach (ExperienceCell experience in previousExperiences)
            {
                if (experience.survived)
                {
                    experiencesSurvivors.Add(experience);
                }
            }

            // Reducir gradualmente la variación de mutación
            UpdateVariationMutation();

            // Generar nuevas células
            GenerateNewGeneration();
        }
    }

    void GenerateInitialCells()
    {
        countRounds++;
        rounds.text = "Ronda: "+countRounds;
        counterGenerations = 1;
        for (int i = 0; i < numberofcells; i++)
        {
            // Generar atributos aleatorios
            Color colorAleatorio = new Color(Random.value, Random.value, Random.value);
            float tamañoAleatorio = Random.Range(0.5f, 1.5f);

            // Crear y configurar la nueva célula
            CrearNuevaCelula(colorAleatorio, tamañoAleatorio, -1);
        }
    }

    void GenerateNewGeneration()
    {
        counterGenerations++;

        if (experiencesSurvivors.Count > 0)
        {
            // Si hay células supervivientes, usarlas como base para la nueva generación
            for (int i = 0; i < numberofcells; i++)
            {
                // Seleccionar un padre basado en su tiempo de vida
                ExperienceCell experienciaPadre = SelectParentByLifetime();

                // Aplicar mutaciones
                Color colorMutado = MutateColor(experienciaPadre.color, currentvariation);
                float tamañoMutado = MutarTamaño(experienciaPadre.size, currentvariation);

                CrearNuevaCelula(colorMutado, tamañoMutado, experienciaPadre.idFather);
            }

            // Resetear ultimaCelulaEliminada ya que tenemos supervivientes
            lastCellDeleted = null;
        }
        else
        {
            // Si no hay células supervivientes, usar la última célula eliminada
            if (lastCellDeleted != null)
            {
                for (int i = 0; i < numberofcells; i++)
                {
                    Color colorMutado = MutateColor(lastCellDeleted.color, currentvariation);
                    float tamañoMutado = MutarTamaño(lastCellDeleted.size, currentvariation);

                    CrearNuevaCelula(colorMutado, tamañoMutado, lastCellDeleted.idFather);
                }
            }
            else
            {
                // Si no hay información, generar células aleatorias
                GenerateInitialCells();
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
        existingcells.Add(nuevaCelula);

        // Registrar la experiencia inicial
        ExperienceCell newExperience = new ExperienceCell();
        newExperience.color = color;
        newExperience.size = tamaño;
        newExperience.survived = false; // Por defecto, asumimos que no sobrevivirá
        newExperience.generation = counterGenerations;
        newExperience.idFather = idPadre;
        newExperience.timeLife = 0f; // Inicializar el tiempo de vida

        currentExperiences.Add(newExperience);

        // Asignar el índice de experiencia a la célula
        Cell scriptCelula = nuevaCelula.GetComponent<Cell>();
        scriptCelula.indiceExperiencia = currentExperiences.Count - 1;

        // Asignar un identificador único a la célula
        scriptCelula.idCelula = countercells;
        countercells++;
    }

    // Función para mutar el color con variación adaptativa
    Color MutateColor(Color colorOriginal, float variacion)
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
    ExperienceCell SelectParentByLifetime()
    {
        float sumaTiempoVida = 0f;
        foreach (var exp in experiencesSurvivors)
        {
            sumaTiempoVida += exp.timeLife;
        }

        float valorAleatorio = Random.Range(0f, sumaTiempoVida);
        float acumulado = 0f;

        foreach (var exp in experiencesSurvivors)
        {
            acumulado += exp.timeLife;
            if (acumulado >= valorAleatorio)
            {
                return exp;
            }
        }

        // En caso de que no se haya retornado aún, devolver el último
        return experiencesSurvivors[experiencesSurvivors.Count - 1];
    }

    // Método para registrar la última célula eliminada
    public void RegisterLastCellDeleted(ExperienceCell experiencia)
    {
        lastCellDeleted = experiencia;
    }

    // Método para reducir gradualmente la variación de mutación
    void UpdateVariationMutation()
    {
        // Reducir la variación en un porcentaje cada generación
        float factorReduccion = 0.9f; // Reduce la variación en un 10% cada generación
        currentvariation *= factorReduccion;

        // Asegurar que la variación no sea menor que la mínima establecida
        currentvariation = Mathf.Max(currentvariation, minimumVariation);
    }
}
