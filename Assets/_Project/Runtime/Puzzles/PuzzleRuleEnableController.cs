using UnityEngine;

namespace OwariNakiTobira
{
    [DisallowMultipleComponent]
    public sealed class PuzzleRuleEnableController : MonoBehaviour
    {
        [SerializeField] private CoverToEraseRule[] rules = System.Array.Empty<CoverToEraseRule>();

        public void SetRulesEnabled(bool enabled)
        {
            for (int i = 0; i < rules.Length; i++)
            {
                if (rules[i] != null)
                {
                    rules[i].SetRuleEnabled(enabled);
                }
            }
        }

        public void EnableRules()
        {
            SetRulesEnabled(true);
        }

        public void DisableRules()
        {
            SetRulesEnabled(false);
        }

        public void EnableRule(CoverToEraseRule rule)
        {
            if (rule != null)
            {
                rule.SetRuleEnabled(true);
            }
        }

        public void DisableRule(CoverToEraseRule rule)
        {
            if (rule != null)
            {
                rule.SetRuleEnabled(false);
            }
        }
    }
}
