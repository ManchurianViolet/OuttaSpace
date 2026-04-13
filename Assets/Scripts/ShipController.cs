using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ShipController : MonoBehaviour
{
    [Header("Bobbing")]
    public float bobAmplitude = 0.15f;
    public float bobSpeed = 1.2f;

    [Header("Engine Flame")]
    public ParticleSystem engineFlame;

    private Vector3 basePosition;
    private SpriteRenderer sr;

    void Start()
    {
        basePosition = transform.localPosition;
        sr = GetComponent<SpriteRenderer>();
        sr.drawMode = SpriteDrawMode.Simple;

        if (engineFlame == null)
            CreateEngineParticle();
    }

    void Update()
    {
        float bob = Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
        transform.localPosition = basePosition + Vector3.up * bob;

        if (engineFlame != null && GameManager.Instance != null)
        {
            var emission = engineFlame.emission;
            float speed = (float)GameManager.Instance.GetTotalSpeed();
            float normalized = Mathf.Clamp01(Mathf.Log10(Mathf.Max(speed, 1f)) / 5f);
            emission.rateOverTime = Mathf.Lerp(5f, 40f, normalized);

            var main = engineFlame.main;
            main.startSpeed = Mathf.Lerp(0.5f, 3f, normalized);
        }
    }

    public void ResetBasePosition(Vector3 newPos)
    {
        basePosition = newPos;
        transform.localPosition = newPos;
    }

    void CreateEngineParticle()
    {
        GameObject particleObj = new GameObject("EngineFlame");
        particleObj.transform.SetParent(transform);
        particleObj.transform.localPosition = new Vector3(0, -0.6f, 0);

        engineFlame = particleObj.AddComponent<ParticleSystem>();
        var main = engineFlame.main;
        main.startLifetime = 0.3f;
        main.startSpeed = 1.5f;
        main.startSize = 0.08f;
        main.maxParticles = 50;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0;

        var colorOverLife = engineFlame.colorOverLifetime;
        colorOverLife.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 0.7f, 0.2f), 0f),
                new GradientColorKey(new Color(1f, 0.3f, 0.1f), 0.5f),
                new GradientColorKey(new Color(0.8f, 0.1f, 0f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.8f, 0f),
                new GradientAlphaKey(0.5f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLife.color = grad;

        var shape = engineFlame.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 15f;
        shape.radius = 0.05f;
        shape.rotation = new Vector3(0, 0, 180);

        var emission = engineFlame.emission;
        emission.rateOverTime = 15f;

        var sizeOverLife = engineFlame.sizeOverLifetime;
        sizeOverLife.enabled = true;
        sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0));

        var renderer = particleObj.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = sr.sortingOrder - 1;
        renderer.material = new Material(Shader.Find("Sprites/Default"));
    }
}
