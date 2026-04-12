global using CastCategory = (string localizationKey, SpeedrunDisplay.DataStructures.Split completionSplit);

using System.Linq;

namespace SpeedrunDisplay.DataStructures;

public record class Category(string LocalizationKey, Split CompletionSplit)
{
    public static implicit operator CastCategory(Category category) => (category.LocalizationKey, category.CompletionSplit);

    public static implicit operator Category(CastCategory casted)
    {
        var validCategories = SpeedrunDisplay.AllCategories.Values.Where(c => c.LocalizationKey == casted.localizationKey);
        var matchedCategories = validCategories.Where(c => c.CompletionSplit == casted.completionSplit);
        return matchedCategories.FirstOrDefault(defaultValue: null);
    }
}