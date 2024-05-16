using Microsoft.AspNetCore.Html;
using System.Text;

namespace AppComunicazioni.Utility
{
    public class ViewHelpers
    {
        public static string FormatNoteForDisplay(string note)
        {
            if (string.IsNullOrEmpty(note))
                return string.Empty;

            var notes = note.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            var formattedNotes = notes.Select(n => $"{n}<br>");
            return string.Join("", formattedNotes);
        }
    }
}
