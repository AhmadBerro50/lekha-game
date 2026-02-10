using UnityEngine;
using Lekha.UI;
using Lekha.Animation;
using Lekha.Audio;
using Lekha.Effects;

namespace Lekha.GameLogic
{
    /// <summary>
    /// Automatically sets up the game when the scene loads.
    /// Add this to any scene and it will create all required game objects.
    /// </summary>
    [DefaultExecutionOrder(-100)] // Run before other scripts
    public class GameSetup : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoSetup()
        {
            // Create a setup object that will initialize everything
            GameObject setupObject = new GameObject("_GameSetup");
            setupObject.AddComponent<GameSetup>();
            DontDestroyOnLoad(setupObject);
        }

        private void Awake()
        {
            // Force landscape orientation only
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;

            // Check if GameManager already exists
            if (GameManager.Instance == null)
            {
                GameObject gmObject = new GameObject("GameManager");
                gmObject.AddComponent<GameManager>();
                DontDestroyOnLoad(gmObject);
                Debug.Log("GameManager created.");
            }

            // Create Premium Visuals (textures and sprites)
            if (PremiumVisuals.Instance == null)
            {
                GameObject visuals = new GameObject("PremiumVisuals");
                visuals.AddComponent<PremiumVisuals>();
                DontDestroyOnLoad(visuals);
                Debug.Log("PremiumVisuals created.");
            }

            // Create Modern UI Theme (2026 glassmorphism theme)
            if (ModernUITheme.Instance == null)
            {
                GameObject theme = new GameObject("ModernUITheme");
                theme.AddComponent<ModernUITheme>();
                DontDestroyOnLoad(theme);
                Debug.Log("ModernUITheme created.");
            }

            // Create Card Sprite Manager (loads HD card images)
            if (CardSpriteManager.Instance == null)
            {
                GameObject spriteManager = new GameObject("CardSpriteManager");
                spriteManager.AddComponent<CardSpriteManager>();
                DontDestroyOnLoad(spriteManager);
                Debug.Log("CardSpriteManager created.");
            }

            // Create Premium Card Generator
            if (PremiumCardGenerator.Instance == null)
            {
                GameObject cardGen = new GameObject("PremiumCardGenerator");
                cardGen.AddComponent<PremiumCardGenerator>();
                DontDestroyOnLoad(cardGen);
                Debug.Log("PremiumCardGenerator created.");
            }

            // Create Particle Effects
            if (ParticleEffects.Instance == null)
            {
                GameObject particles = new GameObject("ParticleEffects");
                particles.AddComponent<ParticleEffects>();
                DontDestroyOnLoad(particles);
                Debug.Log("ParticleEffects created.");
            }

            // Create CardAnimator
            if (CardAnimator.Instance == null)
            {
                GameObject animator = new GameObject("CardAnimator");
                animator.AddComponent<CardAnimator>();
                DontDestroyOnLoad(animator);
                Debug.Log("CardAnimator created.");
            }

            // Create SoundManager
            if (SoundManager.Instance == null)
            {
                GameObject soundManager = new GameObject("SoundManager");
                soundManager.AddComponent<SoundManager>();
                DontDestroyOnLoad(soundManager);
                Debug.Log("SoundManager created.");
            }

            // Create HapticManager
            if (HapticManager.Instance == null)
            {
                GameObject hapticManager = new GameObject("HapticManager");
                hapticManager.AddComponent<HapticManager>();
                DontDestroyOnLoad(hapticManager);
                Debug.Log("HapticManager created.");
            }

            // Create GameUI
            if (GameUI.Instance == null)
            {
                GameObject gameUI = new GameObject("GameUI");
                gameUI.AddComponent<GameUI>();
                DontDestroyOnLoad(gameUI);
                Debug.Log("GameUI created.");
            }

            // Create GameController (manages game flow)
            if (GameController.Instance == null)
            {
                GameObject controller = new GameObject("GameController");
                controller.AddComponent<GameController>();
                DontDestroyOnLoad(controller);
                Debug.Log("GameController created.");
            }

            // Create PauseMenu
            if (PauseMenu.Instance == null)
            {
                GameObject pauseMenu = new GameObject("PauseMenu");
                pauseMenu.AddComponent<PauseMenu>();
                DontDestroyOnLoad(pauseMenu);
                Debug.Log("PauseMenu created.");
            }
        }
    }
}
