using UnityEngine;
using UnityEngine.UI;

public class HealthUI : MonoBehaviour
{
    public Image healthFill;
    public Health health;

    void Update()
    {
        if (healthFill != null && health != null)
            healthFill.fillAmount = health.currentHealth / health.maxHealth;
    }
}
