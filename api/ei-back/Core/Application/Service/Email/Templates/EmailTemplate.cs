using System.Net;

namespace ei_back.Core.Application.Service.Email.Templates;

/// <summary>Shared light-mode email layout based on the frontend's violet/orange theme and wordmark.</summary>
internal static class EmailTemplate
{
    public static string Render(string language, string title, string preview, string content) => $$"""
        <!doctype html>
        <html lang="{{WebUtility.HtmlEncode(language)}}">
        <head>
          <meta charset="utf-8">
          <meta name="viewport" content="width=device-width, initial-scale=1">
          <meta name="color-scheme" content="light">
          <meta name="supported-color-schemes" content="light">
          <title>{{WebUtility.HtmlEncode(title)}} — EI Dungeon</title>
        </head>
        <body style="margin:0;padding:0;color:#1d1b1e;font-family:Manrope,'Helvetica Neue',Arial,sans-serif;color-scheme:light;">
          <div style="display:none;max-height:0;overflow:hidden;mso-hide:all;">{{WebUtility.HtmlEncode(preview)}}</div>
          <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="border-collapse:collapse;">
            <tr>
              <td align="center" style="padding:40px 20px;">
                <!--[if mso]><table role="presentation" width="560" cellpadding="0" cellspacing="0" border="0"><tr><td><![endif]-->
                <table role="presentation" width="100%" cellpadding="0" cellspacing="0" border="0" style="max-width:560px;border-collapse:collapse;">
                  <tr>
                    <td style="padding:0 0 28px;border-bottom:1px solid #e8e0eb;">
                      <span style="font-family:Manrope,'Helvetica Neue',Arial,sans-serif;font-size:13px;font-weight:700;letter-spacing:2px;color:#964900;">EI-DUNGEON</span>
                    </td>
                  </tr>
                  <tr>
                    <td style="padding:32px 0 12px;">
                      <h1 style="margin:0;font-family:'Averia Serif Libre',Georgia,'Times New Roman',serif;font-size:32px;line-height:1.25;font-weight:400;letter-spacing:-0.5px;color:#1d1b1e;">{{WebUtility.HtmlEncode(title)}}</h1>
                    </td>
                  </tr>
        {{content}}
                </table>
                <!--[if mso]></td></tr></table><![endif]-->
              </td>
            </tr>
          </table>
        </body>
        </html>
        """;
}
