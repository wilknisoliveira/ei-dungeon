using AutoMapper;
using ei_back.Core.Application.Repository;
using ei_back.Core.Application.UseCase.Game.Dtos;
using ei_back.Core.Application.UseCase.Game.Interfaces;
using ei_back.Core.Application.Utils;
using ei_back.Infrastructure.Exceptions.ExceptionTypes;

namespace ei_back.Core.Application.UseCase.Game
{
    public class GetGamesUseCase : IGetGamesUseCase
    {
        private readonly IMapper _mapper;
        private readonly IGameRepository _gameRepository;
        private readonly IUserRepository _userRepository;

        public GetGamesUseCase(IMapper mapper, IGameRepository gameRepository, IUserRepository userRepository)
        {
            _mapper = mapper;
            _gameRepository = gameRepository;
            _userRepository = userRepository;
        }

        public async Task<PagedSearchDto<GameDtoResponse>> Handler(string sortDirection, int pageSize, int page, string userName, CancellationToken cancellationToken)
        {
            var user = await _userRepository.FindByUserName(userName) ??
                throw new NotFoundException($"No user found to user name {userName}.");

            var sort = PaginationHelper.ValidateSort(sortDirection);
            var size = PaginationHelper.ValidateSize(pageSize);
            var offset = PaginationHelper.ValidateOffset(page, size);

            var games = await _gameRepository.FindWithPagedSearchAsync(
                sort, size, offset, user.Id, cancellationToken);

            int totalResults = await _gameRepository.GetCountAsync(
                user.Id, cancellationToken);

            return new PagedSearchDto<GameDtoResponse>
            {
                CurrentPage = page,
                Items = games.Select(_mapper.Map<GameDtoResponse>).ToList(),
                PageSize = size,
                SortDirection = sort,
                TotalResults = totalResults
            };
        }
    }
}
