namespace DeepSearch.Core.Exceptions;

/// <summary>
/// נזרק כשהשאילתה שהתקבלה אינה תקינה (למשל גיל מינ' גדול מגיל מקס').
/// שכבת ה-API תתרגם אותה לתשובת 400 Bad Request עם הודעה ברורה.
/// </summary>
public class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
}
