using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ShipController : MonoBehaviour
{
    [Header("Bobbing")]
    public float bobAmplitude = 0.15f;
    public float bobSpeed = 1.2f;

    [Header("Boost Surge")]
    public float surgeDistance = 0.5f;
    public float surgeSpeed = 4f;
    private float currentSurge;

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
        bool boosting = BoosterSystem.Instance != null && BoosterSystem.Instance.isBoosting;

        // 부스트 중이면 앞으로 밀렸다가, 끝나면 복귀
        float targetSurge = boosting ? surgeDistance : 0f;
        currentSurge = Mathf.Lerp(currentSurge, targetSurge, surgeSpeed * Time.deltaTime);

        float bob = Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
        transform.localPosition = basePosition + Vector3.up * bob + Vector3.right * currentSurge;

        if (engineFlame != null && GameManager.Instance != null)
        {
            float speed = (float)GameManager.Instance.GetTotalSpeed();
            float normalized = Mathf.Clamp01(Mathf.Log10(Mathf.Max(speed, 1f)) / 5f);

            var emission = engineFlame.emission;
            var main = engineFlame.main;
            var colorOverLife = engineFlame.colorOverLifetime;

            if (boosting)
            {
                emission.rateOverTime = 60f;
                main.startSpeed = 4f;
                main.startSize = 0.17f;

                // 하늘색~파란 그라데이션
                main.startColor = new ParticleSystem.MinMaxGradient(
                    new Color(0.15f, 0.55f, 1f, 0.95f),
                    new Color(0.45f, 0.9f, 1f, 0.95f)
                );

                Gradient boostGrad = new Gradient();
                boostGrad.SetKeys(
                    new GradientColorKey[]
                    {
                        new GradientColorKey(new Color(0.7f, 0.95f, 1f), 0f),
                        new GradientColorKey(new Color(0.3f, 0.7f, 1f), 0.5f),
                        new GradientColorKey(new Color(0.1f, 0.35f, 1f), 1f)
                    },
                    new GradientAlphaKey[]
                    {
                        new GradientAlphaKey(0.9f, 0f),
                        new GradientAlphaKey(0.5f, 0.5f),
                        new GradientAlphaKey(0f, 1f)
                    }
                );

                colorOverLife.enabled = true;
                colorOverLife.color = new ParticleSystem.MinMaxGradient(boostGrad);
            }
            else
            {
                emission.rateOverTime = Mathf.Lerp(5f, 40f, normalized);
                main.startSpeed = Mathf.Lerp(0.5f, 3f, normalized);
                main.startSize = 0.16f;

                main.startColor = new ParticleSystem.MinMaxGradient(
                    new Color(1f, 0.5f, 0.1f, 0.8f),
                    new Color(1f, 0.7f, 0.2f, 0.8f)
                );

                Gradient normalGrad = new Gradient();
                normalGrad.SetKeys(
                    new GradientColorKey[]
                    {
                        new GradientColorKey(new Color(1f, 0.7f, 0.2f), 0f),
                        new GradientColorKey(new Color(1f, 0.3f, 0.1f), 0.5f),
                        new GradientColorKey(new Color(0.8f, 0.1f, 0f), 1f)
                    },
                    new GradientAlphaKey[]
                    {
                        new GradientAlphaKey(0.8f, 0f),
                        new GradientAlphaKey(0.5f, 0.5f),
                        new GradientAlphaKey(0f, 1f)
                    }
                );

                colorOverLife.enabled = true;
                colorOverLife.color = new ParticleSystem.MinMaxGradient(normalGrad);
            }
        }
    }

    public void ResetBasePosition(Vector3 newPos)
    {
        basePosition = newPos;
        transform.localPosition = newPos;
        currentSurge = 0f;
    }

    void CreateEngineParticle()
    {
        GameObject particleObj = new GameObject("EngineFlame");
        particleObj.transform.SetParent(transform);
        particleObj.transform.localPosition = new Vector3(-3.1f, -0.45f, 0);

        engineFlame = particleObj.AddComponent<ParticleSystem>();
        var main = engineFlame.main;
        main.startLifetime = 0.3f;
        main.startSpeed = 5f;
        main.startSize = 0.05f;
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
        shape.angle = 8f;
        shape.radius = 0.02f;
        shape.rotation = new Vector3(0, 0, 90);

        var velocity = engineFlame.velocityOverLifetime;
        velocity.enabled = true;
        velocity.x = -3f;
        velocity.y = 0f;
        velocity.z = 0f;

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