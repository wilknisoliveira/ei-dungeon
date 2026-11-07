using AutoMapper;
using ei_back.Core.Application.Repository;
using ei_back.Core.Application.UseCase.Play.Dtos;
using ei_back.Core.Application.UseCase.Play.Interfaces;
using ei_back.Core.Application.Utils;
using ei_back.Infrastructure.Exceptions.ExceptionTypes;

namespace ei_back.Core.Application.UseCase.Play
{
    public class GetPlaysUseCase : IGetPlaysUseCase
    {
        private readonly IMapper _mapper;
        private readonly IPlayRepository _playRepository;
        private readonly IGameRepository _gameRepository;
        private readonly IUserRepository _userRepository;

        public GetPlaysUseCase(
            IMapper mapper,
            IPlayRepository playRepository,
            IGameRepository gameRepository,
            IUserRepository userRepository)
        {
            _mapper = mapper;
            _playRepository = playRepository;
            _gameRepository = gameRepository;
            _userRepository = userRepository;
        }

        public async Task<PagedSearchDto<PlayDtoResponse>> Handler(
            Guid gameId,
            string sortDirection,
            int pageSize,
            int page,
            string userName,
            CancellationToken cancellationToken)
        {
            var user = await _userRepository.FindByUserName(userName) ??
                throw new NotFoundException($"No user found to user name {userName}.");

            if (!await _gameRepository.CheckIfExistGameByUser(gameId, user.Id, cancellationToken))
                throw new NotFoundException($"Not found game {gameId} to user {userName}");

            var sort = PaginationHelper.ValidateSort(sortDirection);
            var size = PaginationHelper.ValidateSize(pageSize);
            var offset = PaginationHelper.ValidateOffset(page, size);

            var plays = await _playRepository.GetPlaysByGameAndSizeButSummaryPlay(gameId, size, offset, sort, cancellationToken);

            int totalResults = await _playRepository.CountPlaysByGameButSummaryPlay(gameId, cancellationToken);   

            List<PlayDtoResponse> playDtoResponseList = [];
            foreach (var play in plays)
            {
                var playDtoResponse = _mapper.Map<PlayDtoResponse>(play);
                playDtoResponseList.Add(playDtoResponse);
            }

            return new PagedSearchDto<PlayDtoResponse>
            {
                CurrentPage = page,
                Items = playDtoResponseList,
                PageSize = size,
                SortDirection = sort,
                TotalResults = totalResults
            };
        }
    }
}
