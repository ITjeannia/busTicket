using System.Globalization;
using System.Text;
using QRCoder;

namespace BusTicket.Core;

/// <summary>
/// Billet + reçu de paiement au format PDF (A4), sans bibliothèque externe :
/// texte en Helvetica, formes vectorielles, QR code dessiné module par module (net à toute taille).
/// Le même fichier est produit par le serveur et par l'application (hors ligne).
/// </summary>
public static class TicketPdf
{
    private static readonly Rgb Primary = new(0x15, 0x65, 0xC0);
    private static readonly Rgb PrimarySoft = new(0xE3, 0xF2, 0xFD);
    private static readonly Rgb OnPrimaryMuted = new(0xBB, 0xDE, 0xFB);
    private static readonly Rgb TextPrimary = new(0x1C, 0x1B, 0x1F);
    private static readonly Rgb TextSecondary = new(0x5F, 0x63, 0x68);
    private static readonly Rgb Border = new(0xD6, 0xD5, 0xDF);
    private static readonly Rgb Muted = new(0xF1, 0xF2, 0xF6);
    private static readonly Rgb Success = new(0x2E, 0x7D, 0x32);
    private static readonly Rgb SuccessSoft = new(0xE8, 0xF5, 0xE9);
    private static readonly Rgb Error = new(0xB3, 0x26, 0x1E);
    private static readonly Rgb ErrorSoft = new(0xFC, 0xE8, 0xE6);
    private static readonly Rgb White = new(0xFF, 0xFF, 0xFF);

    public static string FileName(BookingDto booking) => $"Billet-{booking.Reference}.pdf";

