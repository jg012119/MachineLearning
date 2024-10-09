using UnityEngine;

public class Cell : MonoBehaviour
{
    public int indiceExperiencia;
    public int idCelula;
    private GameController gameController;
    private bool fueDestruidaPorJugador = false;
    private float tiempoNacimiento;

    void Start()
    {
        // Obtener referencia al GameController
        gameController = GameObject.Find("GameController").GetComponent<GameController>();
        tiempoNacimiento = Time.time;
    }

    void OnMouseDown()
    {
        // Marcar que fue destruida por el jugador
        fueDestruidaPorJugador = true;

        // Mostrar informaci�n en la consola
        Debug.Log($"C�lula {idCelula} eliminada por el jugador.");

        // Destruir la c�lula
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        float tiempoVida = Time.time - tiempoNacimiento;

        if (gameController != null && indiceExperiencia >= 0 && indiceExperiencia < gameController.currentExperiences.Count)
        {
            var experiencia = gameController.currentExperiences[indiceExperiencia];
            experiencia.timeLife = tiempoVida;

            if (fueDestruidaPorJugador)
            {
                experiencia.survived = false;

                // Registrar esta c�lula como la �ltima eliminada
                gameController.RegisterLastCellDeleted(experiencia);
            }
            else
            {
                experiencia.survived = true;
            }
        }
        gameController.UpdateCounter();
    }
}
