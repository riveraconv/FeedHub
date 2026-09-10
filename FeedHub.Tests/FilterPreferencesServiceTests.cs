using FeedHub_Core.Interfaces;
using FeedHub_Core.Services;

namespace FeedHub.Tests;

public class FilterPreferencesServiceTests
{
    public class TestPreferencesService : IPreferencesService
    {
        private readonly Dictionary<string, string> _values = new();

        //Minimum Implementation for IPreferencesService for Get/Set management

        public string Get(string key, string defaultValue)
        {
            return _values.TryGetValue(key, out var value)
                ? value
                : defaultValue;
        }

        public void Set(string key, string value)
        {
            _values[key] = value;
        }
    }
    [Fact]
    public void IsSourceActive_ReturnsTrueWhenNoDisabledSourcesAreStored()
    {
        // ARRANGE

        var prefs = new TestPreferencesService();

        var service = new FilterPreferencesService(prefs);

        // ACT

        var result = service.IsSourceActive("elpais");

        // ASSERT

        Assert.True(result);

        // Verifies that a source is active when no disabled sources are stored.
    }
    [Fact]
public void IsSourceActive_ReturnsFalseWhenSourceIsDisabled()
{
    // ARRANGE

    var prefs = new TestPreferencesService();

    prefs.Set(
        "disabled_sources",
        """["elpais", "bbc"]""");

    var service = new FilterPreferencesService(prefs);

    // ACT

    var result = service.IsSourceActive("elpais");

    // ASSERT

    Assert.False(result);

    // Verifies that a source is inactive when it is present in the stored disabled sources.
}
[Fact]
public void IsSourceActive_ReturnsTrueWhenStoredSourcesDeserializeToNull()
{
    // ARRANGE

    var prefs = new TestPreferencesService();

    prefs.Set(
        "disabled_sources",
        "null");

    var service = new FilterPreferencesService(prefs);

    // ACT

    var result = service.IsSourceActive("elpais");

    // ASSERT

    Assert.True(result);

    // Verifies that a null deserialized collection is replaced with an empty set.
}
[Fact]
public void SetSourceActive_DisablesSourceWhenActiveIsFalse()
{
    // ARRANGE

    var prefs = new TestPreferencesService();

    var service = new FilterPreferencesService(prefs);

    // ACT

    service.SetSourceActive("elpais", false);

    // ASSERT

    var result = service.IsSourceActive("elpais");

    Assert.False(result);

    // Verifies that a source is added to the disabled sources when active is false.
}
[Fact]
public void SetSourceActive_EnablesSourceWhenActiveIsTrue()
{
    // ARRANGE

    var prefs = new TestPreferencesService();

    prefs.Set(
        "disabled_sources",
        """["elpais", "bbc"]""");

    var service = new FilterPreferencesService(prefs);

    // ACT

    service.SetSourceActive("elpais", true);

    // ASSERT

    var result = service.IsSourceActive("elpais");

    Assert.True(result);

    // Verifies that a source is removed from the disabled sources when active is true.
}
[Fact]
public void IsCategoryActive_ReturnsTrueWhenNoDisabledCategoriesAreStored()
{
    // ARRANGE

    var prefs = new TestPreferencesService();

    var service = new FilterPreferencesService(prefs);

    // ACT

    var result = service.IsCategoryActive("Tecnología");

    // ASSERT

    Assert.True(result);

    // Verifies that a category is active when no disabled categories are stored.
}
[Fact]
public void IsCategoryActive_ReturnsFalseWhenCategoryIsDisabled()
{
    // ARRANGE

    var prefs = new TestPreferencesService();

    prefs.Set(
        "disabled_categories",
        """["Tecnología", "Deportes"]""");

    var service = new FilterPreferencesService(prefs);

    // ACT

    var result = service.IsCategoryActive("Tecnología");

    // ASSERT

    Assert.False(result);

    // Verifies that a category is inactive when it is present in the stored disabled categories.
}
[Fact]
public void SetCategoryActive_DisablesCategoryWhenActiveIsFalse()
{
    // ARRANGE

    var prefs = new TestPreferencesService();

    var service = new FilterPreferencesService(prefs);

    // ACT

    service.SetCategoryActive("Tecnología", false);

    // ASSERT

    var result = service.IsCategoryActive("Tecnología");

    Assert.False(result);

    // Verifies that a category is added to the disabled categories when active is false.
}
[Fact]
public void SetCategoryActive_EnablesCategoryWhenActiveIsTrue()
{
    // ARRANGE

    var prefs = new TestPreferencesService();

    prefs.Set(
        "disabled_categories",
        """["Tecnología", "Deportes"]""");

    var service = new FilterPreferencesService(prefs);

    // ACT

    service.SetCategoryActive("Tecnología", true);

    // ASSERT

    var result = service.IsCategoryActive("Tecnología");

    Assert.True(result);

    // Verifies that a category is removed from the disabled categories when active is true.
}
}


