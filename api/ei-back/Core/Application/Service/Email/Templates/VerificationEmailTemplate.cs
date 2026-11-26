using System.Net;

namespace ei_back.Core.Application.Service.Email.Templates;

/// <summary>Light-mode email markup based on the frontend's violet/orange theme and wordmark.</summary>
internal sealed record VerificationEmailText(string Language, string Title, string Instruction, string Expiration,
    string Unsolicited, string CopyInstruction, string CodeLabel, string DoNotShare, string Footer);

internal static class VerificationEmailTemplate
{
    public static string Render(VerificationEmailText text, string code) => EmailTemplate.Render(text.Language, text.Title,
        $"{text.Instruction} {text.Expiration}", RenderCode(text, code));

    private static string RenderCode(VerificationEmailText text, string code) => $$"""
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
        """;
}
