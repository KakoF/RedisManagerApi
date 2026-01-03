namespace Domain.Records.Requests
{
	public record class FilterKeys(int PageSize = 100, string Prefix = "*");
}
