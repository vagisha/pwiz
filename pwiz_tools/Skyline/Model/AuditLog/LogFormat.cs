using System.Globalization;
using pwiz.Common.SystemUtil;

namespace pwiz.Skyline.Model.AuditLog
{
    public class LogFormat : Immutable
    {
        public LogFormat(LogLevel logLevel, CultureInfo language, SrmDocument.DOCUMENT_TYPE documentType)
        {
            LogLevel = logLevel;
            Language = language;
            DocumentType = documentType;
        }

        public LogLevel LogLevel { get; private set; }

        public LogFormat ChangeLogLevel(LogLevel logLevel)
        {
            return ChangeProp(ImClone(this), im => im.LogLevel = logLevel);
        }
        public CultureInfo Language { get; private set; }

        public LogFormat ChangeLanguage(CultureInfo language)
        {
            return ChangeProp(ImClone(this), im => im.Language = language);
        }

        public SrmDocument.DOCUMENT_TYPE DocumentType { get; private set; }

        public LogFormat ChangeDocumentType(SrmDocument.DOCUMENT_TYPE documentType)
        {
            return ChangeProp(ImClone(this), im => im.DocumentType = documentType);
        }
        public bool ConvertPathsToFilenames { get; private set; }

        public LogFormat ChangeConvertPathsToFilenames(bool convertPathsToFilenames)
        {
            return ChangeProp(ImClone(this), im => im.ConvertPathsToFilenames = convertPathsToFilenames);
        }
    }
}
