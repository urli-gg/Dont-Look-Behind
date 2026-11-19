using TMPro;
using UnityEngine;

public class LookBudgetDisplay : MonoBehaviour
{
    public Creature creature;
    public TMP_Text label;
    private int lastRemaining = -1;

    private void Update()
    {
        int remaining = creature.gameObject.activeInHierarchy ? creature.TurnsRemaining : creature.maxTurns;
        if (remaining == lastRemaining)
            return;
        lastRemaining = remaining;
        label.text = $"MIRADAS   {remaining}/{creature.maxTurns}";
        label.color = remaining == 0 ? new Color(0.96f, 0.45f, 0.39f) : new Color(0.87f, 0.77f, 0.59f);
    }
}