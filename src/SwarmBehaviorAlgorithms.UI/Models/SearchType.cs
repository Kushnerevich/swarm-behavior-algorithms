using System.ComponentModel;

namespace SwarmBehaviorAlgorithms.UI.Models
{
    public enum SearchType
    {
        [Description("Алгоритм бактериального поиска")]
        Bacterial = 1,
        [Description("Алгоритм поиска косяком рыб")]
        FishSchool = 2
    }
}
