using System.Text;

namespace AppointMan.Application.Common.Helpers
{
    /// <summary>
    /// Mini RFC4180 CSV parser. Tirnakli alanlari, alan icindeki virgul ve
    /// satir sonlarini ve iki katli tirnak kacisini ("") destekler.
    /// Harici paket kullanilmaz; test edilebilir saf static siniftir.
    /// </summary>
    public static class CsvParser
    {
        /// <summary>
        /// CSV icerigini satir ve kolonlara ayirir. Tamamen bos satirlar atlanir.
        /// </summary>
        /// <param name="content">Ham CSV metni.</param>
        /// <returns>Her ogesi bir satirin alanlari olan liste.</returns>
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
                        // Iki katli tirnak kacisi: "" -> tek tirnak.
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
                        // \r\n tek satir sonu sayilir.
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

            // Sondaki alani/satiri da ekle (son satirnova'siz bitmis olabilir).
            FlushRow(rows, fields, current, ref hasAnyField);

            return rows;
        }

        /// <summary>
        /// Bekleyen alani satira cevirir. Hic alan yoksa (bos satir) atlar.
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
