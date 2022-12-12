using Newtonsoft.Json;
using System;
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
        private void DeleteLookupTableFile()
        {
            if (File.Exists(PersistentLookupTable.UrlTableLookupFileName))
            {
                File.Delete(PersistentLookupTable.UrlTableLookupFileName);

            }
        }

        private void CreateLookupTableFile(string contents)
        {
            File.WriteAllText(PersistentLookupTable.UrlTableLookupFileName, contents);
        }


        [Fact]
        public void Missing_file_returns_false()
        {
            DeleteLookupTableFile();
            var sut = new PersistentLookupTable();

            bool loaded = sut.LoadUrlLookupTable();

            Assert.False(loaded);
        }


        [Fact]
        public void Exisitng_file_is_loaded_returns_true()
        {
            CreateLookupTableFile(@"{ ""Spanish"": ""English"" }");
            var sut = new PersistentLookupTable();

            bool loaded = sut.LoadUrlLookupTable();

            Assert.True(loaded);
            Assert.Equal(1, sut.Count);
        }


        [Fact]
        public void Saving_table_creates_file()
        {
            DeleteLookupTableFile();
            var sut = new PersistentLookupTable();
            sut.TryAdd("Spanish", "English");

            sut.SaveUrlLookupTable();

            Assert.True(File.Exists(PersistentLookupTable.UrlTableLookupFileName));
        }


        [Fact]
        public void Saving_table_overwrites_existing_file()
        {
            CreateLookupTableFile(@"{ ""Spanish"": ""German"" }");
            var sut = new PersistentLookupTable();
            sut.Clear();
            sut.TryAdd("Spanish", "English");

            sut.SaveUrlLookupTable();
            sut.Clear();
            sut.LoadUrlLookupTable();

            Assert.Equal(1, sut.Count);
            Assert.Equal("English", sut.GetValue("Spanish"));
        }


        [Fact]
        public void New_url_association_is_added()
        {
            var sut = new PersistentLookupTable();
            sut.Clear();

            sut.TryAdd("Spanish", "English");

            Assert.Equal(1, sut.Count);
            Assert.Equal("English", sut.GetValue("Spanish"));
        }


        [Fact]
        public void Adding_existing_key_does_not_change_value()
        {
            var sut = new PersistentLookupTable();
            sut.Clear();
            sut.TryAdd("Spanish", "English");

            sut.TryAdd("Spanish", "German");

            Assert.Equal(1, sut.Count);
            Assert.Equal("English", sut.GetValue("Spanish"));
        }


        [Fact]
        public void Missing_key_returns_false()
        {
            var sut = new PersistentLookupTable();
            sut.Clear();

            bool itemLocated = sut.ContainsKey("Spanish");

            Assert.False(itemLocated);
        }


        [Fact]
        public void Found_key_returns_true()
        {
            var sut = new PersistentLookupTable();
            sut.Clear();
            sut.TryAdd("Spanish", "English");

            bool itemLocated = sut.ContainsKey("Spanish");

            Assert.True(itemLocated);
        }


        [Fact]
        public void Existing_association_returns_value()
        {
            var sut = new PersistentLookupTable();
            sut.Clear();
            sut.TryAdd("Spanish", "English");

            string associatedValue = sut.GetValue("Spanish");

            Assert.Equal("English", associatedValue);
        }
    }
}
