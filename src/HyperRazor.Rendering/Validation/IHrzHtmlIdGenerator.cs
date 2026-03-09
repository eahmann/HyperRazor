namespace HyperRazor.Rendering;

public interface IHrzHtmlIdGenerator
{
    string GetFormId(string formName);

    string GetFieldId(string formName, HrzFieldPath path);

    string GetFieldMessageId(string formName, HrzFieldPath path);

    string GetSummaryId(string formName);
}