    public static byte[] Create(BookingDto booking)
    {
        var trip = booking.Trip;
        var pay = booking.Payment;
        var p = new PdfPage();
        const double left = 40, right = 555, width = right - left;

        // ===== En-tête =====
        p.Rect(0, 0, PdfPage.Width, 110, Primary);
        p.RoundRect(left, 34, 40, 40, 9, White);
        DrawBus(p, left + 8, 44, Primary);
        p.Text(left + 54, 58, "BusTicket", 22, bold: true, White);
        p.Text(left + 54, 76, "Billet électronique · Reçu de paiement", 10, bold: false, OnPrimaryMuted);
        p.Text(right, 52, "RÉFÉRENCE", 9, bold: true, OnPrimaryMuted, Align.Right);
        p.Text(right, 76, booking.Reference, 22, bold: true, White, Align.Right);

        // ===== Billet =====
        const double cardTop = 130, cardBottom = 556;
        p.RoundRect(left, cardTop, width, cardBottom - cardTop, 10, White, Border);

        // Départ → arrivée
        const double inner = left + 20, innerRight = right - 20;
        p.Text(inner, 160, "DÉPART", 9, bold: true, TextSecondary);
        p.Text(inner, 184, p.Fit(trip.From, 18, true, 150), 18, bold: true, TextPrimary);
        p.Text(inner, 216, Formatters.Time(trip.Departure), 28, bold: true, Primary);

        p.Text(innerRight, 160, "ARRIVÉE", 9, bold: true, TextSecondary, Align.Right);
        p.Text(innerRight, 184, p.Fit(trip.To, 18, true, 150), 18, bold: true, TextPrimary, Align.Right);
        p.Text(innerRight, 216, Formatters.Time(trip.Arrival), 28, bold: true, Primary, Align.Right);

        const double lineLeft = 205, lineRight = 390, lineY = 196, mid = (lineLeft + lineRight) / 2;
        p.Line(lineLeft, lineY, lineRight, lineY, Border, 1.5, dash: true);
        p.Circle(lineLeft, lineY, 4, White, Primary);
        p.Circle(lineRight, lineY, 4, Primary);
        p.Circle(mid, lineY, 14, Primary);
        DrawBus(p, mid - 9, lineY - 7, White, scale: 0.75);
        p.Text(mid, lineY - 22, Formatters.Duration(trip.Duration), 11, bold: true, TextPrimary, Align.Center);
        p.Text(mid, lineY + 30, $"{trip.StopsLabel} · {trip.ServiceClass}", 9, bold: false, TextSecondary, Align.Center);

        p.Text(inner, 252, Formatters.LongDate(trip.Departure), 12, bold: true, TextPrimary);
        var (statusText, statusFg, statusBg) = booking.IsBoarded ? ("EMBARQUÉ", Primary, PrimarySoft)
            : booking.IsExpired ? ("EXPIRÉ", Error, ErrorSoft)
            : ("VALIDE", Success, SuccessSoft);
        var pillWidth = p.Measure(statusText, 9, true) + 24;
        p.RoundRect(innerRight - pillWidth, 238, pillWidth, 20, 10, statusBg);
        p.Text(innerRight - pillWidth / 2, 251.5, statusText, 9, bold: true, statusFg, Align.Center);

        p.Line(inner, 270, innerRight, 270, Border, 1);

        // Informations
        var column = (innerRight - inner) / 4;
        void Info(int col, double y, string label, string value)
        {
            var x = inner + col * column;
            p.Text(x, y, label.ToUpperInvariant(), 8, bold: true, TextSecondary);
            p.Text(x, y + 17, p.Fit(value, 11.5, true, column - 10), 11.5, bold: true, TextPrimary);
        }
        Info(0, 292, "Passager", booking.PassengerName);
        Info(1, 292, "Compagnie", trip.Company);
        Info(2, 292, "Classe", trip.ServiceClass);
        Info(3, 292, booking.Seats.Count > 1 ? "Sièges" : "Siège", string.Join(", ", booking.Seats));
        Info(0, 334, "Gare routière", trip.From);
        Info(1, 334, "Présentation", $"Avant {Formatters.Time(trip.Departure.AddMinutes(-30))}");
        Info(2, 334, "Voyageurs", Formatters.Passengers(booking.Seats.Count));
        Info(3, 334, "Prix payé", Formatters.Price(pay.Total));

        // Perforation du billet
        const double perforation = 378;
        p.Line(left + 14, perforation, right - 14, perforation, Border, 1.2, dash: true);
        p.Circle(left, perforation, 10, White, Border);
        p.Circle(right, perforation, 10, White, Border);
        p.Rect(left - 11, perforation - 11, 11, 22, White);   // cache la moitié extérieure des encoches
        p.Rect(right, perforation - 11, 11, 22, White);

        // QR code
        const double qrSize = 150, qrX = inner, qrY = 392;
        DrawQr(p, booking.QrPayload, qrX, qrY, qrSize, booking.IsExpired ? Border : TextPrimary);

        var textX = qrX + qrSize + 24;
        p.Text(textX, 412, "Présentez ce code à l'embarquement", 13, bold: true, TextPrimary);
        string[] rules =
        [
            "Arrivez au moins 30 minutes avant le départ.",
            "Munissez-vous d'une pièce d'identité au nom du passager.",
            "Ce billet n'est valable qu'une fois : il est désactivé au contrôle.",
            $"Bagage inclus : {(trip.IsVip ? "30 kg" : "20 kg")} en soute, 1 sac à main.",
            "Billet nominatif, non cessible.",
        ];
        for (var i = 0; i < rules.Length; i++)
        {
            var y = 436 + i * 18;
            p.Circle(textX + 3, y - 3.5, 1.8, Primary);
            p.Text(textX + 12, y, rules[i], 9.5, bold: false, TextSecondary);
        }
        p.RoundRect(textX, 524, innerRight - textX, 22, 5, Muted);
        p.Text(textX + 10, 539, $"Contrôle manuel : saisir {booking.Reference}", 9, bold: true, TextSecondary);

        // ===== Reçu de paiement =====
        p.Text(left, 592, "REÇU DE PAIEMENT", 12, bold: true, Primary);
        var paidWidth = p.Measure("PAYÉ", 10, true) + 26;
        p.RoundRect(right - paidWidth, 578, paidWidth, 22, 4, SuccessSoft, Success);
        p.Text(right - paidWidth / 2, 593, "PAYÉ", 10, bold: true, Success, Align.Center);

        (string Label, string Value)[] rows =
        [
            ("N° de transaction", pay.TransactionId),
            ("Date du paiement", Formatters.Timestamp(pay.PaidAt)),
            ("Moyen de paiement", pay.MethodLabel),
            ("Compte débité", pay.AccountMasked),
            ($"Billets ({booking.Seats.Count} × {Formatters.Price(trip.Price)})", Formatters.Price(pay.TicketsAmount)),
            ("Frais de service", Formatters.Price(pay.ServiceFee)),
        ];
        for (var i = 0; i < rows.Length; i++)
        {
            var y = 622 + i * 20;
            p.Text(left, y, rows[i].Label, 10, bold: false, TextSecondary);
            p.Text(right, y, rows[i].Value, 10, bold: true, TextPrimary, Align.Right);
            p.Line(left, y + 7, right, y + 7, Muted, 0.8);
        }
        p.Rect(left, 742, width, 30, PrimarySoft);
        p.Text(left + 12, 762, "Total payé", 12, bold: true, TextPrimary);
        p.Text(right - 12, 762.5, Formatters.Price(pay.Total), 14, bold: true, Primary, Align.Right);

        // ===== Pied de page =====
        p.Line(left, 796, right, 796, Border, 0.8);
        p.Text(left, 811, "BusTicket · Réservation de billets de bus au Cameroun", 8, bold: true, TextSecondary);
        p.Text(right, 811, $"Document généré le {Formatters.Timestamp(DateTime.Now)}", 8, bold: false, TextSecondary, Align.Right);
        p.Text(left, 824, "Ce document tient lieu de billet et de facture. Le QR code est signé : toute modification le rend invalide.", 7.5, bold: false, TextSecondary);

        return PdfPage.Write(p, $"Billet {booking.Reference}");
    }

