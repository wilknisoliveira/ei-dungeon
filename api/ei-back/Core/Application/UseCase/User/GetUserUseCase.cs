using AutoMapper;
using ei_back.Core.Application.Repository;
using ei_back.Core.Application.UseCase.User.Dtos;
using ei_back.Core.Application.UseCase.User.Interfaces;
using ei_back.Core.Application.Utils;

namespace ei_back.Core.Application.UseCase.User
{
    public class GetUserUseCase : IGetUserUseCase
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;

        public GetUserUseCase(IUserRepository userRepository, IMapper mapper)
        {
            _userRepository = userRepository;
            _mapper = mapper;
        }

        public async Task<PagedSearchDto<UserGetDtoResponse>> Handler(string? name, string sortDirection, int pageSize, int page, CancellationToken cancellationToken)
        {
            var pagedSearchDto = new PagedSearchDto<UserGetDtoResponse>();

            var sort = PaginationHelper.ValidateSort(sortDirection);
            var size = PaginationHelper.ValidateSize(pageSize);
            var offset = PaginationHelper.ValidateOffset(page, pageSize);

            var users = await _userRepository.FindWithPagedSearchAsync(
                sort,
                size,
                offset,
                name,
                cancellationToken);

            int totalResults = await _userRepository.GetCountAsync(
                name,
                cancellationToken);

            return new PagedSearchDto<UserGetDtoResponse>
            {
                CurrentPage = page,
                Items = users.Select(user => _mapper.Map<UserGetDtoResponse>(user)).ToList(),
                PageSize = size,
                SortDirection = sort,
                TotalResults = totalResults
            };
        }
    }
}
