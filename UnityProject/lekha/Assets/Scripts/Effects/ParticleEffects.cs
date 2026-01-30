using UnityEngine;
using System.Collections;

namespace Lekha.Effects
{
    /// <summary>
    /// Premium particle effects for card game events
    /// </summary>
    public class ParticleEffects : MonoBehaviour
    {
        public static ParticleEffects Instance { get; private set; }

        private ParticleSystem cardPlayParticles;
        private ParticleSystem trickWinParticles;
        private ParticleSystem gameWinParticles;
        private ParticleSystem glowParticles;
        private ParticleSystem sparkleParticles;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            CreateParticleSystems();
        }

        private void CreateParticleSystems()
        {
            cardPlayParticles = CreateCardPlayEffect();
            trickWinParticles = CreateTrickWinEffect();
            gameWinParticles = CreateGameWinEffect();
            glowParticles = CreateGlowEffect();
            sparkleParticles = CreateSparkleEffect();

            Debug.Log("ParticleEffects: All particle systems created");
        }

        private ParticleSystem CreateCardPlayEffect()
        {
            GameObject obj = new GameObject("CardPlayParticles");
            obj.transform.SetParent(transform);

            ParticleSystem ps = obj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 0.5f;
            main.loop = false;
            main.startLifetime = 0.4f;
            main.startSpeed = 200f;
            main.startSize = 15f;
            main.maxParticles = 30;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] {
                new ParticleSystem.Burst(0f, 20)
            });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 30f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(1f, 0.9f, 0.5f), 0f),
                    new GradientColorKey(new Color(1f, 0.7f, 0.3f), 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = gradient;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 1f);
            sizeCurve.AddKey(1f, 0f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            // Renderer setup
            var renderer = obj.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = CreateParticleMaterial(Color.white);

            ps.Stop();
            return ps;
        }

        private ParticleSystem CreateTrickWinEffect()
        {
            GameObject obj = new GameObject("TrickWinParticles");
            obj.transform.SetParent(transform);

            ParticleSystem ps = obj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 1f;
            main.loop = false;
            main.startLifetime = 0.8f;
            main.startSpeed = 300f;
            main.startSize = 20f;
            main.maxParticles = 50;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0.5f;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] {
                new ParticleSystem.Burst(0f, 40)
            });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 45f;
            shape.radius = 20f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(1f, 0.85f, 0.3f), 0f),
                    new GradientColorKey(new Color(0.9f, 0.6f, 0.1f), 0.5f),
                    new GradientColorKey(new Color(0.8f, 0.4f, 0.1f), 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 0.5f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = gradient;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 0.5f);
            sizeCurve.AddKey(0.2f, 1f);
            sizeCurve.AddKey(1f, 0f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            var renderer = obj.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = CreateParticleMaterial(Color.white);

            ps.Stop();
            return ps;
        }

        private ParticleSystem CreateGameWinEffect()
        {
            GameObject obj = new GameObject("GameWinParticles");
            obj.transform.SetParent(transform);

            ParticleSystem ps = obj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 3f;
            main.loop = false;
            main.startLifetime = 2f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(100f, 400f);
            main.startSize = new ParticleSystem.MinMaxCurve(10f, 30f);
            main.maxParticles = 200;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = 0.3f;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] {
                new ParticleSystem.Burst(0f, 100),
                new ParticleSystem.Burst(0.5f, 50),
                new ParticleSystem.Burst(1f, 50)
            });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(800, 100, 1);

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(new Color(1f, 0.85f, 0.3f), 0.3f),
                    new GradientColorKey(new Color(1f, 0.5f, 0.2f), 0.7f),
                    new GradientColorKey(new Color(0.8f, 0.3f, 0.1f), 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 0.7f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = gradient;

            // Rotation
            var rotationOverLifetime = ps.rotationOverLifetime;
            rotationOverLifetime.enabled = true;
            rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(-180f, 180f);

            var renderer = obj.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = CreateParticleMaterial(Color.white);

            ps.Stop();
            return ps;
        }

        private ParticleSystem CreateGlowEffect()
        {
            GameObject obj = new GameObject("GlowParticles");
            obj.transform.SetParent(transform);

            ParticleSystem ps = obj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 1f;
            main.loop = true;
            main.startLifetime = 0.5f;
            main.startSpeed = 0f;
            main.startSize = 100f;
            main.maxParticles = 5;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 3;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 10f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(1f, 0.9f, 0.4f), 0f),
                    new GradientColorKey(new Color(1f, 0.8f, 0.3f), 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.3f, 0.5f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = gradient;

            var renderer = obj.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = CreateParticleMaterial(Color.white);
            renderer.sortingOrder = -1;

            ps.Stop();
            return ps;
        }

        private ParticleSystem CreateSparkleEffect()
        {
            GameObject obj = new GameObject("SparkleParticles");
            obj.transform.SetParent(transform);

            ParticleSystem ps = obj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 2f;
            main.loop = true;
            main.startLifetime = 0.6f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(20f, 50f);
            main.startSize = new ParticleSystem.MinMaxCurve(5f, 15f);
            main.maxParticles = 30;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 15;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Rectangle;
            shape.scale = new Vector3(200, 100, 1);

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(new Color(1f, 0.95f, 0.8f), 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.3f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLifetime.color = gradient;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 0f);
            sizeCurve.AddKey(0.3f, 1f);
            sizeCurve.AddKey(1f, 0f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            var renderer = obj.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = CreateParticleMaterial(Color.white);

            ps.Stop();
            return ps;
        }

        private Material CreateParticleMaterial(Color color)
        {
            // Create a simple additive particle material
            Material mat = new Material(Shader.Find("Particles/Standard Unlit"));
            mat.SetColor("_Color", color);
            mat.SetFloat("_Mode", 1); // Additive

            // Create a simple circle texture for particles
            Texture2D tex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            int center = 32;

            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    float alpha = Mathf.Clamp01(1 - dist / center);
                    alpha = alpha * alpha; // Softer falloff
                    tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }
            }
            tex.Apply();
            mat.mainTexture = tex;

            return mat;
        }

        // Public methods to play effects

        public void PlayCardPlay(Vector3 worldPosition)
        {
            if (cardPlayParticles != null)
            {
                cardPlayParticles.transform.position = worldPosition;
                cardPlayParticles.Play();
            }
        }

        public void PlayTrickWin(Vector3 worldPosition)
        {
            if (trickWinParticles != null)
            {
                trickWinParticles.transform.position = worldPosition;
                trickWinParticles.Play();
            }
        }

        public void PlayGameWin(Vector3 worldPosition)
        {
            if (gameWinParticles != null)
            {
                gameWinParticles.transform.position = worldPosition;
                gameWinParticles.Play();
            }
        }

        public void StartGlow(Vector3 worldPosition)
        {
            if (glowParticles != null)
            {
                glowParticles.transform.position = worldPosition;
                glowParticles.Play();
            }
        }

        public void StopGlow()
        {
            if (glowParticles != null)
            {
                glowParticles.Stop();
            }
        }

        public void StartSparkles(Vector3 worldPosition)
        {
            if (sparkleParticles != null)
            {
                sparkleParticles.transform.position = worldPosition;
                sparkleParticles.Play();
            }
        }

        public void StopSparkles()
        {
            if (sparkleParticles != null)
            {
                sparkleParticles.Stop();
            }
        }

        /// <summary>
        /// Play a burst of particles with custom color at position
        /// </summary>
        public void PlayBurst(Vector3 worldPosition, Color color, int count = 20)
        {
            StartCoroutine(PlayBurstCoroutine(worldPosition, color, count));
        }

        private IEnumerator PlayBurstCoroutine(Vector3 worldPosition, Color color, int count)
        {
            // Temporarily change color
            var main = cardPlayParticles.main;
            var colorModule = cardPlayParticles.colorOverLifetime;

            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(color, 0f),
                    new GradientColorKey(color * 0.7f, 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorModule.color = gradient;

            cardPlayParticles.transform.position = worldPosition;
            var emission = cardPlayParticles.emission;
            emission.SetBursts(new ParticleSystem.Burst[] {
                new ParticleSystem.Burst(0f, (short)count)
            });

            cardPlayParticles.Play();

            yield return new WaitForSeconds(0.5f);

            // Reset to default
            Gradient defaultGradient = new Gradient();
            defaultGradient.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(1f, 0.9f, 0.5f), 0f),
                    new GradientColorKey(new Color(1f, 0.7f, 0.3f), 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorModule.color = defaultGradient;
        }
    }
}
