using UnityEngine;

namespace OwariNakiTobira
{
    [CreateAssetMenu(menuName = "OwariNakiTobira/Levels/Level Definition", fileName = "LevelDefinition")]
    public sealed class LevelDefinition : ScriptableObject
    {
        [SerializeField] private string sceneName;
        [SerializeField] private string displayName = "Level";
        [SerializeField] private GameFlowState flowState = GameFlowState.Level01;
        [SerializeField] private StorySequence openingSequence;
        [SerializeField] private LevelDefinition nextLevel;

        public string SceneName => sceneName;
        public string DisplayName => displayName;
        public GameFlowState FlowState => flowState;
        public StorySequence OpeningSequence => openingSequence;
        public LevelDefinition NextLevel => nextLevel;

        public void Configure(string scene, string display, GameFlowState state, StorySequence opening, LevelDefinition next)
        {
            sceneName = scene;
            displayName = string.IsNullOrWhiteSpace(display) ? scene : display;
            flowState = state;
            openingSequence = opening;
            nextLevel = next;
        }
    }
}
