using System.Text;

namespace Rezervise.Application.Common.Helpers
{
    /// <summary>
    /// Mini RFC4180 CSV parser. Tırnaklı alanları, alan içindeki virgül ve
    /// satır sonlarını ve iki katlı tırnak kaçışını ("") destekler.
    /// Harici paket kullanılmaz; test edilebilir saf static sınıftır.
    /// </summary>
    public static class CsvParser
    {
        /// <summary>
        /// CSV içeriğini satır ve kolonlara ayırır. Tamamen boş satırlar atlanır.
        /// </summary>
        /// <param name="content">Ham CSV metni.</param>
        /// <returns>Her öğesi bir satırın alanları olan liste.</returns>
        public static List<string[]> Parse(string content)
        {
            var rows = new List<string[]>();

            if (string.IsNullOrWhiteSpace(content))
                return rows;

            var fields = new List<string>();
            var current = new StringBuilder();
            var inQuotes = false;
            var hasAnyField = false;

            for (var i = 0; i < content.Length; i++)
            {
                var c = content[i];

                if (inQuotes)
                {
                    if (c == '"')
                    {
                        // İki katlı tırnak kaçışı: "" -> tek tırnak.
                        if (i + 1 < content.Length && content[i + 1] == '"')
                        {
                            current.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        current.Append(c);
                    }

                    continue;
                }

                switch (c)
                {
                    case '"':
                        inQuotes = true;
                        hasAnyField = true;
                        break;

                    case ',':
                        fields.Add(current.ToString());
                        current.Clear();
                        hasAnyField = true;
                        break;

                    case '\r':
                        // \r\n tek satır sonu sayılır.
                        if (i + 1 < content.Length && content[i + 1] == '\n')
                            i++;
                        FlushRow(rows, fields, current, ref hasAnyField);
                        break;

                    case '\n':
                        FlushRow(rows, fields, current, ref hasAnyField);
                        break;

                    default:
                        current.Append(c);
                        hasAnyField = true;
                        break;
                }
            }

            // Sondaki alani/satırı da ekle (son satırnova'siz bitmiş olabilir).
            FlushRow(rows, fields, current, ref hasAnyField);

            return rows;
        }

        /// <summary>
        /// Bekleyen alani satıra çevirir. Hiç alan yoksa (boş satır) atlar.
        /// </summary>
        private static void FlushRow(List<string[]> rows, List<string> fields, StringBuilder current, ref bool hasAnyField)
        {
            if (!hasAnyField && current.Length == 0)
                return;

            fields.Add(current.ToString());
            current.Clear();
            rows.Add(fields.ToArray());
            fields.Clear();
            hasAnyField = false;
        }
    }
}
