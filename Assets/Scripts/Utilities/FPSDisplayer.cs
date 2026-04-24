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
    private int frames;

    private void Update()
    {
        if (timer >= 0.5)
        {
            text.text = $"FPS: {frames * 2}";
            timer = 0;
            frames = 0;
        }
        frames++;
        timer += Time.deltaTime;
    }
}