    /// <summary>Petit bus stylisé (carrosserie, vitres, roues) dans un carré de 24 × 22 points.</summary>
    private static void DrawBus(PdfPage p, double x, double y, Rgb color, double scale = 1)
    {
        double S(double v) => v * scale;
        var windows = color == White ? Primary : White;
        p.RoundRect(x, y, S(24), S(18), S(4), color);
        p.Rect(x + S(3), y + S(3), S(18), S(7), windows);
        p.Rect(x + S(3), y + S(13), S(4), S(2), windows);
        p.Rect(x + S(17), y + S(13), S(4), S(2), windows);
        p.Circle(x + S(6), y + S(19.5), S(2.6), color);
        p.Circle(x + S(18), y + S(19.5), S(2.6), color);
    }

    private static void DrawQr(PdfPage p, string payload, double x, double y, double size, Rgb color)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.M);
        var matrix = data.ModuleMatrix;            // inclut la marge blanche (4 modules)
        var module = size / matrix.Count;

        p.Rect(x, y, size, size, White);
        for (var row = 0; row < matrix.Count; row++)
        {
            var bits = matrix[row];
            var col = 0;
            while (col < bits.Length)
            {
                if (!bits[col]) { col++; continue; }
                var start = col;
                while (col < bits.Length && bits[col]) col++;
                // Légère surépaisseur pour éviter les filets blancs entre modules à l'écran.
                p.Rect(x + start * module, y + row * module, (col - start) * module + 0.15, module + 0.15, color);
            }
        }
    }

    private readonly record struct Rgb(byte R, byte G, byte B)
    {
        public string Op(string op) => FormattableString.Invariant($"{R / 255.0:0.###} {G / 255.0:0.###} {B / 255.0:0.###} {op}");
    }

    private enum Align { Left, Center, Right }

    /// <summary>Une page PDF ; coordonnées en points, origine en haut à gauche (convertie à l'écriture).</summary>
    private sealed class PdfPage
    {
        public const double Width = 595, Height = 842;   // A4

        private readonly StringBuilder _ops = new();

        private static string N(double v) => v.ToString("0.##", CultureInfo.InvariantCulture);
        private static double Y(double top) => Height - top;

        public void Rect(double x, double y, double w, double h, Rgb fill) =>
            _ops.Append($"{fill.Op("rg")} {N(x)} {N(Y(y + h))} {N(w)} {N(h)} re f\n");

        public void RoundRect(double x, double y, double w, double h, double r, Rgb fill, Rgb? stroke = null)
        {
            const double k = 0.5523;
            double x2 = x + w, y2 = y + h;
            _ops.Append(fill.Op("rg")).Append(' ');
            if (stroke is { } s) _ops.Append(s.Op("RG")).Append(" 1 w ");
            _ops.Append($"{N(x + r)} {N(Y(y))} m {N(x2 - r)} {N(Y(y))} l ");
            _ops.Append($"{N(x2 - r + r * k)} {N(Y(y))} {N(x2)} {N(Y(y + r - r * k))} {N(x2)} {N(Y(y + r))} c ");
            _ops.Append($"{N(x2)} {N(Y(y2 - r))} l ");
            _ops.Append($"{N(x2)} {N(Y(y2 - r + r * k))} {N(x2 - r + r * k)} {N(Y(y2))} {N(x2 - r)} {N(Y(y2))} c ");
            _ops.Append($"{N(x + r)} {N(Y(y2))} l ");
            _ops.Append($"{N(x + r - r * k)} {N(Y(y2))} {N(x)} {N(Y(y2 - r + r * k))} {N(x)} {N(Y(y2 - r))} c ");
            _ops.Append($"{N(x)} {N(Y(y + r))} l ");
            _ops.Append($"{N(x)} {N(Y(y + r - r * k))} {N(x + r - r * k)} {N(Y(y))} {N(x + r)} {N(Y(y))} c ");
            _ops.Append(stroke is null ? "f\n" : "B\n");
        }

        public void Circle(double cx, double cy, double r, Rgb fill, Rgb? stroke = null) =>
            RoundRect(cx - r, cy - r, 2 * r, 2 * r, r, fill, stroke);

        public void Line(double x1, double y1, double x2, double y2, Rgb color, double width, bool dash = false) =>
            _ops.Append($"{color.Op("RG")} {N(width)} w {(dash ? "[4 3] 0 d" : "[] 0 d")} " +
                        $"{N(x1)} {N(Y(y1))} m {N(x2)} {N(Y(y2))} l S [] 0 d\n");

        /// <summary><paramref name="y"/> = ligne de base du texte.</summary>
        public void Text(double x, double y, string text, double size, bool bold, Rgb color, Align align = Align.Left)
        {
            var w = Measure(text, size, bold);
            var startX = align switch { Align.Center => x - w / 2, Align.Right => x - w, _ => x };
            _ops.Append($"BT /{(bold ? "F2" : "F1")} {N(size)} Tf {color.Op("rg")} {N(startX)} {N(Y(y))} Td ({Encode(text)}) Tj ET\n");
        }

        public double Measure(string text, double size, bool bold) =>
            Normalize(text).Sum(c => GlyphWidth(c, bold)) * size / 1000;

        /// <summary>Coupe le texte avec « … » s'il dépasse <paramref name="maxWidth"/>.</summary>
        public string Fit(string text, double size, bool bold, double maxWidth)
        {
            if (Measure(text, size, bold) <= maxWidth) return text;
            var cut = text;
            while (cut.Length > 1 && Measure(cut + "…", size, bold) > maxWidth) cut = cut[..^1];
            return cut.TrimEnd() + "…";
        }

        // ----- Texte : Helvetica standard (aucune police à embarquer), encodage WinAnsi -----

        private static string Normalize(string s) => s
            .Replace(' ', ' ').Replace(' ', ' ')       // espaces insécables des prix
            .Replace("→", "-").Replace('’', '\'');

        private static string Encode(string s)
        {
            var sb = new StringBuilder();
            foreach (var c in Normalize(s))
            {
                int code = c switch
                {
                    '•' => 0x95, '…' => 0x85, '–' => 0x96, '—' => 0x97, '€' => 0x80, '×' => 0xD7,
                    _ => c <= 0xFF ? c : '?',
                };
                if (code is '(' or ')' or '\\') sb.Append('\\').Append((char)code);
                else if (code < 32 || code > 126) sb.Append('\\').Append(Convert.ToString(code, 8).PadLeft(3, '0'));
                else sb.Append((char)code);
            }
            return sb.ToString();
        }

        // Chasses Helvetica / Helvetica-Bold (1/1000 em) pour les caractères 32 à 126.
        private static readonly short[] Regular =
        [
            278,278,355,556,556,889,667,191,333,333,389,584,278,333,278,278,556,556,556,556,556,556,556,556,556,556,
            278,278,584,584,584,556,1015,667,667,722,722,667,611,778,722,278,500,667,556,833,722,778,667,778,722,667,
            611,722,667,944,667,667,611,278,278,278,469,556,333,556,556,500,556,556,278,556,556,222,222,500,222,833,
            556,556,556,556,333,500,278,556,500,722,500,500,500,334,260,334,584,
        ];

        private static readonly short[] Bold =
        [
            278,333,474,556,556,889,722,238,333,333,389,584,278,333,278,278,556,556,556,556,556,556,556,556,556,556,
            333,333,584,584,584,611,975,722,722,722,722,667,611,778,722,278,556,722,611,833,722,778,667,778,722,667,
            611,722,667,944,667,667,611,333,278,333,584,556,333,556,611,556,611,556,333,611,611,278,278,556,278,889,
            611,611,611,611,389,556,333,611,556,778,556,556,500,389,280,389,584,
        ];

        private static double GlyphWidth(char c, bool bold)
        {
            var table = bold ? Bold : Regular;
            var basic = c.ToString().Normalize(NormalizationForm.FormD)[0];   // é → e, À → A
            if (basic is >= ' ' and <= '~') return table[basic - 32];
            return c switch { '•' => 350, '…' => 1000, '–' => 556, '—' => 1000, '×' => 584, '·' => 278, _ => 556 };
        }

        // ----- Assemblage du fichier -----

        public static byte[] Write(PdfPage page, string title)
        {
            var content = Encoding.ASCII.GetBytes(page._ops.ToString());
            var objects = new List<byte[]>
            {
                Ascii("<< /Type /Catalog /Pages 2 0 R >>"),
                Ascii("<< /Type /Pages /Kids [3 0 R] /Count 1 >>"),
                Ascii($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {N(Width)} {N(Height)}] " +
                      "/Resources << /Font << /F1 5 0 R /F2 6 0 R >> >> /Contents 4 0 R >>"),
                (byte[])[.. Ascii($"<< /Length {content.Length} >>\nstream\n"), .. content, .. Ascii("\nendstream")],
                Ascii("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>"),
                Ascii("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>"),
                Ascii($"<< /Title ({Encode(title)}) /Producer (BusTicket) /CreationDate (D:{DateTime.Now:yyyyMMddHHmmss}) >>"),
            };

            using var output = new MemoryStream();
            void Put(byte[] bytes) => output.Write(bytes);
            Put(Ascii("%PDF-1.4\n%âãÏÓ\n"));

            var offsets = new List<long>();
            for (var i = 0; i < objects.Count; i++)
            {
                offsets.Add(output.Position);
                Put(Ascii($"{i + 1} 0 obj\n"));
                Put(objects[i]);
                Put(Ascii("\nendobj\n"));
            }

            var xref = output.Position;
            var table = new StringBuilder($"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");
            foreach (var offset in offsets) table.Append($"{offset:D10} 00000 n \n");
            table.Append($"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R /Info {objects.Count} 0 R >>\nstartxref\n{xref}\n%%EOF\n");
            Put(Ascii(table.ToString()));
            return output.ToArray();
        }

        /// <summary>Latin-1 octet par octet (les caractères spéciaux du texte sont déjà échappés).</summary>
        private static byte[] Ascii(string s) => s.Select(c => (byte)c).ToArray();
    }
}
