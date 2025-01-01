namespace NotifyService.Api.Requests;

public record SendEmailRequest(
    Guid EmailRequestId,
    string Subject,
    string From,
    string To,
    string ReplyTo,
    string TemplateId,
    Dictionary<string, string> Data
    );