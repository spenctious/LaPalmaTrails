using FluentAssertions;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaPalmaTrailsAPI.Tests
{
    [ExcludeFromCodeCoverage]
    public class UrlLookupTableTests
    {
        [Fact]
        public void Missing_file_returns_false()
        {
            // Arrange
            TestHelper.DeleteLookupTableFile();

            // Act
            bool loaded = CachedUrlLookupTable.Instance.LoadFromFile();

            // Assert
            loaded.Should().Be(false);
        }


        [Fact]
        public void Exisitng_file_is_loaded_returns_true()
        {
            // Arrange
            TestHelper.CreateLookupTableFile(@"{ ""Spanish"": ""English"" }");

            // Act
            bool loaded = CachedUrlLookupTable.Instance.LoadFromFile();

            // Assert
            loaded.Should().Be(true);
            CachedUrlLookupTable.Instance.Value.Count.Should().Be(1);
        }


        [Fact]
        public void Exisitng_bad_file_is_loaded_returns_false()
        {
            // Arrange
            TestHelper.CreateLookupTableFile(@"{ ""Spanish"" ""English"" }"); // badly formatted JSON

            // Act
            bool loaded = CachedUrlLookupTable.Instance.LoadFromFile();

            // Assert
            loaded.Should().Be(false);
            CachedUrlLookupTable.Instance.Value.Count.Should().Be(0);
        }


        [Fact]
        public void Exisitng_file_with_empty_content_is_loaded_returns_false()
        {
            // Arrange
            TestHelper.CreateLookupTableFile(@"{ }"); // no data

            // Act
            bool loaded = CachedUrlLookupTable.Instance.LoadFromFile();

            // Assert
            loaded.Should().Be(false);
            CachedUrlLookupTable.Instance.Value.Count.Should().Be(0);
        }


        [Fact]
        public void Exisitng_empty_file_is_loaded_returns_false()
        {
            // Arrange
            TestHelper.CreateLookupTableFile(@""); // no data

            // Act
            bool loaded = CachedUrlLookupTable.Instance.LoadFromFile();

            // Assert
            loaded.Should().Be(false);
            CachedUrlLookupTable.Instance.Value.Count.Should().Be(0);
        }


        [Fact]
        public void Saving_table_creates_file()
        {
            // Arrange
            TestHelper.DeleteLookupTableFile();
            CachedUrlLookupTable.Instance.Value.Clear();

            // Act
            bool added = CachedUrlLookupTable.Instance.Value.TryAdd("Spanish", "English");
            CachedUrlLookupTable.Instance.SaveToFile();

            // Assert
            added.Should().BeTrue();
            File.Exists(CachedUrlLookupTable.UrlTableLookupFileName).Should().BeTrue();
        }


        [Fact]
        public void Saving_table_overwrites_existing_file()
        {
            // Arrange
            TestHelper.CreateLookupTableFile(@"{ ""Spanish"": ""German"" }"); // file to be overwritten
            CachedUrlLookupTable.Instance.Value = new ConcurrentDictionary<string, string>();
            CachedUrlLookupTable.Instance.Value.TryAdd("Spanish", "English");

            // Act
            CachedUrlLookupTable.Instance.SaveToFile();
            CachedUrlLookupTable.Instance.Value.Clear();
            CachedUrlLookupTable.Instance.LoadFromFile();

            // Assert
            CachedUrlLookupTable.Instance.Value.Count.Should().Be(1);
            string expectedUrl;
            bool lookupSucceeded = CachedUrlLookupTable.Instance.Value.TryGetValue("Spanish", out expectedUrl);
            lookupSucceeded.Should().BeTrue();
            expectedUrl.Should().Be("English");
        }
    }
}
