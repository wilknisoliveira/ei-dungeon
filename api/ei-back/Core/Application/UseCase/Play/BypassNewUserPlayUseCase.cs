using System.Runtime.CompilerServices;
using ei_back.Core.Application.UseCase.Play.Dtos;
using ei_back.Core.Application.UseCase.Play.Interfaces;
using Microsoft.Extensions.Configuration;

namespace ei_back.Core.Application.UseCase.Play
{
    public class BypassNewUserPlayUseCase : INewUserPlayUseCase
    {
        private static readonly string[] Words =
        [
            "lorem", "ipsum", "dolor", "sit", "amet", "consectetur",
            "adipiscing", "elit", "sed", "do", "eiusmod", "tempor",
            "incididunt", "ut", "labore", "et", "dolore", "magna",
            "aliqua", "enim", "ad", "minim", "veniam", "quis",
            "nostrud", "exercitation", "ullamco", "laboris", "nisi",
            "aliquip", "ex", "ea", "commodo", "consequat"
        ];

        private static readonly Random _random = new();
        private readonly int _characterCount;

        public BypassNewUserPlayUseCase(IConfiguration configuration)
        {
            _characterCount = configuration.GetValue<int>("Features:BypassCharacterCount", 500);
        }

        public async IAsyncEnumerable<StreamPlayDtoResponse> Handler(
            PlayDtoRequest playDtoRequest,
            string userName,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            yield return new StreamPlayDtoResponse { EventType = EventType.Start };

            var totalChars = 0;
            while (totalChars < _characterCount)
            {
                var sentenceLength = _random.Next(8, 20);
                var sentence = string.Join(" ",
                    Enumerable.Range(0, sentenceLength).Select(_ => Words[_random.Next(Words.Length)]));
                sentence = char.ToUpper(sentence[0]) + sentence[1..] + ". ";

                var remaining = _characterCount - totalChars;
                if (sentence.Length > remaining)
                    sentence = sentence[..remaining];

                await Task.Delay(_random.Next(100, 300), cancellationToken);

                yield return new StreamPlayDtoResponse
                {
                    EventType = EventType.Chunk,
                    Content = sentence
                };

                totalChars += sentence.Length;
            }

            yield return new StreamPlayDtoResponse { EventType = EventType.End };
        }
    }
}
