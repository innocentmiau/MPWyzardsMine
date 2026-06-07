using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flasher : MonoBehaviour
{
    private Color       sourceColor = Color.white;
    private Color       flashColor = Color.white;
    private Material    material;

    private float timer;
    private float totalTime;

    void Start()
    {
        Renderer renderer = GetComponent<Renderer>();
        material = new Material(renderer.material);

        material.shader = Shader.Find("Shader Graphs/FlashShader");
        renderer.material = material;

        SpriteRenderer spriteRenderer = renderer as SpriteRenderer;
        if (spriteRenderer != null)
        {
            sourceColor = spriteRenderer.color;

            // Set on hierarchy
            var srs = GetComponentsInChildren<SpriteRenderer>();
            foreach (var sr in srs)
            {
                sr.material = material;
            }
        }

    }

    void Update()
    {
        if (timer < totalTime)
        {
            timer = Mathf.Min(timer + Time.deltaTime, totalTime);

            float t = timer / totalTime;
            Color c = Color.Lerp(flashColor, sourceColor, t);
            c.a = 1 - t;

            material.SetColor("_FlashColor", c);
        }
    }

    public void Flash(Color c, float time)
    {
        totalTime = time;
        timer = 0.0f;
        flashColor = c;
    }
}
