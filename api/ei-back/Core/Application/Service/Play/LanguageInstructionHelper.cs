using ei_back.Core.Domain.Enums;

namespace ei_back.Core.Application.Service.Play
{
    public static class LanguageInstructionHelper
    {
        const string defaultInstruction = "IMPORTANT: Always respond in English. All your responses, narrations, descriptions, and dialogues must be written in English.";
        public static string GetLanguageInstruction(GameLanguage language) => language switch
        {
            GameLanguage.Portuguese => "IMPORTANTE: Responda SEMPRE em português brasileiro. Todas as suas respostas, narrações, descrições e diálogos devem ser escritos em português brasileiro.",
            GameLanguage.English => defaultInstruction,
            GameLanguage.Spanish => "IMPORTANTE: Responda SIEMPRE en español. Todas sus respuestas, narraciones, descripciones y diálogos deben estar escritos en español.",
            _ => defaultInstruction
        };
    }
}
