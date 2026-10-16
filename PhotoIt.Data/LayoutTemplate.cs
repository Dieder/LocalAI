using System;
using System.Collections.Generic;
using System.Text;

namespace PhotoIt.Data
{
    public sealed class LayoutTemplate
    {
        public string Id { get; }
        public string Name { get; }
        public int Columns { get; }
        public int Rows { get; }
        public List<PhotoSlot> Slots { get; } = new List<PhotoSlot>();

        public LayoutTemplate(
            string id,
            string name,
            int columns,
            int rows,
            IEnumerable<PhotoSlot> slots)
        {
            Id = id;
            Name = name;
            Columns = columns;
            Rows = rows;
            Slots = slots.ToList();
        }

        public string ToSvg(int width = 300, int gap = 8)
        {
            var height = Math.Max(1, (int)Math.Round(width * Rows / (double)Columns));
            double cellW = (width - gap * (Columns + 1)) / (double)Columns;
            double cellH = (height - gap * (Rows + 1)) / (double)Rows;

            var sb = new StringBuilder();
            sb.AppendLine($"""<svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}" viewBox="0 0 {width} {height}" style="display:block;width:100%;height:auto;">""");
            sb.AppendLine("""  <rect width="100%" height="100%" fill="#f8f8f8"/>""");

            foreach (var slot in Slots)
            {
                double x = gap + (slot.Col - 1) * (cellW + gap);
                double y = gap + (slot.Row - 1) * (cellH + gap);
                double w = slot.ColSpan * cellW + (slot.ColSpan - 1) * gap;
                double h = slot.RowSpan * cellH + (slot.RowSpan - 1) * gap;

                string fill = slot.Type.Id switch
                {
                    "portrait" => "#7EB6D9",
                    "landscape" => "#8FCB8F",
                    "square" => "#F0C27A",
                    "panorama" => "#C9A0DC",
                    "banner" => "orange",
                    "pagina4" => "#E8A0A0",
                    "paginaL" => "#A0D8D0",
                    _ => "#CCCCCC"
                };

                // Force invariant culture → always uses "." as decimal separator
                string sx = x.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
                string sy = y.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
                string sw = w.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
                string sh = h.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
                string tx = (x + w / 2).ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
                string ty = (y + h / 2).ToString("F1", System.Globalization.CultureInfo.InvariantCulture);

                int fontSize = (w < 50 || h < 40) ? 9 : 12;

                sb.AppendLine($"""  <rect x="{sx}" y="{sy}" width="{sw}" height="{sh}" rx="5" ry="5" fill="{fill}" stroke="#555555" stroke-width="1.5"/>""");
                sb.AppendLine($"""  <text x="{tx}" y="{ty}" text-anchor="middle" dominant-baseline="middle" font-family="Segoe UI, Arial, sans-serif" font-size="{fontSize}" font-weight="500" fill="#333333">{slot.Type.Name}</text>""");
            }

            sb.AppendLine("</svg>");
            return sb.ToString();
        }
    }
}
