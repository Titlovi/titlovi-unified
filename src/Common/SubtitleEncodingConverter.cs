using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Titlovi.Common
{
    internal static class SubtitleEncodingConverter
    {
        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);
        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

        private static readonly char[] Windows1250 = new char[]
        {
            '\u20AC', '\uFFFD', '\u201A', '\uFFFD', '\u201E', '\u2026', '\u2020', '\u2021', '\uFFFD', '\u2030', '\u0160', '\u2039', '\u015A', '\u0164', '\u017D', '\u0179',
            '\uFFFD', '\u2018', '\u2019', '\u201C', '\u201D', '\u2022', '\u2013', '\u2014', '\uFFFD', '\u2122', '\u0161', '\u203A', '\u015B', '\u0165', '\u017E', '\u017A',
            '\u00A0', '\u02C7', '\u02D8', '\u0141', '\u00A4', '\u0104', '\u00A6', '\u00A7', '\u00A8', '\u00A9', '\u015E', '\u00AB', '\u00AC', '\u00AD', '\u00AE', '\u017B',
            '\u00B0', '\u00B1', '\u02DB', '\u0142', '\u00B4', '\u00B5', '\u00B6', '\u00B7', '\u00B8', '\u0105', '\u015F', '\u00BB', '\u013D', '\u02DD', '\u013E', '\u017C',
            '\u0154', '\u00C1', '\u00C2', '\u0102', '\u00C4', '\u0139', '\u0106', '\u00C7', '\u010C', '\u00C9', '\u0118', '\u00CB', '\u011A', '\u00CD', '\u00CE', '\u010E',
            '\u0110', '\u0143', '\u0147', '\u00D3', '\u00D4', '\u0150', '\u00D6', '\u00D7', '\u0158', '\u016E', '\u00DA', '\u0170', '\u00DC', '\u00DD', '\u0162', '\u00DF',
            '\u0155', '\u00E1', '\u00E2', '\u0103', '\u00E4', '\u013A', '\u0107', '\u00E7', '\u010D', '\u00E9', '\u0119', '\u00EB', '\u011B', '\u00ED', '\u00EE', '\u010F',
            '\u0111', '\u0144', '\u0148', '\u00F3', '\u00F4', '\u0151', '\u00F6', '\u00F7', '\u0159', '\u016F', '\u00FA', '\u0171', '\u00FC', '\u00FD', '\u0163', '\u02D9'
        };

        private static readonly char[] Windows1251 = new char[]
        {
            '\u0402', '\u0403', '\u201A', '\u0453', '\u201E', '\u2026', '\u2020', '\u2021', '\u20AC', '\u2030', '\u0409', '\u2039', '\u040A', '\u040C', '\u040B', '\u040F',
            '\u0452', '\u2018', '\u2019', '\u201C', '\u201D', '\u2022', '\u2013', '\u2014', '\uFFFD', '\u2122', '\u0459', '\u203A', '\u045A', '\u045C', '\u045B', '\u045F',
            '\u00A0', '\u040E', '\u045E', '\u0408', '\u00A4', '\u0490', '\u00A6', '\u00A7', '\u0401', '\u00A9', '\u0404', '\u00AB', '\u00AC', '\u00AD', '\u00AE', '\u0407',
            '\u00B0', '\u00B1', '\u0406', '\u0456', '\u0491', '\u00B5', '\u00B6', '\u00B7', '\u0451', '\u2116', '\u0454', '\u00BB', '\u0458', '\u0405', '\u0455', '\u0457',
            '\u0410', '\u0411', '\u0412', '\u0413', '\u0414', '\u0415', '\u0416', '\u0417', '\u0418', '\u0419', '\u041A', '\u041B', '\u041C', '\u041D', '\u041E', '\u041F',
            '\u0420', '\u0421', '\u0422', '\u0423', '\u0424', '\u0425', '\u0426', '\u0427', '\u0428', '\u0429', '\u042A', '\u042B', '\u042C', '\u042D', '\u042E', '\u042F',
            '\u0430', '\u0431', '\u0432', '\u0433', '\u0434', '\u0435', '\u0436', '\u0437', '\u0438', '\u0439', '\u043A', '\u043B', '\u043C', '\u043D', '\u043E', '\u043F',
            '\u0440', '\u0441', '\u0442', '\u0443', '\u0444', '\u0445', '\u0446', '\u0447', '\u0448', '\u0449', '\u044A', '\u044B', '\u044C', '\u044D', '\u044E', '\u044F'
        };

        private static readonly char[] Windows1252 = new char[]
        {
            '\u20AC', '\uFFFD', '\u201A', '\u0192', '\u201E', '\u2026', '\u2020', '\u2021', '\u02C6', '\u2030', '\u0160', '\u2039', '\u0152', '\uFFFD', '\u017D', '\uFFFD',
            '\uFFFD', '\u2018', '\u2019', '\u201C', '\u201D', '\u2022', '\u2013', '\u2014', '\u02DC', '\u2122', '\u0161', '\u203A', '\u0153', '\uFFFD', '\u017E', '\u0178',
            '\u00A0', '\u00A1', '\u00A2', '\u00A3', '\u00A4', '\u00A5', '\u00A6', '\u00A7', '\u00A8', '\u00A9', '\u00AA', '\u00AB', '\u00AC', '\u00AD', '\u00AE', '\u00AF',
            '\u00B0', '\u00B1', '\u00B2', '\u00B3', '\u00B4', '\u00B5', '\u00B6', '\u00B7', '\u00B8', '\u00B9', '\u00BA', '\u00BB', '\u00BC', '\u00BD', '\u00BE', '\u00BF',
            '\u00C0', '\u00C1', '\u00C2', '\u00C3', '\u00C4', '\u00C5', '\u00C6', '\u00C7', '\u00C8', '\u00C9', '\u00CA', '\u00CB', '\u00CC', '\u00CD', '\u00CE', '\u00CF',
            '\u00D0', '\u00D1', '\u00D2', '\u00D3', '\u00D4', '\u00D5', '\u00D6', '\u00D7', '\u00D8', '\u00D9', '\u00DA', '\u00DB', '\u00DC', '\u00DD', '\u00DE', '\u00DF',
            '\u00E0', '\u00E1', '\u00E2', '\u00E3', '\u00E4', '\u00E5', '\u00E6', '\u00E7', '\u00E8', '\u00E9', '\u00EA', '\u00EB', '\u00EC', '\u00ED', '\u00EE', '\u00EF',
            '\u00F0', '\u00F1', '\u00F2', '\u00F3', '\u00F4', '\u00F5', '\u00F6', '\u00F7', '\u00F8', '\u00F9', '\u00FA', '\u00FB', '\u00FC', '\u00FD', '\u00FE', '\u00FF'
        };

        private static readonly char[] Iso88592 = new char[]
        {
            '\u0080', '\u0081', '\u0082', '\u0083', '\u0084', '\u0085', '\u0086', '\u0087', '\u0088', '\u0089', '\u008A', '\u008B', '\u008C', '\u008D', '\u008E', '\u008F',
            '\u0090', '\u0091', '\u0092', '\u0093', '\u0094', '\u0095', '\u0096', '\u0097', '\u0098', '\u0099', '\u009A', '\u009B', '\u009C', '\u009D', '\u009E', '\u009F',
            '\u00A0', '\u0104', '\u02D8', '\u0141', '\u00A4', '\u013D', '\u015A', '\u00A7', '\u00A8', '\u0160', '\u015E', '\u0164', '\u0179', '\u00AD', '\u017D', '\u017B',
            '\u00B0', '\u0105', '\u02DB', '\u0142', '\u00B4', '\u013E', '\u015B', '\u02C7', '\u00B8', '\u0161', '\u015F', '\u0165', '\u017A', '\u02DD', '\u017E', '\u017C',
            '\u0154', '\u00C1', '\u00C2', '\u0102', '\u00C4', '\u0139', '\u0106', '\u00C7', '\u010C', '\u00C9', '\u0118', '\u00CB', '\u011A', '\u00CD', '\u00CE', '\u010E',
            '\u0110', '\u0143', '\u0147', '\u00D3', '\u00D4', '\u0150', '\u00D6', '\u00D7', '\u0158', '\u016E', '\u00DA', '\u0170', '\u00DC', '\u00DD', '\u0162', '\u00DF',
            '\u0155', '\u00E1', '\u00E2', '\u0103', '\u00E4', '\u013A', '\u0107', '\u00E7', '\u010D', '\u00E9', '\u0119', '\u00EB', '\u011B', '\u00ED', '\u00EE', '\u010F',
            '\u0111', '\u0144', '\u0148', '\u00F3', '\u00F4', '\u0151', '\u00F6', '\u00F7', '\u0159', '\u016F', '\u00FA', '\u0171', '\u00FC', '\u00FD', '\u0163', '\u02D9'
        };

        private static readonly char[] Iso88595 = new char[]
        {
            '\u0080', '\u0081', '\u0082', '\u0083', '\u0084', '\u0085', '\u0086', '\u0087', '\u0088', '\u0089', '\u008A', '\u008B', '\u008C', '\u008D', '\u008E', '\u008F',
            '\u0090', '\u0091', '\u0092', '\u0093', '\u0094', '\u0095', '\u0096', '\u0097', '\u0098', '\u0099', '\u009A', '\u009B', '\u009C', '\u009D', '\u009E', '\u009F',
            '\u00A0', '\u0401', '\u0402', '\u0403', '\u0404', '\u0405', '\u0406', '\u0407', '\u0408', '\u0409', '\u040A', '\u040B', '\u040C', '\u00AD', '\u040E', '\u040F',
            '\u0410', '\u0411', '\u0412', '\u0413', '\u0414', '\u0415', '\u0416', '\u0417', '\u0418', '\u0419', '\u041A', '\u041B', '\u041C', '\u041D', '\u041E', '\u041F',
            '\u0420', '\u0421', '\u0422', '\u0423', '\u0424', '\u0425', '\u0426', '\u0427', '\u0428', '\u0429', '\u042A', '\u042B', '\u042C', '\u042D', '\u042E', '\u042F',
            '\u0430', '\u0431', '\u0432', '\u0433', '\u0434', '\u0435', '\u0436', '\u0437', '\u0438', '\u0439', '\u043A', '\u043B', '\u043C', '\u043D', '\u043E', '\u043F',
            '\u0440', '\u0441', '\u0442', '\u0443', '\u0444', '\u0445', '\u0446', '\u0447', '\u0448', '\u0449', '\u044A', '\u044B', '\u044C', '\u044D', '\u044E', '\u044F',
            '\u2116', '\u0451', '\u0452', '\u0453', '\u0454', '\u0455', '\u0456', '\u0457', '\u0458', '\u0459', '\u045A', '\u045B', '\u045C', '\u00A7', '\u045E', '\u045F'
        };

        public static byte[] ConvertToUtf8IfNeeded(byte[] input, string providerLanguage, out string detectedEncoding, out bool converted)
        {
            detectedEncoding = "Unknown";
            converted = false;
            if (input == null || input.Length == 0)
            {
                detectedEncoding = "Empty";
                return input ?? Array.Empty<byte>();
            }

            // UTF-8 BOM: already UTF-8, preserve bytes exactly as downloaded.
            if (StartsWith(input, 0xEF, 0xBB, 0xBF))
            {
                try
                {
                    StrictUtf8.GetString(input, 3, input.Length - 3);
                    detectedEncoding = "UTF-8 BOM";
                    return input;
                }
                catch (DecoderFallbackException) { }
            }

            // Valid UTF-8 without BOM: do not rewrite it.
            try
            {
                StrictUtf8.GetString(input);
                detectedEncoding = "UTF-8";
                return input;
            }
            catch (DecoderFallbackException) { }

            // BOM-tagged Unicode encodings can be converted deterministically.
            string unicodeText = null;
            if (StartsWith(input, 0xFF, 0xFE, 0x00, 0x00) && input.Length >= 4)
            {
                unicodeText = new UTF32Encoding(false, true, true).GetString(input, 4, input.Length - 4);
                detectedEncoding = "UTF-32 LE";
            }
            else if (StartsWith(input, 0x00, 0x00, 0xFE, 0xFF) && input.Length >= 4)
            {
                unicodeText = new UTF32Encoding(true, true, true).GetString(input, 4, input.Length - 4);
                detectedEncoding = "UTF-32 BE";
            }
            else if (StartsWith(input, 0xFF, 0xFE) && input.Length >= 2)
            {
                unicodeText = new UnicodeEncoding(false, true, true).GetString(input, 2, input.Length - 2);
                detectedEncoding = "UTF-16 LE";
            }
            else if (StartsWith(input, 0xFE, 0xFF) && input.Length >= 2)
            {
                unicodeText = new UnicodeEncoding(true, true, true).GetString(input, 2, input.Length - 2);
                detectedEncoding = "UTF-16 BE";
            }

            if (unicodeText != null)
            {
                converted = true;
                return Utf8NoBom.GetBytes(unicodeText);
            }

            var candidates = GetCandidates(providerLanguage);
            Candidate best = null;
            foreach (var candidate in candidates)
            {
                var text = DecodeSingleByte(input, candidate.Table);
                var score = Score(text, providerLanguage, candidate.Name);
                if (best == null || score > best.Score)
                    best = new Candidate(candidate.Name, candidate.Table, text, score);
            }

            if (best == null)
            {
                detectedEncoding = "Unknown";
                return input;
            }

            detectedEncoding = best.Name;
            converted = true;
            return Utf8NoBom.GetBytes(best.Text);
        }

        private static IEnumerable<CandidateDefinition> GetCandidates(string providerLanguage)
        {
            var lang = (providerLanguage ?? string.Empty).Trim().ToLowerInvariant();

            if (lang == "cirilica" || lang == "makedonski")
            {
                yield return new CandidateDefinition("Windows-1251", Windows1251);
                yield return new CandidateDefinition("ISO-8859-5", Iso88595);
                yield return new CandidateDefinition("Windows-1250", Windows1250);
                yield return new CandidateDefinition("Windows-1252", Windows1252);
                yield break;
            }

            if (lang == "english")
            {
                yield return new CandidateDefinition("Windows-1252", Windows1252);
                yield return new CandidateDefinition("Windows-1250", Windows1250);
                yield return new CandidateDefinition("ISO-8859-2", Iso88592);
                yield return new CandidateDefinition("Windows-1251", Windows1251);
                yield break;
            }

            // Croatian, Bosnian, Serbian Latin and Slovenian are normally Windows-1250
            // or ISO-8859-2. Keep Cyrillic encodings as fallbacks because old uploads vary.
            yield return new CandidateDefinition("Windows-1250", Windows1250);
            yield return new CandidateDefinition("ISO-8859-2", Iso88592);
            yield return new CandidateDefinition("Windows-1252", Windows1252);
            yield return new CandidateDefinition("Windows-1251", Windows1251);
            yield return new CandidateDefinition("ISO-8859-5", Iso88595);
        }

        private static string DecodeSingleByte(byte[] input, char[] table)
        {
            var chars = new char[input.Length];
            for (var i = 0; i < input.Length; i++)
            {
                var b = input[i];
                chars[i] = b < 128 ? (char)b : table[b - 128];
            }
            return new string(chars);
        }

        private static double Score(string text, string providerLanguage, string encodingName)
        {
            if (string.IsNullOrEmpty(text)) return double.MinValue;
            var lang = (providerLanguage ?? string.Empty).Trim().ToLowerInvariant();
            var wantsCyrillic = lang == "cirilica" || lang == "makedonski";
            var wantsEnglish = lang == "english";
            double score = 0;

            foreach (var ch in text)
            {
                if (ch == '\r' || ch == '\n' || ch == '\t')
                {
                    score += 0.1;
                    continue;
                }

                if (ch == '\uFFFD')
                {
                    score -= 100;
                    continue;
                }

                if (char.IsControl(ch))
                {
                    score -= 20;
                    continue;
                }

                if (ch >= '\u0400' && ch <= '\u04FF')
                    score += wantsCyrillic ? 5.0 : -3.0;
                else if (IsBalkanLatin(ch))
                    score += wantsCyrillic ? -2.0 : (wantsEnglish ? -0.5 : 5.0);
                else if ((ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z'))
                    score += 0.7;
                else if (char.IsLetter(ch))
                    score += wantsEnglish ? -0.4 : 0.2;
                else if (char.IsDigit(ch) || char.IsWhiteSpace(ch) || char.IsPunctuation(ch))
                    score += 0.2;
                else
                    score -= 0.1;

                // Typical mojibake / wrong-code-page artifacts.
                if (ch == 'Ã' || ch == 'Â' || ch == 'Ð' || ch == 'Ñ') score -= 3.0;
            }

            // Tie-break toward the code pages most common for the requested language.
            if (wantsCyrillic && encodingName == "Windows-1251") score += 2.0;
            else if (wantsEnglish && encodingName == "Windows-1252") score += 2.0;
            else if (!wantsCyrillic && !wantsEnglish && encodingName == "Windows-1250") score += 2.0;

            return score;
        }

        private static bool IsBalkanLatin(char ch)
        {
            switch (ch)
            {
                case 'č': case 'ć': case 'ž': case 'š': case 'đ':
                case 'Č': case 'Ć': case 'Ž': case 'Š': case 'Đ':
                    return true;
                default:
                    return false;
            }
        }

        private static bool StartsWith(byte[] data, params byte[] prefix)
        {
            if (data == null || prefix == null || data.Length < prefix.Length) return false;
            for (var i = 0; i < prefix.Length; i++)
                if (data[i] != prefix[i]) return false;
            return true;
        }

        private sealed class CandidateDefinition
        {
            public CandidateDefinition(string name, char[] table)
            {
                Name = name;
                Table = table;
            }
            public string Name { get; }
            public char[] Table { get; }
        }

        private sealed class Candidate
        {
            public Candidate(string name, char[] table, string text, double score)
            {
                Name = name;
                Table = table;
                Text = text;
                Score = score;
            }
            public string Name { get; }
            public char[] Table { get; }
            public string Text { get; }
            public double Score { get; }
        }
    }
}
