using UnityEngine;

namespace OwariNakiTobira
{
    [CreateAssetMenu(menuName = "OwariNakiTobira/Windowing/Window Layout Definition", fileName = "WindowLayoutDefinition")]
    public sealed class WindowLayoutDefinition : ScriptableObject
    {
        [SerializeField] private Vector2 spawnPosition;
        [SerializeField] private Vector2 size = new Vector2(300f, 180f);
        [SerializeField] private int initialFocusOrder;
        [SerializeField] private bool draggingEnabled = true;
        [SerializeField] private string associatedPuzzleRuleId;
        [SerializeField] private ProjectedWindowBarrierRule associatedPuzzleRule;

        public Vector2 SpawnPosition => spawnPosition;
        public Vector2 Size => size;
        public int InitialFocusOrder => initialFocusOrder;
        public bool DraggingEnabled => draggingEnabled;
        public string AssociatedPuzzleRuleId => associatedPuzzleRuleId;
        public ProjectedWindowBarrierRule AssociatedPuzzleRule => associatedPuzzleRule;

        public void Configure(Vector2 position, Vector2 windowSize, int focusOrder, bool draggable, string puzzleRuleId)
        {
            spawnPosition = position;
            size = new Vector2(Mathf.Max(1f, windowSize.x), Mathf.Max(1f, windowSize.y));
            initialFocusOrder = focusOrder;
            draggingEnabled = draggable;
            associatedPuzzleRuleId = puzzleRuleId ?? string.Empty;
        }
    }
}
