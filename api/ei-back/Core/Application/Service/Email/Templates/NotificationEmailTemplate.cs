using System.Net;

namespace ei_back.Core.Application.Service.Email.Templates;

internal static class NotificationEmailTemplate
{
    public static string Render(string language, string title, string body, (string Label, string Value)[] details,
        (string Label, string Url)? action = null, (string Label, string Value)? footer = null)
    {
        var detailRows = string.Concat(details.Select(detail => $$"""
            <tr>
              <td style="padding:0 0 8px;font-size:14px;line-height:1.6;color:#49454e;">
                <strong>{{WebUtility.HtmlEncode(detail.Label)}}</strong><br>{{WebUtility.HtmlEncode(detail.Value)}}
              </td>
            </tr>
            """));
        var actionRow = action is { } link
            ? $$"""
              <tr>
                <td style="padding:20px 0 28px;">
                  <a href="{{WebUtility.HtmlEncode(link.Url)}}" style="display:inline-block;padding:10px 16px;border:1px solid #7d00fa;border-radius:12px;font-weight:700;color:#7d00fa;text-decoration:none;">{{WebUtility.HtmlEncode(link.Label)}}</a>
                </td>
              </tr>
              """
            : "";
        var footerRow = footer is { } detail
            ? $$"""
              <tr>
                <td style="padding:16px 0 0;border-top:1px solid #e8e0eb;font-size:11px;line-height:1.6;color:#49454e;">
                  {{WebUtility.HtmlEncode(detail.Label)}} {{WebUtility.HtmlEncode(detail.Value)}}
                </td>
              </tr>
              """
            : "";

        return EmailTemplate.Render(language, title, body, $$"""
            <tr>
              <td style="padding:0 0 28px;font-size:16px;line-height:1.7;color:#49454e;">{{WebUtility.HtmlEncode(body)}}</td>
            </tr>
            {{detailRows}}{{actionRow}}{{footerRow}}
            """);
    }
}
