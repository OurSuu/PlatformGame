using UnityEngine;
using UnityEngine.UI;

public class StaminaUI : MonoBehaviour
{
    public Image staminaFill;
    public PlayerController player; // หรือ script stamina ของคุณ

    void Update()
    {
        staminaFill.fillAmount =
            player.currentStamina / player.maxStamina;
    }
}
