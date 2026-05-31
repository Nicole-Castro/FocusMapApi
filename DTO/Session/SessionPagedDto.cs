namespace FocusMapApi.DTO.Session;

public class SessionPagedDto
{
    public List<SessionDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int TotalFinished { get; set; }
    public int TotalInProgress { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
