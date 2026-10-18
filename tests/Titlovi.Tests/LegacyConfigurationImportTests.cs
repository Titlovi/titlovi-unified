using System;
using System.IO;
using System.Text;
using Jellyfin.Titlovi.Configuration;
using Xunit;

namespace Titlovi.Tests
{
    public sealed class LegacyConfigurationImportTests
    {
        [Fact]
        public void OriginalConfiguration_ImportsUsernameAndPasswordWithoutToken()
        {
            WithTemporaryDirectory(directory =>
            {
                const string xml = "<?xml version=\"1.0\"?><TitloviConfiguration><Username>old-user</Username><Password>p&amp;ss word </Password><Token><Id>secret-token</Id><UserId>32</UserId></Token></TitloviConfiguration>";
                var legacy = Path.Combine(directory, LegacyConfigurationImport.LegacyFileName);
                File.WriteAllText(legacy, xml, Encoding.UTF8);

                Assert.True(LegacyConfigurationImport.TryReadForImport(directory, out var imported));
                Assert.Equal("old-user", imported.Username);
                Assert.Equal("p&ss word ", imported.Password);
                Assert.Equal(xml, File.ReadAllText(legacy, Encoding.UTF8));
                Assert.False(File.Exists(Path.Combine(directory, LegacyConfigurationImport.CurrentFileName)));
            });
        }

        [Fact]
        public void ExistingBlankNewConfigurationFile_DoesNotBlockLegacyImport()
        {
            WithTemporaryDirectory(directory =>
            {
                WriteLegacy(directory, "original", "original-secret");
                var current = Path.Combine(directory, LegacyConfigurationImport.CurrentFileName);
                File.WriteAllText(current, "<PluginConfiguration><Username /><Password /><IncludeBshAlternativeLanguages>true</IncludeBshAlternativeLanguages></PluginConfiguration>");

                Assert.True(LegacyConfigurationImport.TryReadForImport(directory, out var imported));
                Assert.True(LegacyConfigurationImport.ShouldImportCredentials("", "", false, false));
                Assert.Equal("original", imported.Username);
                Assert.Equal("original-secret", imported.Password);
                Assert.Contains("IncludeBshAlternativeLanguages>true", File.ReadAllText(current));
            });
        }

        [Fact]
        public void ExistingNewCredentialsTakePrecedenceEvenWhenOldXmlExists()
        {
            WithTemporaryDirectory(directory =>
            {
                WriteLegacy(directory, "original", "original-secret");
                Assert.True(LegacyConfigurationImport.TryReadForImport(directory, out var imported));
                Assert.Equal("original", imported.Username);

                // The legacy XML can still be read, but the plugin does not apply it.
                Assert.False(LegacyConfigurationImport.ShouldImportCredentials(
                    "current", "current-secret", false, false));
            });
        }

        [Fact]
        public void CompletedMigrationDoesNotReimportAfterCredentialsAreCleared()
        {
            Assert.False(LegacyConfigurationImport.ShouldImportCredentials("", "", false, true));
            Assert.True(LegacyConfigurationImport.ShouldImportCredentials("", "", false, false));
        }

        [Theory]
        [InlineData("configured-user", "", false)]
        [InlineData("", "configured-secret", false)]
        [InlineData("", "", true)]
        public void PartialNewCredentialsOrTokenPreventLegacyOverwrite(
            string username, string password, bool tokenPresent)
        {
            Assert.False(LegacyConfigurationImport.ShouldImportCredentials(
                username, password, tokenPresent, false));
        }

        [Fact]
        public void MissingLegacyConfiguration_DoesNothing()
        {
            WithTemporaryDirectory(directory =>
            {
                Assert.False(LegacyConfigurationImport.TryReadForImport(directory, out var imported));
                Assert.Null(imported);
            });
        }

        [Fact]
        public void BrokenLegacyXml_DoesNotBlockImportOrAlterOriginal()
        {
            WithTemporaryDirectory(directory =>
            {
                var legacy = Path.Combine(directory, LegacyConfigurationImport.LegacyFileName);
                const string badXml = "<TitloviConfiguration><Username>unfinished";
                File.WriteAllText(legacy, badXml);

                Assert.False(LegacyConfigurationImport.TryReadForImport(directory, out var imported));
                Assert.Null(imported);
                Assert.Equal(badXml, File.ReadAllText(legacy));
            });
        }

        [Fact]
        public void WrongXmlRoot_IsNotMistakenForOriginalPlugin()
        {
            WithTemporaryDirectory(directory =>
            {
                File.WriteAllText(Path.Combine(directory, LegacyConfigurationImport.LegacyFileName),
                    "<OtherPlugin><Username>old</Username><Password>secret</Password></OtherPlugin>");
                Assert.False(LegacyConfigurationImport.TryReadForImport(directory, out _));
            });
        }

        [Fact]
        public void EmptyCredentials_AreNotImported()
        {
            WithTemporaryDirectory(directory =>
            {
                WriteLegacy(directory, "", "");
                Assert.False(LegacyConfigurationImport.TryReadForImport(directory, out _));
            });
        }

        [Fact]
        public void PartialCredentials_ArePreservedWithoutGuessingMissingData()
        {
            WithTemporaryDirectory(directory =>
            {
                WriteLegacy(directory, "old-user", "");
                Assert.True(LegacyConfigurationImport.TryReadForImport(directory, out var imported));
                Assert.Equal("old-user", imported.Username);
                Assert.Empty(imported.Password);
            });
        }

        [Fact]
        public void NamespacedOriginalXml_CanBeImported()
        {
            WithTemporaryDirectory(directory =>
            {
                File.WriteAllText(Path.Combine(directory, LegacyConfigurationImport.LegacyFileName),
                    "<TitloviConfiguration xmlns=\"urn:legacy\"><Username>old-user</Username><Password>pass</Password></TitloviConfiguration>");
                Assert.True(LegacyConfigurationImport.TryReadForImport(directory, out var imported));
                Assert.Equal("old-user", imported.Username);
                Assert.Equal("pass", imported.Password);
            });
        }

        [Fact]
        public void XmlExternalEntities_AreRejected()
        {
            WithTemporaryDirectory(directory =>
            {
                File.WriteAllText(Path.Combine(directory, LegacyConfigurationImport.LegacyFileName),
                    "<!DOCTYPE TitloviConfiguration [<!ENTITY dangerous SYSTEM \"file:///etc/passwd\">]><TitloviConfiguration><Username>&dangerous;</Username><Password>pass</Password></TitloviConfiguration>");
                Assert.False(LegacyConfigurationImport.TryReadForImport(directory, out _));
            });
        }

        [Fact]
        public void OversizedConfiguration_IsNotImported()
        {
            WithTemporaryDirectory(directory =>
            {
                WriteLegacy(directory, new string('x', 70000), "pass");
                Assert.False(LegacyConfigurationImport.TryReadForImport(directory, out _));
            });
        }

        [Fact]
        public void MissingDirectory_IsHarmless()
        {
            Assert.False(LegacyConfigurationImport.TryReadForImport(null, out var imported));
            Assert.Null(imported);
        }

        private static void WriteLegacy(string directory, string username, string password)
        {
            File.WriteAllText(
                Path.Combine(directory, LegacyConfigurationImport.LegacyFileName),
                "<TitloviConfiguration><Username>" + username + "</Username><Password>" +
                password + "</Password></TitloviConfiguration>");
        }

        private static void WithTemporaryDirectory(Action<string> action)
        {
            var directory = Path.Combine(Path.GetTempPath(), "titlovi-config-migration-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                action(directory);
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}
