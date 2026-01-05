using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Records.Requests
{
	public record CreateKeyValue(string Key, object Value, int TtlSeconds = 0);
}
