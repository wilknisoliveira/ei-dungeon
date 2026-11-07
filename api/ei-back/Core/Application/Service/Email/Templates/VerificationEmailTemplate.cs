using System.Net;

namespace ei_back.Core.Application.Service.Email.Templates;

/// <summary>Light-mode email markup based on the frontend's violet/orange theme and wordmark.</summary>
internal sealed record VerificationEmailText(string Language, string Title, string Instruction, string Expiration,
    string Unsolicited, string CopyInstruction, string CodeLabel, string DoNotShare, string Footer);

internal static class VerificationEmailTemplate
{
    public static string Render(VerificationEmailText text, string code) => $$"""
        <!doctype html>
        <html lang="{{WebUtility.HtmlEncode(text.Language)}}">
        <head>
          <meta charset="utf-8">
          <meta name="viewport" content="width=device-width, initial-scale=1">
          <meta name="color-scheme" content="light">
          <meta name="supported-color-schemes" content="light">
          <title>{{WebUtility.HtmlEncode(text.Title)}} — EI Dungeon</title>
        </head>
        <body style="margin:0;padding:0;color:#1d1b1e;font-family:Manrope,'Helvetica Neue',Arial,sans-serif;color-scheme:light;">
          <div style="display:none;max-height:0;overflow:hidden;mso-hide:all;">{{WebUtility.HtmlEncode(text.Instruction)}} {{WebUtility.HtmlEncode(text.Expiration)}}</div>
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
                      <h1 style="margin:0;font-family:'Averia Serif Libre',Georgia,'Times New Roman',serif;font-size:32px;line-height:1.25;font-weight:400;letter-spacing:-0.5px;color:#1d1b1e;">{{WebUtility.HtmlEncode(text.Title)}}</h1>
                    </td>
                  </tr>
                  <tr>
                    <td style="padding:0 0 28px;font-size:16px;line-height:1.7;color:#49454e;">
                      {{WebUtility.HtmlEncode(text.Instruction)}} {{WebUtility.HtmlEncode(text.CopyInstruction)}}
                    </td>
                  </tr>
                  <tr>
                    <td style="padding:0 0 10px;font-size:11px;line-height:1.5;letter-spacing:1.5px;font-weight:700;color:#49454e;">{{WebUtility.HtmlEncode(text.CodeLabel)}}</td>
                  </tr>
                  <tr>
                    <td align="center" style="padding:20px 12px;border:1px solid #7d00fa;border-radius:12px;font-family:'Courier New',Courier,monospace;font-size:36px;line-height:1.4;letter-spacing:6px;color:#7d00fa;">
                      <strong>{{WebUtility.HtmlEncode(code)}}</strong>
                    </td>
                  </tr>
                  <tr>
                    <td style="padding:16px 0 32px;font-size:13px;line-height:1.7;color:#49454e;">{{WebUtility.HtmlEncode(text.Expiration)}}</td>
                  </tr>
                  <tr>
                    <td style="padding:24px 0 0;border-top:1px solid #e8e0eb;font-size:13px;line-height:1.7;color:#49454e;">
                      {{WebUtility.HtmlEncode(text.Unsolicited)}} {{WebUtility.HtmlEncode(text.DoNotShare)}}
                    </td>
                  </tr>
                  <tr>
                    <td style="padding:16px 0 0;font-size:11px;line-height:1.6;color:#49454e;">{{WebUtility.HtmlEncode(text.Footer)}}</td>
                  </tr>
                </table>
                <!--[if mso]></td></tr></table><![endif]-->
              </td>
            </tr>
          </table>
        </body>
        </html>
        """;
}
