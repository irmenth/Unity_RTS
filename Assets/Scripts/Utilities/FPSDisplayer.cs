using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class FPSDisplayer : MonoBehaviour
{
    private TextMeshProUGUI text;

    private void Awake()
    {
        text = GetComponent<TextMeshProUGUI>();
    }

    private float timer;

    private void Update()
    {
        if (timer >= 0.2)
        {
            text.text = $"FPS: {1 / Time.deltaTime:0.0}";
            timer = 0;
        }
        timer += Time.deltaTime;
    }
}
