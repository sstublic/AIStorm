using AIStorm.Core.Services;
using System.Collections.Generic;
using System.Linq;

namespace Core.Tests.Services;

public class MarkdownSerializerTests
{
    private readonly MarkdownSerializer serializer;

    public MarkdownSerializerTests()
    {
        serializer = new MarkdownSerializer();
    }

    [Fact]
    public void DeserializeDocument_SingleSegment_ReturnsCorrectSegment()
    {
        // Arrange
        var markdown = @"<aistorm type=""test"" value=""123"" />

This is test content.";

        // Act
        var segments = serializer.DeserializeDocument(markdown);

        // Assert
        Assert.Single(segments);
        Assert.Equal("test", segments[0].Properties["type"]);
        Assert.Equal("123", segments[0].Properties["value"]);
        Assert.Equal("This is test content.", segments[0].Content);
    }

    [Fact]
    public void DeserializeDocument_MultipleSegments_ReturnsAllSegments()
    {
        // Arrange
        var markdown = @"<aistorm type=""first"" value=""1"" />

First content.

<aistorm type=""second"" value=""2"" />

Second content.";

        // Act
        var segments = serializer.DeserializeDocument(markdown);

        // Assert
        Assert.Equal(2, segments.Count);
        
        Assert.Equal("first", segments[0].Properties["type"]);
        Assert.Equal("1", segments[0].Properties["value"]);
        Assert.Equal("First content.", segments[0].Content);
        
        Assert.Equal("second", segments[1].Properties["type"]);
        Assert.Equal("2", segments[1].Properties["value"]);
        Assert.Equal("Second content.", segments[1].Content);
    }

    [Fact]
    public void SerializeDocument_SingleSegment_ReturnsCorrectMarkdown()
    {
        // Arrange
        var segment = new MarkdownSegment(
            new Dictionary<string, string> { ["type"] = "test", ["value"] = "123" },
            "This is test content."
        );

        // Act
        var markdown = serializer.SerializeDocument(new List<MarkdownSegment> { segment });

        // Assert
        Assert.Contains("<aistorm type=\"test\" value=\"123\" />", markdown);
        Assert.Contains("This is test content.", markdown);
    }

    [Fact]
    public void SerializeDocument_MultipleSegments_ReturnsCorrectMarkdown()
    {
        // Arrange
        var segments = new List<MarkdownSegment>
        {
            new MarkdownSegment(
                new Dictionary<string, string> { ["type"] = "first", ["value"] = "1" },
                "First content."
            ),
            new MarkdownSegment(
                new Dictionary<string, string> { ["type"] = "second", ["value"] = "2" },
                "Second content."
            )
        };

        // Act
        var markdown = serializer.SerializeDocument(segments);

        // Assert
        Assert.Contains("<aistorm type=\"first\" value=\"1\" />", markdown);
        Assert.Contains("First content.", markdown);
        Assert.Contains("<aistorm type=\"second\" value=\"2\" />", markdown);
        Assert.Contains("Second content.", markdown);
    }

    [Fact]
    public void FindSegment_ExistingType_ReturnsCorrectSegment()
    {
        // Arrange
        var segments = new List<MarkdownSegment>
        {
            new MarkdownSegment(
                new Dictionary<string, string> { ["type"] = "first", ["value"] = "1" },
                "First content."
            ),
            new MarkdownSegment(
                new Dictionary<string, string> { ["type"] = "second", ["value"] = "2" },
                "Second content."
            )
        };

        // Act
        var segment = serializer.FindSegment(segments, "second");

        // Assert
        Assert.NotNull(segment);
        Assert.Equal("second", segment.Properties["type"]);
        Assert.Equal("2", segment.Properties["value"]);
        Assert.Equal("Second content.", segment.Content);
    }

