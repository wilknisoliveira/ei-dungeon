using ei_back.Core.Domain.Enums;

namespace ei_back.Core.Application.Service.Email;

internal sealed record SupportTicketEmailCopy(
    string Mailbox, string ReplyTitle, string StatusTitle, string ClosedTitle, string ReplyBody, string StatusBody,
    string SubjectLabel, string TypeLabel, string StatusLabel, string PreviousStatusLabel, string UpdatedLabel,
    string ViewReply, string ViewConversation, string ReplyToTeam, string ViewAndRate,
    string Open, string InProgress, string WaitingForPlayer, string Closed,
    string Help, string Bug, string Problem, string Suggestion)
{
    public static SupportTicketEmailCopy For(string language) => language switch
    {
        "pt-BR" => new(
            "Correio da Guilda", "A equipe respondeu à sua conversa", "O status da sua conversa mudou",
            "Sua conversa foi encerrada", "A equipe do Dungeon enviou uma resposta. Leia a mensagem no Correio da Guilda.",
            "Veja a atualização abaixo.", "Assunto", "Tipo de conversa", "Status atual", "Status anterior", "Atualizado em",
            "Ver resposta", "Ver conversa", "Responder à equipe", "Ver conversa e avaliar",
            "Aberto", "Em atendimento", "Aguardando sua resposta", "Encerrado",
            "Pedido de ajuda", "Relato de bug", "Resolução de problema", "Sugestão"),
        "es" => new(
            "Correo del Gremio", "El equipo respondió a tu conversación", "Cambió el estado de tu conversación",
            "Tu conversación se cerró", "El equipo de Dungeon respondió. Lee el mensaje en el Correo del Gremio.",
            "Consulta la actualización a continuación.", "Asunto", "Tipo de conversación", "Estado actual", "Estado anterior", "Actualizado el",
            "Ver respuesta", "Ver conversación", "Responder al equipo", "Ver conversación y valorar",
            "Abierto", "En atención", "Esperando tu respuesta", "Cerrado",
            "Solicitud de ayuda", "Informe de error", "Resolución de problema", "Sugerencia"),
        _ => new(
            "Guild Mail", "The team replied to your conversation", "Your conversation status changed",
            "Your conversation is closed", "The Dungeon team replied. Read their message in Guild Mail.",
            "See the update below.", "Subject", "Type", "Current status", "Previous status", "Updated",
            "View reply", "View conversation", "Reply to the team", "View conversation and rate",
            "Open", "In progress", "Waiting for your reply", "Closed",
            "Help request", "Bug report", "Problem resolution", "Suggestion")
    };

    public string FormatStatus(SupportTicketStatus status) => status switch
    {
        SupportTicketStatus.Open => Open,
        SupportTicketStatus.InProgress => InProgress,
        SupportTicketStatus.WaitingForPlayer => WaitingForPlayer,
        SupportTicketStatus.Closed => Closed,
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };

    public string FormatType(SupportTicketType type) => type switch
    {
        SupportTicketType.Help => Help,
        SupportTicketType.Bug => Bug,
        SupportTicketType.Problem => Problem,
        SupportTicketType.Suggestion => Suggestion,
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };
}
