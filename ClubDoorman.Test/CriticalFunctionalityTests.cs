
namespace ClubDoorman.Test;

[TestFixture]
public class CriticalFunctionalityTests
{
    [Test]
    public void TextProcessor_NormalizeText_HandlesNullInput()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => TextProcessor.NormalizeText(null!));
    }

    [Test]
    public void TextProcessor_NormalizeText_HandlesEmptyString()
    {
        // Arrange
        var input = "";

        // Act
        var result = TextProcessor.NormalizeText(input);

        // Assert
        Assert.That(result, Is.EqualTo(""));
    }

    [Test]
    public void TextProcessor_NormalizeText_RemovesFormatting()
    {
        // Arrange
        var input = "\u2068g\u2068o\u2068 \u2068f\u2068a\u2068\u2068\u2068\u2068\u2068\u2068\u2068s\u2068t\u2068\ud83e\udd71";

        // Act
        var result = TextProcessor.NormalizeText(input);

        // Assert
        Assert.That(result, Has.Length.LessThan(70));
        Assert.That(result, Is.EqualTo("go fast "));
    }

    [Test]
    public void SimpleFilters_FindAllRussianWordsWithLookalikeSymbols_HandlesNullInput()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => SimpleFilters.FindAllRussianWordsWithLookalikeSymbolsInNormalizedText(null!));
    }

    [Test]
    public void SimpleFilters_FindAllRussianWordsWithLookalikeSymbols_ReturnsEmptyListForEmptyInput()
    {
        // Arrange
        var input = "";

        // Act
        var result = SimpleFilters.FindAllRussianWordsWithLookalikeSymbolsInNormalizedText(input);

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void SimpleFilters_HasStopWords_HandlesNullInput()
    {
        // Arrange
        string? input = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => SimpleFilters.HasStopWords(input!));
    }

    [Test]
    public void SimpleFilters_HasStopWords_ReturnsFalseForEmptyInput()
    {
        // Arrange
        var input = "";

        // Act
        var result = SimpleFilters.HasStopWords(input);

        // Assert
        Assert.That(result, Is.False);
    }
}