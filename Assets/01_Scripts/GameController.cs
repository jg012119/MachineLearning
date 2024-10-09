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
        public float timeLife; // Tiempo que la c�lula estuvo viva
    }

    private List<ExperienceCell> previousExperiences = new List<ExperienceCell>();
    public List<ExperienceCell> currentExperiences = new List<ExperienceCell>();
    private List<ExperienceCell> experiencesSurvivors = new List<ExperienceCell>();

    // Referencia a la �ltima c�lula eliminada
    private ExperienceCell lastCellDeleted;

    // Variables para ajustar la tasa de mutaci�n
    private float initialvariation = 0.5f; // Mutaci�n inicial alta
    private float minimumVariation = 0.01f; // Mutaci�n m�nima
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

        // Verificar si el objeto 'cantidadText' a�n existe antes de actualizar el texto
        if (quantityCells != null)
        {
            // Actualizar el texto con la cantidad de c�lulas destruidas
            quantityCells.text = "C�lulas Eliminadas: " + counterDestroyedCells;
        }
    }

    IEnumerator ControlGenerations()
    {
        while (true)
        {
            // Esperar hasta el pr�ximo intervalo
            yield return new WaitForSeconds(intervalTime);

            // Destruir c�lulas existentes
            foreach (GameObject celula in existingcells)
            {
                Destroy(celula);
            }
            existingcells.Clear();

            // Esperar un frame para asegurar que OnDestroy() se llama en las c�lulas
            yield return new WaitForEndOfFrame();

            // Preparar experiencias para la siguiente generaci�n
            previousExperiences.Clear();
            previousExperiences.AddRange(currentExperiences);
            currentExperiences.Clear();

            // Filtrar experiencias para obtener las c�lulas supervivientes
            experiencesSurvivors.Clear();
            foreach (ExperienceCell experience in previousExperiences)
            {
                if (experience.survived)
                {
                    experiencesSurvivors.Add(experience);
                }
            }

            // Reducir gradualmente la variaci�n de mutaci�n
            UpdateVariationMutation();

            // Generar nuevas c�lulas
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
            float tama�oAleatorio = Random.Range(0.5f, 1.5f);

            // Crear y configurar la nueva c�lula
            CrearNuevaCelula(colorAleatorio, tama�oAleatorio, -1);
        }
    }

    void GenerateNewGeneration()
    {
        counterGenerations++;

        if (experiencesSurvivors.Count > 0)
        {
            // Si hay c�lulas supervivientes, usarlas como base para la nueva generaci�n
            for (int i = 0; i < numberofcells; i++)
            {
                // Seleccionar un padre basado en su tiempo de vida
                ExperienceCell experienciaPadre = SelectParentByLifetime();

                // Aplicar mutaciones
                Color colorMutado = MutateColor(experienciaPadre.color, currentvariation);
                float tama�oMutado = MutarTama�o(experienciaPadre.size, currentvariation);

                CrearNuevaCelula(colorMutado, tama�oMutado, experienciaPadre.idFather);
            }

            // Resetear ultimaCelulaEliminada ya que tenemos supervivientes
            lastCellDeleted = null;
        }
        else
        {
            // Si no hay c�lulas supervivientes, usar la �ltima c�lula eliminada
            if (lastCellDeleted != null)
            {
                for (int i = 0; i < numberofcells; i++)
                {
                    Color colorMutado = MutateColor(lastCellDeleted.color, currentvariation);
                    float tama�oMutado = MutarTama�o(lastCellDeleted.size, currentvariation);

                    CrearNuevaCelula(colorMutado, tama�oMutado, lastCellDeleted.idFather);
                }
            }
            else
            {
                // Si no hay informaci�n, generar c�lulas aleatorias
                GenerateInitialCells();
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
        existingcells.Add(nuevaCelula);

        // Registrar la experiencia inicial
        ExperienceCell newExperience = new ExperienceCell();
        newExperience.color = color;
        newExperience.size = tama�o;
        newExperience.survived = false; // Por defecto, asumimos que no sobrevivir�
        newExperience.generation = counterGenerations;
        newExperience.idFather = idPadre;
        newExperience.timeLife = 0f; // Inicializar el tiempo de vida

        currentExperiences.Add(newExperience);

        // Asignar el �ndice de experiencia a la c�lula
        Cell scriptCelula = nuevaCelula.GetComponent<Cell>();
        scriptCelula.indiceExperiencia = currentExperiences.Count - 1;

        // Asignar un identificador �nico a la c�lula
        scriptCelula.idCelula = countercells;
        countercells++;
    }

    // Funci�n para mutar el color con variaci�n adaptativa
    Color MutateColor(Color colorOriginal, float variacion)
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

        // En caso de que no se haya retornado a�n, devolver el �ltimo
        return experiencesSurvivors[experiencesSurvivors.Count - 1];
    }

    // M�todo para registrar la �ltima c�lula eliminada
    public void RegisterLastCellDeleted(ExperienceCell experiencia)
    {
        lastCellDeleted = experiencia;
    }

    // M�todo para reducir gradualmente la variaci�n de mutaci�n
    void UpdateVariationMutation()
    {
        // Reducir la variaci�n en un porcentaje cada generaci�n
        float factorReduccion = 0.9f; // Reduce la variaci�n en un 10% cada generaci�n
        currentvariation *= factorReduccion;

        // Asegurar que la variaci�n no sea menor que la m�nima establecida
        currentvariation = Mathf.Max(currentvariation, minimumVariation);
    }
}
