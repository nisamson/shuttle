using System.Drawing;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Shuttle.EFCore;

public class ColorValueConverter : ValueConverter<Color, string> {
    public ColorValueConverter() : base(c => ColorTranslator.ToHtml(c), cs => ColorTranslator.FromHtml(cs)) { }
}
