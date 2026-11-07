using AutoMapper;
using ei_back.Core.Application.UseCase.GameInfo.Dtos;
using ei_back.Core.Application.UseCase.GameInfo.Interfaces;
using ei_back.Core.Domain.Enums;
using ei_back.Core.Application.Repository;
using ei_back.Infrastructure.Exceptions.ExceptionTypes;
using Microsoft.IdentityModel.Tokens;

namespace ei_back.Core.Application.UseCase.GameInfo
{
    public class CreateGameInfoUseCase : ICreateGameInfoUseCase
    {
        private readonly IGameInfoRepository _gameInfoRepository;
        private readonly ILogger<CreateGameInfoUseCase> _logger;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public CreateGameInfoUseCase(
            IGameInfoRepository gameInfoRepository,
            ILogger<CreateGameInfoUseCase> logger,
            IMapper mapper,
            IUnitOfWork unitOfWork)
        {
            _gameInfoRepository = gameInfoRepository;
            _logger = logger;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<GameInfoDto>> Handler(IEnumerable<GameInfoDto> gameInfoDtos, CancellationToken cancellationToken)
        {
            List<InfoType> types = gameInfoDtos.Select(x => x.Type).Distinct().ToList();

            List<Domain.Entity.GameInfo> existInfos = new();
            foreach (var type in types)
            {
                List<string> values = gameInfoDtos.Where(x => x.Type.Equals(type)).Select(x => x.Value).ToList();
                IEnumerable<Domain.Entity.GameInfo>? resultInfos = await _gameInfoRepository.GetItemsByValuesAndType(values, type, cancellationToken);

                if (!resultInfos.IsNullOrEmpty())
                    existInfos.AddRange(resultInfos);
            }

            if (!existInfos.IsNullOrEmpty())
            {
                _logger.LogError("Some values already exist in database.");

                List<string> exitInfoText = existInfos.Select(x => $"{x.Type}, {x.Value}").ToList();
                throw new BadRequestException($"The follow values already exist [type, value]: [{string.Join("], [", exitInfoText)}]");
            }

            List<Domain.Entity.GameInfo> gameInfos = gameInfoDtos.Select(x => _mapper.Map<Domain.Entity.GameInfo>(x)).ToList();
            var gameInfoResult = await _gameInfoRepository.CreateRangeAsync(gameInfos, cancellationToken);

            var changedItems = await _unitOfWork.CommitAsync(cancellationToken);

            if (changedItems == 0)
            {
                var errorMessage = "Something went wrong while attempting to create game.";
                _logger.LogError(errorMessage);
                throw new InternalServerErrorException(errorMessage);
            }

            return gameInfoDtos;
        }
    }
}