    [Fact]
    public void FindSegment_NonExistingType_ReturnsNull()
    {
        // Arrange
        var segments = new List<MarkdownSegment>
        {
            new MarkdownSegment(
                new Dictionary<string, string> { ["type"] = "first", ["value"] = "1" },
                "First content."
            )
        };

        // Act
        var segment = serializer.FindSegment(segments, "nonexistent");

        // Assert
        Assert.Null(segment);
    }

    [Fact]
    public void FindSegments_ExistingType_ReturnsAllMatchingSegments()
    {
        // Arrange
        var segments = new List<MarkdownSegment>
        {
            new MarkdownSegment(
                new Dictionary<string, string> { ["type"] = "message", ["from"] = "user1" },
                "Message 1."
            ),
            new MarkdownSegment(
                new Dictionary<string, string> { ["type"] = "message", ["from"] = "user2" },
                "Message 2."
            ),
            new MarkdownSegment(
                new Dictionary<string, string> { ["type"] = "other", ["value"] = "3" },
                "Other content."
            )
        };

        // Act
        var messageSegments = serializer.FindSegments(segments, "message");

        // Assert
        Assert.Equal(2, messageSegments.Count);
        Assert.Equal("user1", messageSegments[0].Properties["from"]);
        Assert.Equal("user2", messageSegments[1].Properties["from"]);
    }

    [Fact]
    public void GetProperty_ExistingProperty_ReturnsConvertedValue()
    {
        // Arrange
        var segment = new MarkdownSegment(
            new Dictionary<string, string> { ["number"] = "123", ["text"] = "hello" },
            "Content"
        );

        // Act
        var numberValue = segment.GetProperty<int>("number");
        var textValue = segment.GetProperty<string>("text");

        // Assert
        Assert.Equal(123, numberValue);
        Assert.Equal("hello", textValue);
    }

    [Fact]
    public void GetProperty_NonExistingProperty_ReturnsDefaultValue()
    {
        // Arrange
        var segment = new MarkdownSegment(
            new Dictionary<string, string> { ["existing"] = "value" },
            "Content"
        );

        // Act
        var defaultValue = segment.GetProperty<string>("nonexistent", "default");

        // Assert
        Assert.Equal("default", defaultValue);
    }

    [Fact]
    public void GetProperty_InvalidConversion_ReturnsDefaultValue()
    {
        // Arrange
        var segment = new MarkdownSegment(
            new Dictionary<string, string> { ["text"] = "not a number" },
            "Content"
        );

        // Act
        var defaultValue = segment.GetProperty<int>("text", 42);

        // Assert
        Assert.Equal(42, defaultValue);
    }

    [Fact]
    public void RoundTrip_ComplexDocument_PreservesContent()
    {
        // Arrange
        var originalSegments = new List<MarkdownSegment>
        {
            new MarkdownSegment(
                new Dictionary<string, string> { ["type"] = "session", ["created"] = "2025-03-01T15:00:00Z", ["description"] = "Test Session" },
                "# Test Session"
            ),
            new MarkdownSegment(
                new Dictionary<string, string> { ["type"] = "message", ["from"] = "user", ["timestamp"] = "2025-03-01T15:01:00Z" },
                "## [user]:\n\nHello, world!"
            ),
            new MarkdownSegment(
                new Dictionary<string, string> { ["type"] = "message", ["from"] = "agent", ["timestamp"] = "2025-03-01T15:02:00Z" },
                "## [agent]:\n\nHi there!"
            )
        };

        // Act
        var markdown = serializer.SerializeDocument(originalSegments);
        var deserializedSegments = serializer.DeserializeDocument(markdown);

        // Assert
        Assert.Equal(originalSegments.Count, deserializedSegments.Count);
        
        for (int i = 0; i < originalSegments.Count; i++)
        {
            Assert.Equal(originalSegments[i].Properties.Count, deserializedSegments[i].Properties.Count);
            foreach (var key in originalSegments[i].Properties.Keys)
            {
                Assert.Equal(originalSegments[i].Properties[key], deserializedSegments[i].Properties[key]);
            }
            Assert.Equal(originalSegments[i].Content, deserializedSegments[i].Content);
        }
    }
}
