using System;
using System.Linq;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Tokens;
using UglyToad.PdfPig.Core;

var path = args.Length > 0 ? args[0] : @"C:\Users\aquail\AppData\Local\Temp\ebspeese_160598834 (1).pdf";

using var doc = PdfDocument.Open(path);
Console.WriteLine($"Pages: {doc.NumberOfPages}");

var tName = NameToken.Create("T");
var vName = NameToken.Create("V");

// Dump all AcroForm fields (T => V)
Console.WriteLine("\n=== ALL FIELDS T => V ===");
int fieldCount = 0;
foreach (var kvp in doc.Structure.CrossReferenceTable.ObjectOffsets)
{
    try
    {
        var obj = doc.Structure.GetObject(
            new IndirectReference(kvp.Key.ObjectNumber, kvp.Key.Generation));
        if (obj?.Data is not DictionaryToken dict) continue;
        if (!dict.TryGet(tName, out IToken? tToken)) continue;
        var key = tToken!.ToString().Trim('(', ')');
        if (string.IsNullOrWhiteSpace(key)) continue;
        string val = "(no V)";
        if (dict.TryGet(vName, out IToken? vToken))
            val = vToken is HexToken hex ? hex.ToString() : vToken!.ToString().Trim('(', ')');
        Console.WriteLine($"  T=[{key}]  V=[{val}]");
        fieldCount++;
    }
    catch { }
}
Console.WriteLine($"  Total: {fieldCount}");

// Dump page words
Console.WriteLine("\n=== PAGE TEXT WORDS ===");
foreach (var page in doc.GetPages())
{
    Console.WriteLine($"\n=== PAGE {page.Number} ===");
    foreach (var word in page.GetWords().OrderBy(w => -w.BoundingBox.Top).ThenBy(w => w.BoundingBox.Left))
        Console.WriteLine($"  Y={word.BoundingBox.Top,7:F1}  X={word.BoundingBox.Left,7:F1}  [{word.Text}]");
}
