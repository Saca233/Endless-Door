using System;
using UnityEngine;

namespace OwariNakiTobira
{
    [DisallowMultipleComponent]
    public sealed class StackedCityWindowView : MonoBehaviour, IRuntimeResettable
    {
        [SerializeField] private DesktopWindowController windowController;
        [SerializeField] private RuntimeRenderTexture runtimeRenderTexture;
        [SerializeField] private GameWindowCamera cityCamera;
        [SerializeField] private GameWindowView cityWindowView;
        [SerializeField] private AdditiveLevelLoader levelLoader;
        [SerializeField] private LevelDefinition activeLevel;
        [SerializeField] private ErrorWindowPool errorWindowPool;
        [SerializeField] private WindowLayoutDefinition[] errorWindowLayouts = Array.Empty<WindowLayoutDefinition>();
        [SerializeField] private RuntimeResetService runtimeResetService;
        [SerializeField] private bool showWhenLevelLoads = true;
        [SerializeField] private int resetOrder = 25;

        public int ResetOrder => resetOrder;
        public bool Visible => windowController != null && windowController.Model != null && windowController.Model.Visible;
        public GameWindowView CityWindowView => cityWindowView;

        private void OnEnable()
        {
            runtimeResetService?.Register(this);
            SubscribeToLoader();

            if (showWhenLevelLoads && levelLoader != null && levelLoader.CurrentLevel == activeLevel)
            {
                Show();
            }
            else
            {
                Hide();
            }
        }

        private void OnDisable()
        {
            runtimeResetService?.Unregister(this);
            UnsubscribeFromLoader();
            Hide();
        }

        public void SetRuntimeServices(AdditiveLevelLoader loader, RuntimeResetService resetService)
        {
            UnsubscribeFromLoader();
            runtimeResetService?.Unregister(this);
            levelLoader = loader;
            runtimeResetService = resetService;
            if (isActiveAndEnabled)
            {
                runtimeResetService?.Register(this);
                SubscribeToLoader();
            }
        }

        public void Show()
        {
            if (windowController != null && windowController.Model != null)
            {
                windowController.Model.SetVisible(true);
                windowController.ApplyModelToView();
            }

            runtimeRenderTexture?.EnsureRenderTexture();
            errorWindowPool?.ActivateLayouts(errorWindowLayouts);
        }

        public void Hide()
        {
            errorWindowPool?.ReturnAll();
            if (windowController != null && windowController.Model != null)
            {
                windowController.Model.SetVisible(false);
                windowController.ApplyModelToView();
            }
        }

        public void RuntimeReset()
        {
            Hide();
        }

        private void SubscribeToLoader()
        {
            if (levelLoader == null)
            {
                return;
            }

            levelLoader.LevelLoaded += OnLevelLoaded;
            levelLoader.LevelUnloaded += OnLevelUnloaded;
        }

        private void UnsubscribeFromLoader()
        {
            if (levelLoader == null)
            {
                return;
            }

            levelLoader.LevelLoaded -= OnLevelLoaded;
            levelLoader.LevelUnloaded -= OnLevelUnloaded;
        }

        private void OnLevelLoaded(LevelDefinition level, LevelEntryPoint entryPoint)
        {
            if (showWhenLevelLoads && level == activeLevel)
            {
                Show();
            }
            else
            {
                Hide();
            }
        }

        private void OnLevelUnloaded(UnityEngine.SceneManagement.Scene scene)
        {
            Hide();
        }
    }
}
