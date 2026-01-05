using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Records.Requests
{
	public record CreateRedisKeyRequest(string Key, string Value, string Pattern, int? TtlSeconds = 0);
}
