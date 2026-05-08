namespace Janatics.DataEngine.Security.Sanitization;

public interface ISqlSanitizer
{
    string Sanitize(string sql);
}
